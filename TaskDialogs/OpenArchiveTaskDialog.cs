namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using SevenZip;

    using TaskDialogInterop;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend for unraring scene releases.
    /// </summary>
    public class OpenArchiveTaskDialog
    {
        private Thread _thd;
        private FileStream _fs;
        private string _file, _ext, _tdstr;
        private int _tdpos;
        private volatile bool _active, _cancel;

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
                    SevenZipBase.SetLibraryPath(Path.Combine(Signature.InstallPath, @"libs\7z" + (Environment.Is64BitProcess ? "64" : string.Empty) + ".dll"));

                    using (var archive = new SevenZipExtractor(_file))
                    {
                        var files = archive.ArchiveFileData.Where(f => !f.IsDirectory && ShowNames.Regexes.KnownVideo.IsMatch(f.FileName) && !ShowNames.Regexes.SampleVideo.IsMatch(f.FileName)).ToList();

                        if (files.Count == 1)
                        {
                            ExtractFile(files[0]);
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
                                    CustomButtons           = new[] {"OK"}
                                });
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
                                seltd.CommandButtons[i] = c.FileName + "\n" + Utils.GetFileSize((long)c.Size) + "   –   " + c.LastWriteTime;
                                i++;
                            }

                            seltd.CommandButtons[i] = "None of the above";

                            var rez = TaskDialog.Show(seltd);

                            if (rez.CommandButtonResult.HasValue && rez.CommandButtonResult.Value < files.Count)
                            {
                                ExtractFile(files[rez.CommandButtonResult.Value]);
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Begins the file extraction.
        /// </summary>
        /// <param name="file">The file within the archive.</param>
        private void ExtractFile(ArchiveFileInfo file)
        {
            _ext = Path.Combine(Path.GetDirectoryName(_file), Path.GetFileName(file.FileName));

            try
            {
                using (var fs = File.Create(_ext))
                {
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to create new file to '" + _ext + "', retrying with temp path...", ex);

                _ext = Path.Combine(Path.GetTempPath(), Path.GetFileName(file.FileName));

                try
                {
                    using (var fs = File.Create(_ext))
                    {
                        fs.Close();
                    }
                }
                catch (Exception ex2)
                {
                    Log.Warn("Failed to create new temp file to '" + _ext + "', aborting extraction...", ex2);

                    TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon                = VistaTaskDialogIcon.Error,
                                Title                   = "Extraction error",
                                MainInstruction         = "Extraction error",
                                Content                 = "Failed to create new file near archive or in the %TEMP% directory.",
                                AllowDialogCancellation = true,
                                CustomButtons           = new[] { "OK" }
                            });

                    return;
                }
            }

            _active = true;
            _tdstr = "Extracting file...";
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Extracting...",
                    MainInstruction         = Path.GetFileName(file.FileName),
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
                                    _cancel = true;

                                    if (!string.IsNullOrWhiteSpace(_ext) && File.Exists(_ext))
                                    {
                                        new Thread(() =>
                                            {
                                                try { _thd.Abort(); _thd = null; } catch { }
                                                Thread.Sleep(100);
                                                try { _fs.Close(); _fs.Dispose(); } catch { }
                                                Thread.Sleep(100);
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

            _thd = new Thread(() =>
                {
                    using (var fs = _fs = File.Open(_ext, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                    using (var archive = new SevenZipExtractor(_file))
                    { 
                        archive.Extracting += (sender, args) =>
                            {
                                if (_cancel)
                                {
                                    args.Cancel = true;
                                    return;
                                }

                                if (args.PercentDone == 100)
                                {
                                    return;
                                }

                                if ((DateTime.Now - last).TotalMilliseconds < 150)
                                {
                                    return;
                                }

                                last = DateTime.Now;

                                _tdpos = args.PercentDone;
                                _tdstr = "Extracting file: " + Utils.GetFileSize((long)Math.Round((args.PercentDone / 100d) * total)) + " / " + args.PercentDone + "% done...";
                            };

                        archive.ExtractionFinished += (sender, args) =>
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

                        try
                        {
                            archive.ExtractFile(file.Index, fs);
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("Error during file extraction.", ex);
                        }
                    }

                    if (_cancel && File.Exists(_ext))
                    {
                        try { File.Delete(_ext); } catch { }
                    }
                });

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

        /// <summary>
        /// Tries to remove garbage from filename when SharpCompress's decode function goes haywire. 
        /// </summary>
        /// <param name="name">The possibly broken name.</param>
        /// <returns>Possibly fixed name.</returns>
        private string TryFixName(string name)
        {
            if (name.Length == 0 || !char.IsControl(name[0]))
            {
                return name;
            }

            // remove leading 0x00

            name = name.TrimStart("\0".ToCharArray());

            // find start of garbage and remove it

            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsControl(name[i]))
                {
                    name = name.Substring(0, i);
                    break;
                }
            }

            return name;
        }
    }
}
