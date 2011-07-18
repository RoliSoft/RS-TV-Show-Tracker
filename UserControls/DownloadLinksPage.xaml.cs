namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Downloaders.Engines;
    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.TaskDialogs;

    using Image = System.Windows.Controls.Image;

    /// <summary>
    /// Interaction logic for DownloadLinksPage.xaml
    /// </summary>
    public partial class DownloadLinksPage
    {
        /// <summary>
        /// Gets or sets the download links list view item collection.
        /// </summary>
        /// <value>The download links list view item collection.</value>
        public ObservableCollection<LinkItem> DownloadLinksListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the search engines active in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public List<DownloadSearchEngine> SearchEngines { get; set; }

        private List<string> _trackers, _qualities, _actives;
        private List<LinkItem> _results;

        private ListSortDirection _lastSortDirection;
        private GridViewColumnHeader _lastClickedHeader;

        private Tuple<string, BitmapSource> _defaultTorrent, _assocTorrent, _assocUsenet;

        private string _jDlPath;

        private volatile bool _searching;

        /// <summary>
        /// Gets or sets the active search.
        /// </summary>
        /// <value>The active search.</value>
        public DownloadSearch ActiveSearch { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitlesPage"/> class.
        /// </summary>
        public DownloadLinksPage()
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
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (DownloadLinksListViewItemCollection == null)
            {
                DownloadLinksListViewItemCollection = new ObservableCollection<LinkItem>();
                listView.ItemsSource                = DownloadLinksListViewItemCollection;

                filterResults.IsChecked = Settings.Get<bool>("Filter Download Links");
            }

            LoadEngines();

            var tdl = Settings.Get("Torrent Downloader");
            if (!string.IsNullOrWhiteSpace(tdl))
            {
                _defaultTorrent = Utils.GetExecutableInfo(tdl);
            }

            var atr = Utils.GetApplicationForExtension(".torrent");
            if (!string.IsNullOrWhiteSpace(atr))
            {
                _assocTorrent = Utils.GetExecutableInfo(atr);
            }

            var anz = Utils.GetApplicationForExtension(".nzb");
            if (!string.IsNullOrWhiteSpace(anz))
            {
                _assocUsenet = Utils.GetExecutableInfo(anz);
            }

            _jDlPath = Settings.Get("JDownloader");
        }

        #region Settings
        /// <summary>
        /// Loads the engines.
        /// </summary>
        /// <param name="reload">if set to <c>true</c> it will reload all variables; otherwise, it will just load the variables which are null.</param>
        public void LoadEngines(bool reload = false)
        {
            if (reload || SearchEngines == null)
            {
                SearchEngines = typeof(DownloadSearchEngine)
                                .GetDerivedTypes()
                                .Select(type => Activator.CreateInstance(type) as DownloadSearchEngine)
                                .ToList();
            }

            if (reload || _trackers == null)
            {
                _trackers = Settings.GetList("Tracker Order").ToList();
                _trackers.AddRange(SearchEngines
                                   .Where(engine => _trackers.IndexOf(engine.Name) == -1)
                                   .Select(engine => engine.Name));
            }

            if (reload || _qualities == null)
            {
                _qualities = Enum.GetNames(typeof(Qualities)).Reverse().ToList();
            }

            if (reload || _actives == null)
            {
                _actives = Settings.GetList("Active Trackers").ToList();
            }

            if (reload || availableTorrentEngines.Items.Count == 0)
            {
                availableTorrentEngines.Items.Clear();
                availableUsenetEngines.Items.Clear();
                availableHTTPEngines.Items.Clear();
                availablePreEngines.Items.Clear();

                foreach (var engine in SearchEngines.OrderBy(engine => _trackers.IndexOf(engine.Name)))
                {
                    var mi = new MenuItem
                        {
                            Header           = new StackPanel { Orientation = Orientation.Horizontal },
                            IsCheckable      = true,
                            IsChecked        = _actives.Contains(engine.Name),
                            StaysOpenOnClick = true,
                            Tag              = engine.Name
                        };

                    (mi.Header as StackPanel).Children.Add(new Image
                        {
                            Source = new BitmapImage(engine.Icon != null ? new Uri(engine.Icon) : new Uri("/RSTVShowTracker;component/Images/navigation.png", UriKind.Relative), new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable)),
                            Width  = 16,
                            Height = 16,
                            Margin = new Thickness(3, -2, 0, 0),
                        });
                    (mi.Header as StackPanel).Children.Add(new Label
                        {
                            Content = engine.Name,
                            Padding = new Thickness(5, 0, 0, 0),
                            Width   = 105
                        });

                    if (engine.Private)
                    {
                        var cookies = Settings.Get(engine.Name + " Cookies", string.Empty);
                        var tooltip = string.Empty;

                        foreach (var cookie in cookies.Split(';'))
                        {
                            tooltip += "\r\n - " + cookie.Trim().CutIfLonger(60);
                        }

                        (mi.Header as StackPanel).Children.Add(new Image
                            {
                                Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/" + (string.IsNullOrWhiteSpace(cookies) ? "lock" : "key") + ".png", UriKind.Relative)),
                                Width   = 16,
                                Height  = 16,
                                Margin  = new Thickness(3, -2, -100, 0),
                                ToolTip = "This is a private site and therefore cookies are required to search on it.\r\n" +
                                          (string.IsNullOrWhiteSpace(cookies)
                                           ? "Go to Settings, click on the Downloads tab, then select this site to enter the cookies."
                                           : "You have supplied the following cookies:" + tooltip)
                            });
                    }

                    if (engine.Deprecated)
                    {
                        (mi.Header as StackPanel).Children.Add(new Image
                            {
                                Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cross.png", UriKind.Relative)),
                                Width   = 16,
                                Height  = 16,
                                Margin  = new Thickness(3, -2, -70, 0),
                                ToolTip = "This site has been marked as deprecated, which means\r\nit will be removed from the next version of the software.\r\nVisit the website of the application for more informations."
                            });
                    }

                    mi.Checked   += SearchEngineMenuItemChecked;
                    mi.Unchecked += SearchEngineMenuItemUnchecked;

                    switch (engine.Type)
                    {
                        case Types.Torrent:
                            availableTorrentEngines.Items.Add(mi);
                            break;

                        case Types.Usenet:
                            availableUsenetEngines.Items.Add(mi);
                            break;

                        case Types.HTTP:
                        case Types.DirectHTTP:
                            availableHTTPEngines.Items.Add(mi);
                            break;

                        case Types.PreDB:
                            availablePreEngines.Items.Add(mi);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Checked event of the SearchEngineMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchEngineMenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (!_actives.Contains((sender as MenuItem).Tag as string))
            {
                _actives.Add((sender as MenuItem).Tag as string);

                SaveActivated();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the SearchEngineMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchEngineMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (_actives.Contains((sender as MenuItem).Tag as string))
            {
                _actives.Remove((sender as MenuItem).Tag as string);

                SaveActivated();
            }
        }

        /// <summary>
        /// Handles the Checked event of the filterResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FilterResultsChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Filter Download Links", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the filterResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FilterResultsUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Filter Download Links", false);
        }

        /// <summary>
        /// Saves the activated trackers to the XML settings file.
        /// </summary>
        public void SaveActivated()
        {
            Settings.Set("Active Trackers", _actives);
        }
        #endregion

        #region Search
        /// <summary>
        /// Initiates a search on this usercontrol.
        /// </summary>
        /// <param name="query">The query.</param>
        public void Search(string query)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    // cancel if one is running
                    if (_searching)
                    {
                        ActiveSearch.CancelAsync();
                        DownloadSearchDone();
                    }

                    textBox.Text = query;
                    SearchButtonClick(null, null);
                }));
        }

        /// <summary>
        /// Handles the KeyUp event of the textBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void TextBoxKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButtonClick(null, null);
            }
        }

        /// <summary>
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text)) return;

            if (_searching)
            {
                ActiveSearch.CancelAsync();
                DownloadSearchDone();
                return;
            }

            _results = new List<LinkItem>();
            DownloadLinksListViewItemCollection.Clear();

            textBox.IsEnabled    = false;
            searchButton.Content = "Cancel";

            ActiveSearch = new DownloadSearch(SearchEngines
                                              .Where(engine => _actives.Contains(engine.Name))
                                              .Select(engine => engine.GetType()))
                                              { Filter = filterResults.IsChecked };

            ActiveSearch.DownloadSearchDone            += DownloadSearchDone;
            ActiveSearch.DownloadSearchProgressChanged += DownloadSearchProgressChanged;
            ActiveSearch.DownloadSearchError           += DownloadSearchError;

            SetStatus("Searching for download links on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            _searching = true;

            ActiveSearch.SearchAsync(textBox.Text);

            Utils.Win7Taskbar(0, TaskbarProgressBarState.Normal);
        }

        /// <summary>
        /// Called when a download link search progress has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadSearchProgressChanged(object sender, EventArgs<List<Link>, double, List<string>> e)
        {
            if (!_searching)
            {
                return;
            }

            SetStatus("Searching for download links on " + (string.Join(", ", e.Third)) + "...", true);
            Utils.Win7Taskbar((int)e.Second);

            if (e.First != null)
            {
                lock (_results)
                {
                    _results.AddRange(e.First.Select(link => new LinkItem(link)));
                }

                Dispatcher.Invoke((Action)(() =>
                    {
                        DownloadLinksListViewItemCollection.Clear();
                        DownloadLinksListViewItemCollection.AddRange(_results
                                                                     .OrderBy(link => _qualities.IndexOf(link.Quality.ToString()))
                                                                     .ThenBy(link => _trackers.IndexOf(link.Source.Name)));
                    }));
            }
        }

        /// <summary>
        /// Called when a download link search is done on all engines.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void DownloadSearchDone(object sender = null, EventArgs e = null)
        {
            if (!_searching)
            {
                return;
            }

            _searching   = false;
            ActiveSearch = null;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            Dispatcher.Invoke((Action)(() =>
                {
                    textBox.IsEnabled    = true;
                    searchButton.Content = "Search";

                    if (DownloadLinksListViewItemCollection.Count != 0)
                    {
                        SetStatus("Found " + Utils.FormatNumber(DownloadLinksListViewItemCollection.Count, "download link") + "!");
                    }
                    else
                    {
                        SetStatus("Couldn't find any download links.");
                    }
                }));
        }

        /// <summary>
        /// Called when a download link search has encountered an unexpected error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void DownloadSearchError(object sender, EventArgs<string, Exception> e)
        {
            if (e.Second is WebException)
            {
                // Exceptions such as
                // - The operation has timed out
                // - The remote server returned an error: (503) Server Unavailable.
                // - Unable to connect to the remote server
                // indicate that the server is either unreachable or is under heavy load,
                // and these problems are not parsing bugs, so they will be ignored.
                return;
            }

            MainWindow.Active.HandleUnexpectedException(e.Second);
        }
        #endregion

        #region ListView events
        /// <summary>
        /// Handles the Click event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ListViewSortClick(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (header == null || header.Role == GridViewColumnHeaderRole.Padding || _results == null || _results.Count == 0)
            {
                return;
            }

            var direction = header != _lastClickedHeader
                            ? ListSortDirection.Ascending
                            : _lastSortDirection == ListSortDirection.Ascending
                                ? ListSortDirection.Descending
                                : ListSortDirection.Ascending;

            var links = new List<LinkItem>(_results);

            switch (header.Content.ToString())
            {
                case "Site":
                    if (direction == ListSortDirection.Ascending)
                    {
                        links = links
                            .OrderBy(link => this._trackers.IndexOf(link.Source.Name))
                            .ThenBy(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ToList();
                    }
                    else
                    {
                        links = links
                            .OrderByDescending(link => this._trackers.IndexOf(link.Source.Name))
                            .ThenBy(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ToList();
                    }
                    break;

                case "Quality":
                    if (direction == ListSortDirection.Ascending)
                    {
                        links = links
                            .OrderBy(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ThenBy(link => this._trackers.IndexOf(link.Source.Name))
                            .ToList();
                    }
                    else
                    {
                        links = links
                            .OrderByDescending(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ThenBy(link => this._trackers.IndexOf(link.Source.Name))
                            .ToList();
                    }
                    break;

                default:
                    return;
            }

            _lastClickedHeader = header;
            _lastSortDirection = direction;

            DownloadLinksListViewItemCollection.Clear();
            DownloadLinksListViewItemCollection.AddRange(links);
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
            var link = (LinkItem)listView.SelectedValue;

            if (!string.IsNullOrWhiteSpace(link.InfoURL))
            {
                var oib    = new MenuItem();
                oib.Header = "Open details in browser";
                oib.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/page.png")) };
                oib.Click += (s, r) => Utils.Run(link.InfoURL);
                cm.Items.Add(oib);
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && !link.FileURL.StartsWith("magnet:"))
            {
                var oib    = new MenuItem();
                oib.Header = "Download file in browser";
                oib.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/page-dl.png")) };
                oib.Click += (s, r) => Utils.Run(link.FileURL);
                cm.Items.Add(oib);
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && !(link.Source.Downloader is ExternalDownloader) && !link.FileURL.StartsWith("magnet:"))
            {
                var df    = new MenuItem();
                df.Header = "Download file";
                df.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
                df.Click += DownloadFileClick;
                cm.Items.Add(df);
            }

            if (_defaultTorrent != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.Torrent && !(link.Source.Downloader is ExternalDownloader))
            {
                var sap    = new MenuItem();
                sap.Header = "Send to " + _defaultTorrent.Item1;
                sap.Icon   = new Image { Source = _defaultTorrent.Item2 };
                sap.Click += (s, r) => DownloadFileClick("SendToTorrent", r);
                cm.Items.Add(sap);
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type != Types.HTTP && !(link.Source.Downloader is ExternalDownloader))
            {
                var target = link.Source.Type == Types.Torrent
                           ? _assocTorrent
                           : link.Source.Type == Types.Usenet
                             ? _assocUsenet
                             : null;

                var sap    = new MenuItem();
                sap.Header = "Send to " + (target != null ? target.Item1 : "associated");
                sap.Icon   = new Image { Source = target != null ? target.Item2 : new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/defapp.png")) };
                sap.Click += (s, r) => DownloadFileClick("SendToAssociated", r);
                cm.Items.Add(sap);
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && !string.IsNullOrWhiteSpace(_jDlPath) && link.Source.Type == Types.DirectHTTP)
            {
                var jd    = new MenuItem();
                jd.Header = "Send to JDownloader";
                jd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/jdownloader.png")) };
                jd.Click += (s, r) => Utils.Run(_jDlPath, link.FileURL);
                cm.Items.Add(jd);
            }

            TextOptions.SetTextFormattingMode(cm, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(cm, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(cm, BitmapScalingMode.HighQuality);

            cm.IsOpen = true;
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ListViewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var link = (LinkItem)listView.SelectedValue;

            if (_defaultTorrent != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.Torrent && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick("SendToTorrent", e);
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type != Types.HTTP && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick("SendToAssociated", e);
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL) && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick(null, e);
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL) && !string.IsNullOrWhiteSpace(_jDlPath) && link.Source.Type == Types.DirectHTTP)
            {
                Utils.Run(_jDlPath, link.FileURL);
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL))
            {
                Utils.Run(link.FileURL);
            }
            else if (!string.IsNullOrWhiteSpace(link.InfoURL))
            {
                Utils.Run(link.InfoURL);
            }
        }
        #endregion

        #region Download file
        /// <summary>
        /// Handles the Click event of the DownloadFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadFileClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var link = (LinkItem)listView.SelectedValue;

            new LinkDownloadTaskDialog().Download(link, sender is string ? sender as string : "DownloadFile");
        }
        #endregion
    }
}
