namespace RoliSoft.TVShowTracker
{
    using Parsers.Guides;

    /// <summary>
    /// Represents a TV show on the overview list view.
    /// </summary>
    public class OverviewListViewItem
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
        /// Gets or sets the title of the latest episode.
        /// </summary>
        /// <value>The title of the latest episode.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the next episode information.
        /// </summary>
        /// <value>The next episode information.</value>
        public string Next { get; set; }

        /// <summary>
        /// Gets or sets the color of the name.
        /// </summary>
        /// <value>The color of the name.</value>
        public string NameColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the title.
        /// </summary>
        /// <value>The color of the title.</value>
        public string TitleColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the next.
        /// </summary>
        /// <value>The color of the next.</value>
        public string NextColor { get; set; }

        /// <summary>
        /// Gets or sets the count of unseen episodes.
        /// </summary>
        /// <value>The count of new episodes.</value>
        public int NewEpisodes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this TV show has started airing.
        /// </summary>
        /// <value><c>true</c> if started; otherwise, <c>false</c>.</value>
        public bool Started { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        /// <value>The name of the group.</value>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the priority of the group.
        /// </summary>
        /// <value>The priority of the group.</value>
        public int GroupPriority { get; set; }

        /// <summary>
        /// Gets or sets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        public object Sort { get; set; }
    }
}
