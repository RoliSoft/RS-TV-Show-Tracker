namespace RoliSoft.TVShowTracker
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
            try { CheckUpdate(); } catch { }
            try { ProcessMonitor.CheckOpenFiles(); } catch { }
        }

        /// <summary>
        /// Checks if update is required and runs it if it is.
        /// </summary>
        private static void CheckUpdate()
        {
            var last = 0d;
            double.TryParse(Database.Setting("last update"), out last);

            if ((DateTime.Now - Utils.DateTimeFromUnix(last)).TotalHours > 10)
            {
                MainWindow.Active.Dispatcher.Invoke((Action)(() => MainWindow.Active.activeSettingsPage.UpdateDatabaseButtonClick(null, null)));
            }
        }
    }
}
