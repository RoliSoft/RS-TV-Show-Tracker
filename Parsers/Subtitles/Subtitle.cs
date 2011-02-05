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
        public Languages Language { get; set; }

        /// <summary>
        /// Gets or sets the URL to the subtitle.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

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
