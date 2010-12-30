namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Represents a TV show on the overview list view.
    /// </summary>
    public class OverviewListViewItem
    {
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
        /// Gets or sets a value indicating whether to show the "mark as seen visible" menu item.
        /// </summary>
        /// <value>The string boolean.</value>
        public string MarkAsSeenVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the "play next unseen" menu item.
        /// </summary>
        /// <value>The string boolean.</value>
        public string PlayNextVisible { get; set; }
    }
}
