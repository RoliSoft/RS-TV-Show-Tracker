namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;
    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>OnlineVideoSearchEngine</c> class.
    /// </summary>
    public class OnlineVideoSearchEngineTaskDialog
    {
        private OnlineVideoSearchEngine _os;
        private volatile bool _active;

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
            _active = true;
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Searching...",
                    MainInstruction         = "{0} S{1:00}E{2:00}".FormatWith(ep.Show.Name, ep.Season, ep.Number),
                    Content                 = "Searching for the episode...",
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp)
                            {
                                dialog.SetProgressBarMarquee(true, 0);
                                showmbp = true;
                            }

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try { _os.CancelSearch(); } catch { }
                                }

                                return false;
                            }

                            if (!_active)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();

            _os.SearchAsync(ep);

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Called when the online search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnlineSearchDone(object sender, EventArgs<string, string> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            Utils.Run(e.Second);
        }

        /// <summary>
        /// Called when the online search has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnlineSearchError(object sender, EventArgs<string, string, Tuple<string, string, string>> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
            
            var nvftd = new TaskDialogOptions
                {
                    MainIcon                = VistaTaskDialogIcon.Error,
                    Title                   = "No videos found",
                    MainInstruction         = e.First,
                    AllowDialogCancellation = true,
                    Content                 = e.Second
                };

            if (!string.IsNullOrWhiteSpace(e.Third.Item3))
            {
                nvftd.ExpandedInfo = e.Third.Item3;
            }

            if (!string.IsNullOrEmpty(e.Third.Item1))
            {
                nvftd.CommandButtons = new[] { e.Third.Item1, "Close" };
            }
            else
            {
                nvftd.CustomButtons = new[] { "OK" };
            }

            var res = TaskDialog.Show(nvftd);

            if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value == 0)
            {
                Utils.Run(e.Third.Item2);
            }
        }
    }
}
