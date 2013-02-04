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
    using System.Windows.Forms;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.Senders;

    using TaskDialogInterop;

    using MessageBox = System.Windows.MessageBox;

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

                if (lst == null || lst.Count == 0)
                {
                    continue;
                }

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

            // load folders

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
                        DestinationsListViewItemCollection.Add(new DestinationListViewItem
                            {
                                ID        = alt.Key + "|" + app,
                                Icon      = "pack://application:,,,/RSTVShowTracker;component/Images/folder-open-document.png",
                                Name      = Path.GetFileName(app) + " for " + alt.Key,
                                Type      = "Folder destinations",
                                GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/folder.png"
                            });
                    }
                    else
                    {
                        DestinationsListViewItemCollection.Add(new DestinationListViewItem
                            {
                                ID        = alt.Key + "|" + app,
                                Icon      = "pack://application:,,,/RSTVShowTracker;component/Images/exclamation.png",
                                Name      = Path.GetFileName(app) + " for " + alt.Key + " [Folder not found!]",
                                Type      = "Folder destinations",
                                GroupIcon = "pack://application:,,,/RSTVShowTracker;component/Images/folder.png"
                            });
                    }
                }
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

            destinationsEditButton.IsEnabled = destinationsRemoveButton.IsEnabled = destinationsListView.SelectedIndex != -1 && ((DestinationListViewItem)destinationsListView.SelectedItem).Type != "Default local associations";
        }
        
        /// <summary>
        /// Handles the Click event of the destinationsAddAssocButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DestinationsAddAssocButtonClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    Title           = "Select an executable file",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect     = false,
                    Filter          = "Executable|*.exe"
                };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var inf = Utils.GetExecutableInfo(ofd.FileName, false);
            var types = new[] { ".torrent", ".nzb", ".dlc" };
            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Add a new local association",
                    AllowDialogCancellation = true,
                    Content                 = "Please select the file type you want to be associated with " + inf.Item1 + ":",
                    CommandButtons          = types
                });

            if (!res.CommandButtonResult.HasValue || res.CommandButtonResult.Value < 0 || res.CommandButtonResult.Value >= types.Length)
            {
                return;
            }

            var dict = Settings.Get<Dictionary<string, object>>("Alternative Associations");

            if (!dict.ContainsKey(types[res.CommandButtonResult.Value]) || dict[types[res.CommandButtonResult.Value]] == null)
            {
                dict[types[res.CommandButtonResult.Value]] = new List<string>();
            }

            var skey = (List<string>)dict[types[res.CommandButtonResult.Value]];

            skey.Add(ofd.FileName);

            Settings.Set("Alternative Associations", dict);

            ReloadList();
        }

        /// <summary>
        /// Handles the Click event of the destinationsAddRemoteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DestinationsAddRemoteButtonClick(object sender, RoutedEventArgs e)
        {
            if (new SenderSettingsWindow().ShowDialog().GetValueOrDefault())
            {
                ReloadList();
            }
        }

        /// <summary>
        /// Handles the Click event of the destinationsAddFolderButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DestinationsAddFolderButtonClick(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true,
                    Description         = "Select a folder to save files to:"
                };

            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var types = new[] { ".torrent", ".nzb", ".dlc" };
            var res = TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Add a new folder destination",
                    AllowDialogCancellation = true,
                    Content                 = "Please select the file type you want to save into " + Path.GetFileName(fbd.SelectedPath) + ":",
                    CommandButtons          = types
                });

            if (!res.CommandButtonResult.HasValue || res.CommandButtonResult.Value < 0 || res.CommandButtonResult.Value >= types.Length)
            {
                return;
            }

            var dict = Settings.Get<Dictionary<string, object>>("Folder Destinations");

            if (!dict.ContainsKey(types[res.CommandButtonResult.Value]) || dict[types[res.CommandButtonResult.Value]] == null)
            {
                dict[types[res.CommandButtonResult.Value]] = new List<string>();
            }

            var skey = (List<string>)dict[types[res.CommandButtonResult.Value]];

            skey.Add(fbd.SelectedPath);

            Settings.Set("Folder Destinations", dict);

            ReloadList();
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

            switch (sel.Type)
            {
                case "Remote servers":
                {
                    if (new SenderSettingsWindow(sel.ID).ShowDialog().GetValueOrDefault())
                    {
                        ReloadList();
                    }
                }
                break;

                case "Alternative local associations":
                {
                    var ids  = sel.ID.Split("|".ToCharArray(), 2);
                    var dict = Settings.Get<Dictionary<string, object>>("Alternative Associations");
                    var skey = (List<string>)dict[ids[0]];

                    var res = TaskDialog.Show(new TaskDialogOptions
                        {
                            Title                   = "Edit a local association",
                            AllowDialogCancellation = true,
                            Content                 = "What would you like to change about " + sel.Name + "?",
                            CommandButtons          = new[] { "Associated file type", "Executable location", "Nothing" }
                        });

                    if (!res.CommandButtonResult.HasValue || res.CommandButtonResult.Value == 2)
                    {
                        return;
                    }

                    switch (res.CommandButtonResult.Value)
                    {
                        case 0:
                        {
                            var types = new[] { ".torrent", ".nzb", ".dlc" };
                            var res2 = TaskDialog.Show(new TaskDialogOptions
                                {
                                    Title                   = "Edit a local association",
                                    AllowDialogCancellation = true,
                                    Content                 = "Please select a new file type you want to be associated with " + sel.Name + ":",
                                    CommandButtons          = types
                                });

                            if (!res2.CommandButtonResult.HasValue || res2.CommandButtonResult.Value < 0 || res2.CommandButtonResult.Value >= types.Length)
                            {
                                return;
                            }

                            skey.Remove(ids[1]);

                            if (!dict.ContainsKey(types[res2.CommandButtonResult.Value]) || dict[types[res2.CommandButtonResult.Value]] == null)
                            {
                                dict[types[res2.CommandButtonResult.Value]] = new List<string>();
                            }

                            skey = (List<string>)dict[types[res2.CommandButtonResult.Value]];

                            skey.Add(ids[1]);
                        }
                        break;

                        case 1:
                        {
                            var ofd = new OpenFileDialog
                                {
                                    Title = "Select a new executable file for " + sel.Name,
                                    CheckFileExists = true,
                                    CheckPathExists = true,
                                    Multiselect = false,
                                    Filter = "Executable|*.exe",
                                    InitialDirectory = Path.GetDirectoryName(ids[1])
                                };

                            if (ofd.ShowDialog() != DialogResult.OK)
                            {
                                return;
                            }

                            skey[skey.IndexOf(ids[1])] = ofd.FileName;
                        }
                        break;
                    }

                    Settings.Set("Alternative Associations", dict);

                    ReloadList();
                }
                break;

                case "Folder destinations":
                {
                    var ids  = sel.ID.Split("|".ToCharArray(), 2);
                    var dict = Settings.Get<Dictionary<string, object>>("Folder Destinations");
                    var skey = (List<string>)dict[ids[0]];

                    var res = TaskDialog.Show(new TaskDialogOptions
                        {
                            Title                   = "Edit a folder destination",
                            AllowDialogCancellation = true,
                            Content                 = "What would you like to change about " + sel.Name + "?",
                            CommandButtons          = new[] { "Associated file type", "Folder location", "Nothing" }
                        });

                    if (!res.CommandButtonResult.HasValue || res.CommandButtonResult.Value == 2)
                    {
                        return;
                    }

                    switch (res.CommandButtonResult.Value)
                    {
                        case 0:
                        {
                            var types = new[] { ".torrent", ".nzb", ".dlc" };
                            var res2 = TaskDialog.Show(new TaskDialogOptions
                                {
                                    Title                   = "Edit a folder destination",
                                    AllowDialogCancellation = true,
                                    Content                 = "Please select a new file type you want to be associated with " + sel.Name + ":",
                                    CommandButtons          = types
                                });

                            if (!res2.CommandButtonResult.HasValue || res2.CommandButtonResult.Value < 0 || res2.CommandButtonResult.Value >= types.Length)
                            {
                                return;
                            }

                            skey.Remove(ids[1]);

                            if (!dict.ContainsKey(types[res2.CommandButtonResult.Value]) || dict[types[res2.CommandButtonResult.Value]] == null)
                            {
                                dict[types[res2.CommandButtonResult.Value]] = new List<string>();
                            }

                            skey = (List<string>)dict[types[res2.CommandButtonResult.Value]];

                            skey.Add(ids[1]);
                        }
                        break;

                        case 1:
                        {
                            var fbd = new FolderBrowserDialog
                                {
                                    ShowNewFolderButton = true,
                                    Description         = "Select a new folder to save the files previously saved in " + sel.Name + ":",
                                    SelectedPath        = ids[1]
                                };

                            if (fbd.ShowDialog() != DialogResult.OK)
                            {
                                return;
                            }

                            skey[skey.IndexOf(ids[1])] = fbd.SelectedPath;
                        }
                        break;
                    }

                    Settings.Set("Folder Destinations", dict);

                    ReloadList();
                }
                break;
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

            switch (sel.Type)
            {
                case "Remote servers":
                    if (MessageBox.Show("Are you sure you want to remove " + sel.Name + "?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        var dict = Settings.Get<Dictionary<string, object>>("Sender Destinations");

                        dict.Remove(sel.ID);

                        Settings.Set("Sender Destinations", dict);

                        ReloadList();
                    }
                    break;

                case "Alternative local associations":
                    if (MessageBox.Show("Are you sure you want to remove " + sel.Name + "?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        var ids  = sel.ID.Split("|".ToCharArray(), 2);
                        var dict = Settings.Get<Dictionary<string, object>>("Alternative Associations");
                        var skey = (List<string>)dict[ids[0]];

                        skey.Remove(ids[1]);

                        Settings.Set("Alternative Associations", dict);

                        ReloadList();
                    }
                    break;

                case "Folder destinations":
                    if (MessageBox.Show("Are you sure you want to remove " + sel.Name + "?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        var ids  = sel.ID.Split("|".ToCharArray(), 2);
                        var dict = Settings.Get<Dictionary<string, object>>("Folder Destinations");
                        var skey = (List<string>)dict[ids[0]];

                        skey.Remove(ids[1]);

                        Settings.Set("Folder Destinations", dict);

                        ReloadList();
                    }
                    break;
            }
        }
    }
}
