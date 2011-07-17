namespace RoliSoft.TVShowTracker
{
    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Represents an upcoming episode on the guide list view.
    /// </summary>
    public class UpcomingListViewItem
    {
        /// <summary>
        /// Gets or sets the episode.
        /// </summary>
        /// <value>The episode.</value>
        public Episode Episode { get; set; }

        /// <summary>
        /// Gets or sets the name of the show.
        /// </summary>
        /// <value>The name of the show.</value>
        public string Show { get; set; }

        /// <summary>
        /// Gets or sets the name of the episode.
        /// </summary>
        /// <value>The name of the episode.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the airdate.
        /// </summary>
        /// <value>The airdate.</value>
        public string Airdate { get; set; }

        /// <summary>
        /// Gets or sets the relative airdate.
        /// </summary>
        /// <value>The relative airdate.</value>
        public string RelativeDate { get; set; }
    }
}
