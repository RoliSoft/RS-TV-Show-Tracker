namespace RoliSoft.TVShowTracker
{
    using Tables;

    /// <summary>
    /// Represents a TV show on the statistics list view.
    /// </summary>
    public class StatisticsListViewItem
    {
        /// <summary>
        /// Gets or sets the TV show in the database.
        /// </summary>
        /// <value>The TV show in the database.</value>
        public TVShow Show { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the runtime.
        /// </summary>
        /// <value>The runtime.</value>
        public string Runtime { get; set; }

        /// <summary>
        /// Gets or sets the number of episodes.
        /// </summary>
        /// <value>The number of episodes.</value>
        public string Episodes { get; set; }

        /// <summary>
        /// Gets or sets the time wasted.
        /// </summary>
        /// <value>The time wasted.</value>
        public string TimeWasted { get; set; }
    }
}
