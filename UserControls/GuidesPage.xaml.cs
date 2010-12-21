namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;

    /// <summary>
    /// Interaction logic for GuidesPage.xaml
    /// </summary>
    public partial class GuidesPage : UserControl, IRefreshable
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
        
        /// <summary>
        /// Represents a TV show episode on the list view.
        /// </summary>
        public class GuideListViewItem
        {
            public bool SeenIt { get; set; }
            public string Id { get; set; }
            public string Episode { get; set; }
            public string AirDate { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string Picture { get; set; }

            public bool ShowTooltip
            {
                get
                {
                    return !string.IsNullOrWhiteSpace(Summary) || !string.IsNullOrWhiteSpace(Picture);
                }
            }

            public string ShowSummary
            {
                get
                {
                    return string.IsNullOrWhiteSpace(Summary) ? "Hidden" : "Visible";
                }
            }

            public string ShowPicture
            {
                get
                {
                    return string.IsNullOrWhiteSpace(Picture) ? "Hidden" : "Visible";
                }
            }
        }

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
                listView.ItemsSource        = GuideListViewItemCollection;
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
            comboBox.ItemsSource = Database.Query("select name from tvshows order by rowid asc").Select(r => r["name"]).ToList();
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
        private void ComboBoxDropDownOpened(object sender, System.EventArgs e)
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
        private void ComboBoxDropDownClosed(object sender, System.EventArgs e)
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

            var id = Database.Query("select showid from tvshows where name = ? limit 1", comboBox.SelectedValue.ToString())[0]["showid"];

            // fill up general informations

            var airing = bool.Parse(Database.ShowData(id, "airing"));

            var url = Database.ShowData(id, "cover");
            var pic = !string.IsNullOrWhiteSpace(url)
                      ? new Uri(Utils.Coralify(url))
                      : new Uri("/RSTVShowTracker;component/Images/cd.png", UriKind.Relative);
            showGeneralCover.Source = new BitmapImage(pic, new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable));
            
            /*
            var tvdb = Database.ShowData(id, "TVDB.id");
            if (string.IsNullOrWhiteSpace(tvdb))
            {
                try
                {
                    tvdb = new TVDB().GetID(comboBox.SelectedValue.ToString());
                    Database.ShowData(id, "TVDB.id", tvdb);
                } catch { }
            }
            showGeneralCover.Source = string.IsNullOrWhiteSpace(tvdb)
                                      ? new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cd.png", UriKind.Relative))
                                      : new BitmapImage(new Uri(Utils.Coralify("http://thetvdb.com/banners/_cache/posters/" + tvdb + "-1.jpg")), new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable));
            */
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

            showGeneralSub2.Text = "Airs";
            string airday;
            if ((airday = Database.ShowData(id, "airday")) != string.Empty)
            {
                showGeneralSub2.Text += " " + airday;
            }

            string airtime;
            if ((airtime = Database.ShowData(id, "airtime")) != string.Empty)
            {
                showGeneralSub2.Text += " at " + airtime;
            }

            string network;
            if ((network = Database.ShowData(id, "network")) != string.Empty)
            {
                showGeneralSub2.Text += " on " + network;
            }

            if ((airday + airtime + network) == string.Empty)
            {
                showGeneralSub2.Text = string.Empty;
            }

            showGeneralActors.Text = string.Empty;
            string actors;
            if ((actors = Database.ShowData(id, "actors")) != string.Empty)
            {
                showGeneralActors.Text = actors;
            }

            showGeneralDescr.Text = string.Empty;
            string descr;
            if ((descr = Database.ShowData(id, "descr")) != string.Empty)
            {
                showGeneralDescr.Text = descr;
            }

            try
            {
                var last = Database.Query("select name, airdate from episodes where showid = ? and airdate < ? and airdate != 0 order by (season * 1000 + episode) desc limit 1", id, Utils.DateTimeToUnix(DateTime.Now));
                var next = Database.Query("select name, airdate from episodes where showid = ? and airdate > ? order by (season * 1000 + episode) asc limit 1", id, Utils.DateTimeToUnix(DateTime.Now));

                showGeneralLast.Text = last.Count != 0 ? last[0]["name"] : string.Empty;
                showGeneralNext.Text = next.Count != 0 ? next[0]["name"] : string.Empty;

                showGeneralLastDate.Text = last.Count != 0 ? Utils.DateTimeFromUnix(double.Parse(last[0]["airdate"])).NextAir(true) : "no data available";
                showGeneralNextDate.Text = next.Count != 0 ? Utils.DateTimeFromUnix(double.Parse(next[0]["airdate"])).NextAir(true) : airing ? "no data available" : "this show has ended";
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

            var shows = Database.Query("select (select tracking.episodeid from tracking where tracking.episodeid = episodes.episodeid) as seenit, (showid || '|' || episodeid) as id, 'S0' || season || 'E0' || episode as notation, airdate, name, descr, pic from episodes where showid = ? order by (season * 1000 + episode) desc", id);

            foreach (var show in shows)
            {
                GuideListViewItemCollection.Add(new GuideListViewItem
                    {
                        SeenIt  = show["seenit"] != String.Empty,
                        Id      = show["id"],
                        Episode = Regex.Replace(show["notation"], @"(?=[SE][0-9]{3})([SE])0", "$1"),
                        AirDate = show["airdate"] != "0"
                                  ? Utils.DateTimeFromUnix(double.Parse(show["airdate"])).ToString("MMMM d, yyyy", new CultureInfo("en-US")) + (Utils.DateTimeFromUnix(double.Parse(show["airdate"])) > DateTime.Now ? "*" : string.Empty)
                                  : "Unaired episode",
                        Title   = show["name"],
                        Summary = show["descr"],
                        Picture = show["pic"]
                    });
            }

            //var guide = Database.Query("select guide from tvshows where name = ?", comboBox.SelectedValue.ToString())[0];
            //var name = Guide.ToString(guide["guide"]);
            //epgSourceLogo.Text = (name != String.Empty ? "© " : String.Empty) + name;
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

            var path = Settings.Get("Download Path");
            var show = GetSelectedShow();

            SetStatus("Searching for " + show[0] + " " + show[1] + " on the disk...", true);

            var finder = new FileSearch(path, show[0], show[1]);
            finder.FileSearchDone += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.PlayEpisodeFileSearchDone(sender2, e2);
                };
            finder.BeginSearch();
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

            SetStatus("Searching for " + show[0] + " " + show[1] + " on Hulu...", true);

            var os = new Hulu();

            os.OnlineSearchDone += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.OnlineSearchDone(sender2, e2);
                };
            os.OnlineSearchError += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.OnlineSearchError(sender2, e2);
                };

            os.SearchAsync(show[0], show[1], title);
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

            SetStatus("Searching for " + show[0] + " " + show[1] + " on iPlayer...", true);

            var os = new BBCiPlayer();
            
            os.OnlineSearchDone += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.OnlineSearchDone(sender2, e2);
                };
            os.OnlineSearchError += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.OnlineSearchError(sender2, e2);
                };

            os.SearchAsync(show[0], show[1]);
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

            SetStatus("Searching for " + show[0] + " " + show[1] + " on SideReel...", true);

            var os = new SideReel();
            
            os.OnlineSearchDone += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.OnlineSearchDone(sender2, e2);
                };
            os.OnlineSearchError += (sender2, e2) =>
                {
                    ResetStatus();
                    OverviewPage.OnlineSearchError(sender2, e2);
                };

            os.SearchAsync(show[0], show[1]);
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
    }
}
