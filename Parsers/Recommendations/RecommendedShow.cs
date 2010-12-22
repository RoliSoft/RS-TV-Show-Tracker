namespace RoliSoft.TVShowTracker.Parsers.Recommendations
{
    /// <summary>
    /// Represents a recommended TV show.
    /// </summary>
    public class RecommendedShow
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tagline.
        /// </summary>
        /// <value>The tagline.</value>
        public string Tagline { get; set; }

        /// <summary>
        /// Gets or sets the match score.
        /// </summary>
        /// <value>The match score.</value>
        public string Score { get; set; }

        /// <summary>
        /// Gets or sets the runtime.
        /// </summary>
        /// <value>The runtime.</value>
        public string Runtime { get; set; }

        /// <summary>
        /// Gets or sets the episode count.
        /// </summary>
        /// <value>The episode count.</value>
        public string Episodes { get; set; }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        /// <value>The genre.</value>
        public string Genre { get; set; }

        /// <summary>
        /// Gets or sets the Wikipedia article.
        /// </summary>
        /// <value>The Wikipedia article.</value>
        public string Wikipedia { get; set; }

        /// <summary>
        /// Gets or sets the EPGuides listing.
        /// </summary>
        /// <value>The EPGuides listing.</value>
        public string Epguides { get; set; }

        /// <summary>
        /// Gets or sets the IMDb page.
        /// </summary>
        /// <value>The IMDb page.</value>
        public string Imdb { get; set; }
    }
}
