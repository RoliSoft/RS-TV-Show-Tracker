namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Text;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to upgrading the database from SQLite3.
    /// </summary>
    public class DatabaseUpdateTaskDialog
    {
        private WebClient _wc;
        private string _tdstr;
        private int _tdpos;
        private bool _cancel;

        /// <summary>
        /// Asks the user "what now?"
        /// </summary>
        public void Ask()
        {
            if (!File.Exists(Path.Combine(Signature.FullPath, "TVShows.db3.gz")))
            {
                using (var ufs = File.OpenRead(Path.Combine(Signature.FullPath, "TVShows.db3")))
                using (var zfs = File.Create(Path.Combine(Signature.FullPath, "TVShows.db3.gz")))
                using (var zip = new DeflateStream(zfs, CompressionMode.Compress))
                {
                    ufs.CopyTo(zip);
                }
            }

            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon        = VistaTaskDialogIcon.Warning,
                    Title           = "Upgrade database",
                    MainInstruction = "Upgrade TVShows.db3",
                    Content         = "From this version forward, SQLite3 is not used anymore as the database store for the software.\r\n\r\nYour database file needs to be upgraded to the new format, however, SQLite3 is not bundled anymore with the software, so it can't open it and convert it.\r\n\r\nThe TVShows.db3 file will be uploaded to one of my servers and converted there. This file only contains TV show information, it does NOT contain your settings, cookies, and site logins.\r\n\r\nIf you would like to continue, select a server close to you.",
                    CommandButtons  = new[] { "lab.rolisoft.net\nAnaheim, California, United States", "Exit application" }
                });

            if (res.CommandButtonResult.HasValue)
            {
                switch (res.CommandButtonResult.Value)
                {
                    case 0:
                        Start(new Uri("http://ipv4.lab.rolisoft.net/api/migrate/"));
                        break;

                    default:
                        Process.GetCurrentProcess().Kill();
                        break;
                }
            }
        }

        /// <summary>
        /// Starts the upgrading process.
        /// </summary>
        /// <param name="server">The endpoint for the migration script.</param>
        public void Start(Uri server)
        {
            _tdstr = "Connecting to " + server.DnsSafeHost + "...";
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Upgrading...",
                    MainInstruction         = "Upgrading TVShows.db3",
                    Content                 = "Connecting to " + server.DnsSafeHost + "...",
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
                                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                                if (!_cancel)
                                {
                                    try { _wc.CancelAsync(); } catch { }
                                }

                                return false;
                            }

                            if (_cancel)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();

            _wc = new WebClient();
            _wc.UploadFileAsync(server, Path.Combine(Signature.FullPath, "TVShows.db3.gz"));

            _wc.UploadProgressChanged += (s, a) =>
                {
                    if (a.ProgressPercentage > 0 && a.ProgressPercentage < 50)
                    {
                        _tdpos = (int)Math.Round((double)a.BytesSent / (double)a.TotalBytesToSend * 100);
                        _tdstr = "Uploading database file for conversion... ({0:0.00}%)".FormatWith((double)a.BytesSent / (double)a.TotalBytesToSend * 100);
                    }
                    else if (a.ProgressPercentage == 50)
                    {
                        _tdpos = 100;
                        _tdstr = "Waiting for conversion to complete...";
                    }
                    else if (a.ProgressPercentage < 0)
                    {
                        _tdstr = "Downloading converted database... ({0})".FormatWith(Utils.GetFileSize(a.BytesReceived));
                    }
                };

            _wc.UploadFileCompleted += WebClientOnUploadFileCompleted;
        }

        /// <summary>
        /// Fired when the WebClient finishes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="uploadFileCompletedEventArgs">The <see cref="UploadFileCompletedEventArgs" /> instance containing the event data.</param>
        private void WebClientOnUploadFileCompleted(object sender, UploadFileCompletedEventArgs uploadFileCompletedEventArgs)
        {
            if (uploadFileCompletedEventArgs.Result.Length < 50)
            {
                _cancel = true;

                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon        = VistaTaskDialogIcon.Error,
                        Title           = "Upgrade error",
                        MainInstruction = "Upgrade error",
                        Content         = "Invalid response received from server: " + Encoding.UTF8.GetString(uploadFileCompletedEventArgs.Result) + "\r\n\r\nTry again using a different server or send your TVShows.db3 file to rolisoft@gmail.com for a \"manual\" conversion. :)",
                        CustomButtons   = new[] { "OK" }
                    });

                Process.GetCurrentProcess().Kill();
                return;
            }

            using (var ms = new MemoryStream(uploadFileCompletedEventArgs.Result))
            using (var br = new BinaryReader(ms))
            {
                ms.Position = 0;

                try
                {
                    while (ms.Position < ms.Length)
                    {
                        var name = Encoding.UTF8.GetString(br.ReadBytes((int)br.ReadUInt32()));
                        var file = br.ReadBytes((int)br.ReadUInt32());

                        _tdstr = "Extracting file " + name + "...";
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Signature.FullPath, "db", name)));
                        
                        using (var mz = new MemoryStream(file))
                        using (var fs = File.Create(Path.Combine(Signature.FullPath, "db", name)))
                        using (var gz = new DeflateStream(mz, CompressionMode.Decompress))
                        {
                            gz.CopyTo(fs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _cancel = true;

                    TaskDialog.Show(new TaskDialogOptions
                        {
                            MainIcon        = VistaTaskDialogIcon.Error,
                            Title           = "Upgrade error",
                            MainInstruction = "Upgrade error",
                            Content         = "Error while unpacking database: " + ex.Message + "\r\n\r\nTry again using a different server or send your TVShows.db3 file to rolisoft@gmail.com for a \"manual\" conversion. :)",
                            CustomButtons   = new[] { "OK" }
                        });

                    Process.GetCurrentProcess().Kill();
                    return;
                }

                _tdstr = "Finished extracting files!";
            }

            File.Move(Path.Combine(Signature.FullPath, "TVShows.db3"), Path.Combine(Signature.FullPath, "TVShows.db3.old"));
            File.Delete(Path.Combine(Signature.FullPath, "TVShows.db3.gz"));

            _cancel = true;

            TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon        = VistaTaskDialogIcon.Information,
                    Title           = "Upgrade finished",
                    MainInstruction = "Upgrade finished",
                    Content         = "The software will now quit. Please restart it afterwards.",
                    CustomButtons   = new[] { "OK" }
                });

            Process.GetCurrentProcess().Kill();
        }
    }
}
