namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Animation;

    using Microsoft.WindowsAPICodePack.Dialogs;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;

    /// <summary>
    /// Interaction logic for OverviewPage.xaml
    /// </summary>
    public partial class OverviewPage : UserControl, IRefreshable
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

        /// <summary>
        /// Represents a TV show on the overview list view.
        /// </summary>
        public class OverviewListViewItem
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public string Next { get; set; }
            public string TitleColor { get; set; }
            public string NextColor { get; set; }
            public string MarkAsSeenVisible { get; set; }
        }

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
            Dispatcher.Invoke((Action)(() =>
                {
                    var eps = 0;

                    foreach (var show in OverviewListViewItemCollection)
                    {
                        if (show.Title.EndsWith(" · NEW EPISODE!"))
                        {
                            eps++;
                        }
                        else if (show.Title.EndsWith(" NEW EPISODES!"))
                        {
                            eps += int.Parse(Regex.Match(show.Title, " · ([0-9]*) NEW EPISODES!").Groups[1].Value);
                        }
                    }

                    if (eps == 0)
                    {
                        SetStatus("No new episodes.");
                    }
                    else
                    {
                        SetStatus(Utils.FormatNumber(eps, "new episode") + "!");
                    }
                }));
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

            var shows = Database.Query("select name, (select 'S0' || season || 'E0' || episode || ' · ' || name || '||' || showid from episodes where tvshows.showid = episodes.showid and airdate < " + Utils.DateTimeToUnix(DateTime.Now) + " and airdate != 0 order by (season * 1000 + episode) desc limit 1) as title, (select 'S0' || season || 'E0' || episode || ' · ' || name || '||' || airdate from episodes where tvshows.showid = episodes.showid and airdate > " + Utils.DateTimeToUnix(DateTime.Now) + " order by (season * 1000 + episode) asc limit 1) as next, (select value from showdata where showdata.showid = tvshows.showid and key = 'airing') as airing from tvshows order by rowid asc");

            NewHelp.Visibility = shows.Count != 0 ? Visibility.Collapsed : Visibility.Visible;

            foreach (var show in shows)
            {
                var title = show["title"].Split(new[] { "||" }, StringSplitOptions.None);

                show["title"] = Regex.Replace(title[0], @"(?=[SE][0-9]{3})([SE])0", "$1");

                var showid = title[1];
                var count  = Database.Query("select count(episodeid) as count from episodes where showid = " + showid + " and episodeid not in (select episodeid from tracking where showid = " + showid + ") and airdate < " + Utils.DateTimeToUnix(DateTime.Now) + " and airdate != 0")[0]["count"];

                if (count == "1")
                {
                    show["title"] += " · NEW EPISODE!";
                }
                else if (count != "0")
                {
                    show["title"] += " · " + count + " NEW EPISODES!";
                }

                if (show["next"] != String.Empty)
                {
                    var next = show["next"].Split(new[] { "||" }, StringSplitOptions.None);
                    show["next"] = Regex.Replace(next[0], @"(?=[SE][0-9]{3})([SE])0", "$1") + " · " +
                                   Utils.DateTimeFromUnix(double.Parse(next[1])).NextAir();
                }
                else if (bool.Parse(show["airing"]))
                {
                    show["next"] = "No data available";
                }
                else
                {
                    show["next"] = "This show has ended.";
                }

                OverviewListViewItemCollection.Add(new OverviewListViewItem
                    {
                        Name              = show["name"],
                        Title             = show["title"],
                        Next              = show["next"],
                        TitleColor        = count != "0" ? "Red" : "White",
                        NextColor         = show["next"].StartsWith("S") ? "White" : "#50FFFFFF",
                        MarkAsSeenVisible = count != "0" ? "Visible" : "Collapsed"
                    });
            }

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

            var path = Database.XmlSetting("Download Path");
            var show = GetSelectedShow();

            SetStatus("Searching for " + show[0] + " " + show[1] + " on the disk...", true);

            var finder = new FileSearch(path, show[0], show[1]);
            finder.FileSearchDone += (name, files) =>
                {
                    ResetStatus();
                    PlayEpisodeFileSearchDone(name, files);
                };
            finder.BeginSearch();
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchDone</c>.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="files">The files.</param>
        public static void PlayEpisodeFileSearchDone(string name, List<string> files)
        {
            switch (files.Count)
            {
                case 0:
                    new TaskDialog
                        {
                            Icon            = TaskDialogStandardIcon.Error,
                            Caption         = "No files found",
                            InstructionText = name,
                            Text            = "No files were found for this episode.",
                            Cancelable      = true
                        }.Show();
                    break;

                case 1:
                    Utils.Run(files.First());
                    break;

                default:
                    {
                        var td = new TaskDialog
                            {
                                Icon            = TaskDialogStandardIcon.Information,
                                Caption         = "Multiple files found",
                                InstructionText = name,
                                Text            = "Multiple files were found for this episode:",
                                Cancelable      = true,
                                StandardButtons = TaskDialogStandardButtons.Cancel
                            };

                        foreach (var file in files)
                        {
                            var tmp   = file;
                            var fd    = new TaskDialogCommandLink { Text = new FileInfo(file).Name };
                            fd.Click += (sender, e) =>
                                {
                                    td.Close();
                                    Utils.Run(tmp);
                                };

                            td.Controls.Add(fd);
                        }

                        td.Show();
                    }
                    break;
            }
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
        private void ListViewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
        /// Called when the online search is done.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="url">The URL.</param>
        public static void OnlineSearchDone(string name, string url)
        {
            Utils.Run(url);
        }

        /// <summary>
        /// Called when the online search has encountered an error.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="message">The message.</param>
        /// <param name="linkTitle">The link title.</param>
        /// <param name="linkUrl">The link URL.</param>
        /// <param name="detailed">The detailed error message.</param>
        public static void OnlineSearchError(string name, string message, string linkTitle = null, string linkUrl = null, string detailed = null)
        {
            var td = new TaskDialog
            {
                Icon            = TaskDialogStandardIcon.Error,
                Caption         = "No videos found",
                InstructionText = name,
                Text            = message,
                Cancelable      = true,
                StandardButtons = TaskDialogStandardButtons.Ok
            };

            if (!string.IsNullOrWhiteSpace(detailed))
            {
                td.DetailsExpandedText = detailed;
            }

            if (!string.IsNullOrEmpty(linkTitle))
            {
                var fd = new TaskDialogCommandLink { Text = linkTitle };
                fd.Click += (sender, e) =>
                {
                    td.Close();
                    Utils.Run(linkUrl);
                };

                td.Controls.Add(fd);
            }

            td.Show();
        }

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

            SetStatus("Searching for " + show[0] + " " + show[1] + " on Hulu...", true);

            var os = new Hulu();

            os.OnlineSearchDone += (name, url) =>
                {
                    ResetStatus();
                    OnlineSearchDone(name, url);
                };
            os.OnlineSearchError += (name, message, linkTitle, linkUrl, detailed) =>
                {
                    ResetStatus();
                    OnlineSearchError(name, message, linkTitle, linkUrl, detailed);
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

            os.OnlineSearchDone += (name, url) =>
                {
                    ResetStatus();
                    OnlineSearchDone(name, url);
                };
            os.OnlineSearchError += (name, message, linkTitle, linkUrl, detailed) =>
                {
                    ResetStatus();
                    OnlineSearchError(name, message, linkTitle, linkUrl, detailed);
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

            os.OnlineSearchDone += (name, url) =>
                {
                    ResetStatus();
                    OnlineSearchDone(name, url);
                };
            os.OnlineSearchError += (name, message, linkTitle, linkUrl, detailed) =>
                {
                    ResetStatus();
                    OnlineSearchError(name, message, linkTitle, linkUrl, detailed);
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

        #region ListView keys
        /// <summary>
        /// Handles the KeyUp event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        public void ListViewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            // move up

            if (listView.SelectedIndex != -1 && e.Key == Key.Up && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var sel    = (OverviewListViewItem)listView.SelectedItem;
                var rowid  = int.Parse(Database.Query("select rowid from tvshows where name = ?", sel.Name).First()["rowid"]);
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

                MainWindow.Active.DataChanged(false);
            }

            // move down

            if (listView.SelectedIndex != -1 && e.Key == Key.Down && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var sel    = (OverviewListViewItem)listView.SelectedItem;
                var rowid  = int.Parse(Database.Query("select rowid from tvshows where name = ?", sel.Name).First()["rowid"]);
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

                MainWindow.Active.DataChanged(false);
            }

            // add new

            if (e.Key == Key.N && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                throw new NotImplementedException();
                /*var addnew = new AddShowWindow();
                var result = addnew.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    LoadOverviewListView();
                    LoadGuideComboBox();
                }*/
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
