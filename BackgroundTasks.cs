namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Timers;

    using Timer = System.Timers.Timer;

    /// <summary>
    /// Periodically runs tasks asynchronously in the background.
    /// </summary>
    public static class BackgroundTasks
    {
        /// <summary>
        /// Gets or sets the task timer.
        /// </summary>
        /// <value>The task timer.</value>
        public static Timer TaskTimer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the background tasks are currently being run.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the background tasks are currently being run; otherwise, <c>false</c>.
        /// </value>
        public static bool InProgress { get; set; }

        private static DateTime _lastSoftwareUpdate = Utils.UnixEpoch;
        private static int _inProgressCount = 0;
        private static Thread _lastThd;

        /// <summary>
        /// Initializes the <see cref="BackgroundTasks"/> class.
        /// </summary>
        static BackgroundTasks()
        {
            TaskTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            TaskTimer.Elapsed += Tasks;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public static void Start()
        {
            Log.Debug("Starting background tasks timer...");

            TaskTimer.Start();
        }

        /// <summary>
        /// The tasks which will run periodically.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        private static void Tasks(object sender, ElapsedEventArgs e)
        {
            if (InProgress)
            {
                if (_inProgressCount == 5)
                {
                    Log.Warn("It is time to run Tasks(), but the last one didn't finish yet. Thread will be killed and a new Tasks() will run. (6/6)");

                    try
                    {
                        _lastThd.Abort();
                        _lastThd = null;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Exception on stuck Tasks() thread abort.", ex);
                    }
                }
                else
                {
                    Log.Warn("It is time to run Tasks(), but the last one didn't finish yet so it'll be dropped. (" + (_inProgressCount + 1) + "/6)");
                    _inProgressCount++;
                    return;
                }
            }

            _lastThd = Thread.CurrentThread;

            var st = DateTime.Now;
            Log.Debug("Running background tasks...");

            InProgress = true;
            _inProgressCount = 0;

            try
            {
                Library.StartWatching();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while reindexing the library during background tasks.", ex);
            }

            try
            {
                CheckSoftwareUpdate();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while checking for software update during background tasks.", ex);
            }

            try
            {
                CheckDatabaseUpdate();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while checking for database update during background tasks.", ex);
            }

            try
            {
                CheckShowListUpdate();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while checking for show list update during background tasks.", ex);
            }

            try
            {
                ProcessMonitor.CheckOpenFiles();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while checking for open files during background tasks.", ex);
            }

            try
            {
                //AutoDownloader.SearchForMissingEpisodes();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while searching for missing episodes during background tasks.", ex);
            }

            try
            {
                RestartIfNeeded();
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled exception while checking if a software restart is required during background tasks.", ex);
            }

            InProgress = false;

            Log.Debug("Background tasks completed in " + (DateTime.Now - st).TotalSeconds + "s.");
        }

        /// <summary>
        /// Checks if a new version is available from the software.
        /// </summary>
        public static void CheckSoftwareUpdate()
        {
            if ((DateTime.Now - _lastSoftwareUpdate).TotalHours > 0.5)
            {
                _lastSoftwareUpdate = DateTime.Now;
                MainWindow.Active.CheckForUpdate();
            }
        }

        /// <summary>
        /// Checks if update is required and runs it if it is.
        /// </summary>
        public static void CheckDatabaseUpdate()
        {
            var ts = DateTime.Now - (Database.Setting("update") ?? "0").ToDouble().GetUnixTimestamp();
            if (ts.TotalHours > 10 && !Updater.InProgress)
            {
                Log.Debug("The database was last updated " + ts.TotalHours + " hours ago, starting new update.");
                MainWindow.Active.Run(() => MainWindow.Active.UpdateDatabaseClick());
            }
        }

        /// <summary>
        /// Checks if the list of known TV shows is older than a day.
        /// </summary>
        public static void CheckShowListUpdate()
        {
            var fn = Path.Combine(Signature.InstallPath, @"misc\tvshows");

            var ts1 = DateTime.Now - File.GetLastWriteTime(fn);
            if (ts1.TotalDays > 1)
            {
                Log.Debug("The known TV show names were last updated " + ts1.TotalHours + " hours ago, downloading new list.");
                FileNames.Parser.GetAllKnownTVShows();
            }

            var fn2 = Path.Combine(Signature.InstallPath, @"misc\linkchecker");
            var ts2 = DateTime.Now - File.GetLastWriteTime(fn2);
            if (ts2.TotalDays > 1)
            {
                Log.Debug("The link checker definitions were last updated " + ts2.TotalHours + " hours ago, downloading new list.");
                Parsers.LinkCheckers.Engines.UniversalEngine.GetLinkCheckerDefinitions();
            }
        }

        /// <summary>
        /// Restarts if needed.
        /// </summary>
        public static void RestartIfNeeded()
        {
            var memlimit = Settings.Get("Memory Usage Limit", 512);
            var memusage = Process.GetCurrentProcess().WorkingSet64;

            Log.Debug("The current memory usage is " + Utils.GetFileSize(memusage) + "; " + (memlimit < 256 ? "auto-restart is disabled." : "auto-restarting when it exceeds " + memlimit + " MB."));

            if (memlimit < 256)
            {
                return;
            }

            if ((memlimit * 1048576L) < memusage)
            {
                MainWindow.Active.Restart();
            }
        }
    }
}
