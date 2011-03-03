namespace RoliSoft.TVShowTracker.ShowNames
{
    /// <summary>
    /// Represents an episode.
    /// </summary>
    public class ShowEpisode
    {
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
        /// Initializes a new instance of the <see cref="ShowEpisode"/> class.
        /// </summary>
        public ShowEpisode()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowEpisode"/> class.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <param name="episode">The episode.</param>
        public ShowEpisode(int season, int episode)
        {
            Season  = season;
            Episode = episode;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "S{0:00}E{1:00}".FormatWith(Season, Episode);
        }
    }
}
