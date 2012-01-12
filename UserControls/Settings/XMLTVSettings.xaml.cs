namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;

    using VistaControls.TaskDialog;

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
            TitlesListViewSelectionChanged();
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
                                                             Foreign2 = show.GetForeignTitle(lang),
                                                             LangCode = lang,
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
        private void TitlesListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
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
            if (titlesListView.SelectedIndex == -1) return;

            var sels = titlesListView.SelectedItems.OfType<TitlesListViewItem>().ToList();

            if (MessageBox.Show("Are you sure you want to remove the foreign title of " + (sels.Count == 1 ? sels[0].Title : sels.Count + " shows") + "?", "Clear foreign titles", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var sel in sels)
                {
                    Database.ShowData(sel.Show.ShowID, "title." + sel.LangCode, string.Empty);
                }

                ReloadTitles();
            }
        }

        /// <summary>
        /// Handles the Click event of the titlesSearchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TitlesSearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (titlesListView.SelectedIndex == -1) return;

            var sels = titlesListView.SelectedItems.OfType<TitlesListViewItem>().ToList();

            if (MessageBox.Show("Are you sure you want to search for the foreign title of " + (sels.Count == 1 ? sels[0].Title : sels.Count + " shows") + " on an external service?", "Search foreign titles", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var td = new TaskDialog
                    {
                        Title           = "Searching foreign titles...",
                        Instruction     = "Searching foreign titles...",
                        CommonButtons   = TaskDialogButton.Cancel,
                        ShowProgressBar = true
                    };
                
                var thd = new Thread(() =>
                    {
                        var i = 1;
                        foreach (var sel in sels)
                        {
                            Database.ShowData(sel.Show.ShowID, "title." + sel.LangCode, string.Empty);
                            sel.Show.GetForeignTitle(sel.LangCode, true, s => td.Content = s);

                            td.ProgressBarPosition = (int) Math.Round(((double) i/(double) sels.Count)*100d);
                            i++;
                        }

                        td.SimulateButtonClick(-1);
                    });

                td.ButtonClick += (o, a) =>
                    {
                        try
                        {
                            thd.Abort();
                        }
                        catch { }

                        Dispatcher.Invoke((Action)ReloadTitles);
                    };

                new Thread(() => td.Show()).Start();

                thd.Start();
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the ForeignTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void ForeignTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_loaded) return;

            var txtbox = (TextBox)sender;
            var parent = (StackPanel)txtbox.Parent;
            var show   = (TitlesListViewItem)txtbox.DataContext;

            if (txtbox.Background == Brushes.White)
            {
                if ((show.Foreign2 ?? string.Empty) != txtbox.Text.Trim())
                {
                    txtbox.Width -= 16 * 2 + 3;
                    txtbox.Background = Brushes.LightCyan;
                    parent.Children[1].Visibility = parent.Children[2].Visibility = Visibility.Visible;
                }
            }
            else if (txtbox.Background == Brushes.LightCyan)
            {
                if ((show.Foreign2 ?? string.Empty) == txtbox.Text.Trim())
                {
                    txtbox.Width += 16 * 2 + 3;
                    txtbox.Background = Brushes.White;
                    parent.Children[1].Visibility = parent.Children[2].Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Handles the KeyDown event of the ForeignTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void ForeignTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ForeignTickMouseLeftButtonUp((Image)((StackPanel)((TextBox)sender).Parent).Children[1], null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ForeignCrossMouseLeftButtonUp((Image)((StackPanel)((TextBox)sender).Parent).Children[2], null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the ForeignTick control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ForeignTickMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var image  = (Image)sender;
            var parent = (StackPanel)image.Parent;
            var txtbox = (TextBox)parent.Children[0];
            var show   = (TitlesListViewItem)txtbox.DataContext;
            var title  = string.Empty;

            if (txtbox.Text.Trim().Length != 0)
            {
                title = txtbox.Text.Trim();
            }

            txtbox.Text = show.Foreign = show.Foreign2 = title;
            Database.ShowData(show.Show.ShowID, "title." + show.LangCode, title);

            ForeignTextBoxTextChanged(txtbox, null);
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the ForeignCross control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ForeignCrossMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var image  = (Image)sender;
            var parent = (StackPanel)image.Parent;
            var txtbox = (TextBox)parent.Children[0];
            var show   = (TitlesListViewItem)txtbox.DataContext;

            txtbox.Text = show.Foreign2;
            ForeignTextBoxTextChanged(txtbox, null);
        }


        /// <summary>
        /// Handles the GotFocus event of the ForeignTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ForeignTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            var txtbox = (TextBox)sender;

            if (titlesListView.SelectedItem != txtbox.DataContext)
            {
                titlesListView.SelectedItem = txtbox.DataContext;
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
