namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>SubtitlesPage</c> links.
    /// </summary>
    public class SubtitleDownloadTaskDialog
    {
        private TaskDialog _td;
        private IDownloader _dl;
        private FileSearch _fs;
        private Subtitle _link;
        private string _show, _ep;

        #region Download subtitle
        /// <summary>
        /// Downloads the specified link.
        /// </summary>
        /// <param name="link">The link.</param>
        public void Download(Subtitle link)
        {
            _td = new TaskDialog();

            _td.Caption         = "Downloading...";
            _td.InstructionText = link.Release;
            _td.Text            = "Sending request to " + new Uri(link.URL).DnsSafeHost.Replace("www.", string.Empty) + "...";
            _td.StandardButtons = TaskDialogStandardButtons.Cancel;
            _td.Cancelable      = true;
            _td.ProgressBar     = new TaskDialogProgressBar { State = TaskDialogProgressBarState.Marquee };
            _td.Closing        += TaskDialogClosing;

            new Thread(() => _td.Show()).Start();

            _dl                          = link.Source.Downloader;
            _dl.DownloadFileCompleted   += DownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) => _td.Text = "Downloading file... ({0}%)".FormatWith(a.Data);

            _dl.Download(link, Utils.GetRandomFileName());

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
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

            if (e.Third == "LaunchedBrowser")
            {
                return;
            }

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
        }
        #endregion

        #region Download subtitle near video
        /// <summary>
        /// Downloads the specified link near the specified video.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        public void DownloadNearVideo(Subtitle link, string show, string episode)
        {
            _link = link;
            _show = show;
            _ep   = episode;

            var path = Settings.Get("Download Path");

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                new TaskDialog
                    {
                        Icon            = TaskDialogStandardIcon.Error,
                        Caption         = "Search path not configured",
                        InstructionText = "Search path not configured",
                        Text            = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'.",
                        Cancelable      = true
                    }.Show();
                return;
            }

            _td = new TaskDialog();

            _td.Caption         = "Searching...";
            _td.InstructionText = _link.Release;
            _td.Text            = "Searching for the episode...";
            _td.StandardButtons = TaskDialogStandardButtons.Cancel;
            _td.Cancelable      = true;
            _td.ProgressBar     = new TaskDialogProgressBar { State = TaskDialogProgressBarState.Marquee };
            _td.Closing        += TaskDialogClosing;

            new Thread(() => _td.Show()).Start();

            _fs = new FileSearch(path, show, episode);

            _fs.FileSearchDone += NearVideoFileSearchDone;
            _fs.BeginSearch();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchDone</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void NearVideoFileSearchDone(object sender, EventArgs e)
        {
            if (_fs.Files.Count == 0)
            {
                _td.Close(TaskDialogResult.Ok);
                Thread.Sleep(100);

                _td.Icon            = TaskDialogStandardIcon.Error;
                _td.Caption         = "No files found";
                _td.Text            = "No files were found for this episode.\r\nUse the first option to download the subtitle and locate the file manually.";
                _td.StandardButtons = TaskDialogStandardButtons.Ok;
                _td.ProgressBar     = null;
                _td.Show();

                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                return;
            }

            _td.Text = "Sending request to " + new Uri(_link.URL).DnsSafeHost.Replace("www.", string.Empty) + "...";

            _dl                          = _link.Source.Downloader;
            _dl.DownloadFileCompleted   += NearVideoDownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) => _td.Text = "Downloading file... ({0}%)".FormatWith(a.Data);

            _dl.Download(_link, Utils.GetRandomFileName());
        }

        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void NearVideoDownloadFileCompleted(object sender, EventArgs<string, string, string> e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            _td.Close(TaskDialogResult.Ok);
            Thread.Sleep(100);

            _td.Caption = "Download subtitle near video";
            _td.Text    = "The following files were found for {0} {1}.\r\nSelect the desired video file and the subtitle will be placed in the same directory with the same name.".FormatWith(_show, _ep);

            foreach (var file in _fs.Files)
            {
                var tmp     = file;
                var fi      = new FileInfo(file);
                var quality = ThePirateBay.ParseQuality(file);
                var instr   = string.Empty;

                if (quality != Parsers.Downloads.Qualities.Unknown)
                {
                    instr = quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                }

                instr += Utils.GetFileSize(fi.Length)
                       + Environment.NewLine
                       + fi.DirectoryName;

                var fd = new TaskDialogCommandLink
                    {
                        Text        = fi.Name,
                        Instruction = instr
                    };
                fd.Click += (x, r) =>
                    {
                        _td.Close(TaskDialogResult.Ok);

                        var dest = Path.ChangeExtension(tmp, new FileInfo(e.Second).Extension);
                        if (File.Exists(dest))
                        {
                            File.Delete(dest);
                        }

                        File.Move(e.First, dest);
                    };

                _td.Controls.Add(fd);
            }

            _td.Show();
        }
        #endregion

        /// <summary>
        /// Handles the Closing event of the TaskDialog control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.WindowsAPICodePack.Dialogs.TaskDialogClosingEventArgs"/> instance containing the event data.</param>
        private void TaskDialogClosing(object sender, TaskDialogClosingEventArgs e)
        {
            if (e.TaskDialogResult == TaskDialogResult.Cancel)
            {
                if (_fs != null)
                {
                    _fs.CancelSearch();
                }

                if (_dl != null)
                {
                    _dl.CancelAsync();
                }
            }
        }
    }
}
