namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    /// <summary>
    /// Represents a download link.
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Gets the source of the download link.
        /// </summary>
        /// <value>The site.</value>
        public DownloadSearchEngine Source { get; internal set; }

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        /// <value>The release name.</value>
        public string Release { get; set; }

        /// <summary>
        /// Gets or sets the quality of the video.
        /// </summary>
        /// <value>The quality.</value>
        public Qualities Quality { get; set; }

        /// <summary>
        /// Gets or sets the size of the file.
        /// </summary>
        /// <value>The size.</value>
        public string Size { get; set; }

        /// <summary>
        /// Gets or sets the URL to the subtitle.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the URL is a direct link to the download.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the URL is a direct link; otherwise, <c>false</c>.
        /// </value>
        public bool IsLinkDirect { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Link"/> class.
        /// </summary>
        /// <param name="source">The source of this download link.</param>
        public Link(DownloadSearchEngine source)
        {
            Source       = source;
            IsLinkDirect = true;
        }
    }
}
