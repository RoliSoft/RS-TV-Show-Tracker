namespace RoliSoft.TVShowTracker.ListViews
{
    using System.Collections.Generic;
    using System.Windows.Controls;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Represents a show to be added to the database.
    /// </summary>
    public class PendingShowListViewItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the show.
        /// </summary>
        /// <value>The show.</value>
        public TVShow Show { get; set; }

        /// <summary>
        /// Gets or sets the candidates.
        /// </summary>
        /// <value>The candidates.</value>
        public List<ShowID> Candidates { get; set; }

        /// <summary>
        /// Gets or sets the rendered candidates.
        /// </summary>
        /// <value>The rendered candidates.</value>
        public List<StackPanel> CandidateSP { get; set; }

        /// <summary>
        /// Gets or sets the rendered episodes.
        /// </summary>
        /// <value>The rendered episodes.</value>
        public List<StackPanel> EpisodeSP { get; set; } 

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the group.
        /// </summary>
        /// <value>The group.</value>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether show status.
        /// </summary>
        /// <value><c>true</c> if show status; otherwise, <c>false</c>.</value>
        public string ShowStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show candidates.
        /// </summary>
        /// <value><c>true</c> if show candidates; otherwise, <c>false</c>.</value>
        public string ShowCandidates { get; set; }

        /// <summary>
        /// Gets or sets the selected candidate index.
        /// </summary>
        /// <value>The selected candidate index.</value>
        public int SelectedCandidate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show episodes.
        /// </summary>
        /// <value><c>true</c> if show episodes; otherwise, <c>false</c>.</value>
        public string ShowEpisodes { get; set; }

        /// <summary>
        /// Gets or sets the selected episode index.
        /// </summary>
        /// <value>The selected episode index.</value>
        public int SelectedEpisode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingShowListViewItem"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public PendingShowListViewItem(string name)
        {
            Name        = name;
            Status      = string.Empty;
            Group       = "Pending";
            Candidates  = new List<ShowID>();
            CandidateSP = new List<StackPanel>();
            EpisodeSP   = new List<StackPanel>();

            ShowStatus     = "Visible";
            ShowCandidates = "Collapsed";
            ShowEpisodes   = "Collapsed";

            SelectedCandidate = -1;
            SelectedEpisode   = -1;
        }
    }
}
