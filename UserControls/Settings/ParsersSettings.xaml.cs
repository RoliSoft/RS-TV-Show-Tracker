namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

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
                MainWindow.Active.HandleUnexpectedException(ex);
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
            moveUpButton.IsEnabled     = listView.SelectedIndex > 0;
            moveDownButton.IsEnabled   = listView.SelectedIndex != -1 && listView.SelectedIndex < listView.Items.Count - 1;
        }

        /// <summary>
        /// Reloads the parsers list view.
        /// </summary>
        private void ReloadParsers()
        {
            var idx = listView.SelectedIndex;
            DownloadsListViewItemCollection.Clear();

            foreach (var engine in AutoDownloader.SearchEngines.OrderBy(engine => AutoDownloader.Parsers.IndexOf(engine.Name)))
            {
                DownloadsListViewItemCollection.Add(new DownloadsListViewItem
                    {
                        Enabled = AutoDownloader.Actives.Contains(engine.Name),
                        Icon    = engine.Icon,
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

            if (!AutoDownloader.Actives.Contains((sender as CheckBox).Tag as string))
            {
                AutoDownloader.Actives.Add((sender as CheckBox).Tag as string);

                Settings.Set("Active Trackers", AutoDownloader.Actives);
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

            if (AutoDownloader.Actives.Contains((sender as CheckBox).Tag as string))
            {
                AutoDownloader.Actives.Remove((sender as CheckBox).Tag as string);

                Settings.Set("Active Trackers", AutoDownloader.Actives);
            }
        }

        /// <summary>
        /// Handles the Click event of the parserEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ParserEditButtonClick(object sender, RoutedEventArgs e)
        {
            var sel = listView.SelectedItem as DownloadsListViewItem;
            var prs = AutoDownloader.SearchEngines.Single(en => en.Name == sel.Site);

            if (prs.Private)
            {
                if (new ParserWindow(prs).ShowDialog() == true) // Nullable<bool> vs true
                {
                    ReloadParsers();
                }
            }
            else
            {
                MessageBox.Show(prs.Name + " does not require authentication to search the site.", "Login not required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Click event of the moveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var idx = AutoDownloader.Parsers.IndexOf((listView.SelectedItem as DownloadsListViewItem).Site);

            if (idx != 0)
            {
                AutoDownloader.Parsers.MoveUp(idx);
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex - 1);
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
            if (listView.SelectedIndex == -1) return;

            var idx = AutoDownloader.Parsers.IndexOf((listView.SelectedItem as DownloadsListViewItem).Site);

            if (idx != AutoDownloader.Parsers.Count - 1)
            {
                AutoDownloader.Parsers.MoveDown(idx);
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex + 1);
                ListViewSelectionChanged();

                Settings.Set("Tracker Order", AutoDownloader.Parsers);
            }
        }
    }
}
