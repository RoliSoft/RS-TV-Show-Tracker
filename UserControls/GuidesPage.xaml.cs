namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.ContextMenus;
    using RoliSoft.TVShowTracker.ContextMenus.Menus;
    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;
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
        private Thread _coverThd;

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
            SetStatus(string.Empty);
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

            LoadDate = DateTime.Now;
        }

        /// <summary>
        /// Loads the show list.
        /// </summary>
        public void LoadShowList()
        {
            var items = new List<object>();
            items.Add(new GuideDropDownUpcomingItem());

            foreach (var plugin in Extensibility.GetNewInstances<LocalProgrammingPlugin>())
            {
                foreach (var config in plugin.GetConfigurations())
                {
                    items.Add(new GuideDropDownUpcomingItem(config));
                }
            }

            items.AddRange(Database.TVShows.Values.OrderBy(s => s.Name).Select(s => new GuideDropDownTVShowItem(s)));

            var idx = comboBox.SelectedIndex;

            comboBox.ItemsSource = items;

            if (idx != -1 && items.Count > idx)
            {
                comboBox.SelectedIndex = idx;
            }
        }

        /// <summary>
        /// Selects the show.
        /// </summary>
        /// <param name="show">The TV show.</param>
        public void SelectShow(TVShow show)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is GuideDropDownTVShowItem && ((GuideDropDownTVShowItem)comboBox.Items[i]).Show == show)
                {
                    comboBox.SelectedIndex = i;
                    break;
                }
            }
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
            if (comboBox.SelectedItem is GuideDropDownUpcomingItem)
            {
                upcomingListView.Visibility = statusLabel.Visibility = Visibility.Visible;
            }

            if (comboBox.SelectedItem is GuideDropDownTVShowItem)
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

            if (comboBox.SelectedItem is GuideDropDownUpcomingItem)
            {
                if (((GuideDropDownUpcomingItem)comboBox.SelectedItem).Config == null)
                {
                    LoadUpcomingEpisodes();
                }
                else
                {
                    LoadSelectedConfig();
                }
            }
            
            if (comboBox.SelectedItem is GuideDropDownTVShowItem)
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
        /// Loads the upcoming episodes from a plugin.
        /// </summary>
        public void LoadSelectedConfig()
        {
            var config = ((GuideDropDownUpcomingItem)comboBox.SelectedItem).Config;

            SetStatus("Loading " + config.Plugin.Name + "...", true);

            new Thread(() =>
                {
                    var episodes = config.Plugin.GetListing(config).ToList();

                    ResetStatus();

                    Dispatcher.Invoke((Action)(() =>
                        {
                            UpcomingListViewItemCollection.RaiseListChangedEvents = false;

                            foreach (var episode in episodes)
                            {
                                UpcomingListViewItemCollection.Add(new UpcomingListViewItem
                                    {
                                        Programme    = episode,
                                        Show         = episode.Name + (!string.IsNullOrWhiteSpace(episode.Number) ? " " + episode.Number : string.Empty),
                                        Name         = !string.IsNullOrWhiteSpace(episode.Description) ? " · " + episode.Description : string.Empty,
                                        Airdate      = episode.Airdate.DayOfWeek + " / " + episode.Airdate.ToString("h:mm tt") + " / " + episode.Channel,
                                        RelativeDate = episode.Airdate.ToShortRelativeDate()
                                    });
                            }

                            UpcomingListViewItemCollection.RaiseListChangedEvents = true;
                            UpcomingListViewItemCollection.ResetBindings();

                            tabControl.Visibility = generalTab.Visibility = showGeneral.Visibility = episodeListTab.Visibility = listView.Visibility = Visibility.Collapsed;
                            upcomingListView.Visibility = Visibility.Visible;
                        }));
                }).Start();
        }

        /// <summary>
        /// Loads the selected show.
        /// </summary>
        public void LoadSelectedShow()
        {
            ResetStatus();

            if (_coverThd != null)
            {
                try { _coverThd.Abort(); } catch { }
                _coverThd = null;
            }

            var show = ((GuideDropDownTVShowItem)comboBox.SelectedItem).Show;
            var id = _activeShowID = show.ShowID;

            // fill up general informations

            var airing = bool.Parse(show.Data.Get("airing", "False"));

            showGeneralName.Text = show.Name;
            showGeneralCover.Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cd.png", UriKind.Relative));

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
            showGeneralGuideIcon.Source = new BitmapImage(new Uri(guide.Icon));

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
                var last = show.Episodes.Where(ep => ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderByDescending(ep => ep.EpisodeID).Take(1).ToList();
                var next = show.Episodes.Where(ep => ep.Airdate > DateTime.Now).OrderBy(ep => ep.EpisodeID).Take(1).ToList();

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

            var episodes = show.Episodes.OrderByDescending(ep => ep.EpisodeID);
            var icon     = Updater.CreateGuide(show.Data.Get("grabber", "TVRage")).Icon;

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

            // get cover

            
            _coverThd = new Thread(() =>
                {
                    var cover = CoverManager.GetCover(show.Name, s => Dispatcher.Invoke((Action)(() => SetStatus(s, true))));
                    Dispatcher.Invoke((Action)(() =>
                        {
                            showGeneralCover.Source = new BitmapImage(cover ?? new Uri("/RSTVShowTracker;component/Images/cd.png", UriKind.Relative));
                            ResetStatus();
                        }));
                    _coverThd = null;
                });
            _coverThd.Start();
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

            var sel = (UpcomingListViewItem) upcomingListView.SelectedValue;

            if (sel.Episode != null)
            {
                if (!string.IsNullOrWhiteSpace(sel.Episode.URL))
                {
                    Utils.Run(sel.Episode.URL);
                }
                else
                {
                    SelectShow(sel.Episode.Show);
                }
            }
            else if (sel.Programme != null)
            {
                if (!string.IsNullOrWhiteSpace(sel.Programme.URL))
                {
                    Utils.Run(sel.Programme.URL);
                }
                else
                {
                    SelectShow(sel.Programme.Show);
                }
            }
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
            MainWindow.Active.activeDownloadLinksPage.Search(ep.Show.Name + " " + (ep.Show.Data.Get("notation") == "airdate" ? ep.Airdate.ToOriginalTimeZone(ep.Show.Data.Get("timezone")).ToString("yyyy.MM.dd") : string.Format("S{0:00}E{1:00}", ep.Season, ep.Number)));
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
            MainWindow.Active.activeSubtitlesPage.Search(ep.Show.Name + " " + (ep.Show.Data.Get("notation") == "airdate" ? ep.Airdate.ToOriginalTimeZone(ep.Show.Data.Get("timezone")).ToString("yyyy.MM.dd") : string.Format("S{0:00}E{1:00}", ep.Season, ep.Number)));
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

        #region ListView keys
        /// <summary>
        /// Handles the ContextMenuOpening event of the ListViewItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.ContextMenuEventArgs"/> instance containing the event data.</param>
        private void ListViewItemContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;

            if (listView.SelectedIndex == -1) return;

            var cm = new ContextMenu();
            (e.Source as FrameworkElement).ContextMenu = cm;
            var episode = (GuideListViewItem)listView.SelectedValue;

            var spm = -55;
            var lbw = 115;

            // Play episode

            var pla    = new MenuItem();
            pla.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/play.png")) };
            pla.Click += (s, r) => new FileSearchTaskDialog().Search(episode.ID);
            pla.Header = "Play episode";
            cm.Items.Add(pla);

            if (!string.IsNullOrWhiteSpace(episode.URL))
            {
                // Details page

                var det    = new MenuItem();
                det.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/page.png")) };
                det.Click += OpenDetailsPageClick;
                det.Header = "Details page";
                cm.Items.Add(det);
            }

            // Download links

            var sfd    = new MenuItem();
            sfd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
            sfd.Click += SearchDownloadLinksClick;
            sfd.Header = "Download links";
            cm.Items.Add(sfd);

            // Subtitles

            var sfs    = new MenuItem();
            sfs.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/subtitles.png")) };
            sfs.Click += SearchSubtitlesClick;
            sfs.Header = "Subtitles";
            cm.Items.Add(sfs);

            // Search for online videos

            var sov    = new MenuItem();
            sov.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/monitor.png")) };
            sov.Header = "Online videos";
            cm.Items.Add(sov);

            // - Engines

            var ovseidx = -1f;
            foreach (var ovse in Extensibility.GetNewInstances<OnlineVideoSearchEngine>().OrderBy(ovse => ovse.Index))
            {
                if ((ovseidx - ovse.Index) != -1)
                {
                    sov.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
                }

                ovseidx = ovse.Index;

                var ovmi    = new MenuItem();
                ovmi.Tag    = ovse;
                ovmi.Header = ovse.Name;
                ovmi.Icon   = new Image { Source = new BitmapImage(new Uri(ovse.Icon)) };
                ovmi.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog((OnlineVideoSearchEngine)ovmi.Tag).Search(episode.ID);
                sov.Items.Add(ovmi);
            }

            if (ovseidx != -1)
            {
                sov.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            }

            // - Google search

            var gls    = new MenuItem();
            gls.Header = "Google search";
            gls.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/google.png")) };
            gls.Click += (s, r) => Utils.Run("http://www.google.com/search?q=" + Uri.EscapeUriString(string.Format("{0} S{1:00}E{2:00}", episode.ID.Show.Name, episode.ID.Season, episode.ID.Number)));

            sov.Items.Add(gls);

            // Plugins

            foreach (var ovcm in Extensibility.GetNewInstances<EpisodeListingContextMenu>())
            {
                foreach (var ovcmi in ovcm.GetMenuItems(episode.ID))
                {
                    var cmi    = new MenuItem();
                    cmi.Tag    = ovcmi;
                    cmi.Header = ovcmi.Name;
                    cmi.Icon   = ovcmi.Icon;
                    cmi.Click += (s, r) => ((ContextMenuItem<Episode>)cmi.Tag).Click(episode.ID);
                    cm.Items.Add(cmi);
                }
            }

            TextOptions.SetTextFormattingMode(cm, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(cm, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(cm, BitmapScalingMode.HighQuality);

            cm.IsOpen = true;
        }
        #endregion
    }
}
