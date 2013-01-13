namespace RoliSoft.TVShowTracker
{
    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Represents an downloaded episode on the guide list view.
    /// </summary>
    public class DownloadedListViewItem
    {
        /// <summary>
        /// Gets or sets the episode.
        /// </summary>
        /// <value>The episode.</value>
        public Episode Episode { get; set; }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        public string File { get; set; }

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
        /// Gets or sets the summary.
        /// </summary>
        /// <value>The summary.</value>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the URL of the picture.
        /// </summary>
        /// <value>The URL of the picture.</value>
        public string Picture { get; set; }

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

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public string Color { get; set; }

        /// <summary>
        /// Gets a value indicating whether to show the tooltip.
        /// </summary>
        /// <value><c>true</c> if yes; otherwise, <c>false</c>.</value>
        public bool ShowTooltip
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Summary) || !string.IsNullOrWhiteSpace(Picture);
            }
        }

        /// <summary>
        /// Gets a string value indicating whether to show summary in tooltip.
        /// </summary>
        /// <value>The string boolean.</value>
        public string ShowSummary
        {
            get
            {
                return string.IsNullOrWhiteSpace(Summary) ? "Collapsed" : "Visible";
            }
        }

        /// <summary>
        /// Gets a string value indicating whether to show picture in tooltip.
        /// </summary>
        /// <value>The string boolean.</value>
        public string ShowPicture
        {
            get
            {
                return string.IsNullOrWhiteSpace(Picture) ? "Collapsed" : "Visible";
            }
        }
    }

    /// <summary>
    /// Represents a downloaded list in the guide drop-down menu.
    /// </summary>
    public class GuideDropDownDownloadedItem
    {
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "— Downloaded episodes —";
        }
    }
}
