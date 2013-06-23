namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using SharpCompress.Archive;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;
    using RoliSoft.TVShowTracker.ShowNames;
    using RoliSoft.TVShowTracker.Parsers.Guides;

    using Parser = RoliSoft.TVShowTracker.FileNames.Parser;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>SubtitlesPage</c> links.
    /// </summary>
    public class SubtitleDownloadTaskDialog
    {
        private IDownloader _dl;
        private Subtitle _link;
        private Episode _ep;
        private FileSearch _fs;
        private string _file;
        private List<string> _files; 
        private bool _play;
        private volatile bool _active;
        private string _tdstr, _tdtit;
        private int _tdpos;

        #region Download subtitle
        /// <summary>
        /// Downloads the specified link.
        /// </summary>
        /// <param name="link">The link.</param>
        public void Download(Subtitle link)
        {
            _active = true;
            _tdtit = link.Release;
            _tdstr = "Sending request to " + (string.IsNullOrWhiteSpace(link.FileURL ?? link.InfoURL) ? link.Source.Name : new Uri(link.FileURL ?? link.InfoURL).DnsSafeHost.Replace("www.", string.Empty)) + "...";
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Downloading...",
                    MainInstruction         = _tdtit,
                    Content                 = _tdstr,
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    ShowProgressBar         = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp && _tdpos == 0)
                            {
                                dialog.SetProgressBarMarquee(true, 0);

                                showmbp = true;
                            }

                            if (_tdpos > 0 && showmbp)
                            {
                                dialog.SetMarqueeProgressBar(false);
                                dialog.SetProgressBarState(VistaProgressBarState.Normal);
                                dialog.SetProgressBarPosition(_tdpos);

                                showmbp = false;
                            }

                            if (_tdpos > 0)
                            {
                                dialog.SetProgressBarPosition(_tdpos);
                            }

                            dialog.SetContent(_tdstr);

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try { _dl.CancelAsync(); } catch { }
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
            
            _dl                          = link.Source.Downloader;
            _dl.DownloadFileCompleted   += DownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) =>
                {
                    _tdstr = "Downloading file... ({0}%)".FormatWith(a.Data);
                    _tdpos = a.Data;
                };

            _dl.Download(link, Path.Combine(Path.GetTempPath(), Utils.CreateSlug(link.Release.Replace('.', ' ').Replace('_', ' ') + " " + link.Source.Name + " " + Utils.Rand.Next().ToString("x"), false)));

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadFileCompleted(object sender, EventArgs<string, string, string> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (e.Third == "LaunchedBrowser")
            {
                return;
            }

            if (e.First == null && e.Second == null)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Download error",
                        MainInstruction         = _tdtit,
                        Content                 = "There was an error while downloading the requested file." + Environment.NewLine + "Try downloading another file from the list.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });

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
        /// <param name="episode">The episode to search for.</param>
        /// <param name="file">The file.</param>
        public void DownloadNearVideo(Subtitle link, Episode episode, string file)
        {
            _link = link;
            _ep   = episode;
            _file = file;
            
            _active = true;
            _tdtit = link.Release;
            _tdstr = "Sending request to " + (string.IsNullOrWhiteSpace(link.FileURL ?? link.InfoURL) ? link.Source.Name : new Uri(link.FileURL ?? link.InfoURL).DnsSafeHost.Replace("www.", string.Empty)) + "...";
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Downloading...",
                    MainInstruction         = _tdtit,
                    Content                 = _tdstr,
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    ShowProgressBar         = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp && _tdpos == 0)
                            {
                                dialog.SetProgressBarMarquee(true, 0);

                                showmbp = true;
                            }

                            if (_tdpos > 0 && showmbp)
                            {
                                dialog.SetMarqueeProgressBar(false);
                                dialog.SetProgressBarState(VistaProgressBarState.Normal);
                                dialog.SetProgressBarPosition(_tdpos);

                                showmbp = false;
                            }

                            if (_tdpos > 0)
                            {
                                dialog.SetProgressBarPosition(_tdpos);
                            }

                            dialog.SetContent(_tdstr);

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try { _dl.CancelAsync(); } catch { }
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
            
            _dl                          = link.Source.Downloader;
            _dl.DownloadFileCompleted   += NearVideoDownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) =>
                {
                    _tdstr = "Downloading file... ({0}%)".FormatWith(a.Data);
                    _tdpos = a.Data;
                };

            _dl.Download(link, Path.Combine(Path.GetTempPath(), Utils.CreateSlug(link.Release.Replace('.', ' ').Replace('_', ' ') + " " + link.Source.Name + " " + Utils.Rand.Next().ToString("x"), false)));

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }
        
        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void NearVideoDownloadFileCompleted(object sender, EventArgs<string, string, string> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (e.First == null && e.Second == null)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Download error",
                        MainInstruction         = _link.Release,
                        Content                 = "There was an error while downloading the requested file." + Environment.NewLine + "Try downloading another file from the list.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });

                return;
            }

            NearVideoFinishMove(_file, e.First, e.Second);
            
            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Download finished",
                    MainInstruction         = _link.Release,
                    Content                 = "The subtitle has been downloaded, placed near the requested file and renamed to have the same name.",
                    AllowDialogCancellation = true,
                    CommandButtons          = new[]
                        {
                            "Play video", 
                            "Open folder", 
                            "Close"
                        }
                });

            if (!res.CommandButtonResult.HasValue)
            {
                return;
            }

            switch (res.CommandButtonResult.Value)
            {
                case 0:
                    if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(_file).ToLower()))
                    {
                        new OpenArchiveTaskDialog().OpenArchive(_file);
                    }
                    else
                    {
                        Utils.Run(_file);
                    }
                    break;

                case 1:
                    Utils.Run("explorer.exe", "/select,\"" + _file + "\"");
                    break;
            }
        }

        /// <summary>
        /// Moves the downloaded subtitle near the video.
        /// </summary>
        /// <param name="video">The location of the video.</param>
        /// <param name="temp">The location of the downloaded subtitle.</param>
        /// <param name="subtitle">The original file name of the subtitle.</param>
        private void NearVideoFinishMove(string video, string temp, string subtitle)
        {
            if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(subtitle).ToLower()))
            {
                var archive = ArchiveFactory.Open(temp);
                var files   = archive.Entries.Where(f => !f.IsDirectory && Regexes.KnownSubtitle.IsMatch(f.FilePath)).ToList();

                if (files.Count == 1)
                {
                    using (var mstream = new MemoryStream())
                    {
                        subtitle = Utils.SanitizeFileName(files.First().FilePath.Split('\\').Last());
                        files.First().WriteTo(mstream);
                        
                        try { archive.Dispose(); } catch { }
                        try { File.Delete(temp); } catch { }
                        File.WriteAllBytes(temp, mstream.ToArray());
                    }
                }
                else
                {
                    var dlsnvtd = new TaskDialogOptions
                        {
                            Title                   = "Download subtitle near video",
                            MainInstruction         = _link.Release,
                            Content                 = "The downloaded subtitle was a ZIP package with more than one files.\r\nSelect the matching subtitle file to extract it from the package:",
                            AllowDialogCancellation = true,
                            CommandButtons          = new string[files.Count + 1],
                        };

                    var i = 0;
                    for (; i < files.Count; i++)
                    {
                        dlsnvtd.CommandButtons[i] = files[i].FilePath + "\n" + Utils.GetFileSize(files[i].Size) + "   –   " + files[i].LastModifiedTime;
                    }

                    dlsnvtd.CommandButtons[i] = "None of the above";

                    var res = TaskDialog.Show(dlsnvtd);

                    if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value < files.Count)
                    {
                        using (var mstream = new MemoryStream())
                        {
                            subtitle = Utils.SanitizeFileName(files[res.CommandButtonResult.Value].FilePath.Split('\\').Last());
                            files[res.CommandButtonResult.Value].WriteTo(mstream);

                            try { archive.Dispose(); } catch { }
                            try { File.Delete(temp); } catch { }
                            File.WriteAllBytes(temp, mstream.ToArray());
                        }

                        NearVideoFinishMove(video, temp, subtitle);
                    }
                    else
                    {
                        _play = false;
                    }

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

        #region Download subtitle near video (old)
        /// <summary>
        /// Downloads the specified link near the specified video.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="episode">The episode to search for.</param>
        public void DownloadNearVideoOld(Subtitle link, Episode episode)
        {
            _link = link;
            _ep   = episode;

            var paths = Settings.Get<List<string>>("Download Paths");

            if (paths.Count == 0)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Search path not configured",
                        MainInstruction         = "Search path not configured",
                        Content                 = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });
                return;
            }

            _active = true;
            _tdstr = "Searching for the episode...";
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Searching...",
                    MainInstruction         = link.Release,
                    Content                 = "Searching for the episode...",
                    CustomButtons           = new[] { "Cancel" },
                    ShowProgressBar         = true,
                    ShowMarqueeProgressBar  = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp && _tdpos == 0)
                            {
                                dialog.SetProgressBarMarquee(true, 0);

                                showmbp = true;
                            }

                            if (_tdpos > 0 && showmbp)
                            {
                                dialog.SetMarqueeProgressBar(false);
                                dialog.SetProgressBarState(VistaProgressBarState.Normal);
                                dialog.SetProgressBarPosition(_tdpos);

                                showmbp = false;
                            }

                            if (_tdpos > 0)
                            {
                                dialog.SetProgressBarPosition(_tdpos);
                            }

                            dialog.SetContent(_tdstr);

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try
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
                                    catch { }
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

            _fs = new FileSearch(paths, _ep);

            _fs.FileSearchDone += NearVideoFileSearchDoneOld;
            _fs.FileSearchProgressChanged += NearVideoFileSearchProgressChangedOld;
            _fs.BeginSearch();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchProgressChanged</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="string"/> instance containing the event data.</param>
        private void NearVideoFileSearchProgressChangedOld(object sender, EventArgs<string> e)
        {
            _tdstr = e.Data;
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchDone</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.Collections.Generic.List&lt;System.String&gt;&gt;"/> instance containing the event data.</param>
        private void NearVideoFileSearchDoneOld(object sender, EventArgs<List<string>>  e)
        {
            _files = e.Data ?? new List<string>();

            if (_files.Count == 0)
            {
                _active = false;

                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "No files found",
                        MainInstruction         = _link.Release,
                        Content                 = "No files were found for this episode.\r\nUse the first option to download the subtitle and locate the file manually.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });
                return;
            }

            _tdstr = "Sending request to " + new Uri(_link.FileURL ?? _link.InfoURL).DnsSafeHost.Replace("www.", string.Empty) + "...";

            _dl                          = _link.Source.Downloader;
            _dl.DownloadFileCompleted   += NearVideoDownloadFileCompletedOld;
            _dl.DownloadProgressChanged += (s, a) =>
                {
                    _tdstr = "Downloading file... ({0}%)".FormatWith(a.Data);
                    _tdpos = a.Data;
                };

            _dl.Download(_link, Path.Combine(Path.GetTempPath(), Utils.CreateSlug(_link.Release.Replace('.', ' ').Replace('_', ' ') + " " + _link.Source.Name + " " + Utils.Rand.Next().ToString("x"), false)));
        }

        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void NearVideoDownloadFileCompletedOld(object sender, EventArgs<string, string, string> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (e.First == null && e.Second == null)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Download error",
                        MainInstruction         = _link.Release,
                        Content                 = "There was an error while downloading the requested file." + Environment.NewLine + "Try downloading another file from the list.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });

                return;
            }

            var dlsnvtd = new TaskDialogOptions
                {
                    Title                   = "Download subtitle near video",
                    MainInstruction         = _link.Release,
                    Content                 = "The following files were found for {0} S{1:00}E{2:00}.\r\nSelect the desired video file and the subtitle will be placed in the same directory with the same name.".FormatWith(_ep.Show.Title, _ep.Season, _ep.Number),
                    AllowDialogCancellation = true,
                    CommandButtons          = new string[_files.Count + 1],
                    VerificationText        = "Play video after subtitle is downloaded",
                    VerificationByDefault   = Settings.Get("Play Video After Subtitle Download", false)
                };
            
            var i = 0;
            for (; i < _files.Count; i++)
            {
                var fi      = new FileInfo(_files[i]);
                var quality = Parser.ParseQuality(_files[i]);
                var instr   = fi.Name + "\n";

                if (quality != Parsers.Downloads.Qualities.Unknown)
                {
                    instr += quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                }

                dlsnvtd.CommandButtons[i] = instr + Utils.GetFileSize(fi.Length) + "\n" + fi.DirectoryName;
            }

            dlsnvtd.CommandButtons[i] = "None of the above";

            var res = TaskDialog.Show(dlsnvtd);

            if (res.VerificationChecked.HasValue)
            {
                Settings.Set("Play Video After Subtitle Download", _play = res.VerificationChecked.Value);
            }

            if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value < _files.Count)
            {
                new Thread(() =>
                    {
                        NearVideoFinishMoveOld(_files[res.CommandButtonResult.Value], e.First, e.Second);

                        if (_play)
                        {
                            if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(_files[res.CommandButtonResult.Value]).ToLower()))
                            {
                                new OpenArchiveTaskDialog().OpenArchive(_files[res.CommandButtonResult.Value]);
                            }
                            else
                            {
                                Utils.Run(_files[res.CommandButtonResult.Value]);
                            }
                        }
                    }).Start();
            }
        }

        /// <summary>
        /// Moves the downloaded subtitle near the video.
        /// </summary>
        /// <param name="video">The location of the video.</param>
        /// <param name="temp">The location of the downloaded subtitle.</param>
        /// <param name="subtitle">The original file name of the subtitle.</param>
        private void NearVideoFinishMoveOld(string video, string temp, string subtitle)
        {
            if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(subtitle).ToLower()))
            {
                var archive = ArchiveFactory.Open(temp);
                var files   = archive.Entries.Where(f => !f.IsDirectory && Regexes.KnownSubtitle.IsMatch(f.FilePath)).ToList();

                if (files.Count == 1)
                {
                    using (var mstream = new MemoryStream())
                    {
                        subtitle = Utils.SanitizeFileName(files.First().FilePath.Split('\\').Last());
                        files.First().WriteTo(mstream);
                        
                        try { archive.Dispose(); } catch { }
                        try { File.Delete(temp); } catch { }
                        File.WriteAllBytes(temp, mstream.ToArray());
                    }
                }
                else
                {
                    var dlsnvtd = new TaskDialogOptions
                        {
                            Title                   = "Download subtitle near video",
                            MainInstruction         = _link.Release,
                            Content                 = "The downloaded subtitle was a ZIP package with more than one files.\r\nSelect the matching subtitle file to extract it from the package:",
                            AllowDialogCancellation = true,
                            CommandButtons          = new string[files.Count + 1],
                        };

                    var i = 0;
                    for (; i < files.Count; i++)
                    {
                        dlsnvtd.CommandButtons[i] = files[i].FilePath + "\n" + Utils.GetFileSize(files[i].Size) + "   –   " + files[i].LastModifiedTime;
                    }

                    dlsnvtd.CommandButtons[i] = "None of the above";

                    var res = TaskDialog.Show(dlsnvtd);

                    if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value < files.Count)
                    {
                        using (var mstream = new MemoryStream())
                        {
                            subtitle = Utils.SanitizeFileName(files[res.CommandButtonResult.Value].FilePath.Split('\\').Last());
                            files[res.CommandButtonResult.Value].WriteTo(mstream);

                            try { archive.Dispose(); } catch { }
                            try { File.Delete(temp); } catch { }
                            File.WriteAllBytes(temp, mstream.ToArray());
                        }

                        NearVideoFinishMoveOld(video, temp, subtitle);
                    }
                    else
                    {
                        _play = false;
                    }

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
    }
}
