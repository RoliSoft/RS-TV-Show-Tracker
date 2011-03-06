namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>OnlineVideoSearchEngine</c> class.
    /// </summary>
    public class OnlineVideoSearchEngineTaskDialog<T> where T : OnlineVideoSearchEngine, new()
    {
        private TaskDialog _td;
        private Result _res;
        private T _os;

        /// <summary>
        /// Searches for the specified show and its episode.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        /// <param name="extra">The extra which might be needed by the engine.</param>
        public void Search(string show, string episode, object extra = null)
        {
            _td = new TaskDialog
                {
                    Title           = "Searching...",
                    Instruction     = show + " " + episode,
                    Content         = "Searching for the episode...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed += TaskDialogDestroyed;

            new Thread(() => _res = _td.Show().CommonButton).Start();

            _os = new T();

            _os.OnlineSearchDone  += OnlineSearchDone;
            _os.OnlineSearchError += OnlineSearchError;
            _os.SearchAsync(show, episode, extra);

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the Destroyed event of the _td control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TaskDialogDestroyed(object sender, EventArgs e)
        {
            if (_res == Result.Cancel)
            {
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
