namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;

    /// <summary>
    /// Interaction logic for XMLTVSettings.xaml
    /// </summary>
    public partial class XMLTVSettings
    {
        /// <summary>
        /// Gets or sets the xmltv list view item collection.
        /// </summary>
        /// <value>The xmltv list view item collection.</value>
        public ObservableCollection<XMLTVListViewItem> XMLTVListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the titles list view item collection.
        /// </summary>
        /// <value>The titles list view item collection.</value>
        public ObservableCollection<TitlesListViewItem> TitlesListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XMLTVSettings"/> class.
        /// </summary>
        public XMLTVSettings()
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
                XMLTVListViewItemCollection = new ObservableCollection<XMLTVListViewItem>();
                xmltvListView.ItemsSource = XMLTVListViewItemCollection;

                ReloadConfigs();

                TitlesListViewItemCollection = new ObservableCollection<TitlesListViewItem>();
                titlesListView.ItemsSource = TitlesListViewItemCollection;
                titlesListView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Language"));

                ReloadTitles();
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            _loaded = true;

            XMLTVListViewSelectionChanged();
        }

        /// <summary>
        /// Reloads the XMLTV list view.
        /// </summary>
        public void ReloadConfigs()
        {
            XMLTVListViewItemCollection.Clear();

            foreach (var setting in Settings.Get<List<Dictionary<string, object>>>("XMLTV"))
            {
                if (!setting.ContainsKey("Name") || !(setting["Name"] is string) || !setting.ContainsKey("File") || !(setting["File"] is string))
                {
                    continue;
                }
                
                XMLTVListViewItemCollection.Add(new XMLTVListViewItem(setting));
            }

            XMLTVListViewSelectionChanged();
        }

        /// <summary>
        /// Reloads the titles list view.
        /// </summary>
        public void ReloadTitles()
        {
            TitlesListViewItemCollection.Clear();

            var langs = Settings.Get<List<Dictionary<string, object>>>("XMLTV").Where(x => x.ContainsKey("Language") && x["Language"] is string && ((string)x["Language"]).Length == 2 && (string)x["Language"] != "en").Select(x => ((string)x["Language"]).ToLower()).Distinct().ToList();

            foreach (var show in Database.TVShows.Values.OrderBy(t => t.Name))
            {
                foreach (var lang in langs)
                {
                    TitlesListViewItemCollection.Add(new TitlesListViewItem
                                                         {
                                                             Show     = show,
                                                             Title    = show.Name,
                                                             Foreign  = show.GetForeignTitle(lang),
                                                             Language = Languages.List[lang]
                                                         });
                }
            }

            TitlesListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the xmltvListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void XMLTVListViewSelectionChanged(object sender = null, System.Windows.Controls.SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            xmltvEditButton.IsEnabled = xmltvRemoveButton.IsEnabled = xmltvListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the Click event of the xmltvAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void XMLTVAddButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            new XMLTVWindow().ShowDialog();

            ReloadConfigs();
            ReloadTitles();
            MainWindow.Active.activeGuidesPage.Refresh();
        }

        /// <summary>
        /// Handles the Click event of the xmltvEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void XMLTVEditButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (xmltvListView.SelectedIndex == -1) return;

            new XMLTVWindow(((XMLTVListViewItem)xmltvListView.SelectedItem).Config).ShowDialog();

            ReloadConfigs();
            ReloadTitles();
            MainWindow.Active.activeGuidesPage.Refresh();
        }

        /// <summary>
        /// Handles the Click event of the xmltvRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void XMLTVRemoveButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (xmltvListView.SelectedIndex == -1) return;

            var sel = (XMLTVListViewItem)xmltvListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Name + " (" + sel.File + ")?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Settings.Get<List<Dictionary<string, object>>>("XMLTV").Remove(sel.Config);
                Settings.Save();

                ReloadConfigs();
                ReloadTitles();
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the titlesListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void TitlesListViewSelectionChanged(object sender = null, System.Windows.Controls.SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            titlesSearchButton.IsEnabled = titlesRemoveButton.IsEnabled = titlesListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the Click event of the titlesRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TitlesRemoveButtonClick(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Handles the Click event of the titlesSearchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TitlesSearchButtonClick(object sender, RoutedEventArgs e)
        {

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
