namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    /// <summary>
    /// Represents a subtitle.
    /// </summary>
    public class Subtitle
    {
        /// <summary>
        /// Gets the source of the subtitle.
        /// </summary>
        /// <value>The site.</value>
        public SubtitleSearchEngine Source { get; internal set; }

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        /// <value>The release name.</value>
        public string Release { get; set; }

        /// <summary>
        /// Gets or sets the language of the subtitle.
        /// </summary>
        /// <value>The language.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this subtitle is corrected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if corrected; otherwise, <c>false</c>.
        /// </value>
        public bool Corrected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this subtitle contains hearing impaired notations.
        /// </summary>
        /// <value>
        ///   <c>true</c> if contains hearing impaired notations; otherwise, <c>false</c>.
        /// </value>
        public bool HINotations { get; set; }

        /// <summary>
        /// Gets or sets the URL to the details page.
        /// </summary>
        /// <value>The URL.</value>
        public string InfoURL { get; set; }

        /// <summary>
        /// Gets or sets the URL to the downloadable subtitle.
        /// </summary>
        /// <value>The URL.</value>
        public string FileURL { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subtitle"/> class.
        /// </summary>
        /// <param name="source">The source of the subtitle.</param>
        public Subtitle(SubtitleSearchEngine source)
        {
            Source = source;
        }
    }
}
