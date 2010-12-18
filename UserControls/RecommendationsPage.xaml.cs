namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    using RoliSoft.TVShowTracker.Parsers.Recommendations;

    /// <summary>
    /// Interaction logic for RecommendationsPage.xaml
    /// </summary>
    public partial class RecommendationsPage : UserControl
    {
        /// <summary>
        /// Gets or sets the recommendations list view item collection.
        /// </summary>
        /// <value>The recommendations list view item collection.</value>
        public ObservableCollection<RecommendationEngine.RecommendedShow> RecommendationsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommendationsPage"/> class.
        /// </summary>
        public RecommendationsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the status message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="activity">if set to <c>true</c> an animating spinner will be displayed.</param>
        public void SetStatus(string message, bool activity = false)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    statusLabel.Content = message;

                    if (activity)
                    {
                        statusLabel.Padding = new Thickness(24, 0, 24, 0);
                        statusThrobber.Visibility = Visibility.Visible;
                        ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
                    }
                    else
                    {
                        ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Stop();
                        statusThrobber.Visibility = Visibility.Hidden;
                        statusLabel.Padding = new Thickness(7, 0, 7, 0);
                    }
                }));
        }

        /// <summary>
        /// Resets the status.
        /// </summary>
        public void ResetStatus()
        {
            SetStatus(String.Empty);
        }

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (RecommendationsListViewItemCollection == null)
            {
                RecommendationsListViewItemCollection = new ObservableCollection<RecommendationEngine.RecommendedShow>();
                listView.ItemsSource                  = RecommendationsListViewItemCollection;
            }

            if (MainWindow.Active != null && MainWindow.Active.IsActive && comboBox.SelectedIndex == -1)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        #region ComboBox events
        /// <summary>
        /// Handles the DropDownOpened event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ComboBoxDropDownOpened(object sender, System.EventArgs e)
        {
            listView.Visibility = statusLabel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Handles the DropDownClosed event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ComboBoxDropDownClosed(object sender, System.EventArgs e)
        {
            listView.Visibility = statusLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecommendationEngine rec;

            switch(comboBox.SelectedIndex)
            {
                case 0:
                case 1:
                    rec = new RSTVShowRecommendation("S2qNfbCFCWoQ8RoL1S0FTbjbW", Utils.GetUID(), comboBox.SelectedIndex);
                    SetStatus("Downloading recommendations from lab.rolisoft.net/tv...", true);
                    break;

                case 2:
                    rec = new TasteKid();
                    SetStatus("Downloading recommendations from tastekid.com...", true);
                    break;

                default:
                    return;
            }

            RecommendationsListViewItemCollection.Clear();

            rec.RecommendationDone  += RecommendationDone;
            rec.RecommendationError += RecommendationError;

            rec.GetListAsync(Database.Query("select name from tvshows").Select(r => r["name"]).ToList());
        }
        #endregion

        #region Recommendation events
        /// <summary>
        /// Called when the recommendation engine has encountered an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="detailed">The detailed error message.</param>
        public void RecommendationError(string message, string detailed = null)
        {
            SetStatus(message);
        }

        /// <summary>
        /// Called when a recommendation request is processed.
        /// </summary>
        /// <param name="shows">The recommended shows.</param>
        public void RecommendationDone(List<RecommendationEngine.RecommendedShow> shows)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    // unfortunately there's no AddRange() for ObservableCollection<T> :(
                    foreach (var show in shows)
                    {
                        RecommendationsListViewItemCollection.Add(show);
                    }

                    if (shows.Count != 0)
                    {
                        SetStatus("There are " + shows.Count + " shows on the list which you might like.");
                    }
                    else
                    {
                        SetStatus("Unfortunately the selected service couldn't recommend you anything.");
                    }
                }));
        }
        #endregion

        #region Context menu tools
        /// <summary>
        /// Handles the Click event of the ViewImdb control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ViewImdbClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run(((RecommendationEngine.RecommendedShow)listView.SelectedValue).Imdb);
        }

        /// <summary>
        /// Handles the Click event of the ViewWikipedia control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ViewWikipediaClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run(((RecommendationEngine.RecommendedShow)listView.SelectedValue).Wikipedia);
        }

        /// <summary>
        /// Handles the Click event of the ViewEpguides control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ViewEpguidesClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run(((RecommendationEngine.RecommendedShow)listView.SelectedValue).Epguides);
        }

        /// <summary>
        /// Handles the Click event of the SearchYouTube control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchYouTubeClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run("http://www.youtube.com/results?search_query=" + Uri.EscapeUriString(((RecommendationEngine.RecommendedShow)listView.SelectedValue).Name) + "+promo");
        }
        #endregion
    }
}
