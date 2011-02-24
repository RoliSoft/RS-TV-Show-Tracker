namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>DownloadLinksPage</c> links.
    /// </summary>
    public class LinkDownloadTaskDialog
    {
        private TaskDialog _td;
        private IDownloader _dl;

        /// <summary>
        /// Downloads the specified link.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="token">The token.</param>
        public void Download(LinkItem link, string token)
        {
            _td = new TaskDialog();

            _td.Caption         = "Downloading...";
            _td.InstructionText = link.Release;
            _td.Text            = "Sending request to " + new Uri(link.FileURL).DnsSafeHost.Replace("www.", string.Empty) + "...";
            _td.StandardButtons = TaskDialogStandardButtons.Cancel;
            _td.Cancelable      = true;
            _td.ProgressBar     = new TaskDialogProgressBar { State = TaskDialogProgressBarState.Marquee };
            _td.Closing        += TaskDialogClosing;

            new Thread(() => _td.Show()).Start();

            _dl                          = link.Source.Downloader;
            _dl.DownloadFileCompleted   += DownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) => _td.Text = "Downloading file... ({0}%)".FormatWith(a.Data);

            _dl.Download(link, Utils.GetRandomFileName(link.Source.Type == Types.Torrent ? "torrent" : link.Source.Type == Types.Usenet ? "nzb" : null), !string.IsNullOrWhiteSpace(token) ? token : "DownloadFile");

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
                _dl.CancelAsync();
            }
        }
        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadFileCompleted(object sender, EventArgs<string, string, string> e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            _td.Close(TaskDialogResult.Ok);

            switch (e.Third)
            {
                case "DownloadFile":
                    var sfd = new SaveFileDialog
                    {
                        CheckPathExists = true,
                        FileName = e.Second
                    };

                    if (sfd.ShowDialog().Value)
                    {
                        if (File.Exists(sfd.FileName))
                        {
                            File.Delete(sfd.FileName);
                        }

                        File.Move(e.First, sfd.FileName);
                    }
                    else
                    {
                        File.Delete(e.First);
                    }
                    break;

                case "SendToAssociated":
                    Utils.Run(e.First);
                    break;

                case "SendToTorrent":
                    Utils.Run(Settings.Get("Torrent Downloader"), e.First);
                    break;
            }
        }
    }
}
