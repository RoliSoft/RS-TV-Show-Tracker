namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Represents a feed on the list view.
    /// </summary>
    public class FeedsListViewItem
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FeedsListViewItem"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the site.
        /// </summary>
        /// <value>The site.</value>
        public string Site { get; set; }

        /// <summary>
        /// Gets or sets the language of the site.
        /// </summary>
        /// <value>The language of the site.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the icon of the language.
        /// </summary>
        /// <value>The icon of the language.</value>
        public string LangIcon { get; set; }
    }
}
