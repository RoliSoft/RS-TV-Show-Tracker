namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Interaction logic for SubtitlesPage.xaml
    /// </summary>
    public partial class SubtitlesPage : UserControl
    {
        /// <summary>
        /// Gets or sets the subtitles list view item collection.
        /// </summary>
        /// <value>The subtitles list view item collection.</value>
        public ObservableCollection<Subtitle> SubtitlesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the search engines active in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public List<SubtitleSearchEngine> SearchEngines { get; set; }

        private List<string> _excludes, _langExcl;

        /// <summary>
        /// Gets or sets the active search.
        /// </summary>
        /// <value>The active search.</value>
        public SubtitleSearch ActiveSearch { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitlesPage"/> class.
        /// </summary>
        public SubtitlesPage()
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
            if (SubtitlesListViewItemCollection == null)
            {
                SubtitlesListViewItemCollection = new ObservableCollection<Subtitle>();
                listView.ItemsSource            = SubtitlesListViewItemCollection;
            }

            if (SearchEngines == null)
            {
                SearchEngines = typeof(SubtitleSearchEngine)
                                .GetDerivedTypes()
                                .Select(type => Activator.CreateInstance(type) as SubtitleSearchEngine)
                                .ToList();
            }

            if (_excludes == null)
            {
                _excludes = Settings.Get("Subtitle Site Exclusions").Split(',').ToList();
            }

            if (_langExcl == null)
            {
                _langExcl = Settings.Get("Subtitle Language Exclusions").Split(',').ToList();
            }

            if (availableEngines.Items.Count == 0)
            {
                foreach (var engine in SearchEngines)
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

            if (languages.Items.Count == 0)
            {
                var langs = new Dictionary<string, string>
                    {
                        {"English",   "flag-en"},
                        {"Hungarian", "flag-hu"},
                        {"Romanian",  "flag-ro"},
                        {"Unknown",   "unknown"}
                    };

                foreach (var lang in langs)
                {
                    var mi = new MenuItem
                    {
                        Header           = new StackPanel { Orientation = Orientation.Horizontal },
                        IsCheckable      = true,
                        IsChecked        = !_langExcl.Contains(lang.Key),
                        StaysOpenOnClick = true,
                        Tag              = lang.Key
                    };

                    (mi.Header as StackPanel).Children.Add(new Image
                        {
                            Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/" + lang.Value + ".png", UriKind.Relative)),
                            Width = 16,
                            Height = 16
                        });
                    (mi.Header as StackPanel).Children.Add(new Label
                        {
                            Content = lang.Key,
                            Padding = new Thickness(5, 0, 0, 0)
                        });

                    mi.Checked   += LanguageMenuItemChecked;
                    mi.Unchecked += LanguageMenuItemUnchecked;

                    languages.Items.Add(mi);
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
            Settings.Set("Subtitle Site Exclusions", _excludes.Aggregate(string.Empty, (current, engine) => current + (engine + ",")).Trim(','));
        }

        /// <summary>
        /// Handles the Checked event of the LanguageMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LanguageMenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (_langExcl.Contains((sender as MenuItem).Tag as string))
            {
                _langExcl.Remove((sender as MenuItem).Tag as string);

                SaveLanguageExclusions();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the LanguageMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LanguageMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_langExcl.Contains((sender as MenuItem).Tag as string))
            {
                _langExcl.Add((sender as MenuItem).Tag as string);

                SaveLanguageExclusions();
            }
        }

        /// <summary>
        /// Saves the exclusions to the XML settings file.
        /// </summary>
        public void SaveLanguageExclusions()
        {
            Settings.Set("Subtitle Language Exclusions", _langExcl.Aggregate(string.Empty, (current, lang) => current + (lang + ",")).Trim(','));
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
                    if (searchButton.Content.ToString() == "Cancel")
                    {
                        ActiveSearch.CancelAsync();
                        SubtitleSearchDone();
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

            if (searchButton.Content.ToString() == "Cancel")
            {
                ActiveSearch.CancelAsync();
                SubtitleSearchDone();
                return;
            }

            SubtitlesListViewItemCollection.Clear();

            textBox.IsEnabled    = false;
            searchButton.Content = "Cancel";

            ActiveSearch = new SubtitleSearch(SearchEngines
                                              .Where(engine => !_excludes.Contains(engine.Name))
                                              .Select(engine => engine.GetType()));

            ActiveSearch.SubtitleSearchDone            += SubtitleSearchDone;
            ActiveSearch.SubtitleSearchProgressChanged += SubtitleSearchProgressChanged;
            
            SetStatus("Searching for subtitles on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            ActiveSearch.SearchAsync(textBox.Text);
        }

        /// <summary>
        /// Called when a subtitle search progress has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.Collections.Generic.List&lt;RoliSoft.TVShowTracker.Parsers.Subtitles.SubtitleSearchEngine.Subtitle&gt;,System.Double,System.Collections.Generic.List&lt;System.String&gt;&gt;"/> instance containing the event data.</param>
        private void SubtitleSearchProgressChanged(object sender, EventArgs<List<Subtitle>, double, List<string>> e)
        {
            SetStatus("Searching for subtitles on " + (string.Join(", ", e.Third)) + "...", true);

            if (e.First != null)
            {
                Dispatcher.Invoke((Action)(() => SubtitlesListViewItemCollection.AddRange(e.First.Where(sub => !_langExcl.Contains(sub.Language.ToString())))));
            }
        }

        /// <summary>
        /// Called when a subtitle search is done on all engines.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void SubtitleSearchDone(object sender = null, EventArgs e = null)
        {
            ActiveSearch = null;
            
            Dispatcher.Invoke((Action)(() =>
                {
                    textBox.IsEnabled    = true;
                    searchButton.Content = "Search";

                    if (SubtitlesListViewItemCollection.Count != 0)
                    {
                        SetStatus("Found " + Utils.FormatNumber(SubtitlesListViewItemCollection.Count, "subtitle") + "!");
                    }
                    else
                    {
                        SetStatus("Couldn't find any subtitles.");
                    }
                }));
        }

        /// <summary>
        /// Handles the Click event of the DownloadSubtitle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadSubtitleClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var sub = (Subtitle)listView.SelectedValue;

            if (!sub.IsLinkDirect)
            {
                Utils.Run(sub.URL);
                return;
            }

            var uri = new Uri(sub.URL);
            SetStatus("Sending request to " + uri.DnsSafeHost.Replace("www.", string.Empty) + "...", true);

            var wc  = new Utils.SmarterWebClient();
            var tmp = Utils.GetRandomFileName();

            wc.Headers[HttpRequestHeader.Referer] = "http://" + uri.DnsSafeHost + "/";
            wc.DownloadFileCompleted             += WebClientDownloadFileCompleted;
            wc.DownloadProgressChanged           += (s, a) => SetStatus("Downloading file... (" + a.ProgressPercentage + "%)", true);

            wc.DownloadFileAsync(uri, tmp, new[] { tmp });
        }

        /// <summary>
        /// Handles the DownloadFileCompleted event of the wc control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.AsyncCompletedEventArgs"/> instance containing the event data.</param>
        private void WebClientDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var web   = sender as Utils.SmarterWebClient;
            var token = e.UserState as string[];
            var file  = web.FileName;

            var sfd = new SaveFileDialog
                {
                    CheckPathExists = true,
                    FileName        = file
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
        }
    }
}
