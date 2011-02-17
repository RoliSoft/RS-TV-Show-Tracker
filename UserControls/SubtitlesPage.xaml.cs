namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Interaction logic for SubtitlesPage.xaml
    /// </summary>
    public partial class SubtitlesPage
    {
        /// <summary>
        /// Gets or sets the subtitles list view item collection.
        /// </summary>
        /// <value>The subtitles list view item collection.</value>
        public ObservableCollection<SubtitleItem> SubtitlesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the search engines active in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public List<SubtitleSearchEngine> SearchEngines { get; set; }

        private List<string> _actives, _langs;

        private volatile bool _searching;

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
                SubtitlesListViewItemCollection = new ObservableCollection<SubtitleItem>();
                listView.ItemsSource            = SubtitlesListViewItemCollection;
            }

            if (SearchEngines == null)
            {
                SearchEngines = typeof(SubtitleSearchEngine)
                                .GetDerivedTypes()
                                .Select(type => Activator.CreateInstance(type) as SubtitleSearchEngine)
                                .OrderBy(engine => engine.Name)
                                .ToList();
            }

            if (_actives == null)
            {
                _actives = Settings.GetList("Active Subtitle Sites").ToList();
            }

            if (_langs == null)
            {
                _langs = Settings.GetList("Active Subtitle Languages").ToList();
            }

            if (availableEngines.Items.Count == 0)
            {
                foreach (var engine in SearchEngines)
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
                            Margin = new Thickness(3, -2, 0, 0)
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
                foreach (var lang in Languages.List)
                {
                    var mi = new MenuItem
                        {
                            Header           = new StackPanel { Orientation = Orientation.Horizontal },
                            IsCheckable      = true,
                            IsChecked        = _langs.Contains(lang.Key),
                            StaysOpenOnClick = true,
                            Tag              = lang.Key
                        };

                    (mi.Header as StackPanel).Children.Add(new Image
                        {
                            Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/flag-" + lang.Key + ".png", UriKind.Relative)),
                            Width  = 16,
                            Height = 16,
                            Margin = new Thickness(3, -2, 0, 0)
                        });
                    (mi.Header as StackPanel).Children.Add(new Label
                        {
                            Content = lang.Value,
                            Padding = new Thickness(5, 0, 0, 0)
                        });

                    mi.Checked   += LanguageMenuItemChecked;
                    mi.Unchecked += LanguageMenuItemUnchecked;

                    languages.Items.Add(mi);
                }

                var mi2 = new MenuItem
                    {
                        Header           = new StackPanel { Orientation = Orientation.Horizontal },
                        IsCheckable      = true,
                        IsChecked        = _langs.Contains("null"),
                        StaysOpenOnClick = true,
                        Tag              = "null"
                    };

                (mi2.Header as StackPanel).Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/unknown.png", UriKind.Relative)),
                        Width  = 16,
                        Height = 16,
                        Margin = new Thickness(3, -2, 0, 0)
                    });
                (mi2.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Unknown",
                        Padding = new Thickness(5, 0, 0, 0)
                    });

                mi2.Checked   += LanguageMenuItemChecked;
                mi2.Unchecked += LanguageMenuItemUnchecked;

                languages.Items.Add(mi2);
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

                SaveActiveSites();
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

                SaveActiveSites();
            }
        }

        /// <summary>
        /// Saves the active sites to the XML settings file.
        /// </summary>
        public void SaveActiveSites()
        {
            Settings.Set("Active Subtitle Sites", _actives);
        }

        /// <summary>
        /// Handles the Checked event of the LanguageMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LanguageMenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (!_langs.Contains((sender as MenuItem).Tag as string))
            {
                _langs.Add((sender as MenuItem).Tag as string);

                SaveActiveLanguages();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the LanguageMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LanguageMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (_langs.Contains((sender as MenuItem).Tag as string))
            {
                _langs.Remove((sender as MenuItem).Tag as string);

                SaveActiveLanguages();
            }
        }

        /// <summary>
        /// Saves the active languages to the XML settings file.
        /// </summary>
        public void SaveActiveLanguages()
        {
            Settings.Set("Active Subtitle Languages", _langs);
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
                        SubtitleSearchDone();
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
                SubtitleSearchDone();
                return;
            }

            SubtitlesListViewItemCollection.Clear();

            textBox.IsEnabled    = false;
            searchButton.Content = "Cancel";

            ActiveSearch = new SubtitleSearch(SearchEngines
                                              .Where(engine => _actives.Contains(engine.Name))
                                              .Select(engine => engine.GetType()));

            ActiveSearch.SubtitleSearchDone            += SubtitleSearchDone;
            ActiveSearch.SubtitleSearchProgressChanged += SubtitleSearchProgressChanged;
            ActiveSearch.SubtitleSearchError           += SubtitleSearchError;
            
            SetStatus("Searching for subtitles on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            _searching = true;

            ActiveSearch.SearchAsync(textBox.Text);

            Utils.Win7Taskbar(0, TaskbarProgressBarState.Normal);
        }

        /// <summary>
        /// Called when a subtitle search progress has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SubtitleSearchProgressChanged(object sender, EventArgs<List<Subtitle>, double, List<string>> e)
        {
            if (!_searching)
            {
                return;
            }

            SetStatus("Searching for subtitles on " + (string.Join(", ", e.Third)) + "...", true);
            Utils.Win7Taskbar((int)e.Second);

            if (e.First != null)
            {
                Dispatcher.Invoke((Action)(() => SubtitlesListViewItemCollection.AddRange(e.First
                                                                                           .Where(sub => _langs.Contains(sub.Language))
                                                                                           .Select(sub => new SubtitleItem(sub)))));
            }
        }

        /// <summary>
        /// Called when a subtitle search is done on all engines.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void SubtitleSearchDone(object sender = null, EventArgs e = null)
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
        /// Called when the subtitle search has encountered an unexpected error.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void SubtitleSearchError(object sender, EventArgs<string, Exception> e)
        {
            if (e.Second is WebException || e.Second is CookComputing.XmlRpc.XmlRpcException)
            {
                // Exceptions such as
                // - The operation has timed out
                // - The remote server returned an error: (503) Server Unavailable.
                // - Unable to connect to the remote server
                // - Service Not Available (seen in XML-RPC, usually when OpenSubtitles is on vacation)
                // indicate that the server is either unreachable or is under heavy load,
                // and these problems are not parsing bugs, so they will be ignored.
                return;
            }

            MainWindow.Active.HandleUnexpectedException(e.Second);
        }

        /// <summary>
        /// Handles the ContextMenuOpening event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.ContextMenuEventArgs"/> instance containing the event data.</param>
        private void ListViewContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (listView.SelectedIndex == -1)
            {
                e.Handled = true;
            }
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

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
            SetStatus("Sending request to " + new Uri(sub.URL).DnsSafeHost.Replace("www.", string.Empty) + "...", true);

            var dl = sub.Source.Downloader;

            dl.DownloadFileCompleted   += DownloadFileCompleted;
            dl.DownloadProgressChanged += (s, a) => SetStatus("Downloading file... (" + a.Data + "%)", true);

            dl.Download(sub, Utils.GetRandomFileName());
        }

        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadFileCompleted(object sender, EventArgs<string, string, string> e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (e.Third == "LaunchedBrowser")
            {
                SetStatus("File sent to browser successfully.");
                return;
            }

            var sfd = new SaveFileDialog
                {
                    CheckPathExists = true,
                    FileName        = e.Second
                };

            if (sfd.ShowDialog().Value)
            {
                if (File.Exists(sfd.FileName))
                {
                    File.Delete(sfd.FileName);
                }

                File.Move(e.First, sfd.FileName);
            }
            else
            {
                File.Delete(e.First);
            }

            SetStatus("File downloaded successfully.");
        }

        /// <summary>
        /// Handles the Click event of the DownloadSubtitle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadSubtitleNearVideoClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var sub   = (Subtitle)listView.SelectedValue;
            var path  = Settings.Get("Download Path");
            var show  = ShowNames.Tools.Split(textBox.Text);
            var query = textBox.Text;

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                new TaskDialog
                    {
                        Icon            = TaskDialogStandardIcon.Error,
                        Caption         = "Search path not configured",
                        InstructionText = "Search path not configured",
                        Text            = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'.",
                        Cancelable      = true
                    }.Show();
                return;
            }
            
            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
            SetStatus("Searching for " + show[0] + " " + show[1] + " on the disk...", true);

            var finder = new FileSearch(path, show[0], show[1]);
            finder.FileSearchDone += (d, f) =>
                {
                    if (finder.Files.Count == 0)
                    {
                        new TaskDialog
                            {
                                Icon            = TaskDialogStandardIcon.Error,
                                Caption         = "No files found",
                                InstructionText = query,
                                Text            = "No files were found for this episode.\r\nUse the first option to download the subtitle and locate the file manually.",
                                Cancelable      = true
                            }.Show();

                        Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                        SetStatus("Couldn't find video file for the subtitle.");
                        return;
                    }

                    SetStatus("Sending request to " + new Uri(sub.URL).DnsSafeHost.Replace("www.", string.Empty) + "...", true);

                    var dl = sub.Source.Downloader;

                    dl.DownloadProgressChanged += (s, a) => SetStatus("Downloading file... (" + a.Data + "%)", true);
                    dl.DownloadFileCompleted   += (s, a) =>
                        {
                            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                            var td = new TaskDialog
                                {
                                    Icon            = TaskDialogStandardIcon.Information,
                                    Caption         = "Download subtitle near video",
                                    InstructionText = sub.Release,
                                    Text            = "The following files were found for {0}.\r\nSelect the desired video file and the subtitle will be placed in the same directory with the same name.".FormatWith(query),
                                    Cancelable      = true,
                                    StandardButtons = TaskDialogStandardButtons.Cancel
                                };

                            foreach (var file in finder.Files)
                            {
                                var tmp     = file;
                                var fi      = new FileInfo(file);
                                var quality = ThePirateBay.ParseQuality(file);
                                var instr   = string.Empty;

                                if (quality != Parsers.Downloads.Qualities.Unknown)
                                {
                                    instr = quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                                }

                                instr += Utils.GetFileSize(fi.Length)
                                       + Environment.NewLine
                                       + fi.DirectoryName;

                                var fd = new TaskDialogCommandLink
                                    {
                                        Text        = fi.Name,
                                        Instruction = instr
                                    };
                                fd.Click += (x, r) =>
                                    {
                                        td.Close();
                                        
                                        var dest = Path.ChangeExtension(tmp, new FileInfo(a.Second).Extension);
                                        if (File.Exists(dest))
                                        {
                                            File.Delete(dest);
                                        }

                                        File.Move(a.First, dest);
                                    };

                                td.Controls.Add(fd);
                            }

                            td.Show();
                            SetStatus("File downloaded successfully.");
                        };

                    dl.Download(sub, Utils.GetRandomFileName());
                };
            finder.BeginSearch();
        }
    }
}
