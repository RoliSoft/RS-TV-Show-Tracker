namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;
    using RoliSoft.TVShowTracker.Tables;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>OnlineVideoSearchEngine</c> class.
    /// </summary>
    public class OnlineVideoSearchEngineTaskDialog
    {
        private TaskDialog _td;
        private Result _res;
        private OnlineVideoSearchEngine _os;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineVideoSearchEngineTaskDialog"/> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        public OnlineVideoSearchEngineTaskDialog(OnlineVideoSearchEngine engine)
        {
            _os = engine;

            _os.OnlineSearchDone  += OnlineSearchDone;
            _os.OnlineSearchError += OnlineSearchError;
        }

        /// <summary>
        /// Searches for the specified show and its episode.
        /// </summary>
        /// <param name="ep">The episode.</param>
        public void Search(Episode ep)
        {
            _td = new TaskDialog
                {
                    Title           = "Searching...",
                    Instruction     = "{0} S{1:00}E{2:00}".FormatWith(ep.Show.Name, ep.Season, ep.Number),
                    Content         = "Searching for the episode...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;

            new Thread(() => _res = _td.Show().CommonButton).Start();

            _os.SearchAsync(ep);

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the Destroyed event of the _td control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TaskDialogDestroyed(object sender, EventArgs e)
        {
            if (_res == Result.Cancel || (e is ClickEventArgs && (e as ClickEventArgs).ButtonID == 2))
            {
                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                _res = Result.Cancel;

                _os.CancelSearch();
            }
        }
        
        /// <summary>
        /// Called when the online search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnlineSearchDone(object sender, EventArgs<string, string> e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (_td != null && _td.IsShowing)
            {
                _td.SimulateButtonClick(-1);
            }

            if (_res == Result.Cancel)
            {
                return;
            }

            Utils.Run(e.Second);
        }

        /// <summary>
        /// Called when the online search has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnlineSearchError(object sender, EventArgs<string, string, Tuple<string, string, string>> e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (_td != null && _td.IsShowing)
            {
                _td.SimulateButtonClick(-1);
            }

            if (_res == Result.Cancel)
            {
                return;
            }

            var nvftd = new TaskDialog
                {
                    CommonIcon    = TaskDialogIcon.Stop,
                    Title         = "No videos found",
                    Instruction   = e.First,
                    Content       = e.Second,
                    CommonButtons = TaskDialogButton.OK
                };

            if (!string.IsNullOrWhiteSpace(e.Third.Item3))
            {
                nvftd.ExpandedInformation = e.Third.Item3;
            }

            if (!string.IsNullOrEmpty(e.Third.Item1))
            {
                nvftd.UseCommandLinks = true;
                nvftd.CustomButtons   = new[] { new CustomButton(0, e.Third.Item1) };
                nvftd.ButtonClick    += (s, c) =>
                    {
                        if (c.ButtonID == 0)
                        {
                            Utils.Run(e.Third.Item2);
                        }
                    };
            }

            nvftd.Show();
        }
    }
}
