namespace RoliSoft.TVShowTracker.Synchronization
{
    /// <summary>
    /// Provides status information for the synchronization.
    /// </summary>
    public static class Status
    {
        /// <summary>
        /// Gets or sets a value indicating whether synchronization is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the active synchronization engine.
        /// </summary>
        /// <value>The sync engine.</value>
        public static SyncEngine Engine { get; set; }

        /// <summary>
        /// Initializes the <see cref="Status"/> class.
        /// </summary>
        static Status()
        {
            if (Settings.Get<bool>("Synchronization Enabled"))
            {
                var auth = Settings.GetList("Synchronization Authentication");

                if (auth != null && auth.Length == 2)
                {
                    LoadEngine("RoliSoftDotNetAPI", auth);
                }
            }
        }

        /// <summary>
        /// Loads the specified engine.
        /// </summary>
        /// <param name="engine">The name of the engine.</param>
        /// <param name="auth">The authentication token required for the engine.</param>
        public static void LoadEngine(string engine, string[] auth)
        {
            //Enabled = true;
            //Engine  = new Engines.RoliSoftDotNetAPI(auth[0], auth[1]);
        }
    }
}
