namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    using Image = System.Windows.Controls.Image;

    /// <summary>
    /// Interaction logic for DownloadLinksPage.xaml
    /// </summary>
    public partial class DownloadLinksPage
    {
        /// <summary>
        /// Gets or sets the name of the specified torrent downloader.
        /// </summary>
        /// <value>The default torrent.</value>
        public string DefaultTorrent { get; set; }

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

        private List<string> _trackers, _qualities, _excludes;
        private List<LinkItem> _results;

        private ListSortDirection _lastSortDirection;
        private GridViewColumnHeader _lastClickedHeader;

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

        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        private static extern int ExtractIconExA(string lpszFile, int nIconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr hIcon);

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
            }

            LoadEngines();

            var cm  = listView.ContextMenu;
            var tdl = Settings.Get("Torrent Downloader");

            if (!string.IsNullOrWhiteSpace(tdl))
            {
                ((MenuItem)cm.Items[3]).Visibility = Visibility.Visible;
                DefaultTorrent = FileVersionInfo.GetVersionInfo(tdl).ProductName;
                ((MenuItem)cm.Items[3]).Header = "Send to " + DefaultTorrent;

                try
                {
                    var largeIcon = IntPtr.Zero;
                    var smallIcon = IntPtr.Zero;

                    ExtractIconExA(tdl, 0, ref largeIcon, ref smallIcon, 1);
                    DestroyIcon(largeIcon);

                    if (smallIcon != IntPtr.Zero)
                    {
                        ((MenuItem)cm.Items[3]).Icon = new Image
                            {
                                Source = Imaging.CreateBitmapSourceFromHBitmap(Icon.FromHandle(smallIcon).ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                            };
                    }
                }
                catch { }
            }
            else
            {
                ((MenuItem)cm.Items[3]).Visibility = Visibility.Collapsed;
            }
        }

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

            if (reload || _excludes == null)
            {
                _excludes = Settings.GetList("Tracker Exclusions").ToList();
            }

            if (reload || availableEngines.Items.Count == 0)
            {
                availableEngines.Items.Clear();

                foreach (var engine in SearchEngines.OrderBy(engine => _trackers.IndexOf(engine.Name)))
                {
                    var mi = new MenuItem
                        {
                            Header           = new StackPanel { Orientation = Orientation.Horizontal },
                            IsCheckable      = true,
                            IsChecked        = !_excludes.Contains(engine.Name),
                            StaysOpenOnClick = true,
                            Tag              = engine.Name
                        };

                    (mi.Header as StackPanel).Children.Add(new Image
                        {
                            Source = new BitmapImage(new Uri(engine.Icon), new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable)),
                            Width  = 16,
                            Height = 16
                        });
                    (mi.Header as StackPanel).Children.Add(new Label
                        {
                            Content = engine.Name,
                            Padding = new Thickness(5, 0, 0, 0)
                        });

                    mi.Checked   += SearchEngineMenuItemChecked;
                    mi.Unchecked += SearchEngineMenuItemUnchecked;

                    availableEngines.Items.Add(mi);
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
            if (_excludes.Contains((sender as MenuItem).Tag as string))
            {
                _excludes.Remove((sender as MenuItem).Tag as string);

                SaveExclusions();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the SearchEngineMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchEngineMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_excludes.Contains((sender as MenuItem).Tag as string))
            {
                _excludes.Add((sender as MenuItem).Tag as string);

                SaveExclusions();
            }
        }

        /// <summary>
        /// Saves the exclusions to the XML settings file.
        /// </summary>
        public void SaveExclusions()
        {
            Settings.Set("Tracker Exclusions", _excludes);
        }

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

            textBox.IsEnabled = false;
            searchButton.Content = "Cancel";

            ActiveSearch = new DownloadSearch(SearchEngines
                                              .Where(engine => !_excludes.Contains(engine.Name))
                                              .Select(engine => engine.GetType()));

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
                                                                     .ThenBy(link => _trackers.IndexOf(link.Site)));
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
                            .OrderBy(link => this._trackers.IndexOf(link.Site))
                            .ThenBy(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ToList();
                    }
                    else
                    {
                        links = links
                            .OrderByDescending(link => this._trackers.IndexOf(link.Site))
                            .ThenBy(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ToList();
                    }
                    break;

                case "Quality":
                    if (direction == ListSortDirection.Ascending)
                    {
                        links = links
                            .OrderBy(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ThenBy(link => this._trackers.IndexOf(link.Site))
                            .ToList();
                    }
                    else
                    {
                        links = links
                            .OrderByDescending(link => this._qualities.IndexOf(link.Quality.ToString()))
                            .ThenBy(link => this._trackers.IndexOf(link.Site))
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

        #region Open page
        /// <summary>
        /// Handles the Click event of the OpenPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OpenPageClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            Utils.Run(((LinkItem)listView.SelectedValue).URL);
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

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);

            var link = (LinkItem)listView.SelectedValue;

            var uri = new Uri(link.URL);
            SetStatus("Sending request to " + uri.DnsSafeHost.Replace("www.", string.Empty) + "...", true);

            var wc  = new Utils.SmarterWebClient();
            var tmp = Utils.GetRandomFileName("torrent");

            var cookies = Settings.Get(link.Site + " Cookies");
            if (!string.IsNullOrWhiteSpace(cookies))
            {
                wc.Headers[HttpRequestHeader.Cookie] = cookies;
            }

            wc.Headers[HttpRequestHeader.Referer] = "http://" + uri.DnsSafeHost + "/";
            wc.DownloadFileCompleted             += WebClientDownloadFileCompleted;
            wc.DownloadProgressChanged           += (s, a) => SetStatus("Downloading file... (" + a.ProgressPercentage + "%)", true);

            wc.DownloadFileAsync(uri, tmp, new[]
                {
                    // temporary file name
                    tmp,
                    // action to do when finished
                    sender is string ? sender as string : "DownloadFile"
                });
        }
        #endregion

        #region Send to associated
        /// <summary>
        /// Handles the Click event of the SendToAssociated control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendToAssociatedClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            DownloadFileClick("SendToAssociated", e);
        }
        #endregion

        #region Send to [torrent application]
        /// <summary>
        /// Handles the Click event of the SendToTorrent control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendToTorrentClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            DownloadFileClick("SendToTorrent", e);
        }
        #endregion

        /// <summary>
        /// Handles the DownloadFileCompleted event of the wc control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.AsyncCompletedEventArgs"/> instance containing the event data.</param>
        private void WebClientDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var web   = sender as Utils.SmarterWebClient;
            var token = e.UserState as string[];
            var file  = web.FileName;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            switch (token[1])
            {
                case "DownloadFile":
                    var sfd = new SaveFileDialog
                        {
                            CheckPathExists = true,
                            FileName = file
                        };

                    if (sfd.ShowDialog().Value)
                    {
                        if (File.Exists(sfd.FileName))
                        {
                            File.Delete(sfd.FileName);
                        }

                        File.Move(token[0], sfd.FileName);
                    }
                    else
                    {
                        File.Delete(token[0]);
                    }

                    SetStatus("File downloaded successfully.");
                    break;

                case "SendToAssociated":
                    Utils.Run(token[0]);

                    SetStatus("File sent to associated application successfully.");
                    break;

                case "SendToTorrent":
                    Utils.Run(Settings.Get("Torrent Downloader"), token[0]);

                    SetStatus("File sent to " + DefaultTorrent + " successfully.");
                    break;
            }
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

            if (link.ShowOpenPage == "Visible")
            {
                OpenPageClick(sender, e);
            }
            else if(link.ShowSendToTorrent == "Visible")
            {
                SendToTorrentClick(sender, e);
            }
            else if (link.ShowSendToAssociated == "Visible")
            {
                SendToAssociatedClick(sender, e);
            }
            else
            {
                DownloadFileClick(sender, e);
            }
        }
    }
}
