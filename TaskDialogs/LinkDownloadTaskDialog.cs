namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>DownloadLinksPage</c> links.
    /// </summary>
    public class LinkDownloadTaskDialog
    {
        private TaskDialog _td;
        private Result _res;
        private IDownloader _dl;

        /// <summary>
        /// Downloads the specified link.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="token">The token.</param>
        public void Download(LinkItem link, string token)
        {
            if (link.FileURL.StartsWith("magnet:"))
            {
                DownloadFileCompleted(null, new EventArgs<string, string, string>(link.FileURL, null, token));
                return;
            }

            _td = new TaskDialog
                {
                    Title           = "Downloading...",
                    Instruction     = link.Release,
                    Content         = "Sending request to " + new Uri(link.FileURL).DnsSafeHost.Replace("www.", string.Empty) + "...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;

            new Thread(() => _res = _td.Show().CommonButton).Start();

            var prm = true;

            _dl                          = link.Source.Downloader;
            _dl.DownloadFileCompleted   += DownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) =>
                {
                    if (_td != null && _td.IsShowing)
                    {
                        if (prm)
                        {
                            _td.SetMarqueeProgressBar(false);
                            _td.Navigate(_td);
                            prm = false;
                        }

                        _td.Content = "Downloading file... ({0}%)".FormatWith(a.Data);
                        _td.ProgressBarPosition = a.Data;
                    }
                };

            _dl.Download(link, Utils.GetRandomFileName(link.Source.Type == Types.Torrent ? "torrent" : link.Source.Type == Types.Usenet ? "nzb" : null), !string.IsNullOrWhiteSpace(token) ? token : "DownloadFile");

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

            if (_td != null && _td.IsShowing)
            {
                _td.SimulateButtonClick(-1);
            }

            if (_res == Result.Cancel)
            {
                return;
            }
            
            if (e.First == null && e.Second == null)
            {
                new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.Stop,
                        Title       = "Download error",
                        Instruction = _td.Instruction,
                        Content     = "There was an error while downloading the requested file." + Environment.NewLine + "Try downloading another file from the list."
                    }.Show();
                return;
            }

            switch (e.Third)
            {
                case "DownloadFile":
                    var sfd = new SaveFileDialog
                        {
                            CheckPathExists = true,
                            FileName        = e.Second
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
