namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;

    using RoliSoft.TVShowTracker.Parsers.News;

    /// <summary>
    /// Interaction logic for FeedsSettings.xaml
    /// </summary>
    public partial class FeedsSettings
    {
        /// <summary>
        /// Gets or sets the feeds list view item collection.
        /// </summary>
        /// <value>The feeds list view item collection.</value>
        public ObservableCollection<FeedsListViewItem> FeedsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedsSettings"/> class.
        /// </summary>
        public FeedsSettings()
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
                FeedsListViewItemCollection = new ObservableCollection<FeedsListViewItem>();
                listView.ItemsSource = FeedsListViewItemCollection;

                ReloadParsers();
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;
        }

        /// <summary>
        /// Reloads the parsers list view.
        /// </summary>
        private void ReloadParsers()
        {
            var idx = listView.SelectedIndex;
            listView.Items.GroupDescriptions.Clear();
            FeedsListViewItemCollection.Clear();

            foreach (var engine in Extensibility.GetNewInstances<FeedReaderEngine>().OrderBy(x => x.Language).ThenBy(x => x.Name))
            {
                FeedsListViewItemCollection.Add(new FeedsListViewItem
                    {
                        Enabled  = GuidesPage.Actives.Contains(engine.Name),
                        Icon     = "/RSTVShowTracker;component/Images/navigation.png",
                        Site     = engine.Name,
                        Language = Languages.List[engine.Language],
                        LangIcon = "pack://application:,,,/RSTVShowTracker;component/Images/flag-" + engine.Language + ".png"
                    });
            }

            listView.SelectedIndex = idx;
            listView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Language"));
        }

        /// <summary>
        /// Handles the Checked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            
            var item = (FeedsListViewItem)((FrameworkElement)sender).DataContext;

            if (!GuidesPage.Actives.Contains(item.Site))
            {
                GuidesPage.Actives.Add(item.Site);

                Settings.Set("Active News Sites", GuidesPage.Actives);
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

            var item = (FeedsListViewItem)((FrameworkElement)sender).DataContext;

            if (GuidesPage.Actives.Contains(item.Site))
            {
                GuidesPage.Actives.Remove(item.Site);

                Settings.Set("Active News Sites", GuidesPage.Actives);
            }
        }

        /// <summary>
        /// Handles the Click event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            Utils.Run(((Hyperlink)sender).NavigateUri.ToString());
        }
    }
}
