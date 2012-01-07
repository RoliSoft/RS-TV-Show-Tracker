namespace RoliSoft.TVShowTracker
{
    using Tables;

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
        /// Gets or sets the programme.
        /// </summary>
        /// <value>The programme.</value>
        public LocalProgrammingPlugin.Programme Programme { get; set; }

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

    /// <summary>
    /// Represents a local programming in the guide drop-down menu.
    /// </summary>
    public class GuideDropDownUpcomingItem
    {
        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public LocalProgrammingPlugin.Configuration Config { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuideDropDownUpcomingItem"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public GuideDropDownUpcomingItem(LocalProgrammingPlugin.Configuration config = null)
        {
            Config = config;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "— " + (Config ?? (object)"Upcoming episodes") + " —";
        }
    }
}
