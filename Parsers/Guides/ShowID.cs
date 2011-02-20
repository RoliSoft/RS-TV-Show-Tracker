namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    /// <summary>
    /// Represents a TV show ID, which contains information used to identify it in the guide's database.
    /// </summary>
    public class ShowID
    {
        /// <summary>
        /// Gets or sets the ID of the show.
        /// </summary>
        /// <value>The ID.</value>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the title of the show.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        
        /// <summary>
        /// Gets or sets the URL location of a DVD cover.
        /// </summary>
        /// <value>The URL of the cover.</value>
        public string Cover { get; set; }

        /// <summary>
        /// Gets or sets the language of the show's episode listing.
        /// </summary>
        /// <value>The episode listing's language.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the URL of the show in the guide.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }
    }
}
