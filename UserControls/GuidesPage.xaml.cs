namespace RoliSoft.TVShowTracker
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines;
    using RoliSoft.TVShowTracker.Tables;
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
        public BindingList<GuideListViewItem> GuideListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the upcoming list view item collection.
        /// </summary>
        /// <value>The upcoming list view item collection.</value>
        public BindingList<UpcomingListViewItem> UpcomingListViewItemCollection { get; set; }

        private int _activeShowID;
        private string _activeShowUrl;

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
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (GuideListViewItemCollection == null)
            {
                GuideListViewItemCollection = new BindingList<GuideListViewItem>();
                ((CollectionViewSource)FindResource("cvs")).Source = GuideListViewItemCollection;
            }

            if (UpcomingListViewItemCollection == null)
            {
                UpcomingListViewItemCollection = new BindingList<UpcomingListViewItem>();
                ((CollectionViewSource)FindResource("cvs2")).Source = UpcomingListViewItemCollection;
            }

            if (MainWindow.Active != null && MainWindow.Active.IsActive && LoadDate < Database.DataChange)
            {
                Refresh();
            }

            if (comboBox.SelectedIndex == -1)
            {
                comboBox.SelectedIndex = 0;
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
            var items = Database.TVShows.Values.OrderBy(s => s.Name).Select(s => s.Name).ToList();
            items.Insert(0, "— Upcoming episodes —");

            comboBox.ItemsSource = items;
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

            upcomingListView.Visibility = tabControl.Visibility = statusLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Handles the DropDownClosed event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ComboBoxDropDownClosed(object sender, EventArgs e)
        {
            if (comboBox.SelectedIndex <= 0)
            {
                upcomingListView.Visibility = statusLabel.Visibility = Visibility.Visible;
            }

            if (comboBox.SelectedIndex >= 1)
            {
                tabControl.Visibility = statusLabel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the comboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            upcomingListView.Visibility = generalTab.Visibility = showGeneral.Visibility = episodeListTab.Visibility = listView.Visibility = Visibility.Collapsed;
            GuideListViewItemCollection.Clear();
            UpcomingListViewItemCollection.Clear();

            if (comboBox.SelectedIndex <= 0)
            {
                LoadUpcomingEpisodes();
            }

            if (comboBox.SelectedIndex >= 1)
            {
                LoadSelectedShow();
            }
        }

        /// <summary>
        /// Loads the upcoming episodes.
        /// </summary>
        public void LoadUpcomingEpisodes()
        {
            var episodes = Database.Episodes.Where(ep => ep.Airdate > DateTime.Now).OrderBy(ep => ep.Airdate).Take(100);

            UpcomingListViewItemCollection.RaiseListChangedEvents = false;

            foreach (var episode in episodes)
            {
                var network = string.Empty;
                if (episode.Show.Data.TryGetValue("network", out network))
                {
                    network = " / " + network;
                }

                UpcomingListViewItemCollection.Add(new UpcomingListViewItem
                    {
                        Episode      = episode,
                        Show         = "{0} S{1:00}E{2:00}".FormatWith(episode.Show.Name, episode.Season, episode.Number),
                        Name         = " · " + episode.Name,
                        Airdate      = episode.Airdate.DayOfWeek + " / " + episode.Airdate.ToString("h:mm tt") + network,
                        RelativeDate = episode.Airdate.ToShortRelativeDate()
                    });
            }

            UpcomingListViewItemCollection.RaiseListChangedEvents = true;
            UpcomingListViewItemCollection.ResetBindings();

            tabControl.Visibility = generalTab.Visibility = showGeneral.Visibility = episodeListTab.Visibility = listView.Visibility = Visibility.Collapsed;
            upcomingListView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Loads the selected show.
        /// </summary>
        public void LoadSelectedShow()
        {
            var show = Database.TVShows.Values.First(s => s.Name == comboBox.SelectedValue.ToString());
            var id = _activeShowID = show.ShowID;

            // fill up general informations

            var airing = bool.Parse(show.Data["airing"]);

            showGeneralCover.Source = new BitmapImage(new Uri("http://" + Remote.API.EndPoint + "?/GetShowCover/" + Uri.EscapeUriString(comboBox.SelectedValue.ToString())), new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable));
            
            showGeneralName.Text = show.Name;

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
                var last = show.Episodes.Where(ep => ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderByDescending(ep => ep.Season * 1000 + ep.Number).Take(1).ToList();
                var next = show.Episodes.Where(ep => ep.Airdate > DateTime.Now).OrderBy(ep => ep.Season * 1000 + ep.Number).Take(1).ToList();

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

            // fill up episode list

            var episodes = show.Episodes.OrderByDescending(ep => ep.Season * 1000 + ep.Number);
            var icon     = Updater.CreateGuide(show.Data["grabber"]).Icon;

            GuideListViewItemCollection.RaiseListChangedEvents = false;

            foreach (var episode in episodes)
            {
                GuideListViewItemCollection.Add(new GuideListViewItem
                    {
                        ID          = episode,
                        SeenIt      = episode.Watched,
                        Season      = "Season " + episode.Season,
                        Episode     = "S{0:00}E{1:00}".FormatWith(episode.Season, episode.Number),
                        Airdate     = episode.Airdate != Utils.UnixEpoch
                                      ? episode.Airdate.ToString("MMMM d, yyyy", new CultureInfo("en-US")) + (episode.Airdate > DateTime.Now ? "*" : string.Empty)
                                      : "Unaired episode",
                        Title       = episode.Name,
                        Summary     = episode.Description,
                        Picture     = episode.Picture,
                        URL         = episode.URL,
                        GrabberIcon = icon
                    });
            }

            GuideListViewItemCollection.RaiseListChangedEvents = true;
            GuideListViewItemCollection.ResetBindings();

            upcomingListView.Visibility = Visibility.Collapsed;
            tabControl.Visibility = generalTab.Visibility = showGeneral.Visibility = episodeListTab.Visibility = listView.Visibility = Visibility.Visible;
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

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            new FileSearchTaskDialog().Search(ep.Show.Name, string.Format("S{0:00}E{1:00}", ep.Season, ep.Number), ep.Airdate.ToOriginalTimeZone(ep.Show.Data["timezone"]));
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

        /// <summary>
        /// Handles the MouseDoubleClick event of the upcomingListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void UpcomingListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (upcomingListView.SelectedIndex == -1) return;
            SelectShow(((UpcomingListViewItem)upcomingListView.SelectedValue).Episode.Show.Name);
        }

        #region Search for download links
        /// <summary>
        /// Handles the Click event of the SearchDownloadLinks control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchDownloadLinksClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            MainWindow.Active.tabControl.SelectedIndex = 2;
            MainWindow.Active.activeDownloadLinksPage.Search(ep.Show.Name + " " + string.Format("S{0:00}E{1:00}", ep.Season, ep.Number));
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

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            MainWindow.Active.tabControl.SelectedIndex = 3;
            MainWindow.Active.activeSubtitlesPage.Search(ep.Show.Name + " " + string.Format("S{0:00}E{1:00}", ep.Season, ep.Number));
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

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            new OnlineVideoSearchEngineTaskDialog<Hulu>().Search(ep.Show.Name, string.Format("S{0:00}E{1:00}", ep.Season, ep.Number), ep.Name);
        }

        /// <summary>
        /// Handles the Click event of the iPlayer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IPlayerClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            new OnlineVideoSearchEngineTaskDialog<BBCiPlayer>().Search(ep.Show.Name, string.Format("S{0:00}E{1:00}", ep.Season, ep.Number));
        }

        /// <summary>
        /// Handles the Click event of the iTunes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ITunesClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + ep.Show.Name + " intitle:\"season " + ep.Season + "\" site:itunes.apple.com inurl:/tv-season/"));
        }

        /// <summary>
        /// Handles the Click event of the Amazon control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AmazonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + ep.Show.Name + " intitle:\"Season " + ep.Season + ", Episode " + ep.Number + "\" site:amazon.com"));
        }

        /// <summary>
        /// Handles the Click event of the SideReel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SideReelClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            new OnlineVideoSearchEngineTaskDialog<SideReel>().Search(ep.Show.Name, string.Format("S{0:00}E{1:00}", ep.Season, ep.Number));
        }

        /// <summary>
        /// Handles the Click event of the GoogleSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GoogleSearchClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var ep = ((GuideListViewItem)listView.SelectedValue).ID;

            Utils.Run("http://www.google.com/search?q=" + Uri.EscapeUriString(ep.Show.Name + " " + string.Format("S{0:00}E{1:00}", ep.Season, ep.Number)));
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
            var episode = (Episode)((CheckBox)e.OriginalSource).Tag;

            if (listView.SelectedItems.Count > 1 && listView.SelectedItems.Cast<GuideListViewItem>().Any(x => x.ID == episode))
            {
                // check all selected

                try
                {
                    var tr = Database.Connection.BeginTransaction();

                    foreach (GuideListViewItem item in listView.SelectedItems)
                    {
                        Database.ExecuteOnTransaction(tr, "insert into tracking values (" + item.ID.ShowID + ", '" + item.ID.EpisodeID + "')");
                        Database.Trackings.Add(item.ID.EpisodeID);
                        item.ID.Watched = item.SeenIt = true;
                        item.RefreshSeenIt();
                    }

                    tr.Commit();
                }
                catch
                {
                    SetStatus("Couldn't mark the selected episodes as seen due to a database error.");
                }
                finally
                {
                    MainWindow.Active.DataChanged(false);
                }
            }
            else
            {
                // check only one

                try
                {
                    Database.Execute("insert into tracking values (" + episode.ShowID + ", '" + episode.EpisodeID + "')");
                    Database.Trackings.Add(episode.EpisodeID);
                    episode.Watched = true;
                }
                catch
                {
                    SetStatus("Couldn't mark the episode as seen due to a database error.");
                }
                finally
                {
                    MainWindow.Active.DataChanged(false);
                }
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the SeenIt control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SeenItUnchecked(object sender, RoutedEventArgs e)
        {
            var episode = (Episode)((CheckBox)e.OriginalSource).Tag;

            if (listView.SelectedItems.Count > 1 && listView.SelectedItems.Cast<GuideListViewItem>().Any(x => x.ID == episode))
            {
                // check all selected

                try
                {
                    var tr = Database.Connection.BeginTransaction();

                    foreach (GuideListViewItem item in listView.SelectedItems)
                    {
                        Database.ExecuteOnTransaction(tr, "delete from tracking where showid = " + item.ID.ShowID + " and episodeid = '" + item.ID.EpisodeID + "'");
                        Database.Trackings.Remove(item.ID.EpisodeID);
                        item.ID.Watched = item.SeenIt = false;
                        item.RefreshSeenIt();
                    }

                    tr.Commit();
                }
                catch
                {
                    SetStatus("Couldn't mark the selected episodes as not seen due to a database error.");
                }
                finally
                {
                    MainWindow.Active.DataChanged(false);
                }
            }
            else
            {
                // check only one

                try
                {
                    Database.Execute("delete from tracking where showid = " + episode.ShowID + " and episodeid = '" + episode.EpisodeID + "'");
                    Database.Trackings.Remove(episode.EpisodeID);
                    episode.Watched = false;
                }
                catch
                {
                    SetStatus("Couldn't mark the episode as not seen due to a database error.");
                }
                finally
                {
                    MainWindow.Active.DataChanged(false);
                }
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
