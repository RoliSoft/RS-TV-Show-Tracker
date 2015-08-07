namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Interaction logic for ParsersSettings.xaml
    /// </summary>
    public partial class ParsersSettings
    {
        /// <summary>
        /// Gets or sets the downloads list view item collection.
        /// </summary>
        /// <value>The downloads list view item collection.</value>
        public ObservableCollection<DownloadsListViewItem> DownloadsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsersSettings"/> class.
        /// </summary>
        public ParsersSettings()
        {
            InitializeComponent();
        }

        private bool _loaded;

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loaded) return;

            try
            {
                DownloadsListViewItemCollection = new ObservableCollection<DownloadsListViewItem>();
                listView.ItemsSource = DownloadsListViewItemCollection;

                ReloadParsers();
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;

            ListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            parserEditButton.IsEnabled = listView.SelectedIndex != -1;
            moveUpButton.IsEnabled     = listView.SelectedIndex > 0 && ((DownloadsListViewItem)listView.SelectedItem).Type == "Download links";
            moveDownButton.IsEnabled   = listView.SelectedIndex != -1 && listView.SelectedIndex < listView.Items.Count - 1  && ((DownloadsListViewItem)listView.SelectedItem).Type == "Download links";
        }

        /// <summary>
        /// Reloads the parsers list view.
        /// </summary>
        private void ReloadParsers()
        {
            var idx = listView.SelectedIndex;
            listView.Items.GroupDescriptions.Clear();
            DownloadsListViewItemCollection.Clear();

            foreach (var engine in AutoDownloader.SearchEngines.OrderBy(engine => AutoDownloader.Parsers.IndexOf(engine.Name)))
            {
                DownloadsListViewItemCollection.Add(new DownloadsListViewItem
                    {
                        Enabled = AutoDownloader.Actives.Contains(engine.Name),
                        Type    = "Download links",
                        Icon    = "/RSTVShowTracker;component/Images/navigation.png",
                        Site    = engine.Name,
                        Login   = engine.Private
                                  ? !string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Login"))
                                    ? "/RSTVShowTracker;component/Images/tick.png"
                                    : !string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Cookies"))
                                      ? "/RSTVShowTracker;component/Images/cookie.png"
                                      : "/RSTVShowTracker;component/Images/cross.png"
                                  : "/RSTVShowTracker;component/Images/na.png",
                        Version = engine.Version.ToString().PadRight(14, '0')
                    });
            }
            
            foreach (var engine in SubtitlesPage.SearchEngines)
            {
                DownloadsListViewItemCollection.Add(new DownloadsListViewItem
                    {
                        Enabled = SubtitlesPage.Actives.Contains(engine.Name),
                        Type    = "Subtitles",
                        Icon    = "/RSTVShowTracker;component/Images/navigation.png",
                        Site    = engine.Name,
                        Login   = engine.Private
                                  ? !string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Login"))
                                    ? "/RSTVShowTracker;component/Images/tick.png"
                                    : !string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Cookies"))
                                      ? "/RSTVShowTracker;component/Images/cookie.png"
                                      : "/RSTVShowTracker;component/Images/cross.png"
                                  : "/RSTVShowTracker;component/Images/na.png",
                        Version = engine.Version.ToString().PadRight(14, '0')
                    });
            }

            listView.SelectedIndex = idx;
            listView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
            ListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the Checked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            var item = (DownloadsListViewItem)((FrameworkElement)sender).DataContext;

            switch (item.Type)
            {
                case "Download links":
                    if (!AutoDownloader.Actives.Contains(item.Site))
                    {
                        AutoDownloader.Actives.Add(item.Site);

                        Settings.Set("Active Trackers", AutoDownloader.Actives);
                    }
                    break;

                case "Subtitles":
                    if (!SubtitlesPage.Actives.Contains(item.Site))
                    {
                        SubtitlesPage.Actives.Add(item.Site);

                        Settings.Set("Active Subtitle Sites", SubtitlesPage.Actives);
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            var item = (DownloadsListViewItem)((FrameworkElement)sender).DataContext;

            switch (item.Type)
            {
                case "Download links":
                    if (AutoDownloader.Actives.Contains(item.Site))
                    {
                        AutoDownloader.Actives.Remove(item.Site);

                        Settings.Set("Active Trackers", AutoDownloader.Actives);
                    }
                    break;

                case "Subtitles":
                    if (SubtitlesPage.Actives.Contains(item.Site))
                    {
                        SubtitlesPage.Actives.Remove(item.Site);

                        Settings.Set("Active Subtitle Sites", SubtitlesPage.Actives);
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles the Click event of the parserEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ParserEditButtonClick(object sender, RoutedEventArgs e)
        {
            dynamic parser = null;
            var sel = (DownloadsListViewItem)listView.SelectedItem;

            switch (sel.Type)
            {
                case "Download links":
                    parser = AutoDownloader.SearchEngines.Single(en => en.Name == sel.Site);
                    break;

                case "Subtitles":
                    parser = SubtitlesPage.SearchEngines.Single(en => en.Name == sel.Site);
                    break;
            }

            if (parser == null)
            {
                return;
            }

            if (parser.Private)
            {
                if (new ParserWindow(parser).ShowDialog() == true) // Nullable<bool> vs true
                {
                    ReloadParsers();
                }
            }
            else
            {
                MessageBox.Show(parser.Name + " does not require authentication to search the site.", "Login not required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Click event of the moveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1 || ((DownloadsListViewItem)listView.SelectedItem).Type != "Download links") return;

            var idx = AutoDownloader.Parsers.IndexOf(((DownloadsListViewItem)listView.SelectedItem).Site);

            if (idx != 0)
            {
                AutoDownloader.Parsers.MoveUp(idx);
                listView.Items.GroupDescriptions.Clear();
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex - 1);
                listView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
                ListViewSelectionChanged();

                Settings.Set("Tracker Order", AutoDownloader.Parsers);
            }
        }

        /// <summary>
        /// Handles the Click event of the moveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1 || ((DownloadsListViewItem)listView.SelectedItem).Type != "Download links") return;

            var idx = AutoDownloader.Parsers.IndexOf(((DownloadsListViewItem)listView.SelectedItem).Site);

            if (idx != AutoDownloader.Parsers.Count - 1)
            {
                AutoDownloader.Parsers.MoveDown(idx);
                listView.Items.GroupDescriptions.Clear();
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex + 1);
                listView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
                ListViewSelectionChanged();

                Settings.Set("Tracker Order", AutoDownloader.Parsers);
            }
        }
    }
}
