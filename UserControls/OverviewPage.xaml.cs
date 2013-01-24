namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.ContextMenus;
    using RoliSoft.TVShowTracker.ContextMenus.Menus;
    using RoliSoft.TVShowTracker.Dependencies.GreyableImage;
    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;
    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.TaskDialogs;

    using Timer = System.Timers.Timer;

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
        /// Gets a list of sort/group pairs that work well together.
        /// </summary>
        public List<string[]> SortGroupPairs = new List<string[]>
            {
                new[]
                    {
                        "Alphabetical sort with no grouping",
                        "name", string.Empty, "ascending"
                    },
                new[]
                    {
                        "Recently aired episodes sorted by air date",
                        "lastair", "unseen", "descending"
                    },
                new[]
                    {
                        "Recently aired episodes sorted alphabetically",
                        "lastair", "name", "ascending"
                    },
                new[]
                    {
                        "Upcoming episodes sorted by air date",
                        "nextair", "upcoming", "ascending"
                    },
            };

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
                ((CollectionViewSource)FindResource("cvs")).Source = OverviewListViewItemCollection;

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

        #region Loading
        /// <summary>
        /// Loads the TV shows into the list view.
        /// </summary>
        public void LoadOverviewListView()
        {
            OverviewListViewItemCollection.Clear();
            ((CollectionViewSource)FindResource("cvs")).SortDescriptions.Clear();
            ((CollectionViewSource)FindResource("cvs")).GroupDescriptions.Clear();

            var sorting  = Settings.Get("Sorting");
            var sortdir  = Settings.Get("Sort Direction");
            var grouping = Settings.Get("Grouping");
            var hideend  = Settings.Get<bool>("Hide Ended");
            var fadeend  = Settings.Get<bool>("Fade Ended");

            if (!string.IsNullOrWhiteSpace(grouping))
            {
                ((CollectionViewSource)FindResource("cvs")).GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            }

            var items = new List<OverviewListViewItem>();
            var ndate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddDays(1);
                 _eps = 0;

            foreach (var show in Database.TVShows.Values)
            {
                // last episode
                
                var count  = show.Episodes.Count(ep => !ep.Watched && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch);
                     _eps += count;
                
                if (hideend && !show.Airing && count == 0)
                {
                    continue;
                }

                var lastep = show.Episodes.Where(ep => ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderByDescending(ep => ep.ID).Take(1).ToList();
                var last   = lastep.Count != 0
                           ? "S{0:00}E{1:00} · {2}".FormatWith(lastep[0].Season, lastep[0].Number, lastep[0].Name)
                           : "This show hasn't started yet.";

                if (count == 1)
                {
                    last += " · NEW EPISODE!";
                }
                else if (count >= 2)
                {
                    last += " · " + count + " NEW EPISODES!";
                }

                // next episode

                var nextep = show.Episodes.Where(ep => ep.Airdate > DateTime.Now).OrderBy(ep => ep.ID).Take(1).ToList();
                string next;

                if (nextep.Count != 0)
                {
                    next = "S{0:00}E{1:00} · {2} · {3}".FormatWith(nextep[0].Season, nextep[0].Number, nextep[0].Name, nextep[0].Airdate.ToRelativeDate());

                    if (nextep[0].Airdate < ndate)
                    {
                        ndate = nextep[0].Airdate;
                    }
                }
                else if (show.Airing)
                {
                    var epid = lastep.Count != 0
                             ? lastep[0].ID + 1
                             : 0;

                    nextep = show.Episodes.Where(ep => ep.ID >= epid).OrderBy(ep => ep.ID).Take(1).ToList();

                    if (nextep.Count != 0)
                    {
                        next = "S{0:00}E{1:00} · {2}".FormatWith(nextep[0].Season, nextep[0].Number, string.IsNullOrWhiteSpace(nextep[0].Name) ? "TBA" : nextep[0].Name);
                    }
                    else
                    {
                        next = "No data available";
                    }
                }
                else
                {
                    next = "This show has ended.";
                }

                // sort

                var sort = (object)null;

                switch (sorting)
                {
                    case "name":
                        sort = show.Name;
                        break;

                    case "epcount":
                        sort = show.Episodes.Count();
                        break;

                    case "lastair":
                        if (lastep.Count != 0)
                        {
                            sort = lastep[0].Airdate;
                        }
                        else
                        {
                            sort = DateTime.MinValue;
                        }
                        break;

                    case "nextair":
                        if (nextep.Count != 0)
                        {
                            sort = nextep[0].Airdate;
                        }
                        else
                        {
                            sort = DateTime.MaxValue;
                        }
                        break;
                }

                // group

                var group = string.Empty;
                var grpri = int.MaxValue;

                switch (grouping)
                {
                    case "network":
                        group = show.Network;

                        if (string.IsNullOrWhiteSpace(show.Network))
                        {
                            group = "Syndicated";
                        }

                        if (string.IsNullOrWhiteSpace(group) || group == "Syndicated")
                        {
                            group = "Unknown";
                        }

                        grpri = group[0];
                        break;

                    case "airday":
                        var ep = (Episode)null;

                        if (lastep.Count != 0)
                        {
                            ep = lastep[0];
                        }
                        else if (show.Episodes.Count() != 0)
                        {
                            ep = show.Episodes.First();
                        }

                        if (ep != null)
                        {
                            group = ep.Airdate.DayOfWeek.ToString();
                            grpri = (int)ep.Airdate.DayOfWeek;
                        }
                        else
                        {
                            group = "Unknown";
                        }
                        break;

                    case "upcoming":
                        if (nextep.Count != 0)
                        {
                            group = nextep[0].Airdate.ToShortRelativeDate();
                            grpri = nextep[0].Airdate.ToRelativeDatePriority();
                        }
                        else if (show.Airing)
                        {
                            group  = "Unknown";
                            grpri -= 1;
                        }
                        else
                        {
                            group = "Never";
                        }
                        break;

                    case "unseen":
                        if (count != 0)
                        {
                            if ((DateTime.Now - lastep[0].Airdate).TotalDays < 14)
                            {
                                group = "Recently aired new episodes";
                                grpri = 0;
                            }
                            else
                            {
                                group = "New episodes";
                                grpri = 1;
                            }
                        }
                        else
                        {
                            group = "No new episodes";
                        }
                        break;
                }

                // insertion

                items.Add(new OverviewListViewItem
                    {
                        Show          = show,
                        Name          = show.Name,
                        Title         = last,
                        Next          = next,
                        NameColor     = fadeend && !show.Airing ? "#50FFFFFF" : "White",
                        TitleColor    = count != 0 ? "Red" : (lastep.Count != 0 ? (fadeend && !show.Airing ? "#50FFFFFF" : "White") : "#50FFFFFF"),
                        NextColor     = nextep.Count != 0 ? (fadeend && !show.Airing ? "#50FFFFFF" : "White") : "#50FFFFFF",
                        NewEpisodes   = count,
                        Started       = lastep.Count != 0,
                        Group         = group,
                        GroupPriority = grpri,
                        Sort          = sort
                    });
            }

            // sort items

            var eitems = items as IEnumerable<OverviewListViewItem>;

            if (!string.IsNullOrWhiteSpace(grouping))
            {
                eitems = eitems.OrderBy(i => i.GroupPriority);
            }

            if (!string.IsNullOrWhiteSpace(sorting))
            {
                if (eitems is IOrderedEnumerable<OverviewListViewItem>)
                {
                    if (sortdir == "descending")
                    {
                        eitems = (eitems as IOrderedEnumerable<OverviewListViewItem>).ThenByDescending(i => i.Sort);
                    }
                    else
                    {
                        eitems = (eitems as IOrderedEnumerable<OverviewListViewItem>).ThenBy(i => i.Sort);
                    }
                }
                else
                {
                    if (sortdir == "descending")
                    {
                        eitems = eitems.OrderByDescending(i => i.Sort);
                    }
                    else
                    {
                        eitems = eitems.OrderBy(i => i.Sort);
                    }
                }
            }

            // insert from 'items' to 'OverviewListViewItemCollection'

            OverviewListViewItemCollection.AddRange(eitems);

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
            MainWindow.Active.activeGuidesPage.SelectShow(((OverviewListViewItem)listView.SelectedValue).Show);
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

        #region ListView manipulation
        /// <summary>
        /// Moves the selected show up in the list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void MoveUpShow(object sender = null, EventArgs e = null)
        {
            if (listView.SelectedIndex == -1 || !string.IsNullOrEmpty(Settings.Get("Sorting")) || !string.IsNullOrEmpty(Settings.Get("Grouping")))
            {
                return;
            }
            
            var sel    = (OverviewListViewItem)listView.SelectedItem;
            var rowid  = Database.TVShows.Values.First(s => s.Name == sel.Name).RowID;
            var listid = listView.SelectedIndex;

            if (rowid == 1)
            {
                return;
            }

            Database.TVShows.First(x => x.Value.RowID == rowid - 1).Value.RowID = Int16.MaxValue;
            Database.TVShows.First(x => x.Value.RowID == rowid).Value.RowID = rowid - 1;
            Database.TVShows.First(x => x.Value.RowID == Int16.MaxValue).Value.RowID = rowid;

            Database.TVShows.First(x => x.Value.RowID == rowid - 1).Value.SaveData();
            Database.TVShows.First(x => x.Value.RowID == rowid).Value.SaveData();

            OverviewListViewItemCollection.Move(listid, listid - 1);
            listView.SelectedIndex = listid - 1;
            listView.ScrollIntoView(listView.SelectedItem);

            MainWindow.Active.DataChanged(false);
        }

        /// <summary>
        /// Moves the selected show down in the list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void MoveDownShow(object sender = null, EventArgs e = null)
        {
            if (listView.SelectedIndex == -1 || !string.IsNullOrEmpty(Settings.Get("Sorting")) || !string.IsNullOrEmpty(Settings.Get("Grouping")))
            {
                return;
            }
            
            var sel    = (OverviewListViewItem)listView.SelectedItem;
            var rowid  = Database.TVShows.Values.First(s => s.Name == sel.Name).RowID;
            var listid = listView.SelectedIndex;

            if (rowid == OverviewListViewItemCollection.Count)
            {
                return;
            }

            Database.TVShows.First(x => x.Value.RowID == rowid + 1).Value.RowID = Int16.MaxValue;
            Database.TVShows.First(x => x.Value.RowID == rowid).Value.RowID = rowid + 1;
            Database.TVShows.First(x => x.Value.RowID == Int16.MaxValue).Value.RowID = rowid;

            Database.TVShows.First(x => x.Value.RowID == rowid + 1).Value.SaveData();
            Database.TVShows.First(x => x.Value.RowID == rowid).Value.SaveData();

            OverviewListViewItemCollection.Move(listid, listid + 1);
            listView.SelectedIndex = listid + 1;
            listView.ScrollIntoView(listView.SelectedItem);

            MainWindow.Active.DataChanged(false);
        }

        /// <summary>
        /// Removes the selected show from the list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void RemoveShow(object sender = null, EventArgs e = null)
        {
            if (listView.SelectedIndex == -1)
            {
                return;
            }
            
            var sel = (OverviewListViewItem)listView.SelectedItem;

            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon        = VistaTaskDialogIcon.Warning,
                    Title           = "Remove " + sel.Name,
                    MainInstruction = sel.Name,
                    Content         = "Are you sure you want to remove " + sel.Name + " from the database?\r\n\r\nYou should still leave TV shows on your list, even if they ended or are on some kind of pause.",
                    CustomButtons   = new[] { "Yes", "No" }
                });

            if (res.CustomButtonResult.HasValue && res.CustomButtonResult.Value == 0)
            {
                Database.Remove(sel.Show);
                MainWindow.Active.DataChanged();
            }
        }

        /// <summary>
        /// Configures the selected show on the list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void ConfigureShow(object sender = null, EventArgs e = null)
        {
            if (listView.SelectedIndex == -1)
            {
                return;
            }

            var sel = (OverviewListViewItem)listView.SelectedItem;

            new EditShowWindow(sel.Show.ID, sel.Show.Title).ShowDialog();
        }

        /// <summary>
        /// Updates the selected show on the list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void UpdateShow(object sender = null, EventArgs e = null)
        {
            if (listView.SelectedIndex == -1)
            {
                return;
            }

            var sel = ((OverviewListViewItem)listView.SelectedItem).Show;
            
            new Thread(() => Database.Update(sel, (i, s) =>
                {
                    switch (i)
                    {
                        case 0:
                            SetStatus(s, true);
                            break;

                        case -1:
                            SetStatus(s);
                            break;

                        case 1:
                            MainWindow.Active.DataChanged();
                            break;
                    }
                })).Start();
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
                MoveUpShow();
            }

            // move down

            if (listView.SelectedIndex != -1 && e.Key == Key.Down && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                MoveDownShow();
            }

            // add new

            if (e.Key == Key.N && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                new AddNewWindow().ShowDialog();
            }

            // remove

            if (listView.SelectedIndex != -1 && e.Key == Key.Delete)
            {
                RemoveShow();
            }
        }
        
        /// <summary>
        /// Handles the Click event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ListViewSortClick(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (header == null || header.Role == GridViewColumnHeaderRole.Padding)
            {
                return;
            }

            switch (header.Content.ToString())
            {
                case "Show name":
                    Settings.Set("Sorting", "name");
                    Settings.Set("Sort Direction", "ascending");
                    break;

                case "Last episode":
                    Settings.Set("Sorting", "lastair");
                    Settings.Set("Sort Direction", "descending");
                    break;

                case "Next episode":
                    Settings.Set("Sorting", "nextair");
                    Settings.Set("Sort Direction", "ascending");
                    break;

                default:
                    return;
            }

            Refresh();
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
                var dbep   = show.Show.Episodes.Where(ep => !ep.Watched && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderBy(ep => ep.ID).First();
                var nextnt = dbep.Show.Data.Get("notation") == "airdate";
                var nextdt = dbep.Airdate.ToOriginalTimeZone(dbep.Show.TimeZone).ToString("yyyy.MM.dd");
                var nextep = "S{0:00}E{1:00}".FormatWith(dbep.Season, dbep.Number);
                
                // Play next unseen episode

                var pla    = new MenuItem();
                pla.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/next.png")) };
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
                        Padding    = new Thickness(0, 1, 15, 0)
                    });

                cm.Items.Add(pla);

                // - Files

                if (Signature.IsActivated)
                {
                    List<string> nextfn;
                    if (Library.Files.TryGetValue(dbep.ID, out nextfn) && nextfn.Count != 0)
                    {
                        // Open folder

                        var opf    = new MenuItem();
                        opf.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/folder-open-film.png")) };
                        opf.Header = new StackPanel
                                         {
                                             Orientation = Orientation.Horizontal,
                                             Margin      = new Thickness(0, 0, spm, 0)
                                         };

                        (opf.Header as StackPanel).Children.Add(new Label
                                                                    {
                                                                        Foreground = Brushes.DarkGray,
                                                                        Content    = "➥",
                                                                        Padding    = new Thickness(0),
                                                                        Width      = 15
                                                                    });
                        (opf.Header as StackPanel).Children.Add(new Label
                                                                    {
                                                                        Content = "Open folder",
                                                                        Padding = new Thickness(0),
                                                                        Width   = lbw - 15
                                                                    });
                        cm.Items.Add(opf);

                        foreach (var file in nextfn.OrderByDescending(FileNames.Parser.ParseQuality))
                        {
                            BitmapSource bmp;

                            try
                            {
                                var ext = Path.GetExtension(file);

                                if (string.IsNullOrWhiteSpace(ext))
                                {
                                    throw new Exception();
                                }

                                var ico = Utils.Icons.GetFileIcon(ext, Utils.Icons.SHGFI_SMALLICON);

                                if (ico == null || ico.Handle == IntPtr.Zero)
                                {
                                    throw new Exception();
                                }

                                bmp = Imaging.CreateBitmapSourceFromHBitmap(ico.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            }
                            catch (Exception)
                            {
                                bmp = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/film-timeline.png"));
                            }

                            var plf    = new MenuItem();
                            plf.Icon   = new Image { Source = bmp, Height = 16, Width = 16 };
                            plf.Tag    = file;
                            plf.Header = Path.GetFileName(file);
                            plf.Click += (s, r) => Utils.Run((string)((MenuItem)s).Tag);

                            pla.Items.Add(plf);

                            var off    = new MenuItem();
                            off.Icon   = new Image { Source = bmp, Height = 16, Width = 16 };
                            off.Tag    = file;
                            off.Header = Path.GetFileName(file);
                            off.Click += (s, r) => Utils.Run("explorer.exe", "/select,\"" + (string)((MenuItem)s).Tag + "\"");

                            opf.Items.Add(off);
                        }
                    }
                    else
                    {
                        ((Image)pla.Icon).SetValue(ImageGreyer.IsGreyableProperty, true);
                        pla.IsEnabled = false;
                    }
                }
                else
                {
                    pla.Click += (s, r) => new FileSearchTaskDialog().Search(dbep);
                }

                // Search for download links

                var sfd    = new MenuItem();
                sfd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
                sfd.Click += (s, r) => SearchDownloadLinksClick(show.Name, nextnt ? nextdt : nextep);
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
                sfs.Click += (s, r) => SearchSubtitlesClick(show.Name, nextnt ? nextdt : nextep);
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
                    ovmi.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog((OnlineVideoSearchEngine)ovmi.Tag).Search(dbep);
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
                gls.Click += (s, r) => Utils.Run("http://www.google.com/search?q=" + Utils.EncodeURL(show.Name + " " + (nextnt ? nextdt : nextep)));

                sov.Items.Add(gls);

                cm.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            }

            if (show.Started)
            {
                var dbep2  = show.Show.Episodes.Where(ep => ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch).OrderByDescending(ep => ep.ID).First();
                var lastnt = dbep2.Show.Data.Get("notation") == "airdate";
                var lastdt = dbep2.Airdate.ToOriginalTimeZone(dbep2.Show.TimeZone).ToString("yyyy.MM.dd");
                var lastep = "S{0:00}E{1:00}".FormatWith(dbep2.Season, dbep2.Number);
                
                // Play last aired episode

                var pla    = new MenuItem();
                pla.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/play.png")) };
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
                        Padding    = new Thickness(0, 1, 15, 0)
                    });

                cm.Items.Add(pla);
                
                // - Files
                if (Signature.IsActivated)
                {
                    List<string> lastfn;
                    if (Library.Files.TryGetValue(dbep2.ID, out lastfn) && lastfn.Count != 0)
                    {
                        // Open folder

                        var opf    = new MenuItem();
                        opf.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/folder-open-film.png")) };
                        opf.Header = new StackPanel
                                         {
                                             Orientation = Orientation.Horizontal,
                                             Margin      = new Thickness(0, 0, spm, 0)
                                         };

                        (opf.Header as StackPanel).Children.Add(new Label
                                                                    {
                                                                        Foreground = Brushes.DarkGray,
                                                                        Content    = "➥",
                                                                        Padding    = new Thickness(0),
                                                                        Width      = 15
                                                                    });
                        (opf.Header as StackPanel).Children.Add(new Label
                                                                    {
                                                                        Content = "Open folder",
                                                                        Padding = new Thickness(0),
                                                                        Width   = lbw - 15
                                                                    });
                        cm.Items.Add(opf);

                        foreach (var file in lastfn.OrderByDescending(FileNames.Parser.ParseQuality))
                        {
                            BitmapSource bmp;

                            try
                            {
                                var ext = Path.GetExtension(file);

                                if (string.IsNullOrWhiteSpace(ext))
                                {
                                    throw new Exception();
                                }

                                var ico = Utils.Icons.GetFileIcon(ext, Utils.Icons.SHGFI_SMALLICON);

                                if (ico == null || ico.Handle == IntPtr.Zero)
                                {
                                    throw new Exception();
                                }

                                bmp = Imaging.CreateBitmapSourceFromHBitmap(ico.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            }
                            catch (Exception)
                            {
                                bmp = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/film-timeline.png"));
                            }

                            var plf    = new MenuItem();
                            plf.Icon   = new Image { Source = bmp, Height = 16, Width = 16 };
                            plf.Tag    = file;
                            plf.Header = Path.GetFileName(file);
                            plf.Click += (s, r) => Utils.Run((string)((MenuItem)s).Tag);

                            pla.Items.Add(plf);

                            var off    = new MenuItem();
                            off.Icon   = new Image { Source = bmp, Height = 16, Width = 16 };
                            off.Tag    = file;
                            off.Header = Path.GetFileName(file);
                            off.Click += (s, r) => Utils.Run("explorer.exe", "/select,\"" + (string)((MenuItem)s).Tag + "\"");

                            opf.Items.Add(off);
                        }
                    }
                    else
                    {
                        ((Image)pla.Icon).SetValue(ImageGreyer.IsGreyableProperty, true);
                        pla.IsEnabled = false;
                    }
                }
                else
                {
                    pla.Click += (s, r) => new FileSearchTaskDialog().Search(dbep2);
                }

                // Search for download links

                var sfd    = new MenuItem();
                sfd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
                sfd.Click += (s, r) => SearchDownloadLinksClick(show.Name, lastnt ? lastdt : lastep);
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
                sfs.Click += (s, r) => SearchSubtitlesClick(show.Name, lastnt ? lastdt : lastep);
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
                    ovmi.Click += (s, r) => new OnlineVideoSearchEngineTaskDialog((OnlineVideoSearchEngine)ovmi.Tag).Search(dbep2);
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
                gls.Click += (s, r) => Utils.Run("http://www.google.com/search?q=" + Utils.EncodeURL(show.Name + " " + (lastnt ? lastdt : lastep)));

                sov.Items.Add(gls);

                cm.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            }

            // View episode list

            var vel    = new MenuItem();
            vel.Header = "View episode list";
            vel.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/guides.png")) };
            vel.Click += EpisodeListClick;
            cm.Items.Add(vel);

            // Plugins

            cm.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

            var z = 0;
            foreach (var ovcm in Extensibility.GetNewInstances<OverviewContextMenu>())
            {
                foreach (var ovcmi in ovcm.GetMenuItems(show.Show))
                {
                    var cmi    = new MenuItem();
                    cmi.Tag    = ovcmi;
                    cmi.Header = ovcmi.Name;
                    cmi.Icon   = ovcmi.Icon;
                    cmi.Click += (s, r) => ((ContextMenuItem<TVShow>)cmi.Tag).Click(show.Show);
                    cm.Items.Add(cmi);

                    z++;
                }
            }

            if (z != 0)
            {
                cm.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            }

            // Settings

            BuildSettingsMenu(cm);

            TextOptions.SetTextFormattingMode(cm, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(cm, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(cm, BitmapScalingMode.HighQuality);

            cm.IsOpen = true;
        }

        /// <summary>
        /// Builds the settings menu.
        /// </summary>
        /// <param name="cm">The context menu.</param>
        public void BuildSettingsMenu(ContextMenu cm)
        {
            var spm = -55;
            var lbw = 135;

            var sorting  = Settings.Get("Sorting", string.Empty);
            var sortdir  = Settings.Get("Sort Direction", string.Empty);
            var grouping = Settings.Get("Grouping", string.Empty);
            var hideend  = Settings.Get<bool>("Hide Ended");
            var fadeend  = Settings.Get<bool>("Fade Ended");

            // ../Settings

            var sti    = new MenuItem();
            sti.Header = "Settings";
            sti.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/settings.png")) };
            cm.Items.Add(sti);

            // Sort by

            var srt    = new MenuItem();
            srt.Header = "Sort by";
            srt.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/order.png")) };
            sti.Items.Add(srt);

            // - Custom

            var mrt         = new MenuItem();
            mrt.IsCheckable = true;
            mrt.IsChecked   = string.IsNullOrEmpty(sorting);
            mrt.Header      = "Custom";
            mrt.Tag         = new[] { string.Empty, string.Empty };
            mrt.Click      += SortBy_Click;
            mrt.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-pencil.png")) };
            srt.Items.Add(mrt);

            // --

            srt.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

            // - Alphabetical

            var alp         = new MenuItem();
            alp.IsCheckable = true;
            alp.IsChecked   = sorting == "name";
            alp.Tag         = new[] { "name", "ascending" };
            alp.Click      += SortBy_Click;
            alp.Header      = "Alphabetical";
            alp.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-alphabet.png")) };
            srt.Items.Add(alp);
            
            // - Last episode's date
            
            var dle         = new MenuItem();
            dle.IsCheckable = true;
            dle.IsChecked   = sorting == "lastair";
            dle.Tag         = new[] { "lastair", "descending" };
            dle.Click      += SortBy_Click;
            dle.Header      = "Last episode's date";
            dle.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-date.png")) };
            srt.Items.Add(dle);
            
            // - Next episode's date
            
            var dne         = new MenuItem();
            dne.IsCheckable = true;
            dne.IsChecked   = sorting == "nextair";
            dne.Tag         = new[] { "nextair", "ascending" };
            dne.Click      += SortBy_Click;
            dne.Header      = "Next episode's date";
            dne.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-date-descending.png")) };
            srt.Items.Add(dne);
            
            // - Episode count / DESC

            var epd         = new MenuItem();
            epd.IsCheckable = true;
            epd.IsChecked   = sorting == "epcount" && sortdir == "desc";
            epd.Tag         = new[] { "epcount", "descending" };
            epd.Click      += SortBy_Click;
            epd.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-number-descending.png")) };
            epd.Header      = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin      = new Thickness(0, 0, spm, 0)
                };
            (epd.Header as StackPanel).Children.Add(new Label
                {
                    Content = "Episode count",
                    Padding = new Thickness(0),
                    Width   = lbw
                });
            (epd.Header as StackPanel).Children.Add(new Label
                {
                    Foreground = Brushes.DarkGray,
                    Content    = "DESC",
                    Padding    = new Thickness(0)
                });
            srt.Items.Add(epd);

            // - Episode count / ASC

            var epc         = new MenuItem();
            epc.IsCheckable = true;
            epc.IsChecked   = sorting == "epcount" && sortdir == "asc";
            epc.Tag         = new[] { "epcount", "ascending" };
            epc.Click      += SortBy_Click;
            epc.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-number.png")) };
            epc.Header      = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin      = new Thickness(0, 0, spm, 0)
                };
            (epc.Header as StackPanel).Children.Add(new Label
                {
                    Content = "Episode count",
                    Padding = new Thickness(0),
                    Width   = lbw
                });
            (epc.Header as StackPanel).Children.Add(new Label
                {
                    Foreground = Brushes.DarkGray,
                    Content    = "  ASC",
                    Padding    = new Thickness(0)
                });
            srt.Items.Add(epc);
            
            // Group by

            var grp    = new MenuItem();
            grp.Header = "Group by";
            grp.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/group.png")) };
            sti.Items.Add(grp);

            // - None

            var nrt         = new MenuItem();
            nrt.IsCheckable = true;
            nrt.IsChecked   = string.IsNullOrEmpty(grouping);
            nrt.Header      = "None";
            nrt.Tag         = string.Empty;
            nrt.Click      += GroupBy_Click;
            nrt.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/layer.png")) };
            grp.Items.Add(nrt);

            // --
            
            grp.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

            // - Air day
            
            var aid         = new MenuItem();
            aid.IsCheckable = true;
            aid.IsChecked   = grouping == "airday";
            aid.Header      = "Air day";
            aid.Tag         = "airday";
            aid.Click      += GroupBy_Click;
            aid.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/calendar-select-days.png")) };
            grp.Items.Add(aid);
            
            // - Recently aired episodes
            
            var rae         = new MenuItem();
            rae.IsCheckable = true;
            rae.IsChecked   = grouping == "unseen";
            rae.Header      = "Recently aired episodes";
            rae.Tag         = "unseen";
            rae.Click      += GroupBy_Click;
            rae.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-date.png")) };
            grp.Items.Add(rae);
            
            // - Upcoming episodes
            
            var upc         = new MenuItem();
            upc.IsCheckable = true;
            upc.IsChecked   = grouping == "upcoming";
            upc.Header      = "Upcoming episodes";
            upc.Tag         = "upcoming";
            upc.Click      += GroupBy_Click;
            upc.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/sort-date-descending.png")) };
            grp.Items.Add(upc);

            // - Network
            
            var net         = new MenuItem();
            net.IsCheckable = true;
            net.IsChecked   = grouping == "network";
            net.Header      = "Network";
            net.Tag         = "network";
            net.Click      += GroupBy_Click;
            net.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/television-image.png")) };
            grp.Items.Add(net);

            // --

            sti.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });
            
            // Presets

            var prs    = new MenuItem();
            prs.Header = "Sort/group presets";
            prs.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/layers-stack.png")) };
            sti.Items.Add(prs);

            // - Presets

            foreach (var sgpair in SortGroupPairs)
            {
                var sgp         = new MenuItem();
                sgp.IsCheckable = true;
                sgp.IsChecked   = sorting == sgpair[1] && grouping == sgpair[2];
                sgp.Header      = sgpair[0];
                sgp.Tag         = sgpair;
                sgp.Click      += SortGroupPair_Click;
                sgp.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/navigation.png")) };
                prs.Items.Add(sgp);
            }

            // --

            sti.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

            // Move up

            var mvu       = new MenuItem();
            mvu.IsEnabled = string.IsNullOrEmpty(sorting) && string.IsNullOrWhiteSpace(grouping);
            mvu.Icon      = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/up.png")) };
            mvu.Click    += MoveUpShow;
            mvu.Header    = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin      = new Thickness(0, 0, spm, 0)
                };

            (mvu.Header as StackPanel).Children.Add(new Label
                {
                    Content = "Move up",
                    Padding = new Thickness(0),
                    Width   = lbw
                });
            (mvu.Header as StackPanel).Children.Add(new Label
                {
                    Foreground = Brushes.DarkGray,
                    Content    = "     Ctrl+Up",
                    Padding    = new Thickness(0)
                });
            ((Image)mvu.Icon).SetValue(ImageGreyer.IsGreyableProperty, true);
            sti.Items.Add(mvu);

            // Move down

            var mvd       = new MenuItem();
            mvd.IsEnabled = string.IsNullOrEmpty(sorting) && string.IsNullOrWhiteSpace(grouping);
            mvd.Icon      = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/down.png")) };
            mvd.Click    += MoveDownShow;
            mvd.Header    = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin      = new Thickness(0, 0, spm, 0)
                };

            (mvd.Header as StackPanel).Children.Add(new Label
                {
                    Content = "Move down",
                    Padding = new Thickness(0),
                    Width   = lbw
                });
            (mvd.Header as StackPanel).Children.Add(new Label
                {
                    Foreground = Brushes.DarkGray,
                    Content    = "Ctrl+Down",
                    Padding    = new Thickness(0)
                });
            ((Image)mvd.Icon).SetValue(ImageGreyer.IsGreyableProperty, true);
            sti.Items.Add(mvd);

            // --

            sti.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

            // Hide ended

            var hed         = new MenuItem();
            hed.IsCheckable = true;
            hed.IsChecked   = hideend;
            hed.Header      = "Hide ended shows";
            hed.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/table-join-row.png")) };
            hed.Click      += HideEnded_Click;
            sti.Items.Add(hed);
            
            // Fade ended

            var fed         = new MenuItem();
            fed.IsCheckable = true;
            fed.IsChecked   = fadeend;
            fed.Header      = "Fade ended shows";
            fed.Icon        = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/table-select-row.png")) };
            fed.Click      += FadeEnded_Click;
            sti.Items.Add(fed);

            // --

            sti.Items.Add(new Separator { Margin = new Thickness(0, -5, 0, -3) });

            // Edit

            var cnf    = new MenuItem();
            cnf.Header = "Edit";
            cnf.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/pencil.png")) };
            cnf.Click += ConfigureShow;
            sti.Items.Add(cnf);

            // Update

            var upd    = new MenuItem();
            upd.Header = "Update";
            upd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/refresh.png")) };
            upd.Click += UpdateShow;
            sti.Items.Add(upd);

            // Remove

            var rem    = new MenuItem();
            rem.Header = "Remove";
            rem.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cross.png")) };
            rem.Click += RemoveShow;
            sti.Items.Add(rem);
        }

        /// <summary>
        /// Handles the Click event of the SortBy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void SortBy_Click(object sender, RoutedEventArgs e)
        {
            Settings.Set("Sorting", ((string[])((MenuItem)sender).Tag)[0]);
            Settings.Set("Sort Direction", ((string[])((MenuItem)sender).Tag)[1]);
            Refresh();
        }

        /// <summary>
        /// Handles the Click event of the GroupBy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void GroupBy_Click(object sender, RoutedEventArgs e)
        {
            Settings.Set("Grouping", (string)((MenuItem)sender).Tag);
            Refresh();
        }

        /// <summary>
        /// Handles the Click event of the SortGroupPair control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void SortGroupPair_Click(object sender, RoutedEventArgs e)
        {
            Settings.Set("Sorting", ((string[])((MenuItem)sender).Tag)[1]);
            Settings.Set("Grouping", ((string[])((MenuItem)sender).Tag)[2]);
            Settings.Set("Sort Direction", ((string[])((MenuItem)sender).Tag)[3]);
            Refresh();
        }

        /// <summary>
        /// Handles the Click event of the HideEnded control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void HideEnded_Click(object sender, RoutedEventArgs e)
        {
            Settings.Toggle("Hide Ended");
            Refresh();
        }

        /// <summary>
        /// Handles the Click event of the FadeEnded control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void FadeEnded_Click(object sender, RoutedEventArgs e)
        {
            Settings.Toggle("Fade Ended");
            Refresh();
        }
        #endregion
    }
}
