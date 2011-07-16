namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines;
    using RoliSoft.TVShowTracker.TaskDialogs;

    /// <summary>
    /// Interaction logic for GuidesPage.xaml
    /// </summary>
    public partial class GuidesPage : IRefreshable
    {
        /// <summary>
        /// Gets or sets the date when this control was loaded.
        /// </summary>
        /// <value>The load date.</value>
        public DateTime LoadDate { get; set; } 

        /// <summary>
        /// Gets or sets the guide list view item collection.
        /// </summary>
        /// <value>The guide list view item collection.</value>
        public ObservableCollection<GuideListViewItem> GuideListViewItemCollection { get; set; }

        private string _activeShowUrl, _activeShowID;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuidesPage"/> class.
        /// </summary>
        public GuidesPage()
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
        /// Gets the selected show name and episode on the list view.
        /// </summary>
        /// <returns>An array with the name as the first item and episode number as second.</returns>
        public string[] GetSelectedShow()
        {
            return new[]
                {
                    comboBox.SelectedValue.ToString(),
                    ((GuideListViewItem)listView.SelectedValue).Episode
                };
        }

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (GuideListViewItemCollection == null)
            {
                GuideListViewItemCollection = new ObservableCollection<GuideListViewItem>();
                (FindResource("cvs") as CollectionViewSource).Source = GuideListViewItemCollection;
            }

            if (MainWindow.Active != null && MainWindow.Active.IsActive && LoadDate < Database.DataChange)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Refreshes the data on this instance.
        /// </summary>
        public void Refresh()
        {
            LoadShowList();

            if (GuideListViewItemCollection != null && comboBox.SelectedIndex != -1)
            {
                ComboBoxSelectionChanged(null, null);
            }

            LoadDate = DateTime.Now;
        }

        /// <summary>
        /// Loads the show list.
        /// </summary>
        public void LoadShowList()
        {
            comboBox.ItemsSource = Database.TVShows.Select(s => s.Name).ToList();
        }

        /// <summary>
        /// Selects the show.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        public void SelectShow(string name)
        {
            comboBox.SelectedIndex = comboBox.Items.IndexOf(name);
        }

        #region ComboBox events
        /// <summary>
        /// Handles the DropDownOpened event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ComboBoxDropDownOpened(object sender, EventArgs e)
        {
            // the dropdown's background is transparent and if it opens while the guide listview
            // is populated, then you won't be able to read the show names due to the mess

            tabControl.Visibility = statusLabel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Handles the DropDownClosed event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ComboBoxDropDownClosed(object sender, EventArgs e)
        {
            tabControl.Visibility = statusLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showGeneral.Visibility = Visibility.Hidden;
            GuideListViewItemCollection.Clear();

            var sel = comboBox.SelectedValue;
            if (sel == null)
            {
                return;
            }

            var qid = Database.TVShows.Where(s => s.Name == sel.ToString()).ToList();
            if (qid.Count == 0)
            {
                return;
            }

            var id = qid[0].ShowID.ToString();
            _activeShowID = id;

            // fill up general informations

            var airing = bool.Parse(Database.ShowData(id, "airing"));

            showGeneralCover.Source = new BitmapImage(new Uri("http://" + Remote.API.EndPoint + "?/GetShowCover/" + Uri.EscapeUriString(comboBox.SelectedValue.ToString())), new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable));
            
            showGeneralName.Text = comboBox.SelectedValue.ToString();

            showGeneralSub.Text = string.Empty;
            string genre;
            if ((genre = Database.ShowData(id, "genre")) != string.Empty)
            {
                showGeneralSub.Text = genre + " show; ";
            }

            string runtime;
            if ((runtime = Database.ShowData(id, "runtime")) != string.Empty)
            {
                showGeneralSub.Text += runtime + " minutes";
            }

            var airs = string.Empty;
            string airday;
            if ((airday = Database.ShowData(id, "airday")) != string.Empty)
            {
                airs += " " + airday;
            }

            string airtime;
            if ((airtime = Database.ShowData(id, "airtime")) != string.Empty)
            {
                airs += " at " + airtime;
            }

            string network;
            if ((network = Database.ShowData(id, "network")) != string.Empty)
            {
                airs += " on " + network;
            }

            if ((airday + airtime + network) != string.Empty)
            {
                showGeneralSub.Text += Environment.NewLine + "Airs" + airs;
            }

            var guide = Updater.CreateGuide(Database.ShowData(id, "grabber"));

            showGeneralSub.Text        += Environment.NewLine + "Episode listing provided by " + guide.Name;
            showGeneralGuideIcon.Source = new BitmapImage(new Uri(guide.Icon, UriKind.Relative));

            _activeShowUrl = Database.ShowData(id, "url");
            if (string.IsNullOrWhiteSpace(_activeShowUrl))
            {
                _activeShowUrl = guide.Site;
            }

            showGeneralDescr.Text = string.Empty;
            string descr;
            if ((descr = Database.ShowData(id, "descr")) != string.Empty)
            {
                showGeneralDescr.Text = descr;
            }

            try
            {
                var last = Database.Episodes.Where(ep => ep.ShowID.ToString() == id && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderByDescending(ep => ep.Season * 1000 + ep.Number).Take(1).ToList();
                var next = Database.Episodes.Where(ep => ep.ShowID.ToString() == id && ep.Airdate > DateTime.Now).OrderBy(ep => ep.Season * 1000 + ep.Number).Take(1).ToList();

                if (last.Count != 0)
                {
                    showGeneralLastPanel.Visibility = Visibility.Visible;
                    showGeneralLast.Text            = last[0].Name;
                    showGeneralLastDate.Text        = last[0].Airdate.ToRelativeDate(true);
                }
                else
                {
                    showGeneralLastPanel.Visibility = Visibility.Collapsed;
                }

                if (next.Count != 0)
                {
                    showGeneralNextPanel.Visibility = Visibility.Visible;
                    showGeneralNext.Text            = next[0].Name;
                    showGeneralNextDate.Text        = next[0].Airdate.ToRelativeDate(true);
                }
                else
                {
                    showGeneralNextPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                showGeneralLast.Text = string.Empty;
                showGeneralNext.Text = string.Empty;

                showGeneralLastDate.Text = "no data available";
                showGeneralNextDate.Text = airing ? "no data available" : "this show has ended";
            }

            showGeneral.Visibility = Visibility.Visible;

            // fill up episode list

            var shows = Database.Episodes.Where(ep => ep.ShowID.ToString() == id).OrderByDescending(ep => ep.Season * 1000 + ep.Number);
            var icon  = Updater.CreateGuide(Database.ShowData(id, "grabber")).Icon;

            foreach (var show in shows)
            {
                GuideListViewItemCollection.Add(new GuideListViewItem
                    {
                        SeenIt      = show.Watched,
                        Id          = show.ShowID + "|" + show.EpisodeID,
                        Season      = "Season " + show.Season,
                        Episode     = "S{0:00}E{1:00}".FormatWith(show.Season, show.Number),
                        Airdate     = show.Airdate != Utils.UnixEpoch
                                      ? show.Airdate.ToString("MMMM d, yyyy", new CultureInfo("en-US")) + (show.Airdate > DateTime.Now ? "*" : string.Empty)
                                      : "Unaired episode",
                        Title       = show.Name,
                        Summary     = show.Description,
                        Picture     = show.Picture,
                        URL         = show.URL,
                        GrabberIcon = icon
                    });
            }
        }
        #endregion

        #region Play episode
        /// <summary>
        /// Handles the Click event of the PlayEpisode control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PlayEpisodeClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();

            new FileSearchTaskDialog().Search(show[0], show[1]);
        }
        #endregion

        #region Open details page
        /// <summary>
        /// Handles the Click event of the OpenDetailsPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OpenDetailsPageClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1 || string.IsNullOrWhiteSpace(((GuideListViewItem)listView.SelectedValue).URL)) return;
            Utils.Run(((GuideListViewItem)listView.SelectedValue).URL);
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenDetailsPageClick(null, null);
        }
        #endregion

        #region Search for download links
        /// <summary>
        /// Handles the Click event of the SearchDownloadLinks control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchDownloadLinksClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();

            MainWindow.Active.tabControl.SelectedIndex = 2;
            MainWindow.Active.activeDownloadLinksPage.Search(show[0] + " " + show[1]);
        }
        #endregion

        #region Search for subtitles
        /// <summary>
        /// Handles the Click event of the SearchSubtitles control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchSubtitlesClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();

            MainWindow.Active.tabControl.SelectedIndex = 3;
            MainWindow.Active.activeSubtitlesPage.Search(show[0] + " " + show[1]);
        }
        #endregion

        #region Search for online videos
        /// <summary>
        /// Handles the Click event of the Hulu control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HuluClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show  = GetSelectedShow();
            var title = ((GuideListViewItem)listView.SelectedValue).Title;

            new OnlineVideoSearchEngineTaskDialog<Hulu>().Search(show[0], show[1], title);
        }

        /// <summary>
        /// Handles the Click event of the iPlayer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IPlayerClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();

            new OnlineVideoSearchEngineTaskDialog<BBCiPlayer>().Search(show[0], show[1]);
        }

        /// <summary>
        /// Handles the Click event of the iTunes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ITunesClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();
            var epnr = ShowNames.Parser.ExtractEpisode(show[1]);

            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + show[0] + " intitle:\"season " + epnr.Season + "\" site:itunes.apple.com inurl:/tv-season/"));
        }

        /// <summary>
        /// Handles the Click event of the Amazon control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AmazonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();
            var epnr = ShowNames.Parser.ExtractEpisode(show[1]);

            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + show[0] + " intitle:\"Season " + epnr.Season + ", Episode " + epnr.Episode + "\" site:amazon.com"));
        }

        /// <summary>
        /// Handles the Click event of the SideReel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SideReelClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();

            new OnlineVideoSearchEngineTaskDialog<SideReel>().Search(show[0], show[1]);
        }

        /// <summary>
        /// Handles the Click event of the GoogleSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GoogleSearchClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show = GetSelectedShow();
            Utils.Run("http://www.google.com/search?q=" + Uri.EscapeUriString(show[0] + " " + show[1]));
        }
        #endregion

        #region Seen it
        /// <summary>
        /// Handles the Checked event of the SeenIt control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SeenItChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                var val = ((CheckBox)e.OriginalSource).Tag.ToString().Split('|');
                Database.Execute("insert into tracking values (" + val[0] + ", '" + val[1] + "')");

                if (Synchronization.Status.Enabled)
                {
                    Synchronization.Status.Engine.MarkEpisodes(val[0], new[] { val[1].ToInteger() - (val[0].ToInteger() * 100000) }.ToList());
                }

                Database.Trackings.Add(int.Parse(val[1]));
                Database.Episodes.First(ep => ep.EpisodeID.ToString() == val[1]).Watched = true;
                MainWindow.Active.DataChanged(false);
            }
            catch
            {
                SetStatus("Couldn't mark episode as seen due to a database error.");
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the SeenIt control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SeenItUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                var val = ((CheckBox)e.OriginalSource).Tag.ToString().Split('|');
                Database.Execute("delete from tracking where showid = " + val[0] + " and episodeid = '" + val[1] + "'");

                if (Synchronization.Status.Enabled)
                {
                    Synchronization.Status.Engine.UnmarkEpisodes(val[0], new[] { val[1].ToInteger() - (val[0].ToInteger() * 100000) }.ToList());
                }

                Database.Trackings.Remove(int.Parse(val[1]));
                Database.Episodes.First(ep => ep.EpisodeID.ToString() == val[1]).Watched = false;
                MainWindow.Active.DataChanged(false);
            }
            catch
            {
                SetStatus("Couldn't mark episode as not seen due to a database error.");
            }
        }
        #endregion

        /// <summary>
        /// Handles the ImageFailed event of the showGeneralCover control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.ExceptionRoutedEventArgs"/> instance containing the event data.</param>
        private void ShowGeneralCoverImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            showGeneralCover.Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cd.png", UriKind.Relative));
        }

        #region Icons
        /// <summary>
        /// Handles the MouseLeftButtonUp event of the showGeneralGuideIcon control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ShowGeneralGuideIconMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Utils.Run(_activeShowUrl);
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the showGeneralWikipedia control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ShowGeneralWikipediaMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString(showGeneralName.Text + " TV Series site:en.wikipedia.org"));
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the showGeneralImdb control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ShowGeneralImdbMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString(showGeneralName.Text + " intitle:\"TV Series\" site:imdb.com"));
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the showGeneralGoogle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ShowGeneralGoogleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Utils.Run("http://www.google.com/search?hl=en&q=" + Uri.EscapeUriString(showGeneralName.Text));
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the showGeneralSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ShowGeneralSettingsMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            new EditShowWindow(_activeShowID, showGeneralName.Text).ShowDialog();
        }
        #endregion
    }
}
