namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    /// <summary>
    /// Represents a TV show database.
    /// </summary>
    public abstract class Guide
    {
        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public abstract TVShow GetData(string id);

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public abstract string GetID(string name);
    }
}
