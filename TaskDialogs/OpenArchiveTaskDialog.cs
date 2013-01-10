namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using SharpCompress.Archive;

    using TaskDialogInterop;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend for unraring scene releases.
    /// </summary>
    public class OpenArchiveTaskDialog
    {
        private Thread _thd;
        private string _file, _ext, _tdstr;
        private int _tdpos;
        private volatile bool _active;

        /// <summary>
        /// Gets a list of supported archive file extensions.
        /// </summary>
        public static string[] SupportedArchives
        {
            get
            {
                return new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".001" };
            }
        }

        /// <summary>
        /// Downloads the specified link near the specified video.
        /// </summary>
        /// <param name="file">The path to the archive.</param>
        public void OpenArchive(string file)
        {
            _file = file;

            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Open archive",
                    MainInstruction         = Path.GetFileName(file),
                    Content                 = "The episode you are trying to play is compressed.",
                    AllowDialogCancellation = true,
                    CommandButtons          = new[]
                        {
                            "Open archive with default video player\nSome video players are be able to open files inside archives.", 
                            "Decompress archive before opening", 
                            "None of the above"
                        }
                });

            if (!res.CommandButtonResult.HasValue)
            {
                return;
            }
            
            switch (res.CommandButtonResult.Value)
            {
                case 0:
                    var apps = Utils.GetDefaultVideoPlayers();

                    if (apps.Length == 0)
                    {
                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon                = VistaTaskDialogIcon.Error,
                                Title                   = "No associations found",
                                MainInstruction         = "No associations found",
                                Content                 = "No application is registered to open any of the known video types.",
                                AllowDialogCancellation = true,
                                CustomButtons           = new[] { "OK" }
                            });

                        return;
                    }

                    Utils.Run(apps[0], _file);
                    break;

                case 1:
                    var archive = ArchiveFactory.Open(_file);
                    var files   = archive.Entries.Where(f => !f.IsDirectory && ShowNames.Regexes.KnownVideo.IsMatch(f.FilePath) && !ShowNames.Regexes.SampleVideo.IsMatch(f.FilePath)).ToList();
                    
                    if (files.Count == 1)
                    {
                        ExtractFile(files[0], archive);
                    }
                    else if (files.Count == 0)
                    {
                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon                = VistaTaskDialogIcon.Error,
                                Title                   = "No files found",
                                MainInstruction         = "No files found",
                                Content                 = "The archive doesn't contain any video files.",
                                AllowDialogCancellation = true,
                                CustomButtons           = new[] { "OK" }
                            });

                        return;
                    }
                    else
                    {
                        var seltd = new TaskDialogOptions
                            {
                                Title                   = "Open archive",
                                MainInstruction         = Path.GetFileName(_file),
                                Content                 = "The archive contains more than one video files.\r\nSelect the one to decompress and open:",
                                AllowDialogCancellation = true,
                                CommandButtons          = new string[files.Count + 1]
                            };

                        var i = 0;
                        foreach (var c in files)
                        {
                            seltd.CommandButtons[i] = c.FilePath + "\n" + Utils.GetFileSize(c.Size) + "   –   " + c.LastModifiedTime;
                            i++;
                        }

                        seltd.CommandButtons[i] = "None of the above";

                        var rez = TaskDialog.Show(seltd);

                        if (rez.CommandButtonResult.HasValue && rez.CommandButtonResult.Value < files.Count)
                        {
                            ExtractFile(files[rez.CommandButtonResult.Value], archive);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Begins the file extraction.
        /// </summary>
        /// <param name="file">The file within the archive.</param>
        /// <param name="archive">The archive.</param>
        private void ExtractFile(IArchiveEntry file, IArchive archive)
        {
            _ext = Path.Combine(Path.GetDirectoryName(_file), Path.GetFileName(file.FilePath));
            _active = true;
            _tdstr = "Extracting file...";
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Extracting...",
                    MainInstruction         = Path.GetFileName(file.FilePath),
                    Content                 = _tdstr,
                    CustomButtons           = new[] { "Cancel" },
                    ShowProgressBar         = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            dialog.SetProgressBarPosition(_tdpos);
                            dialog.SetContent(_tdstr);

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try { _thd.Abort(); _thd = null; } catch { }

                                    if (!string.IsNullOrWhiteSpace(_ext) && File.Exists(_ext))
                                    {
                                        new Thread(() =>
                                            {
                                                Thread.Sleep(1000);
                                                try { File.Delete(_ext); } catch { }
                                            }).Start();
                                    }
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

            var total = file.Size;
            var last = DateTime.MinValue;

            archive.CompressedBytesRead += (sender, args) =>
                {
                    if (args.CurrentFilePartCompressedBytesRead == total)
                    {
                        return;
                    }

                    if ((DateTime.Now - last).TotalMilliseconds < 150)
                    {
                        return;
                    }

                    last = DateTime.Now;

                    var perc = ((double)args.CurrentFilePartCompressedBytesRead / (double)total) * 100;
                    _tdpos = (int)perc;
                    _tdstr = "Extracting file: " + Utils.GetFileSize(args.CurrentFilePartCompressedBytesRead) + " / " + perc.ToString("0.00") + "% done...";
                };
            archive.EntryExtractionEnd += (sender, args) =>
                {
                    _active = false;

                    if (!File.Exists(_ext))
                    {
                        return;
                    }

                    new Thread(() =>
                        {
                            Thread.Sleep(250);
                            Utils.Run(_ext);
 
                            Thread.Sleep(10000);
                            AskAfterUse();
                        }).Start();
                };

            _thd = new Thread(() => file.WriteToFile(_ext));
            _thd.Start();
        }
        
        /// <summary>
        /// Waits until the file is not in use anymore,
        /// and asks the user if s/he wants to remove it.
        /// </summary>
        private void AskAfterUse()
        {
            while (Utils.IsFileLocked(_ext))
            {
                Thread.Sleep(1000);
            }
            
            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon                = VistaTaskDialogIcon.Information,
                    Title                   = Signature.Software,
                    MainInstruction         = "Extracted file not in use anymore",
                    Content                 = "Would you like to remove the extracted file " + Path.GetFileName(_ext) + " now that you've watched it?",
                    AllowDialogCancellation = true,
                    CustomButtons           = new[] { "Yes", "No" }
                });

            if (res.CustomButtonResult.HasValue && res.CustomButtonResult.Value == 0)
            {
                try { File.Delete(_ext); } catch { }
            }
        }
    }
}
