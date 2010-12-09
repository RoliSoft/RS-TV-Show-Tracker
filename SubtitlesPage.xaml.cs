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

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.Helpers;
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
        public ObservableCollection<SubtitleSearchEngine.Subtitle> SubtitlesListViewItemCollection { get; set; }

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
            Dispatcher.Invoke((Func<bool>)delegate
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
                return true;
            });
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
                SubtitlesListViewItemCollection = new ObservableCollection<SubtitleSearchEngine.Subtitle>();
                listView.ItemsSource            = SubtitlesListViewItemCollection;
            }
        }

        /// <summary>
        /// Initiates a search on this usercontrol.
        /// </summary>
        /// <param name="query">The query.</param>
        public void Search(string query)
        {
            Dispatcher.Invoke((Func<bool>)delegate
            {
                // cancel if one is running
                if (searchButton.Content.ToString() == "Cancel")
                {
                    ActiveSearch.CancelAsync();
                    SubtitleSearchDone();
                }

                textBox.Text = query;
                SearchButtonClick(null, null);

                return true;
            });
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

            ActiveSearch                                = new SubtitleSearch();
            ActiveSearch.SubtitleSearchDone            += SubtitleSearchDone;
            ActiveSearch.SubtitleSearchProgressChanged += SubtitleSearchProgressChanged;
            
            SetStatus("Searching for subtitles on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            ActiveSearch.SearchAsync(textBox.Text);
        }

        /// <summary>
        /// Called when a subtitle search progress has changed.
        /// </summary>
        private void SubtitleSearchProgressChanged(List<SubtitleSearchEngine.Subtitle> subtitles, double percentage, List<string> remaining)
        {
            SetStatus("Searching for subtitles on " + (string.Join(", ", remaining)) + "...", true);

            Dispatcher.Invoke((Func<bool>)delegate
                {
                    if (subtitles != null)
                    {
                        SubtitlesListViewItemCollection.AddRange(subtitles.Where(sub => sub.Language != SubtitleSearchEngine.Subtitle.Languages.Unknown));
                    }

                    return true;
                });
        }

        /// <summary>
        /// Called when a subtitle search is done on all engines.
        /// </summary>
        private void SubtitleSearchDone()
        {
            Dispatcher.Invoke((Func<bool>)delegate
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

                    return true;
                });
        }

        /// <summary>
        /// Handles the Click event of the DownloadSubtitle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadSubtitleClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var sub = (SubtitleSearchEngine.Subtitle)listView.SelectedValue;

            if (!sub.IsLinkDirect)
            {
                Utils.Run(sub.URL);
                return;
            }

            var uri = new Uri(sub.URL);
            SetStatus("Sending request to " + uri.DnsSafeHost.Replace("www.", string.Empty) + "...", true);

            var wc  = new WebClientExt();
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
            var web   = sender as WebClientExt;
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
