namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Ionic.Zip;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.FileNames;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>SubtitlesPage</c> links.
    /// </summary>
    public class SubtitleDownloadTaskDialog
    {
        private TaskDialog _td;
        private Result _res;
        private IDownloader _dl;
        private FileSearch _fs;
        private Subtitle _link;
        private string _show, _episode;
        private bool _play;

        #region Download subtitle
        /// <summary>
        /// Downloads the specified link.
        /// </summary>
        /// <param name="link">The link.</param>
        public void Download(Subtitle link)
        {
            _td = new TaskDialog
                {
                    Title           = "Downloading...",
                    Instruction     = link.Release,
                    Content         = "Sending request to " + new Uri(link.URL).DnsSafeHost.Replace("www.", string.Empty) + "...",
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

            if (_td != null && _td.IsShowing)
            {
                _td.SimulateButtonClick(-1);
            }

            if (_res == Result.Cancel)
            {
                return;
            }

            if (e.Third == "LaunchedBrowser")
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
        /// <param name="airdate">The airdate.</param>
        public void DownloadNearVideo(Subtitle link, string show, string episode, DateTime? airdate = null)
        {
            _link    = link;
            _show    = show;
            _episode = episode;

            var paths = Settings.Get<List<string>>("Download Paths");

            if (paths.Count == 0)
            {
                new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.Stop,
                        Title       = "Search path not configured",
                        Instruction = "Search path not configured",
                        Content     = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'."
                    }.Show();
                return;
            }

            _td = new TaskDialog
                {
                    Title           = "Searching...",
                    Instruction     = link.Release,
                    Content         = "Searching for the episode...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;

            new Thread(() => _res = _td.Show().CommonButton).Start();

            _fs = new FileSearch(paths, show, episode, airdate);

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
                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                if (_td != null && _td.IsShowing)
                {
                    _td.SimulateButtonClick(-1);
                }

                if (_res == Result.Cancel)
                {
                    return;
                }

                new TaskDialog
                    {
                        CommonIcon    = TaskDialogIcon.Stop,
                        Title         = "No files found",
                        Instruction   = _link.Release,
                        Content       = "No files were found for this episode.\r\nUse the first option to download the subtitle and locate the file manually.",
                        CommonButtons = TaskDialogButton.OK
                    }.Show();
                return;
            }

            _td.Content = "Sending request to " + new Uri(_link.URL).DnsSafeHost.Replace("www.", string.Empty) + "...";

            var prm = true;

            _dl                          = _link.Source.Downloader;
            _dl.DownloadFileCompleted   += NearVideoDownloadFileCompleted;
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
                        Instruction = _link.Release,
                        Content     = "There was an error while downloading the requested file." + Environment.NewLine + "Try downloading another file from the list."
                    }.Show();
                return;
            }

            var dlsnvtd = new TaskDialog
                {
                    Title                 = "Download subtitle near video",
                    Instruction           = _link.Release,
                    Content               = "The following files were found for {0} {1}.\r\nSelect the desired video file and the subtitle will be placed in the same directory with the same name.".FormatWith(_show, _episode),
                    CommonButtons         = TaskDialogButton.Cancel,
                    CustomButtons         = new CustomButton[_fs.Files.Count],
                    UseCommandLinks       = true,
                    VerificationText      = "Play video after subtitle is downloaded",
                    IsVerificationChecked = false
                };

            dlsnvtd.VerificationClick += (s, c) => _play = c.IsChecked ;
            dlsnvtd.ButtonClick       += (s, c) =>
                {
                    if (c.ButtonID < _fs.Files.Count)
                    {
                        new Thread(() =>
                            {
                                NearVideoFinishMove(_fs.Files[c.ButtonID], e.First, e.Second);

                                if (_play)
                                {
                                    Utils.Run(_fs.Files[c.ButtonID]);
                                }
                            }).Start();
                    }
                };

            var i = 0;
            foreach (var f in _fs.Files)
            {
                var file    = f;
                var fi      = new FileInfo(file);
                var quality = Parser.ParseQuality(file);
                var instr   = fi.Name + "\n";

                if (quality != Parsers.Downloads.Qualities.Unknown)
                {
                    instr += quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                }

                dlsnvtd.CustomButtons[i] = new CustomButton(i, instr + Utils.GetFileSize(fi.Length) + "\n" + fi.DirectoryName);
                i++;
            }

            dlsnvtd.Show();
        }

        /// <summary>
        /// Moves the downloaded subtitle near the video.
        /// </summary>
        /// <param name="video">The location of the video.</param>
        /// <param name="temp">The location of the downloaded subtitle.</param>
        /// <param name="subtitle">The original file name of the subtitle.</param>
        private void NearVideoFinishMove(string video, string temp, string subtitle)
        {
            if (new FileInfo(subtitle).Extension == ".zip")
            {
                var zip = ZipFile.Read(temp);

                if (zip.Entries.Count == 1)
                {
                    using (var mstream = new MemoryStream())
                    {
                        subtitle = Utils.SanitizeFileName(zip.Entries.First().FileName);
                        zip.Entries.First().Extract(mstream);
                        
                        try { zip.Dispose();     } catch { }
                        try { File.Delete(temp); } catch { }
                        File.WriteAllBytes(temp, mstream.ToArray());
                    }
                }
                else
                {
                    var dlsnvtd = new TaskDialog
                        {
                            Title           = "Download subtitle near video",
                            Instruction     = _link.Release,
                            Content         = "The downloaded subtitle was a ZIP package with more than one files.\r\nSelect the matching subtitle file to extract it from the package:",
                            CommonButtons   = TaskDialogButton.Cancel,
                            CustomButtons   = new CustomButton[zip.Count],
                            UseCommandLinks = true
                        };

                    dlsnvtd.ButtonClick += (s, c) =>
                        {
                            if (c.ButtonID < zip.Count)
                            {
                                using (var mstream = new MemoryStream())
                                {
                                    var ent = zip.Entries.ToList();

                                    subtitle = Utils.SanitizeFileName(ent[c.ButtonID].FileName);
                                    ent[c.ButtonID].Extract(mstream);

                                    try { zip.Dispose();     } catch { }
                                    try { File.Delete(temp); } catch { }
                                    File.WriteAllBytes(temp, mstream.ToArray());
                                }

                                NearVideoFinishMove(video, temp, subtitle);
                            }
                            else
                            {
                                _play = false;
                            }
                        };

                    var i = 0;
                    foreach (var c in zip.Entries)
                    {
                        dlsnvtd.CustomButtons[i] = new CustomButton(i, c.FileName + "\n" + Utils.GetFileSize(c.UncompressedSize) + "   –   " + c.LastModified);
                        i++;
                    }

                    dlsnvtd.Show();
                    return;
                }
            }

            var ext = new FileInfo(subtitle).Extension;

            if (Settings.Get<bool>("Append Language to Subtitle") && Languages.List.ContainsKey(_link.Language))
            {
                ext = "." + Languages.List[_link.Language].Substring(0, 3).ToLower() + ext;
            }

            var dest = Path.ChangeExtension(video, ext);

            if (File.Exists(dest))
            {
                try { File.Delete(dest); } catch { }
            }

            File.Move(temp, dest);
        }
        #endregion

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
