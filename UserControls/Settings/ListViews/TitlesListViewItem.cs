namespace RoliSoft.TVShowTracker
{
    using Tables;

    /// <summary>
    /// Represents an mapped title on the list view.
    /// </summary>
    public class TitlesListViewItem
    {
        /// <summary>
        /// Gets or sets the TV show in the database.
        /// </summary>
        /// <value>
        /// The TV show in the database.
        /// </value>
        public TVShow Show { get; set; }

        /// <summary>
        /// Gets or sets the original/English title.
        /// </summary>
        /// <value>
        /// The original/English title.
        /// </value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the mapped/foreign title.
        /// </summary>
        /// <value>
        /// The mapped/foreign title.
        /// </value>
        public string Foreign { get; set; }

        /// <summary>
        /// Gets or sets the mapped/foreign title.
        /// </summary>
        /// <remarks>
        /// This is a quick work-around the fact that TextBox modified this whole object.
        /// </remarks>
        /// <value>
        /// The mapped/foreign title.
        /// </value>
        public string Foreign2 { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>
        /// The language.
        /// </value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the ISO 639-1 code of the language.
        /// </summary>
        /// <value>
        /// The ISO 639-1 code of the language.
        /// </value>
        public string LangCode { get; set; }
    }
}
