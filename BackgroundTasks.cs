﻿namespace RoliSoft.TVShowTracker
{
    using System;
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
            try { CheckSoftwareUpdate(); }           catch { }
            try { CheckUpdate(); }                   catch { }
            try { ProcessMonitor.CheckOpenFiles(); } catch { }
            try { Synchronization.GetChanges(); }    catch { }
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
        public static void CheckUpdate()
        {
            double last;
            double.TryParse(Database.Setting("last update"), out last);

            if ((DateTime.Now - last.GetUnixTimestamp()).TotalHours > 10)
            {
                MainWindow.Active.Dispatcher.Invoke((Action)(() => MainWindow.Active.UpdateDatabaseClick()));
            }
        }
    }
}
