namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles
{
    /// <summary>
    /// Represents a foreign title search engine.
    /// </summary>
    public abstract class ForeignTitleEngine : ParserEngine
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public abstract string Language { get; }

        /// <summary>
        /// Searches the foreign title of the specified show.
        /// </summary>
        /// <param name="name">The name of the show to search for.</param>
        /// <returns>The foreign title or <c>null</c>.</returns>
        public abstract string Search(string name);
    }
}
