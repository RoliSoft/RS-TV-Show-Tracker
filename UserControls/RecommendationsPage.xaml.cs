namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.Recommendations;
    using RoliSoft.TVShowTracker.Parsers.Recommendations.Engines;

    /// <summary>
    /// Interaction logic for RecommendationsPage.xaml
    /// </summary>
    public partial class RecommendationsPage : UserControl
    {
        /// <summary>
        /// Gets or sets the recommendations list view item collection.
        /// </summary>
        /// <value>The recommendations list view item collection.</value>
        public ObservableCollection<RecommendedShow> RecommendationsListViewItemCollection { get; set; }

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
                RecommendationsListViewItemCollection = new ObservableCollection<RecommendedShow>();
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
                    rec = new RSTVShowRecommendation(comboBox.SelectedIndex);
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

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }
        #endregion

        #region Recommendation events
        /// <summary>
        /// Called when the recommendation engine has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="string"/> instance containing the event data.</param>
        public void RecommendationError(object sender, EventArgs<string, Exception> e)
        {
            SetStatus(e.First);

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            MainWindow.Active.HandleUnexpectedException(e.Second);
        }

        /// <summary>
        /// Called when a recommendation request is processed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void RecommendationDone(object sender, EventArgs<List<RecommendedShow>> e)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    if (e.Data.Count != 0)
                    {
                        // unfortunately there's no AddRange() for ObservableCollection<T> :(
                        foreach (var show in e.Data)
                        {
                            RecommendationsListViewItemCollection.Add(show);
                        }

                        SetStatus("There are " + e.Data.Count + " shows on the list which you might like.");
                    }
                    else
                    {
                        SetStatus("Unfortunately the selected service couldn't recommend you anything.");
                    }
                }));

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
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
            Utils.Run(((RecommendedShow)listView.SelectedValue).Imdb);
        }

        /// <summary>
        /// Handles the Click event of the ViewWikipedia control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ViewWikipediaClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run(((RecommendedShow)listView.SelectedValue).Wikipedia);
        }

        /// <summary>
        /// Handles the Click event of the ViewEpguides control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ViewEpguidesClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run(((RecommendedShow)listView.SelectedValue).Epguides);
        }

        /// <summary>
        /// Handles the Click event of the SearchYouTube control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchYouTubeClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            Utils.Run("http://www.youtube.com/results?search_query=" + Uri.EscapeUriString(((RecommendedShow)listView.SelectedValue).Name) + "+promo");
        }
        #endregion
    }
}
