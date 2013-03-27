namespace RoliSoft.TVShowTracker
{
    using System;
    using System.IO;
    using System.Timers;

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
                return;
            }

            InProgress = true;

            try { Library.StartWatching(); }         catch { }
            try { CheckSoftwareUpdate(); }           catch { }
            try { CheckDatabaseUpdate(); }           catch { }
            try { CheckShowListUpdate(); }           catch { }
            try { ProcessMonitor.CheckOpenFiles(); } catch { }

            InProgress = false;
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
            if ((DateTime.Now - (Database.Setting("update") ?? "0").ToDouble().GetUnixTimestamp()).TotalHours > 10 && !Updater.InProgress)
            {
                MainWindow.Active.Run(() => MainWindow.Active.UpdateDatabaseClick());
            }
        }

        /// <summary>
        /// Checks if the list of known TV shows is older than a day.
        /// </summary>
        public static void CheckShowListUpdate()
        {
            var fn = Path.Combine(Signature.InstallPath, @"misc\tvshows");

            if ((DateTime.Now - File.GetLastWriteTime(fn)).TotalDays > 1)
            {
                FileNames.Parser.GetAllKnownTVShows();
            }

            var fn2 = Path.Combine(Signature.InstallPath, @"misc\linkchecker");

            if ((DateTime.Now - File.GetLastWriteTime(fn2)).TotalDays > 1)
            {
                Parsers.LinkCheckers.Engines.UniversalEngine.GetLinkCheckerDefinitions();
            }
        }
    }
}
