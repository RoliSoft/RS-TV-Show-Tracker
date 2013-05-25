namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;

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
        /// Gets or sets the second episode number.
        /// For example 2 in S01E01-02.
        /// </summary>
        /// <value>The second episode.</value>
        public int? SecondEpisode { get; set; }

        /// <summary>
        /// Gets or sets the air date.
        /// </summary>
        /// <value>The air date.</value>
        public DateTime? AirDate { get; set; }

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
        /// Initializes a new instance of the <see cref="ShowEpisode"/> class.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <param name="episode">The episode.</param>
        /// <param name="episode2">The second episode.</param>
        public ShowEpisode(int season, int episode, int episode2)
        {
            Season        = season;
            Episode       = episode;
            SecondEpisode = episode2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowEpisode"/> class.
        /// </summary>
        public ShowEpisode(DateTime airdate)
        {
            AirDate = airdate;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (AirDate.HasValue && Season == 0 && Episode == 0)
            {
                return AirDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                return SecondEpisode.HasValue
                       ? "S{0:00}E{1:00}-{2:00}".FormatWith(Season, Episode, SecondEpisode)
                       : "S{0:00}E{1:00}".FormatWith(Season, Episode);
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var ep = obj as ShowEpisode;
            return ep != null
                && Season == ep.Season
                && Episode == ep.Episode
                && SecondEpisode == ep.SecondEpisode
                && AirDate == ep.AirDate;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
