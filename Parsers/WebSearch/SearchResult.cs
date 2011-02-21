namespace RoliSoft.TVShowTracker.Parsers.WebSearch
{
    /// <summary>
    /// Represents a search result in a web search.
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Gets or sets the title of the webpage.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the location of the webpage.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }
    }
}