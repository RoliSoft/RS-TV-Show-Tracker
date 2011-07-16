namespace RoliSoft.TVShowTracker.Tables
{
    /// <summary>
    /// Represents a TV show key-value store in the SQLite database.
    /// </summary>
    public struct ShowData
    {
        /// <summary>
        /// Gets or sets the row ID.
        /// </summary>
        /// <value>
        /// The row ID.
        /// </value>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the show ID.
        /// </summary>
        /// <value>
        /// The show ID.
        /// </value>
        public int ShowID { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }
    }
}
