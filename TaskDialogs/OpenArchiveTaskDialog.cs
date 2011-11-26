namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using SharpCompress.Archive;
    using SharpCompress.Common;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend for unraring scene releases.
    /// </summary>
    public class OpenArchiveTaskDialog
    {
        private TaskDialog _td;
        private ExtractionListener _el;
        private Thread _thd;
        private string _file, _ext;

        /// <summary>
        /// Gets a list of supported archive file extensions.
        /// </summary>
        public static string[] SupportedArchives
        {
            get
            {
                return new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".000" };
            }
        }

        /// <summary>
        /// Downloads the specified link near the specified video.
        /// </summary>
        /// <param name="file">The path to the archive.</param>
        public void OpenArchive(string file)
        {
            _file = file;

            _td = new TaskDialog
                {
                    Title           = "Open archive",
                    Instruction     = Path.GetFileName(file),
                    Content         = "The episode you are trying to play is compressed.",
                    UseCommandLinks = true,
                    CommonButtons   = TaskDialogButton.Cancel,
                    CustomButtons   = new[]
                                          {
                                              new CustomButton(0, "Open archive with default video player\nSome video players are be able to open files inside archives."), 
                                              new CustomButton(1, "Decompress archive before opening"), 
                                          },
                };

            _td.ButtonClick += (s, e) => new Thread(() => TaskDialogButtonClick(s, e)).Start();

            new Thread(() => _td.Show()).Start();
        }

        /// <summary>
        /// Handles the ButtonClick event of the _td control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TaskDialogButtonClick(object sender, EventArgs e)
        {
            switch (((ClickEventArgs)e).ButtonID)
            {
                case 0:
                    var apps = Utils.GetDefaultVideoPlayers();

                    if (apps.Length == 0)
                    {
                        new TaskDialog
                            {
                                CommonIcon  = TaskDialogIcon.Stop,
                                Title       = "No associations found",
                                Instruction = "No associations found",
                                Content     = "No application is registered to open any of the known video types."
                            }.Show();
                        return;
                    }

                    Utils.Run(apps[0], _file);
                    break;

                case 1:
                    var archive = ArchiveFactory.Open(_file);
                    var files   = archive.Entries.Where(f => !f.IsDirectory && ShowNames.Regexes.KnownVideo.IsMatch(f.FilePath) && !ShowNames.Regexes.SampleVideo.IsMatch(f.FilePath)).ToList();

                    if (files.Count == 1)
                    {
                        ExtractFile(files[0]);
                    }
                    else if (files.Count == 0)
                    {
                        new TaskDialog
                            {
                                CommonIcon  = TaskDialogIcon.Stop,
                                Title       = "No files found",
                                Instruction = "No files found",
                                Content     = "The archive doesn't contain any video files."
                            }.Show();
                        return;
                    }
                    else
                    {
                        var seltd = new TaskDialog
                            {
                                Title           = "Open archive",
                                Instruction     = Path.GetFileName(_file),
                                Content         = "The archive contains more than one video files.\r\nSelect the one to decompress and open:",
                                CommonButtons   = TaskDialogButton.Cancel,
                                CustomButtons   = new CustomButton[files.Count],
                                UseCommandLinks = true
                            };

                        seltd.ButtonClick += (s, c) =>
                            {
                                if (c.ButtonID < files.Count)
                                {
                                    ExtractFile(files[c.ButtonID]);
                                }
                            };

                        var i = 0;
                        foreach (var c in files)
                        {
                            seltd.CustomButtons[i] = new CustomButton(i, c.FilePath + "\n" + Utils.GetFileSize(c.Size) + "   –   " + c.LastModifiedTime);
                            i++;
                        }

                        seltd.Show();
                    }
                    break;
            }
        }

        /// <summary>
        /// Begins the file extraction.
        /// </summary>
        /// <param name="file">The file within the archive.</param>
        private void ExtractFile(IArchiveEntry file)
        {
            _td = new TaskDialog
                {
                    Title           = "Extracting...",
                    Instruction     = Path.GetFileName(file.FilePath),
                    Content         = "Extracting file...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;

            new Thread(() => _td.Show()).Start();

            _ext = Path.Combine(Path.GetDirectoryName(_file), Path.GetFileName(file.FilePath));

            _el = new ExtractionListener();

            _el.TaskDialog = _td;
            _el.Finished = () =>
                {
                    _el.Finished = null;
                    _td.SimulateButtonClick(-1);

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

            _thd = new Thread(() => file.WriteToFile(_ext, _el));
            _thd.Start();
        }

        /// <summary>
        /// Handles the Destroyed event of the _td control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TaskDialogDestroyed(object sender, EventArgs e)
        {
            if (((ClickEventArgs)e).ButtonID == 2)
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
            
            var res = new TaskDialog
                {
                    CommonIcon    = TaskDialogIcon.SecurityWarning,
                    Title         = Signature.Software,
                    Instruction   = "Extracted file not in use anymore",
                    Content       = "Would you like to remove the extracted file " + Path.GetFileName(_ext) + " now that you've watched it?",
                    CommonButtons = TaskDialogButton.Yes | TaskDialogButton.No
                }.Show();

            if (res.ButtonID == 6)
            {
                try { File.Delete(_ext); } catch { }
            }
        }

        /// <summary>
        /// Provides an extraction listener to <see cref="IArchive"/>.
        /// </summary>
        private class ExtractionListener : IExtractionListener
        {
            /// <summary>
            /// Gets or sets the task dialog.
            /// </summary>
            /// <value>
            /// The task dialog.
            /// </value>
            public TaskDialog TaskDialog { get; set; }

            /// <summary>
            /// Gets or sets the method to call when finished.
            /// </summary>
            /// <value>
            /// The method to call when finished.
            /// </value>
            public Action Finished { get; set; }

            private long _total;
            private DateTime _last = DateTime.MinValue;
            
            /// <summary>
            /// Called when file entry extraction initialized.
            /// </summary>
            /// <param name="entryFileName">Name of the entry file.</param>
            /// <param name="totalEntryCompressedBytes">The total entry compressed bytes.</param>
            public void OnFileEntryExtractionInitialized(string entryFileName, long? totalEntryCompressedBytes)
            {
                _total = totalEntryCompressedBytes.Value;
            }

            /// <summary>
            /// Called when file part extraction initialized.
            /// </summary>
            /// <param name="partFileName">Name of the part file.</param>
            /// <param name="totalPartCompressedBytes">The total part compressed bytes.</param>
            public void OnFilePartExtractionInitialized(string partFileName, long totalPartCompressedBytes)
            {
            }

            /// <summary>
            /// Called when compressed bytes read.
            /// </summary>
            /// <param name="currentPartCompressedBytes">The current part compressed bytes.</param>
            /// <param name="currentEntryCompressedBytes">The current entry compressed bytes.</param>
            public void OnCompressedBytesRead(long currentPartCompressedBytes, long currentEntryCompressedBytes)
            {
                if (currentEntryCompressedBytes == _total)
                {
                    if (Finished != null)
                    {
                        Finished();
                    }

                    return;
                }

                if ((DateTime.Now - _last).TotalMilliseconds < 150)
                {
                    return;
                }

                _last = DateTime.Now;

                var perc = ((double)currentEntryCompressedBytes / (double)_total) * 100;
                TaskDialog.ProgressBarPosition = (int)perc;
                TaskDialog.Content = "Extracting file: " + Utils.GetFileSize(currentEntryCompressedBytes) + " / " + perc.ToString("0.00") + "% done...";
            }

            /// <summary>
            /// Called when information.
            /// </summary>
            /// <param name="message">The message.</param>
            public void OnInformation(string message)
            {
            }
        }
    }
}
