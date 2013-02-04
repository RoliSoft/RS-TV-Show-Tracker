namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.ContextMenus;
    using RoliSoft.TVShowTracker.ContextMenus.Menus;
    using RoliSoft.TVShowTracker.Downloaders.Engines;
    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Senders;
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
        /// Gets or sets the active search.
        /// </summary>
        /// <value>The active search.</value>
        public DownloadSearch ActiveSearch { get; set; }

        /// <summary>
        /// Gets or sets the senders.
        /// </summary>
        /// <value>The senders.</value>
        public Dictionary<string, SenderEngine> Senders { get; set; }

        private List<LinkItem> _results;

        private ListSortDirection _lastSortDirection;
        private GridViewColumnHeader _lastClickedHeader;

        private Tuple<string, BitmapSource> _assocTorrent, _assocUsenet, _assocLinks;
        private Dictionary<string, List<Tuple<string, string, BitmapSource>>> _altAssoc;
        private Dictionary<string, List<string>> _destDirs;

        private static Dictionary<Types, string> _typeAssocMap = new Dictionary<Types, string>
            {
                { Types.Torrent, ".torrent" },
                { Types.Usenet, ".nzb" },
                { Types.DirectHTTP, ".dlc" }
            };

        private volatile bool _searching;

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

                if (Signature.IsActivated)
                {
                    highlightFree.IsChecked = Settings.Get<bool>("Highlight Free Torrents");
                    fadeDead.IsChecked      = Settings.Get("Fade Dead Torrents", true);
                }
                else
                {
                    highlightFree.IsEnabled = fadeDead.IsEnabled = false;
                }
            }

            LoadEngines();
            LoadDestinations();
        }

        #region Settings
        /// <summary>
        /// Loads the engines.
        /// </summary>
        /// <param name="reload">if set to <c>true</c> it will reload all variables; otherwise, it will just load the variables which are null.</param>
        public void LoadEngines(bool reload = false)
        {
            if (reload || availableTorrentEngines.Items.Count == 0)
            {
                availableTorrentEngines.Items.Clear();
                availableUsenetEngines.Items.Clear();
                availableHTTPEngines.Items.Clear();
                availablePreEngines.Items.Clear();

                foreach (var engine in AutoDownloader.SearchEngines.OrderBy(engine => AutoDownloader.Parsers.IndexOf(engine.Name)))
                {
                    var mi = new MenuItem
                        {
                            Header           = new StackPanel { Orientation = Orientation.Horizontal },
                            IsCheckable      = true,
                            IsChecked        = AutoDownloader.Actives.Contains(engine.Name),
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
                        var login   = Settings.Get(engine.Name + " Login", string.Empty);
                        var cookies = Settings.Get(engine.Name + " Cookies", string.Empty);
                        var tooltip = string.Empty;

                        if (string.IsNullOrWhiteSpace(login) && string.IsNullOrWhiteSpace(cookies))
                        {
                            if (engine.CanLogin)
                            {
                                tooltip = "Go to Settings, click on the Parsers tab, then select this site and enter the username and password.";
                            }
                            else
                            {
                                tooltip = "Go to Settings, click on the Parsers tab, then select this site and enter the extracted cookies.";
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(login))
                        {
                            try
                            {
                                var ua = Utils.Decrypt(engine, login);
                                tooltip = "You have supplied login credentials for " + ua[0] + ".";
                            }
                            catch (Exception ex)
                            {
                                tooltip = "Error while decrypting login credentials: " + ex.Message;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(cookies))
                        {
                            try
                            {
                                var cs = Utils.Decrypt(engine, cookies)[0];
                                tooltip = "You have supplied the following cookies:";

                                foreach (var cookie in cs.Split(';'))
                                {
                                    if (cookie.TrimStart().StartsWith("pass"))
                                    {
                                        tooltip += "\r\n - " + Regex.Replace(cookie.Trim(), "(?<=pass=.{4})(.+)", m => new string('▪', m.Groups[1].Value.Length)).CutIfLonger(60);
                                    }
                                    else
                                    {
                                        tooltip += "\r\n - " + cookie.Trim().CutIfLonger(60);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                tooltip = "Error while decrypting cookies: " + ex.Message;
                            }
                        }

                        (mi.Header as StackPanel).Children.Add(new Image
                            {
                                Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/" + (string.IsNullOrWhiteSpace(login) && string.IsNullOrWhiteSpace(cookies) ? "lock" : "key") + ".png", UriKind.Relative)),
                                Width   = 16,
                                Height  = 16,
                                Margin  = new Thickness(3, -2, -100, 0),
                                ToolTip = "This is a private site and therefore a valid account is required to search on it.\r\n" + tooltip
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

                    if (engine.Type == Types.DirectHTTP)
                    {
                        (mi.Header as StackPanel).Children.Add(new Image
                            {
                                Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/jdownloader.png", UriKind.Relative)),
                                Width   = 16,
                                Height  = 16,
                                Margin  = new Thickness(3, -2, -100, 0),
                                ToolTip = "This parser extracts direct links which can be passed to JDownloader."
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
        /// Loads the destinations.
        /// </summary>
        public void LoadDestinations()
        {
            // load default associations

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

            var adl = Utils.GetApplicationForExtension(".dlc");
            if (!string.IsNullOrWhiteSpace(adl))
            {
                _assocLinks = Utils.GetExecutableInfo(adl);
            }

            // load alternative associations

            _altAssoc = new Dictionary<string, List<Tuple<string, string, BitmapSource>>>
                {
                    { ".torrent", new List<Tuple<string, string, BitmapSource>>() },
                    { ".nzb",     new List<Tuple<string, string, BitmapSource>>() },
                    { ".dlc",     new List<Tuple<string, string, BitmapSource>>() }
                };

            foreach (var alt in Settings.Get<Dictionary<string, object>>("Alternative Associations"))
            {
                var lst = (List<string>)alt.Value;

                if (lst == null || lst.Count == 0)
                {
                    continue;
                }

                foreach (var app in lst)
                {
                    Tuple<string, BitmapSource> sci;
                    if ((sci = Utils.GetExecutableInfo(app)) != null)
                    {
                        _altAssoc[alt.Key].Add(new Tuple<string, string, BitmapSource>(sci.Item1, app, sci.Item2));
                    }
                }
            }

            // load folder destinations

            _destDirs = new Dictionary<string, List<string>>
                {
                    { ".torrent", new List<string>() },
                    { ".nzb",     new List<string>() },
                    { ".dlc",     new List<string>() }
                };

            foreach (var alt in Settings.Get<Dictionary<string, object>>("Folder Destinations"))
            {
                var lst = (List<string>)alt.Value;

                if (lst == null || lst.Count == 0)
                {
                    continue;
                }

                foreach (var app in lst)
                {
                    if (Directory.Exists(app))
                    {
                        _destDirs[alt.Key].Add(app);
                    }
                }
            }

            // load remote servers

            Senders = new Dictionary<string, SenderEngine>();

            var plugins = Extensibility.GetNewInstances<SenderEngine>().ToList();
            var actives = Settings.Get<Dictionary<string, object>>("Sender Destinations");

            if (actives.Count == 0)
            {
                return;
            }

            foreach (var activekv in actives)
            {
                var active = (Dictionary<string, object>)activekv.Value;
                var plugin = plugins.FirstOrDefault(p => p.Name == (string)active["Sender"]);
                if (plugin == null)
                {
                    continue;
                }

                var inst = (SenderEngine)Activator.CreateInstance(plugin.GetType());

                inst.Title = (string)active["Name"];
                inst.Location = (string)active["Location"];

                if (active.ContainsKey("Login"))
                {
                    var login = Utils.Decrypt(inst, (string)active["Login"]);
                    inst.Login = new NetworkCredential(login[0], login[1]);
                }

                Senders[activekv.Key] = inst;
            }
        }

        /// <summary>
        /// Handles the Checked event of the SearchEngineMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchEngineMenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (!AutoDownloader.Actives.Contains((sender as MenuItem).Tag as string))
            {
                AutoDownloader.Actives.Add((sender as MenuItem).Tag as string);

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
            if (AutoDownloader.Actives.Contains((sender as MenuItem).Tag as string))
            {
                AutoDownloader.Actives.Remove((sender as MenuItem).Tag as string);

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
        /// Handles the Checked event of the hightlightFree control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HighlightFreeChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Highlight Free Torrents", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the highlightFree control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HighlightFreeUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Highlight Free Torrents", false);
        }

        /// <summary>
        /// Handles the Checked event of the fadeDead control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FadeDeadChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Fade Dead Torrents", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the fadeDead control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FadeDeadUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Fade Dead Torrents", false);
        }

        /// <summary>
        /// Saves the activated trackers to the XML settings file.
        /// </summary>
        public void SaveActivated()
        {
            Settings.Set("Active Trackers", AutoDownloader.Actives);
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

            ActiveSearch = new DownloadSearch(AutoDownloader.ActiveSearchEngines, filterResults.IsChecked);

            ActiveSearch.DownloadSearchDone          += DownloadSearchDone;
            ActiveSearch.DownloadSearchEngineNewLink += DownloadSearchEngineNewLink;
            ActiveSearch.DownloadSearchEngineDone    += DownloadSearchEngineDone;
            ActiveSearch.DownloadSearchEngineError   += DownloadSearchEngineError;

            SetStatus("Searching for download links on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            _searching = true;

            ActiveSearch.SearchAsync(textBox.Text);

            Utils.Win7Taskbar(0, TaskbarProgressBarState.Normal);
        }

        /// <summary>
        /// Occurs when a download link search found a new link.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadSearchEngineNewLink(object sender, EventArgs<Link> e)
        {
            lock (_results)
            {
                if (Signature.IsActivated || !Regex.IsMatch(e.Data.Source.Site + e.Data.InfoURL + e.Data.FileURL, @"(?:sceneaccess|thegft)\.(?:[a-z]{2,3})/", RegexOptions.IgnoreCase))
                {
                    _results.Add(new LinkItem(e.Data));
                }
            }

            Dispatcher.Invoke((Action)(() =>
                {
                    lock (DownloadLinksListViewItemCollection)
                    {
                        DownloadLinksListViewItemCollection.Clear();
                        DownloadLinksListViewItemCollection.AddRange(_results
                                                                     .OrderBy(link => FileNames.Parser.QualityCount - (int)link.Quality)
                                                                     .ThenBy(link => AutoDownloader.Parsers.IndexOf(link.Source.Name)));
                    }
                }));
        }

        /// <summary>
        /// Called when a download link search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadSearchEngineDone(object sender, EventArgs<List<DownloadSearchEngine>> e)
        {
            if (!_searching)
            {
                return;
            }

            SetStatus("Searching for download links on " + (string.Join(", ", e.Data.Select(l => l.Name))) + "...", true);
            try { Utils.Win7Taskbar((int)((double)(ActiveSearch.SearchEngines.Count - e.Data.Count) / ActiveSearch.SearchEngines.Count * 100)); } catch { }
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

                    if (Signature.IsActivated && Settings.Get<bool>("One-Click Hoster Link Checker"))
                    {
                        foreach (var item in DownloadLinksListViewItemCollection.Where(x => x.Source.Type == Types.DirectHTTP).ToList())
                        {
                            new Thread(item.CheckLink).Start();
                        }
                    }

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
        public void DownloadSearchEngineError(object sender, EventArgs<string, Exception> e)
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

            MainWindow.HandleUnexpectedException(e.Second);
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
                            .OrderBy(link => AutoDownloader.Parsers.IndexOf(link.Source.Name))
                            .ThenBy(link => FileNames.Parser.QualityCount - (int)link.Quality)
                            .ToList();
                    }
                    else
                    {
                        links = links
                            .OrderByDescending(link => AutoDownloader.Parsers.IndexOf(link.Source.Name))
                            .ThenBy(link => FileNames.Parser.QualityCount - (int)link.Quality)
                            .ToList();
                    }
                    break;

                case "Quality":
                    if (direction == ListSortDirection.Ascending)
                    {
                        links = links
                            .OrderBy(link => FileNames.Parser.QualityCount - (int)link.Quality)
                            .ThenBy(link => AutoDownloader.Parsers.IndexOf(link.Source.Name))
                            .ToList();
                    }
                    else
                    {
                        links = links
                            .OrderByDescending(link => FileNames.Parser.QualityCount - (int)link.Quality)
                            .ThenBy(link => AutoDownloader.Parsers.IndexOf(link.Source.Name))
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
            ((FrameworkElement)e.Source).ContextMenu = cm;
            var link = (LinkItem)listView.SelectedValue;

            if (!string.IsNullOrWhiteSpace(link.InfoURL))
            {
                var oib    = new MenuItem();
                oib.Header = "Open details in browser";
                oib.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/page.png")) };
                oib.Click += (s, r) => Utils.Run(link.InfoURL);
                cm.Items.Add(oib);
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && !link.FileURL.StartsWith("magnet:") && !(link.Source.Downloader is BinSearchDownloader))
            {
                var oib    = new MenuItem();
                oib.Header = "Download file in browser";
                oib.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/page-dl.png")) };
                oib.Click += (s, r) => ProcessLink(link, x =>
                    {
                        foreach (var url in x.FileURL.Split('\0'))
                        {
                            Utils.Run(url);
                        }
                    });
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

            if (_assocTorrent != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.Torrent && !(link.Source.Downloader is ExternalDownloader))
            {
                var sap    = new MenuItem();
                sap.Header = "Send to " + CleanExeName(_assocTorrent.Item1);
                sap.Icon   = new Image { Source = _assocTorrent.Item2, Height = 16, Width = 16 };
                sap.Click += (s, r) => DownloadFileClick("UseAssociated", r); // was SendToTorrent
                cm.Items.Add(sap);
            }

            if (_assocUsenet != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.Usenet && !(link.Source.Downloader is ExternalDownloader))
            {
                var sap    = new MenuItem();
                sap.Header = "Send to " + CleanExeName(_assocUsenet.Item1);
                sap.Icon   = new Image { Source = _assocUsenet.Item2, Height = 16, Width = 16 };
                sap.Click += (s, r) => DownloadFileClick("UseAssociated", r); // was SendToUsenet
                cm.Items.Add(sap);
            }

            if (_assocLinks != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.DirectHTTP)
            {
                var jd    = new MenuItem();
                jd.Header = "Send to " + CleanExeName(_assocLinks.Item1);
                jd.Icon   = new Image { Source = _assocLinks.Item2, Height = 16, Width = 16 };
                jd.Click += (s, r) => ProcessLink(link, SendToLinkContainerDownloader);
                cm.Items.Add(jd);
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && _typeAssocMap.ContainsKey(link.Source.Type) && _altAssoc[_typeAssocMap[link.Source.Type]].Count != 0)
            {
                foreach (var alt in _altAssoc[_typeAssocMap[link.Source.Type]])
                {
                    var app    = alt;
                    var snd    = new MenuItem();
                    snd.Header = "Send to " + CleanExeName(app.Item1);
                    snd.Icon   = new Image { Source = app.Item3, Height = 16, Width = 16 };

                    if (link.Source.Type == Types.DirectHTTP)
                    {
                        snd.Click += (s, r) => ProcessLink(link, x => new LinkDownloadTaskDialog().Download(RewriteHTTPLinksToDLC(x), "SendTo|" + app.Item2));
                    }
                    else
                    {
                        snd.Click += (s, r) => DownloadFileClick("SendTo|" + app.Item2, r);
                    }

                    cm.Items.Add(snd);
                }
            }

            if (Signature.IsActivated)
            { 
                foreach (var se in Senders)
                {
                    if (se.Value.Type == link.Source.Type)
                    {
                        var id     = se.Key;
                        var cmi    = new MenuItem();
                        cmi.Tag    = se;
                        cmi.Header = "Send to " + se.Value.Title;
                        cmi.Icon   = new Image { Source = new BitmapImage(new Uri(se.Value.Icon)), Height = 16, Width = 16 };
                        cmi.Click += (s, r) => DownloadFileClick("SendToSender|" + id, r);
                        cm.Items.Add(cmi);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(link.FileURL) && _typeAssocMap.ContainsKey(link.Source.Type) && _destDirs[_typeAssocMap[link.Source.Type]].Count != 0)
            {
                foreach (var alt in _destDirs[_typeAssocMap[link.Source.Type]])
                {
                    var app    = alt;
                    var snd    = new MenuItem();
                    snd.Header = "Save to " + Path.GetFileName(app);
                    snd.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/folder-open-document.png")), Height = 16, Width = 16 };

                    if (link.Source.Type == Types.DirectHTTP)
                    {
                        snd.Click += (s, r) => ProcessLink(link, x => new LinkDownloadTaskDialog().Download(RewriteHTTPLinksToDLC(x), "SendToFolder|" + app));
                    }
                    else
                    {
                        snd.Click += (s, r) => DownloadFileClick("SendToFolder|" + app, r);
                    }

                    cm.Items.Add(snd);
                }
            }

            foreach (var dlcm in Extensibility.GetNewInstances<DownloadLinkContextMenu>())
            {
                foreach (var dlcmi in dlcm.GetMenuItems(link))
                {
                    var cmi    = new MenuItem();
                    cmi.Tag    = dlcmi;
                    cmi.Header = dlcmi.Name;
                    cmi.Icon   = dlcmi.Icon;
                    cmi.Click += (s, r) => ((ContextMenuItem<Link>)cmi.Tag).Click(link);
                    cm.Items.Add(cmi);
                }
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

            var link = (Link)listView.SelectedValue;

            if (_assocTorrent != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.Torrent && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick("UseAssociated", e); // was SendToTorrent
            }
            else if (_assocUsenet != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.Usenet && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick("UseAssociated", e); // was SendToUsenet
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type != Types.HTTP && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick("UseAssociated", e);
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL) && !(link.Source.Downloader is ExternalDownloader))
            {
                DownloadFileClick(null, e);
            }
            else if (_assocLinks != null && !string.IsNullOrWhiteSpace(link.FileURL) && link.Source.Type == Types.DirectHTTP)
            {
                ProcessLink(link, SendToLinkContainerDownloader);
            }
            else if (!string.IsNullOrWhiteSpace(link.FileURL))
            {
                ProcessLink(link, x =>
                    {
                        foreach (var url in x.FileURL.Split('\0'))
                        {
                            Utils.Run(url);
                        }
                    });
            }
            else if (!string.IsNullOrWhiteSpace(link.InfoURL))
            {
                Utils.Run(link.InfoURL);
            }
        }

        /// <summary>
        /// Creates a temporary container and sends it to the default associated application to DLC.
        /// </summary>
        /// <param name="link">The link.</param>
        private void SendToLinkContainerDownloader(Link link)
        {
            var tmp = Path.Combine(Path.GetTempPath(), Utils.CreateSlug(link.Release.Replace('.', ' ').Replace('_', ' ') + " " + link.Source.Name + " " + Utils.Rand.Next().ToString("x"), false) + ".dlc");
            File.WriteAllText(tmp, DLCAPI.CreateDLC(link.Release, link.FileURL.Split('\0')));
            Utils.Run(tmp);
        }

        /// <summary>
        /// Rewrites a list of HTTP links to a local DLC container path.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <returns>New copy of the link pointing to a DLC container.</returns>
        private Link RewriteHTTPLinksToDLC(Link link)
        {
            var tmp = Path.Combine(Path.GetTempPath(), Utils.CreateSlug(link.Release.Replace('.', ' ').Replace('_', ' ') + " " + link.Source.Name + " " + Utils.Rand.Next().ToString("x"), false) + ".dlc");
            File.WriteAllText(tmp, DLCAPI.CreateDLC(link.Release, link.FileURL.Split('\0')));
            return new Link(link.Source)
                {
                    Release = link.Release,
                    InfoURL = link.InfoURL,
                    FileURL = tmp,
                    Infos   = link.Infos,
                    Quality = link.Quality,
                    Size    = link.Size
                };
        }

        /// <summary>
        /// Cleans the name of the executable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Cleaned name.</returns>
        public static string CleanExeName(string name)
        {
            return Regex.Replace(name, @"\s*(?:Web Browser|Launcher|for Windows|\-Qt).+", string.Empty, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Processes the link.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="callback">The callback.</param>
        private void ProcessLink(Link link, Action<Link> callback)
        {
            if (link.Source is ILinkExpander<Link>)
            {
                var url = string.Empty;

                new GenericAsyncTaskDialog(
                    "Expanding links...",
                    "Bypassing the link protection on " + new Uri(link.FileURL.Split('\0')[0]).Host + "...",
                    () => url = ((ILinkExpander<Link>)link.Source).ExpandLinks(link),
                    () => { link.FileURL = url; callback(link); }
                ).Run();
            }
            else
            {
                callback(link);
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

            ProcessLink(
                (Link)listView.SelectedValue,
                link => new LinkDownloadTaskDialog().Download(link, sender is string ? sender as string : "DownloadFile")
            );
        }
        #endregion
    }
}
