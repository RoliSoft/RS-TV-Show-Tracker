namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Dialogs;

    using RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines;
    using RoliSoft.TVShowTracker.Tables;
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
            OverviewListViewItemCollection.Clear();

                 _eps = 0;
            var ndate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddDays(1);

            NewHelp.Visibility = Database.TVShows.Count != 0 ? Visibility.Collapsed : Visibility.Visible;

            foreach (var show in Database.TVShows)
            {
                var lastep = Database.Episodes.Where(ep => ep.ShowID == show.ShowID && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderByDescending(ep => ep.Season * 1000 + ep.Number).Take(1).ToList();
                var last   = lastep.Count != 0
                           ? "S{0:00}E{1:00} · {2}".FormatWith(lastep[0].Season, lastep[0].Number, lastep[0].Name)
                           : "This show hasn't started yet.";

                var count  = Database.Episodes.Where(ep => !ep.Watched && ep.ShowID == show.ShowID && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).Count();
                     _eps += count;

                if (count == 1)
                {
                    last += " · NEW EPISODE!";
                }
                else if (count >= 2)
                {
                    last += " · " + count + " NEW EPISODES!";
                }

                var nextep = Database.Episodes.Where(ep => ep.ShowID == show.ShowID && ep.Airdate > DateTime.Now).OrderBy(ep => ep.Season * 1000 + ep.Number).Take(1).ToList();
                string next;

                if (nextep.Count != 0)
                {
                    next = "S{0:00}E{1:00} · {2} · {3}".FormatWith(nextep[0].Season, nextep[0].Number, nextep[0].Name, nextep[0].Airdate.ToRelativeDate());

                    if (nextep[0].Airdate < ndate)
                    {
                        ndate = nextep[0].Airdate;
                    }
                }
                else if (Database.ShowData(show.ShowID.ToString(), "airing") == "True")
                {
                    next = "No data available";
                }
                else
                {
                    next = "This show has ended.";
                }

                OverviewListViewItemCollection.Add(new OverviewListViewItem
                    {
                        Show        = show,
                        Name        = show.Name,
                        Title       = last,
                        Next        = next,
                        TitleColor  = count != 0
                                      ? "Red"
                                      : lastep.Count != 0
                                        ? "White"
                                        : "#50FFFFFF",
                        NextColor   = nextep.Count != 0
                                      ? "White"
                                      : "#50FFFFFF",
                        NewEpisodes = count,
                        Started     = lastep.Count != 0
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
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        private void SearchDownloadLinksClick(string show, string episode)
        {
            MainWindow.Active.tabControl.SelectedIndex = 2;
            MainWindow.Active.activeDownloadLinksPage.Search(show + " " + episode);
        }
        #endregion

        #region Search for subtitles
        /// <summary>
        /// Handles the Click event of the SearchSubtitles control.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        private void SearchSubtitlesClick(string show, string episode)
        {
            MainWindow.Active.tabControl.SelectedIndex = 3;
            MainWindow.Active.activeSubtitlesPage.Search(show + " " + episode);
        }
        #endregion

        #region Search for online videos
        /// <summary>
        /// Handles the Click event of the iTunes control.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        private void ITunesClick(string show, string episode)
        {
            var epnr = ShowNames.Parser.ExtractEpisode(episode);
            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + show + " intitle:\"season " + epnr.Season + "\" site:itunes.apple.com inurl:/tv-season/"));
        }

        /// <summary>
        /// Handles the Click event of the Amazon control.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        private void AmazonClick(string show, string episode)
        {
            var epnr = ShowNames.Parser.ExtractEpisode(episode);
            Utils.Run("http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + show + " intitle:\"Season " + epnr.Season + ", Episode " + epnr.Episode + "\" site:amazon.com"));
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
                var rowid  = Database.TVShows.First(s => s.Name == sel.Name).RowID;
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

                Database.TVShows = Database.GetTVShows();
                MainWindow.Active.DataChanged(false);
            }

            // move down

            if (listView.SelectedIndex != -1 && e.Key == Key.Down && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var sel    = (OverviewListViewItem)listView.SelectedItem;
                var rowid  = Database.TVShows.First(s => s.Name == sel.Name).RowID;
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

                Database.TVShows = Database.GetTVShows();
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
                var showid = Database.TVShows.First(s => s.Name == sel.Name).ShowID;

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
                        Synchronization.Status.Engine.RemoveShow(showid.ToString());
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
                    Database.CopyToMemory();

                    MainWindow.Active.DataChanged();
                }
            }
        }

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
            var show = (OverviewListViewItem)listView.SelectedValue;

            var spm = -55;
            var lbw = 115;

            if (show.NewEpisodes >= 2)
            {
                var dbep   = Database.Episodes.Where(ep => !ep.Watched && ep.ShowID == show.Show.ShowID && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderBy(ep => ep.Season * 1000 + ep.Number).First();
                var nextep = "S{0:00}E{1:00}".FormatWith(dbep.Season, dbep.Number);
                
                // Play next unseen episode

                var pla    = new MenuItem();
                pla.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/next.png")) };
                pla.Click += (s, r) => new FileSearchTaskDialog().Search(show.Name, nextep);
                pla.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };
                
                (pla.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Play next unseen ep.",
                        Padding = new Thickness(0),
                        Width   = lbw
                    });
                (pla.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = nextep,
                        Padding    = new Thickness(0)
                    });

                cm.Items.Add(pla);

                // Search for download links

                var sfd    = new MenuItem();
                sfd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
                sfd.Click += (s, r) => SearchDownloadLinksClick(show.Name, nextep);
                sfd.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };
                
                (sfd.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = "➥",
                        Padding    = new Thickness(0),
                        Width      = 15
                    });
                (sfd.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Download links",
                        Padding = new Thickness(0),
                        Width   = lbw - 15
                    });

                cm.Items.Add(sfd);

                // Search for subtitles

                var sfs    = new MenuItem();
                sfs.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/subtitles.png")) };
                sfs.Click += (s, r) => SearchSubtitlesClick(show.Name, nextep);
                sfs.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };
                
                (sfs.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = "➥",
                        Padding    = new Thickness(0),
                        Width      = 15
                    });
                (sfs.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Subtitles",
                        Padding = new Thickness(0),
                        Width   = lbw - 15
                    });

                cm.Items.Add(sfs);

                // Search for online videos

                var sov    = new MenuItem();
                sov.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/monitor.png")) };
                sov.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };

                (sov.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = "➥",
                        Padding    = new Thickness(0),
                        Width      = 15
                    });
                (sov.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Online videos",
                        Padding = new Thickness(0),
                        Width   = lbw - 15
                    });

                cm.Items.Add(sov);

                // - Hulu

                var hul    = new MenuItem();
                hul.Header = "Hulu";
                hul.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/hulu.png")) };
                hul.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog<Hulu>().Search(show.Name, nextep, show.Next.Substring(9));

                sov.Items.Add(hul);

                // - iPlayer

                var ipl    = new MenuItem();
                ipl.Header = "iPlayer";
                ipl.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/bbc.png")) };
                ipl.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog<BBCiPlayer>().Search(show.Name, nextep);

                sov.Items.Add(ipl);

                sov.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

                // - iTunes

                var itu    = new MenuItem();
                itu.Header = "iTunes";
                itu.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/apple.png")) };
                itu.Click += (s, r) => ITunesClick(show.Name, nextep);

                sov.Items.Add(itu);

                // - Amazon

                var amz    = new MenuItem();
                amz.Header = "Amazon Instant Video";
                amz.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/amazon.png")) };
                amz.Click += (s, r) => AmazonClick(show.Name, nextep);

                sov.Items.Add(amz);

                sov.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

                // - SideReel

                var srs    = new MenuItem();
                srs.Header = "SideReel";
                srs.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sidereel.png")) };
                srs.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog<SideReel>().Search(show.Name, nextep);

                sov.Items.Add(srs);

                // - Google search

                var gls    = new MenuItem();
                gls.Header = "Google search";
                gls.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/google.png")) };
                gls.Click += (s, r) => Utils.Run("http://www.google.com/search?q=" + Uri.EscapeUriString(show.Name + " " + nextep));

                sov.Items.Add(gls);

                cm.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            }

            if (show.Started)
            {
                var lastep = show.Title.Substring(0, 6);

                var pla    = new MenuItem();
                pla.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/play.png")) };
                pla.Click += (s, r) => new FileSearchTaskDialog().Search(show.Name, lastep);
                pla.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };
                
                (pla.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Play last aired ep.",
                        Padding = new Thickness(0),
                        Width   = lbw
                    });
                (pla.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = lastep,
                        Padding    = new Thickness(0)
                    });

                cm.Items.Add(pla);

                // Search for download links

                var sfd    = new MenuItem();
                sfd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
                sfd.Click += (s, r) => SearchDownloadLinksClick(show.Name, lastep);
                sfd.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };
                
                (sfd.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = "➥",
                        Padding    = new Thickness(0),
                        Width      = 15
                    });
                (sfd.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Download links",
                        Padding = new Thickness(0),
                        Width   = lbw - 15
                    });

                cm.Items.Add(sfd);

                // Search for subtitles

                var sfs    = new MenuItem();
                sfs.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/subtitles.png")) };
                sfs.Click += (s, r) => SearchSubtitlesClick(show.Name, lastep);
                sfs.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };
                
                (sfs.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = "➥",
                        Padding    = new Thickness(0),
                        Width      = 15
                    });
                (sfs.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Subtitles",
                        Padding = new Thickness(0),
                        Width   = lbw - 15
                    });

                cm.Items.Add(sfs);

                // Search for online videos

                var sov    = new MenuItem();
                sov.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/monitor.png")) };
                sov.Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(0, 0, spm, 0)
                    };

                (sov.Header as StackPanel).Children.Add(new Label
                    {
                        Foreground = Brushes.DarkGray,
                        Content    = "➥",
                        Padding    = new Thickness(0),
                        Width      = 15
                    });
                (sov.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Online videos",
                        Padding = new Thickness(0),
                        Width   = lbw - 15
                    });

                cm.Items.Add(sov);

                // - Hulu

                var hul    = new MenuItem();
                hul.Header = "Hulu";
                hul.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/hulu.png")) };
                hul.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog<Hulu>().Search(show.Name, lastep, Regex.Replace(show.Title.Substring(9), " · .+", string.Empty));

                sov.Items.Add(hul);

                // - iPlayer

                var ipl    = new MenuItem();
                ipl.Header = "iPlayer";
                ipl.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/bbc.png")) };
                ipl.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog<BBCiPlayer>().Search(show.Name, lastep);

                sov.Items.Add(ipl);

                sov.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

                // - iTunes

                var itu    = new MenuItem();
                itu.Header = "iTunes";
                itu.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/apple.png")) };
                itu.Click += (s, r) => ITunesClick(show.Name, lastep);

                sov.Items.Add(itu);

                // - Amazon

                var amz    = new MenuItem();
                amz.Header = "Amazon Instant Video";
                amz.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/amazon.png")) };
                amz.Click += (s, r) => AmazonClick(show.Name, lastep);

                sov.Items.Add(amz);

                sov.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

                // - SideReel

                var srs    = new MenuItem();
                srs.Header = "SideReel";
                srs.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sidereel.png")) };
                srs.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog<SideReel>().Search(show.Name, lastep);

                sov.Items.Add(srs);

                // - Google search

                var gls    = new MenuItem();
                gls.Header = "Google search";
                gls.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/google.png")) };
                gls.Click += (s, r) => Utils.Run("http://www.google.com/search?q=" + Uri.EscapeUriString(show.Name + " " + lastep));

                sov.Items.Add(gls);

                cm.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            }

            var vel    = new MenuItem();
            vel.Header = "View episode list";
            vel.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/guides.png")) };
            vel.Click += EpisodeListClick;
            cm.Items.Add(vel);
            
            TextOptions.SetTextFormattingMode(cm, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(cm, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(cm, BitmapScalingMode.HighQuality);

            cm.IsOpen = true;
        }
        #endregion
    }
}
