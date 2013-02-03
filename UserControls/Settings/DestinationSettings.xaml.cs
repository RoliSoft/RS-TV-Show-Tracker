namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.Senders;

    /// <summary>
    /// Interaction logic for DestinationSettings.xaml
    /// </summary>
    public partial class DestinationSettings
    {
        /// <summary>
        /// Gets or sets the destinations list view item collection.
        /// </summary>
        /// <value>The destinations list view item collection.</value>
        public ObservableCollection<DestinationListViewItem> DestinationsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DestinationSettings"/> class.
        /// </summary>
        public DestinationSettings()
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
                DestinationsListViewItemCollection = new ObservableCollection<DestinationListViewItem>();
                destinationsListView.ItemsSource = DestinationsListViewItemCollection;
                destinationsListView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Type"));

                ReloadList();
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;

            DestinationsListViewSelectionChanged();
        }

        /// <summary>
        /// Reloads the list.
        /// </summary>
        public void ReloadList()
        {
            DestinationsListViewItemCollection.Clear();
            
            // load torrent

            var atr = Utils.GetApplicationForExtension(".torrent");
            Tuple<string, BitmapSource> tri;
            if (!string.IsNullOrWhiteSpace(atr) && (tri = Utils.GetExecutableInfo(atr)) != null)
            {
                DestinationsListViewItemCollection.Add(new DestinationListViewItem
                    {
                        Icon      = tri.Item2,
                        Name      = tri.Item1 + " for .torrent",
                        Type      = "Default local associations",
                        GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/application-blue.png"
                    });
            }
            
            // load usenet

            var anz = Utils.GetApplicationForExtension(".nzb");
            Tuple<string, BitmapSource> nzi;
            if (!string.IsNullOrWhiteSpace(anz) && (nzi = Utils.GetExecutableInfo(anz)) != null)
            {
                DestinationsListViewItemCollection.Add(new DestinationListViewItem
                    {
                        Icon      = nzi.Item2,
                        Name      = nzi.Item1 + " for .nzb",
                        Type      = "Default local associations",
                        GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/application-blue.png"
                    });
            }
            
            // load dlc

            var adl = Utils.GetApplicationForExtension(".dlc");
            Tuple<string, BitmapSource> dli;
            if (!string.IsNullOrWhiteSpace(adl) && (dli = Utils.GetExecutableInfo(adl)) != null)
            {
                DestinationsListViewItemCollection.Add(new DestinationListViewItem
                    {
                        Icon      = dli.Item2,
                        Name      = dli.Item1 + " for .dlc",
                        Type      = "Default local associations",
                        GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/application-blue.png"
                    });
            }

            // load html

            var aht = Utils.GetApplicationForExtension(".html");
            Tuple<string, BitmapSource> hti;
            if (!string.IsNullOrWhiteSpace(aht) && (hti = Utils.GetExecutableInfo(aht)) != null)
            {
                DestinationsListViewItemCollection.Add(new DestinationListViewItem
                    {
                        Icon      = hti.Item2,
                        Name      = hti.Item1 + " for .html",
                        Type      = "Default local associations",
                        GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/application-blue.png"
                    });
            }

            // load alternatives

            foreach (var alt in Settings.Get<Dictionary<string, object>>("Alternative Associations"))
            {
                var lst = (List<string>)alt.Value;
                foreach (var app in lst)
                {
                    Tuple<string, BitmapSource> sci;
                    if ((sci = Utils.GetExecutableInfo(app)) != null)
                    {
                        DestinationsListViewItemCollection.Add(new DestinationListViewItem
                            {
                                ID        = alt.Key + "|" + app,
                                Icon      = sci.Item2,
                                Name      = sci.Item1 + " for " + alt.Key,
                                Type      = "Alternative local associations",
                                GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/application.png"
                            });
                    }
                    else
                    {
                        DestinationsListViewItemCollection.Add(new DestinationListViewItem
                            {
                                ID        = alt.Key + "|" + app,
                                Icon      = "pack://application:,,,/RSTVShowTracker;component/Images/exclamation.png",
                                Name      = Path.GetFileName(app) + " for " + alt.Key + " [File not found!]",
                                Type      = "Alternative local associations",
                                GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/application.png"
                            });
                    }
                }
            }

            // load senders

            var senders = Extensibility.GetNewInstances<SenderEngine>().ToList();

            foreach (var dest in Settings.Get<Dictionary<string, object>>("Sender Destinations"))
            {
                var conf = (Dictionary<string, object>)dest.Value;
                Uri uri;
                DestinationsListViewItemCollection.Add(new DestinationListViewItem
                    {
                        ID        = dest.Key,
                        Icon      = senders.First(s => s.Name == (string)conf["Sender"]).Icon,
                        Name      = (string)conf["Sender"] + " at " + (Uri.TryCreate((string)conf["Location"], UriKind.Absolute, out uri) ? uri.DnsSafeHost + ":" + uri.Port : (string)conf["Location"]),
                        Type      = "Remote servers",
                        GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/server-cast.png"
                    });
            }

            DestinationsListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the destinationsListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void DestinationsListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            destinationsEditButton.IsEnabled = destinationsRemoveButton.IsEnabled = destinationsListView.SelectedIndex != -1 && ((DestinationListViewItem)destinationsListView.SelectedItem).Type == "Remote servers";
        }
        
        /// <summary>
        /// Handles the Click event of the destinationsAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DestinationsAddButtonClick(object sender, RoutedEventArgs e)
        {
            if (new SenderSettingsWindow().ShowDialog().GetValueOrDefault())
            {
                ReloadList();
            }
        }

        /// <summary>
        /// Handles the Click event of the destinationsEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DestinationsEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (destinationsListView.SelectedIndex == -1) return;

            var sel = (DestinationListViewItem)destinationsListView.SelectedItem;

            if (sel.Type != "Remote servers") return;

            if (new SenderSettingsWindow(sel.ID).ShowDialog().GetValueOrDefault())
            {
                ReloadList();
            }
        }

        /// <summary>
        /// Handles the Click event of the destinationsRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DestinationsRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (destinationsListView.SelectedIndex == -1) return;

            var sel = (DestinationListViewItem)destinationsListView.SelectedItem;

            if (sel.Type != "Remote servers") return;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Name + "?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var dict = Settings.Get<Dictionary<string, object>>("Sender Destinations");

                dict.Remove(sel.ID);

                Settings.Set("Sender Destinations", dict);

                ReloadList();
            }
        }
    }
}
