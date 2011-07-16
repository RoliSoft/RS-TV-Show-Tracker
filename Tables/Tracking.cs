namespace RoliSoft.TVShowTracker.Tables
{
    /// <summary>
    /// Represents a tracking information in the SQLite database.
    /// </summary>
    public struct Tracking
    {
        /// <summary>
        /// Gets or sets the show ID.
        /// </summary>
        /// <value>
        /// The show ID.
        /// </value>
        public int ShowID { get; set; }

        /// <summary>
        /// Gets or sets the episode ID.
        /// </summary>
        /// <value>
        /// The episode ID.
        /// </value>
        public int EpisodeID { get; set; }
    }
}
