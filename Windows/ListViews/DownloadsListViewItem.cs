namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Represents a download search engine on the list view.
    /// </summary>
    public class DownloadsListViewItem
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DownloadsListViewItem"/> is enabled.
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
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance requires cookies.
        /// </summary>
        /// <value>The string boolean.</value>
        public string RequiresCookies { get; set; }

        /// <summary>
        /// Gets or sets the developer.
        /// </summary>
        /// <value>The developer.</value>
        public string Developer { get; set; }

        /// <summary>
        /// Gets or sets the last update in relative time representation.
        /// </summary>
        /// <value>The last update date.</value>
        public string LastUpdate { get; set; }
    }
}
