namespace RoliSoft.TVShowTracker.FileNames
{
    using System.IO;

    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Represents a TV show video file.
    /// </summary>
    public class ShowFile
    {
        /// <summary>
        /// Gets or sets a value indicating whether the file name was successfully parsed.
        /// </summary>
        /// <value><c>true</c> if parsed successfully; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the original name of the file.
        /// </summary>
        /// <value>The file name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the original extension of the file.
        /// </summary>
        /// <value>The extension.</value>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the quality of the file.
        /// </summary>
        /// <value>The quality.</value>
        public string Quality { get; set; }

        /// <summary>
        /// Gets or sets the name of the show.
        /// </summary>
        /// <value>The show.</value>
        public string Show { get; set; }

        /// <summary>
        /// Gets or sets the season of the episode.
        /// </summary>
        /// <value>The season.</value>
        public int Season { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        /// <value>The episode.</value>
        public int Episode { get; set; }

        /// <summary>
        /// Gets or sets the second episode number.
        /// </summary>
        /// <value>The second episode.</value>
        public int? SecondEpisode { get; set; }

        /// <summary>
        /// Gets or sets the title of the episode.
        /// </summary>
        /// <value>The episode title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        public ShowFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        /// <param name="location">The location of the file.</param>
        public ShowFile(string location)
        {
            Name      = Path.GetFileName(location);
            Extension = Path.GetExtension(Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        /// <param name="name">The name of the original file.</param>
        /// <param name="show">The name of the show.</param>
        /// <param name="ep">The parsed season and episode.</param>
        /// <param name="title">The title of the episode.</param>
        /// <param name="quality">The quality of the file.</param>
        /// <param name="success">if set to <c>true</c> the file was successfully parsed.</param>
        public ShowFile(string name, string show, ShowEpisode ep, string title, string quality, bool success = true)
        {
            Name          = name;
            Extension     = Path.GetExtension(Name);
            Show          = show;
            Season        = ep.Season;
            Episode       = ep.Episode;
            SecondEpisode = ep.SecondEpisode;
            Title         = title;
            Quality       = quality;
            Success       = success;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return SecondEpisode.HasValue
                   ? "{0} S{1:00}E{2:00}-{3:00}".FormatWith(Show, Season, Episode, SecondEpisode)
                   : "{0} S{1:00}E{2:00}".FormatWith(Show, Season, Episode);
        }
    }
}
