namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Timers;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Animation;

    using Microsoft.WindowsAPICodePack.Dialogs;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines;
    using RoliSoft.TVShowTracker.TaskDialogs;

    /// <summary>
    /// Interaction logic for OverviewPage.xaml
    /// </summary>
    public partial class OverviewPage : IRefreshable
    {
        /// <summary>
        /// Gets or sets the date when this control was loaded.
        /// </summary>
        /// <value>The load date.</value>
        public DateTime LoadDate { get; set; } 

        /// <summary>
        /// Gets or sets the overview list view item collection.
        /// </summary>
        /// <value>The overview list view item collection.</value>
        public ObservableCollection<OverviewListViewItem> OverviewListViewItemCollection { get; set; }

        private int _eps;
        private Timer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewPage"/> class.
        /// </summary>
        public OverviewPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (OverviewListViewItemCollection == null)
            {
                OverviewListViewItemCollection = new ObservableCollection<OverviewListViewItem>();
                listView.ItemsSource           = OverviewListViewItemCollection;

                _timer = new Timer { AutoReset = false };
                _timer.Elapsed += (a, b) => MainWindow.Active.DataChanged();
            }

            if (LoadDate < Database.DataChange)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Refreshes the data on this instance.
        /// </summary>
        public void Refresh()
        {
            LoadOverviewListView();

            LoadDate = DateTime.Now;
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
            Dispatcher.Invoke((Action)(() => SetStatus(_eps == 0 ? "No new episodes." : Utils.FormatNumber(_eps, "new episode") + "!")));
        }

        /// <summary>
        /// Gets the selected show name and episode on the list view.
        /// </summary>
        /// <returns>An array with the name as the first item and episode number as second.</returns>
        public string[] GetSelectedShow()
        {
            return new[]
                {
                    ((OverviewListViewItem)listView.SelectedValue).Name,
                    ((OverviewListViewItem)listView.SelectedValue).Title.Substring(0, 6)
                };
        }

        #region Loading
        /// <summary>
        /// Loads the TV shows into the list view.
        /// </summary>
        public void LoadOverviewListView()
        {
            // TODO: This function was directly copied from v1 and it's a huge mess. Rewrite it later.

            OverviewListViewItemCollection.Clear();

            var sep = '☃';

            var shows = Database.Query("select showid, name, (select season || '" + sep + "' || episode || '" + sep + "' || name from episodes where tvshows.showid = episodes.showid and airdate < " + DateTime.Now.ToUnixTimestamp() + " and airdate != 0 order by (season * 1000 + episode) desc limit 1) as lastep, (select season || '" + sep + "' || episode || '" + sep + "' || name || '" + sep + "' || airdate from episodes where tvshows.showid = episodes.showid and airdate > " + DateTime.Now.ToUnixTimestamp() + " order by (season * 1000 + episode) asc limit 1) as nextep, (select value from showdata where showdata.showid = tvshows.showid and key = 'airing') as airing from tvshows order by rowid asc");
                 _eps = 0;
            var ndate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddDays(1);

            NewHelp.Visibility = shows.Count != 0 ? Visibility.Collapsed : Visibility.Visible;

            foreach (var show in shows)
            {
                var lastep = show["lastep"].Split(sep);
                var nextep = show["nextep"].Split(sep);

                var last = string.Empty;
                var next = string.Empty;

                if (lastep.Length == 3)
                {
                    last = "S{0:00}E{1:00} · {2}".FormatWith(lastep[0].ToInteger(), lastep[1].ToInteger(), lastep[2]);
                }
                else
                {
                    last = "This show hasn't started yet.";
                }

                var count  = Database.Query("select count(episodeid) as count from episodes where showid = " + show["showid"] + " and episodeid not in (select episodeid from tracking where showid = " + show["showid"] + ") and airdate < " + DateTime.Now.ToUnixTimestamp() + " and airdate != 0")[0]["count"].ToInteger();
                     _eps += count;

                if (count == 1)
                {
                    last += " · NEW EPISODE!";
                }
                else if (count >= 2)
                {
                    last += " · " + count + " NEW EPISODES!";
                }

                if (nextep.Length == 4)
                {
                    var nair = nextep[3].ToDouble().GetUnixTimestamp();
                        next = "S{0:00}E{1:00} · {2} · {3}".FormatWith(nextep[0].ToInteger(), nextep[1].ToInteger(), nextep[2], nair.ToRelativeDate());

                    if (nair < ndate)
                    {
                        ndate = nair;
                    }
                }
                else if (show["airing"] == "True")
                {
                    next = "No data available";
                }
                else
                {
                    next = "This show has ended.";
                }

                OverviewListViewItemCollection.Add(new OverviewListViewItem
                    {
                        Name              = show["name"],
                        Title             = last,
                        Next              = next,
                        TitleColor        = count != 0
                                            ? "Red"
                                            : lastep.Length == 3
                                              ? "White"
                                              : "#50FFFFFF",
                        NextColor         = nextep.Length == 4
                                            ? "White"
                                            : "#50FFFFFF",
                        MarkAsSeenVisible = count != 0 
                                            ? "Visible"
                                            : "Collapsed",
                        PlayNextVisible   = count >= 2
                                            ? "Visible"
                                            : "Collapsed"
                    });
            }

            // set timer to the next air date

            _timer.Stop();
            _timer.Interval = (ndate - DateTime.Now).Add(TimeSpan.FromSeconds(1)).TotalMilliseconds;
            _timer.Start();

            // set status

            ResetStatus();
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

        /// <summary>
        /// Handles the Click event of the PlayNextEpisode control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PlayNextEpisodeClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var show    = GetSelectedShow();
            var showid  = Database.GetShowID(show[0]);
            var dbep    = Database.Query("select season, episode from episodes where showid = " + showid + " and episodeid not in (select episodeid from tracking where showid = " + showid + ") and airdate < " + DateTime.Now.ToUnixTimestamp() + " and airdate != 0  order by (season * 1000 + episode) asc limit 1")[0];
            var episode = "S" + dbep["season"].ToInteger().ToString("00") + "E" + dbep["episode"].ToInteger().ToString("00");

            new FileSearchTaskDialog().Search(show[0], episode);
        }
        #endregion

        #region Mark as seen
        /// <summary>
        /// Handles the Click event of the Seen control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SeenClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            try
            {
                var show      = GetSelectedShow();
                var showid    = Database.Query("select showid from tvshows where name = ? limit 1", show[0]).First()["showid"];
                var episodeid = Database.Query("select episodeid from episodes where showid = ? and season = ? and episode = ? limit 1", showid, ((OverviewListViewItem)listView.SelectedItem).Title.Substring(1, 2).TrimStart('0'), ((OverviewListViewItem)listView.SelectedItem).Title.Substring(4, 2).TrimStart('0')).First()["episodeid"];

                Database.Execute("insert into tracking values (" + showid + ", '" + episodeid + "')");

                if (Synchronization.Status.Enabled)
                {
                    Synchronization.Status.Engine.MarkEpisodes(showid, new[] { episodeid.ToInteger() - (showid.ToInteger() * 100000) }.ToList());
                }

                MainWindow.Active.DataChanged();
            }
            catch
            {
                SetStatus("Couldn't mark episode as seen due to a database error.");
            }
        }
        #endregion

        #region View episode list
        /// <summary>
        /// Handles the Click event of the EpisodeList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EpisodeListClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            MainWindow.Active.tabControl.SelectedIndex = 1;
            MainWindow.Active.activeGuidesPage.LoadShowList();
            MainWindow.Active.activeGuidesPage.SelectShow(((OverviewListViewItem)listView.SelectedValue).Name);
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            // double clicking opens the guide list, so just send this event to the one on the context menu
            EpisodeListClick(sender, e);
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
            var title = ((OverviewListViewItem)listView.SelectedValue).Title.Substring(9);
            if (title.Contains(" · "))
            {
                title = title.Substring(0, title.IndexOf(" · "));
            }

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

        #region ListView keys
        /// <summary>
        /// Handles the KeyUp event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        public void ListViewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            // move up

            if (listView.SelectedIndex != -1 && e.Key == Key.Up && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var sel    = (OverviewListViewItem)listView.SelectedItem;
                var rowid  = Database.Query("select rowid from tvshows where name = ?", sel.Name).First()["rowid"].ToInteger();
                var listid = listView.SelectedIndex;

                if (rowid == 1)
                {
                    return;
                }

                Database.Execute("update tvshows set rowid = ? where rowid = ?", Int16.MaxValue, rowid - 1);
                Database.Execute("update tvshows set rowid = ? where rowid = ?", rowid - 1, rowid);
                Database.Execute("update tvshows set rowid = ? where rowid = ?", rowid, Int16.MaxValue);

                OverviewListViewItemCollection.Move(listid, listid - 1);
                listView.SelectedIndex = listid - 1;
                listView.ScrollIntoView(listView.SelectedItem);

                if (Synchronization.Status.Enabled)
                {
                    Synchronization.Status.Engine.ReorderList();
                }

                MainWindow.Active.DataChanged(false);
            }

            // move down

            if (listView.SelectedIndex != -1 && e.Key == Key.Down && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var sel    = (OverviewListViewItem)listView.SelectedItem;
                var rowid  = Database.Query("select rowid from tvshows where name = ?", sel.Name).First()["rowid"].ToInteger();
                var listid = listView.SelectedIndex;

                if (rowid == OverviewListViewItemCollection.Count)
                {
                    return;
                }

                Database.Execute("update tvshows set rowid = ? where rowid = ?", Int16.MaxValue, rowid + 1);
                Database.Execute("update tvshows set rowid = ? where rowid = ?", rowid + 1, rowid);
                Database.Execute("update tvshows set rowid = ? where rowid = ?", rowid, Int16.MaxValue);

                OverviewListViewItemCollection.Move(listid, listid + 1);
                listView.SelectedIndex = listid + 1;
                listView.ScrollIntoView(listView.SelectedItem);

                if (Synchronization.Status.Enabled)
                {
                    Synchronization.Status.Engine.ReorderList();
                }

                MainWindow.Active.DataChanged(false);
            }

            // add new

            if (e.Key == Key.N && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                new AddNewWindow().ShowDialog();
            }

            // remove

            if (listView.SelectedIndex != -1 && e.Key == Key.Delete)
            {
                var sel    = (OverviewListViewItem)listView.SelectedItem;
                var showid = Database.GetShowID(sel.Name);

                var td = new TaskDialog
                    {
                        Caption         = "Remove " + sel.Name,
                        Icon            = TaskDialogStandardIcon.Warning,
                        InstructionText = sel.Name,
                        Text            = "Are you sure you want to remove " + sel.Name + " from the database?\r\n\r\nYou should still leave TV shows on your list, even if they ended or are on some kind of pause.",
                        StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No
                    };

                if(td.Show() == TaskDialogResult.Yes)
                {
                    if (Synchronization.Status.Enabled)
                    {
                        Synchronization.Status.Engine.RemoveShow(showid);
                    }

                    Database.Execute("delete from tvshows where showid = ?", showid);
                    Database.Execute("delete from showdata where showid = ?", showid);
                    Database.Execute("delete from episodes where showid = ?", showid);
                    Database.Execute("delete from tracking where showid = ?", showid);

                    Database.Execute("update tvshows set rowid = rowid * -1");

                    var tr = Database.Connection.BeginTransaction();

                    var shows = Database.Query("select showid from tvshows order by rowid desc");
                    var i = 1;
                    foreach (var show in shows)
                    {
                        Database.ExecuteOnTransaction(tr, "update tvshows set rowid = ? where showid = ?", i, show["showid"]);
                        i++;
                    }

                    tr.Commit();

                    Database.Execute("vacuum;");

                    MainWindow.Active.DataChanged();
                }
            }
        }
        #endregion
    }
}
