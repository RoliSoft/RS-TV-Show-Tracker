namespace RoliSoft.TVShowTracker.Parsers.Recommendations
{
    /// <summary>
    /// Represents a recommended TV show.
    /// </summary>
    public class RecommendedShow
    {
        /*
         * ListView
         */

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
        /// Gets or sets the official page.
        /// </summary>
        /// <value>The official page.</value>
        public string Official { get; set; }

        /// <summary>
        /// Gets or sets the TVRage listing.
        /// </summary>
        /// <value>The TVRage listing.</value>
        public string TVRage { get; set; }

        /// <summary>
        /// Gets or sets the The TVDB listing.
        /// </summary>
        /// <value>The The TVDB listing.</value>
        public string TVDB { get; set; }

        /// <summary>
        /// Gets or sets the TV.com listing.
        /// </summary>
        /// <value>The TV.com listing.</value>
        public string TVcom { get; set; }

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

        /// <summary>
        /// Gets or sets the TV Tropes page.
        /// </summary>
        /// <value>The TV Tropes page.</value>
        public string TVTropes { get; set; }

        /*
         * ToolTip
         */

        /// <summary>
        /// Gets or sets the description. This is only set when the tooltip opens.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the info. This is only set when the tooltip opens.
        /// </summary>
        /// <value>The info.</value>
        public string Info { get; set; }

        /// <summary>
        /// Gets or sets the picture. This is only set when the tooltip opens.
        /// </summary>
        /// <value>The picture.</value>
        public string Picture { get; set; }

        /// <summary>
        /// Gets or sets the information source. This is only set when the tooltip opens.
        /// </summary>
        /// <value>The information source.</value>
        public string InfoSource { get; set; }
    }
}
