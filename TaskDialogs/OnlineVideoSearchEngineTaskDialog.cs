namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>OnlineVideoSearchEngine</c> class.
    /// </summary>
    public class OnlineVideoSearchEngineTaskDialog<T> where T : OnlineVideoSearchEngine, new()
    {
        private TaskDialog _td;
        private T _os;

        /// <summary>
        /// Searches for the specified show and its episode.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        /// <param name="extra">The extra which might be needed by the engine.</param>
        public void Search(string show, string episode, object extra = null)
        {
            _td = new TaskDialog();

            _td.Caption         = "Searching...";
            _td.InstructionText = show + " " + episode;
            _td.Text            = "Searching for the episode...";
            _td.StandardButtons = TaskDialogStandardButtons.Cancel;
            _td.Cancelable      = true;
            _td.ProgressBar     = new TaskDialogProgressBar { State = TaskDialogProgressBarState.Marquee };
            _td.Closing        += TaskDialogClosing;

            new Thread(() => _td.Show()).Start();

            _os = new T();

            _os.OnlineSearchDone  += OnlineSearchDone;
            _os.OnlineSearchError += OnlineSearchError;
            _os.SearchAsync(show, episode, extra);

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the Closing event of the TaskDialog control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.WindowsAPICodePack.Dialogs.TaskDialogClosingEventArgs"/> instance containing the event data.</param>
        private void TaskDialogClosing(object sender, TaskDialogClosingEventArgs e)
        {
            if (e.TaskDialogResult == TaskDialogResult.Cancel)
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

            _td.Close(TaskDialogResult.Ok);
            Thread.Sleep(100);

            _td.Icon            = TaskDialogStandardIcon.Error;
            _td.Caption         = "No videos found";
            _td.InstructionText = e.First;
            _td.Text            = e.Second;
            _td.StandardButtons = TaskDialogStandardButtons.Ok;
            _td.ProgressBar     = null;

            if (!string.IsNullOrWhiteSpace(e.Third.Item3))
            {
                _td.DetailsExpandedText = e.Third.Item3;
            }

            if (!string.IsNullOrEmpty(e.Third.Item1))
            {
                var fd = new TaskDialogCommandLink { Text = e.Third.Item1 };
                fd.Click += (s, r) =>
                    {
                        _td.Close();
                        Utils.Run(e.Third.Item2);
                    };

                _td.Controls.Add(fd);
            }

            _td.Show();
        }
    }
}
