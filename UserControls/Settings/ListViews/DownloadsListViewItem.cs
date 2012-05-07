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
        /// Gets or sets the type of the parser.
        /// </summary>
        /// <value>
        /// The type of the parser.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the site.
        /// </summary>
        /// <value>The site.</value>
        public string Site { get; set; }

        /// <summary>
        /// Gets or sets the login status.
        /// </summary>
        /// <value>
        /// The login status.
        /// </value>
        public string Login { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }
    }
}
