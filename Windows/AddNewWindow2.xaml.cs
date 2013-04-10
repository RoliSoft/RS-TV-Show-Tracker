namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.ListViews;

    using Xceed.Wpf.Toolkit.Primitives;

    /// <summary>
    /// Interaction logic for AddNewWindow2.xaml
    /// </summary>
    public partial class AddNewWindow2
    {
        private bool _loaded;
        private List<Guide> _guides; 
        private string _lang;
        private List<PendingShowListViewItem> _list;
        private Thread _searchThd;

        /// <summary>
        /// Gets or sets the pending show list view item collection.
        /// </summary>
        /// <value>The pending show list view item collection.</value>
        public ObservableCollection<PendingShowListViewItem> PendingShowListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        public AddNewWindow2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (AeroGlassCompositionEnabled)
            {
                SetAeroGlassTransparency();
            }

            databaseCheckListBox.SelectedItems.Add(databaseCheckListBox.Items[0]);
            _loaded = true;
            databaseCheckListBox.SelectedItems.Add(databaseCheckListBox.Items[1]);

            PendingShowListViewItemCollection = new ObservableCollection<PendingShowListViewItem>();
            listView.ItemsSource = PendingShowListViewItemCollection;
            listView.Items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        }

        /// <summary>
        /// Handles the OnItemSelectionChanged event of the databaseCheckListBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemSelectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void DatabaseCheckListBoxOnItemSelectionChanged(object sender, ItemSelectionChangedEventArgs e)
        {
            if (!_loaded) return;

            var sellang = language.SelectedIndex != -1 ? (language.SelectedItem as StackPanel).Tag as string : string.Empty;
            var selidx  = 0;

            language.Items.Clear();

            if (databaseCheckListBox.SelectedItems.Count == 0)
            {
                return;
            }

            var langs = Languages.List.Keys.ToList();

            foreach (ListBoxItem item in databaseCheckListBox.SelectedItems)
            {
                langs = langs.Intersect(Updater.CreateGuide(item.Tag as string).SupportedLanguages).ToList();
            }

            foreach (var lang in langs)
            {
                if (sellang == lang)
                {
                    selidx = language.Items.Count;
                }

                var sp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Tag         = lang
                    };

                sp.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/flag-" + lang + ".png", UriKind.Relative)),
                        Height = 16,
                        Width  = 16,
                        Margin = new Thickness(0, 1, 0, 0)
                    });

                sp.Children.Add(new Label
                    {
                        Content = " " + Languages.List[lang],
                        Padding = new Thickness(0)
                    });

                language.Items.Add(sp);
            }

            language.SelectedIndex = selidx;
        }

        /// <summary>
        /// Handles the Click event of the dbMoveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DbMoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (databaseCheckListBox.SelectedItems.Count == 0) return;

            var sel = databaseCheckListBox.SelectedItems[databaseCheckListBox.SelectedItems.Count - 1];
            var idx = databaseCheckListBox.Items.IndexOf(sel);

            if (idx > 0)
            {
                databaseCheckListBox.Items.Remove(sel);
                databaseCheckListBox.Items.Insert(idx - 1, sel);
            }
        }

        /// <summary>
        /// Handles the Click event of the dbMoveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DbMoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (databaseCheckListBox.SelectedItems.Count == 0) return;

            var sel = databaseCheckListBox.SelectedItems[databaseCheckListBox.SelectedItems.Count - 1];
            var idx = databaseCheckListBox.Items.IndexOf(sel);

            if (idx < databaseCheckListBox.Items.Count - 1)
            {
                databaseCheckListBox.Items.Remove(sel);
                databaseCheckListBox.Items.Insert(idx + 1, sel);
            }
        }

        /// <summary>
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (namesTextBox.Text == null || string.IsNullOrWhiteSpace(namesTextBox.Text.Replace(";", string.Empty)))
            {
                MessageBox.Show("Enter at least one show and end it with ; in order to include it.", Signature.Software, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (databaseCheckListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select at least one database from which to download information for the shows.", Signature.Software, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _lang = language.SelectedIndex != -1 ? (language.SelectedItem as StackPanel).Tag as string : "en";
            _guides = new List<Guide>();

            foreach (ListBoxItem db in databaseCheckListBox.Items)
            {
                if (databaseCheckListBox.SelectedItems.Contains(db))
                {
                    _guides.Add(Updater.CreateGuide(db.Tag as string));
                }
            }

            _list = new List<PendingShowListViewItem>();

            foreach (var name in namesTextBox.Text.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _list.Add(new PendingShowListViewItem(name.Trim()));
                }
            }

            PendingShowListViewItemCollection.Clear();
            PendingShowListViewItemCollection.AddRange(_list);

            nextButton.IsEnabled     = false;
            addTabItem.Visibility    = Visibility.Collapsed;
            listTabItem.Visibility   = Visibility.Visible;
            tabControl.SelectedIndex = 1;

            _searchThd = new Thread(() =>
                {
                    var ok = false;

                    foreach (var item in _list)
                    {
                        var err = false;

                        Dispatcher.Invoke(() => listView.ScrollIntoView(item));

                        foreach (var guide in _guides)
                        {
                            item.Status = "Searching on " + guide.Name + "...";

                            Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());

                            try
                            {
                                item.Candidates.AddRange(guide.GetID(item.Name));
                            }
                            catch (Exception ex)
                            {
                                err = true;
                                MainWindow.HandleUnexpectedException(ex);
                            }
                        }

                        if (item.Candidates.Count == 0)
                        {
                            if (err)
                            {
                                item.Group  = "Failed";
                                item.Status = "No shows found, possibly due to errors.";
                            }
                            else
                            {
                                item.Group  = "Failed";
                                item.Status = "No shows found matching this name.";
                            }
                        }
                        else
                        {
                            ok = true;
                            
                            Dispatcher.Invoke(() =>
                                {
                                    foreach (var cand in item.Candidates)
                                    {
                                        var sp = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal
                                            };
                                
                                        sp.Children.Add(new Label
                                            {
                                                Content    = cand.Title,
                                                FontWeight = FontWeights.Bold,
                                                Padding    = new Thickness(0)
                                            });
                                
                                        sp.Children.Add(new Label
                                            {
                                                Content = " at ",
                                                Opacity = 0.5,
                                                Padding = new Thickness(0)
                                            });

                                        sp.Children.Add(new Image
                                            {
                                                Source = new BitmapImage(new Uri(cand.Guide.Icon, UriKind.Absolute)),
                                                Height = 16,
                                                Width  = 16,
                                                Margin = new Thickness(0, 0, 0, 0)
                                            });

                                        sp.Children.Add(new Label
                                            {
                                                Content = " " + cand.Guide.Name,
                                                Padding = new Thickness(0)
                                            });

                                        item.CandidateSP.Add(sp);
                                    }
                                });

                            item.Group          = "Found";
                            item.ShowStatus     = "Collapsed";
                            item.ShowCandidates = "Visible";
                        }

                        Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());
                    }

                    if (ok)
                    {
                        Dispatcher.Invoke(() => nextButton.IsEnabled = true);
                    }
                });
            _searchThd.Start();
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the Selector control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void SelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var ps = cb.DataContext as PendingShowListViewItem;

            if (cb.SelectedIndex == -1 || ps == null) return;

            ps.SelectedCandidate = cb.SelectedIndex;
        }

        /// <summary>
        /// Handles the OnClick event of the backButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void BackButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (_searchThd != null && _searchThd.IsAlive)
            {
                if (MessageBox.Show("An operation is currently in progress. Do you want to abort it and discard the results so far?", Signature.Software, MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _searchThd.Abort();
                        _searchThd = null;
                    }
                    catch { }
                }
                else
                {
                    return;
                }
            }

            listTabItem.Visibility   = Visibility.Collapsed;
            addTabItem.Visibility    = Visibility.Visible;
            tabControl.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles the OnClick event of the nextButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void NextButtonOnClick(object sender, RoutedEventArgs e)
        {
            nextButton.IsEnabled = false;

            foreach (var item in _list.Where(x => x.Candidates.Count != 0))
            {
                item.Name           = item.Candidates[item.SelectedCandidate].Title;
                item.Group          = "Pending";
                item.Status         = string.Empty;
                item.ShowCandidates = "Collapsed";
                item.ShowStatus     = "Visible";
            }

            CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh();

            _searchThd = new Thread(() =>
                {
                    foreach (var item in _list.Where(x => x.Candidates.Count != 0))
                    {
                        var err = false;

                        Dispatcher.Invoke(() => listView.ScrollIntoView(item));

                        item.Status = "Downloading from " + item.Candidates[item.SelectedCandidate].Guide.Name + "...";

                        Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());

                        try
                        {
                            //item.Candidates.AddRange(guide.GetID(item.Name));
                        }
                        catch (Exception ex)
                        {
                            err = true;
                            MainWindow.HandleUnexpectedException(ex);
                        }

                        if (err)
                        {
                            item.Group  = "Failed";
                            item.Status = "Error while downloading guide.";
                        }
                        else
                        {
                            /*Dispatcher.Invoke(() =>
                                {
                                    foreach (var cand in item.Candidates)
                                    {
                                        var sp = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal
                                            };
                                
                                        sp.Children.Add(new Label
                                            {
                                                Content    = cand.Title,
                                                FontWeight = FontWeights.Bold,
                                                Padding    = new Thickness(0)
                                            });
                                
                                        sp.Children.Add(new Label
                                            {
                                                Content = " at ",
                                                Opacity = 0.5,
                                                Padding = new Thickness(0)
                                            });

                                        sp.Children.Add(new Image
                                            {
                                                Source = new BitmapImage(new Uri(cand.Guide.Icon, UriKind.Absolute)),
                                                Height = 16,
                                                Width  = 16,
                                                Margin = new Thickness(0, 0, 0, 0)
                                            });

                                        sp.Children.Add(new Label
                                            {
                                                Content = " " + cand.Guide.Name,
                                                Padding = new Thickness(0)
                                            });

                                        item.CandidateSP.Add(sp);
                                    }
                                });

                            item.ShowStatus     = "Collapsed";
                            item.ShowCandidates = "Visible";*/
                            item.Group = "Added";
                        }

                        Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());
                    }
                });
            _searchThd.Start();
        }
    }
}
