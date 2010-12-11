namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Threading;

    /// <summary>
    /// Periodically runs tasks asynchronously in the background.
    /// </summary>
    public static class BackgroundTasks
    {
        /// <summary>
        /// Gets or sets the task thread.
        /// </summary>
        /// <value>The task thread.</value>
        public static Thread TaskThread { get; set; }

        /// <summary>
        /// Initializes the <see cref="BackgroundTasks"/> class.
        /// </summary>
        static BackgroundTasks()
        {
            TaskThread = new Thread(Tasks);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public static void Start()
        {
            if (TaskThread.IsAlive)
            {
                TaskThread.Abort();
            }

            TaskThread.Start();
        }

        /// <summary>
        /// The tasks which will run periodically.
        /// </summary>
        private static void Tasks()
        {
            while (true)
            {
                try { CheckUpdate(); } catch { }
                try { ProcessMonitor.CheckOpenFiles(); } catch { }

                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
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
                MainWindow.Active.Dispatcher.Invoke((Func<bool>)delegate
                    {
                        MainWindow.Active.activeSettingsPage.UpdateDatabaseButtonClick(null, null);
                        return true;
                    });
            }
        }
    }
}
