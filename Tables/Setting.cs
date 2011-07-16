namespace RoliSoft.TVShowTracker.Tables
{
    /// <summary>
    /// Represents a key-value store in the SQLite database.
    /// </summary>
    public struct Setting
    {
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
