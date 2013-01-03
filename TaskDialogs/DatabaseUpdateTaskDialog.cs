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

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to upgrading the database from SQLite3.
    /// </summary>
    public class DatabaseUpdateTaskDialog
    {
        private TaskDialog _td;
        private Result _res;
        private WebClient _wc;

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

            _td = new TaskDialog
                {
                    CommonIcon      = TaskDialogIcon.SecurityWarning,
                    Title           = "Upgrade database",
                    Instruction     = "Upgrade TVShows.db3",
                    Content         = "From this version forward, SQLite3 is not used anymore as the database store for the software.\r\n\r\nYour database file needs to be upgraded to the new format, however, SQLite3 is not bundled anymore with the software, so it can't open it and convert it.\r\n\r\nThe TVShows.db3 file will be uploaded to one of my servers and converted there. This file only contains TV show information, it does NOT contain your settings, cookies, and site logins.\r\n\r\nIf you would like to continue, select a server close to you.",
                    DefaultButton   = 0,
                    UseCommandLinks = true,
                    CommonButtons   = TaskDialogButton.Cancel,
                    CustomButtons   = new[] { new CustomButton(0, "lab.rolisoft.net\nAnaheim, California, United States") },
                };

            /*if (new FileInfo(Path.Combine(Signature.FullPath, "TVShows.db3.gz")).Length < 1.9 * 1024 * 1024)
            {
                _td.CustomButtons = new[]
                    {
                        new CustomButton(0, "lab.rolisoft.net\nAnaheim, California, United States"),
                        new CustomButton(1, "aws-us.rolisoft.net\nArlington, Virginia, United States"),
                        new CustomButton(2, "aws-eu.rolisoft.net\nDublin, Ireland"),
                        new CustomButton(3, "aws-as.rolisoft.net\nSomewhere in Singapore"),
                    };
            }*/

            _td.ButtonClick += (s, e) => new Thread(() => TaskDialogButtonClick(s, e)).Start();

            new Thread(() => _td.Show()).Start();
        }

        /// <summary>
        /// Starts the upgrading process.
        /// </summary>
        /// <param name="server">The endpoint for the migration script.</param>
        public void Start(Uri server)
        {
            _td = new TaskDialog
                {
                    Title               = "Upgrading...",
                    Instruction         = "Upgrading TVShows.db3",
                    Content             = "Connecting to " + server.DnsSafeHost + "...",
                    CommonButtons       = TaskDialogButton.Cancel,
                    ShowProgressBar     = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;

            new Thread(() => _res = _td.Show().CommonButton).Start();

            var prm = true;

            _wc = new WebClient();
            _wc.UploadFileAsync(server, Path.Combine(Signature.FullPath, "TVShows.db3.gz"));

            _wc.UploadProgressChanged += (s, a) =>
                {
                    if (_td != null && _td.IsShowing)
                    {
                        if (prm)
                        {
                            _td.SetMarqueeProgressBar(false);
                            _td.Navigate(_td);
                            prm = false;
                        }

                        if (a.ProgressPercentage > 0 && a.ProgressPercentage < 50)
                        {
                            _td.ProgressBarPosition = (int)Math.Round((double)a.BytesSent / (double)a.TotalBytesToSend * 100);
                            _td.Content = "Uploading database file for conversion... ({0:0.00}%)".FormatWith((double)a.BytesSent / (double)a.TotalBytesToSend * 100);
                        }
                        else if (a.ProgressPercentage == 50)
                        {
                            _td.ProgressBarPosition = 100;
                            _td.Content = "Waiting for conversion to complete...";
                        }
                        else if (a.ProgressPercentage < 0)
                        {
                            _td.Content = "Downloading converted database... ({0})".FormatWith(Utils.GetFileSize(a.BytesReceived));
                        }
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
                _td.SimulateButtonClick(-1);
                new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.SecurityError,
                        Title       = "Upgrade error",
                        Instruction = "Upgrade error",
                        Content     = "Invalid response received from server: " + Encoding.UTF8.GetString(uploadFileCompletedEventArgs.Result) + "\r\n\r\nTry again using a different server or send your TVShows.db3 file to rolisoft@gmail.com for a \"manual\" conversion. :)"
                    }.Show();
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

                        _td.Content = "Extracting file " + name + "...";
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
                    _td.SimulateButtonClick(-1);
                    new TaskDialog
                        {
                            CommonIcon  = TaskDialogIcon.SecurityError,
                            Title       = "Upgrade error",
                            Instruction = "Upgrade error",
                            Content     = "Error while unpacking database: " + ex.Message + "\r\n\r\nTry again using a different server or send your TVShows.db3 file to rolisoft@gmail.com for a \"manual\" conversion. :)"
                        }.Show();
                    Process.GetCurrentProcess().Kill();
                    return;
                }

                _td.Content = "Finished extracting files!";
            }

            File.Move(Path.Combine(Signature.FullPath, "TVShows.db3"), Path.Combine(Signature.FullPath, "TVShows.db3.old"));
            File.Delete(Path.Combine(Signature.FullPath, "TVShows.db3.gz"));

            _td.SimulateButtonClick(-1);
            new TaskDialog
                {
                    CommonIcon  = TaskDialogIcon.SecuritySuccess,
                    Title       = "Upgrade finished",
                    Instruction = "Upgrade finished",
                    Content     = "The software will now quit. Please restart it afterwards."
                }.Show();
            Process.GetCurrentProcess().Kill();
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
                    Start(new Uri("http://ipv4.lab.rolisoft.net/api/migrate/"));
                    break;

                default:
                    Process.GetCurrentProcess().Kill();
                    break;

                    /*case 1:
                    Start(new Uri("http://aws-us.rolisoft.net/migrate.php"));
                    break;

                case 2:
                    Start(new Uri("http://aws-eu.rolisoft.net/migrate.php"));
                    break;

                case 3:
                    Start(new Uri("http://aws-as.rolisoft.net/migrate.php"));
                    break;*/
            }
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

                _wc.CancelAsync();
            }
        }
    }
}
