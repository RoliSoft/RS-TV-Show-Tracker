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
                        ShowID      = show["showid"],
                        Name        = show["name"],
                        Title       = last,
                        Next        = next,
                        TitleColor  = count != 0
                                      ? "Red"
                                      : lastep.Length == 3
                                        ? "White"
                                        : "#50FFFFFF",
                        NextColor   = nextep.Length == 4
                                      ? "White"
                                      : "#50FFFFFF",
                        NewEpisodes = count,
                        Started     = lastep.Length == 3
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
            var lbw = 160;

            if (show.NewEpisodes >= 2)
            {
                var dbep   = Database.Query("select season, episode from episodes where showid = " + show.ShowID + " and episodeid not in (select episodeid from tracking where showid = " + show.ShowID + ") and airdate < " + DateTime.Now.ToUnixTimestamp() + " and airdate != 0  order by (season * 1000 + episode) asc limit 1")[0];
                var nextep = "S{0:00}E{1:00}".FormatWith(dbep["season"].ToInteger(), dbep["episode"].ToInteger());

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
                        Content = "Play next unseen episode",
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
                        Content = "Search for download links",
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
                        Content = "Search for subtitles",
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
                        Content = "Search for online videos",
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
                        Content = "Play last aired episode",
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
                        Content = "Search for download links",
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
                        Content = "Search for subtitles",
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
                        Content = "Search for online videos",
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
