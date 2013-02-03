namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Represents a destination on the list view.
    /// </summary>
    public class DestinationListViewItem
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        public object Icon { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the group icon.
        /// </summary>
        /// <value>The group icon.</value>
        public string GroupIcon { get; set; }
    }
}
