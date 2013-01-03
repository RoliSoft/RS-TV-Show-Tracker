namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.IO;
    using System.Linq;

    using ShowNames;
    using Parsers.Guides;

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
        /// Gets or sets the parse error if the file wasn't identifiable.
        /// </summary>
        /// <value>The parse error.</value>
        public FailureReasons? ParseError { get; set; }

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
        /// Gets or sets the name of the scene group which released this file.
        /// </summary>
        /// <value>The group.</value>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the name of the show.
        /// </summary>
        /// <value>The show.</value>
        public string Show { get; set; }

        /// <summary>
        /// Gets or sets the episode of the show.
        /// </summary>
        /// <value>The episode.</value>
        public ShowEpisode Episode { get; set; }

        /// <summary>
        /// Gets or sets the title of the episode.
        /// </summary>
        /// <value>The episode title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the airdate of the episode.
        /// </summary>
        /// <value>The episode airdate.</value>
        public DateTime Airdate { get; set; }

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
        /// <param name="error">The reason why the parsing has failed.</param>
        public ShowFile(string location, FailureReasons? error = null)
        {
            Name       = Path.GetFileName(location);
            Extension  = Path.GetExtension(Name);
            ParseError = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        /// <param name="name">The name of the original file.</param>
        /// <param name="show">The name of the show.</param>
        /// <param name="ep">The parsed season and episode.</param>
        /// <param name="title">The title of the episode.</param>
        /// <param name="quality">The quality of the file.</param>
        /// <param name="airdate">The airdate of the episode.</param>
        /// <param name="success">if set to <c>true</c> the file was successfully parsed.</param>
        public ShowFile(string name, string show, ShowEpisode ep, string title, string quality, string group, DateTime airdate, bool success = true)
        {
            Name          = name;
            Extension     = Path.GetExtension(Name);
            Show          = show;
            Episode       = ep;
            Title         = title;
            Quality       = quality;
            Group         = group;
            Airdate       = airdate;
            Success       = success;
        }

        /// <summary>
        /// Searches for this episode in the local database and returns a reference to it.
        /// </summary>
        /// <returns>
        /// The reference to the equivalent object in the database.
        /// </returns>
        public Episode GetDatabaseEquivalent()
        {
            if (!Success)
            {
                return null;
            }

            try
            {
                return Database.TVShows.First(tv => tv.Value.Name == Show).Value
                               .Episodes.First(ep => ep.Season == Episode.Season && ep.Number == Episode.Episode);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ParseError.HasValue && ParseError.Value != FailureReasons.ShowNotIdentified
                   ? ParseError.Value.ToString()
                   : Show + " " + Episode;
        }

        /// <summary>
        /// A short list of bad excuses why the parser failed to identify the file.
        /// </summary>
        public enum FailureReasons
        {
            /// <summary>
            /// The file name didn't contain a recognizable episode numbering.
            /// </summary>
            EpisodeNumberingNotFound,
            /// <summary>
            /// The name of the show could not be extracted from the file name.
            /// It should be placed before the episode numbering. In this case it wasn't.
            /// </summary>
            ShowNameNotFound,
            /// <summary>
            /// The extracted name was not identifiable by the following process:
            /// - matching against the local database
            /// - matching against the monster database located at lab.rolisoft.net
            /// - asking TVRage's API
            /// - asking TheTVDB's API
            /// </summary>
            ShowNotIdentified,
            /// <summary>
            /// An exception was thrown when the file was processed.
            /// </summary>
            ExceptionOccurred
        }
    }
}
