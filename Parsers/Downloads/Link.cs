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
        /// Gets or sets the URL to the details page.
        /// </summary>
        /// <value>The URL.</value>
        public string InfoURL { get; set; }

        /// <summary>
        /// Gets or sets the URL to the downloadable file.
        /// </summary>
        /// <value>The URL.</value>
        public string FileURL { get; set; }

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
        /// Gets or sets additional information about the file.
        /// </summary>
        /// <value>The info.</value>
        public string Infos { get; set; }

        /// <summary>
        /// Gets the string format for displaying a seed/leech information.
        /// </summary>
        /// <value>The S/L string format.</value>
        public static string SeedLeechFormat
        {
            get
            {
                return "{0} seed / {1} leech";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Link"/> class.
        /// </summary>
        /// <param name="source">The source of this download link.</param>
        public Link(DownloadSearchEngine source)
        {
            Source = source;
        }
    }
}
