namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.ListViews;

    using Xceed.Wpf.Toolkit.Primitives;

    /// <summary>
    /// Interaction logic for AddNewWindow.xaml
    /// </summary>
    public partial class AddNewWindow
    {
        private bool _loaded, _addedOne;
        private List<string> _successful;
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
        public AddNewWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public AddNewWindow(string name)
        {
            InitializeComponent();

            namesTextBox.InitializeTokensFromText(name + namesTextBox.TokenDelimiter);
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
            databaseCheckListBox.SelectedItems.Add(databaseCheckListBox.Items[2]);

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
            var names = namesTextBox.GetTokens().ToList();

            if (!names.Any())
            {
                MessageBox.Show("Enter at least one show and end it with ; in order to include it.", Signature.Software, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (databaseCheckListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select at least one database from which to download information for the shows.", Signature.Software, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _addedOne = false;
            _successful = new List<string>();

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

            foreach (var name in names)
            {
                _list.Add(new PendingShowListViewItem(name));
            }

            PendingShowListViewItemCollection.Clear();
            PendingShowListViewItemCollection.AddRange(_list);

            cancelButton.Visibility  = nextButton.Visibility = Visibility.Visible;
            restartButton.Visibility = backButton.Visibility = Visibility.Collapsed;
            cancelButton.IsEnabled   = true;
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

                                    {
                                        var sp = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal
                                            };

                                        sp.Children.Add(new Image
                                            {
                                                Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cross.png", UriKind.Absolute)),
                                                Height = 16,
                                                Width  = 16,
                                                Margin = new Thickness(0, 0, 0, 0)
                                            });

                                        sp.Children.Add(new Label
                                            {
                                                Content = " None of the above",
                                                Padding = new Thickness(0)
                                            });

                                        item.CandidateSP.Add(sp);
                                    }
                                });

                            item.SelectedCandidate = 0;
                            
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

                    Dispatcher.Invoke(() =>
                        {
                            cancelButton.Visibility = Visibility.Collapsed;
                            backButton.Visibility   = Visibility.Visible;
                            backButton.IsEnabled    = true;
                        });
                });
            _searchThd.Start();
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the Episodes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void EpisodesOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var ps = cb.DataContext as PendingShowListViewItem;

            if (cb.SelectedIndex == -1 || ps == null) return;

            ps.Show.Episodes.ForEach(ep => ep.Watched = false);

            var sp = cb.SelectedItem as StackPanel;

            if (sp == null || !(sp.Tag is int)) goto end;

            var id = (int)sp.Tag;

            foreach (var ep in ps.Show.Episodes.Where(ep => ep.ID <= id))
            {
                ep.Watched = true;
            }

          end:
            ps.Show.SaveTracking();
            MainWindow.Active.DataChanged();
        }

        /// <summary>
        /// Handles the OnClick event of the backButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void BackButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (_addedOne)
            {
                namesTextBox.DeleteTokens(_successful);
            }

            listTabItem.Visibility   = Visibility.Collapsed;
            addTabItem.Visibility    = Visibility.Visible;
            tabControl.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles the OnClick event of the cancelButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void CancelButtonOnClick(object sender, RoutedEventArgs e)
        {
            cancelButton.IsEnabled = false;

            if (_searchThd != null && _searchThd.IsAlive)
            {
                try
                {
                    _searchThd.Abort();
                    _searchThd = null;
                }
                catch { }
            }

            try
            {
                foreach (var item in _list)
                {
                    if (item.Group != "Added" && item.Group != "Failed")
                    {
                        item.Status         = "Operation canceled.";
                        item.ShowCandidates = "Collapsed";
                        item.ShowEpisodes   = "Collapsed";
                        item.ShowStatus     = "Visible";
                    }
                }

                Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());
            }
            catch { }

            cancelButton.Visibility  = backButton.Visibility = nextButton.Visibility = Visibility.Collapsed;
            cancelButton.IsEnabled   = nextButton.IsEnabled  = false;
            restartButton.IsEnabled  = true;
            restartButton.Visibility = Visibility.Visible;

            if (_addedOne)
            {
                MainWindow.Active.DataChanged();
                Task.Factory.StartNew(Library.SearchForFiles);
            }
        }

        /// <summary>
        /// Handles the OnClick event of the nextButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void NextButtonOnClick(object sender, RoutedEventArgs e)
        {
            backButton.Visibility   = nextButton.Visibility    = Visibility.Collapsed;
            cancelButton.Visibility = restartButton.Visibility = Visibility.Visible;
            cancelButton.IsEnabled  = true;
            restartButton.IsEnabled = false;

            foreach (var item in _list.Where(x => x.Candidates.Count != 0 && x.SelectedCandidate != x.Candidates.Count))
            {
                item.Name           = item.Candidates[item.SelectedCandidate].Title;
                item.Group          = "Pending";
                item.Status         = string.Empty;
                item.ShowCandidates = "Collapsed";
                item.ShowStatus     = "Visible";
            }

            foreach (var item in _list.Where(x => x.Candidates.Count != 0 && x.SelectedCandidate == x.Candidates.Count))
            {
                item.Group          = "Failed";
                item.Status         = "Skipped by user request.";
                item.ShowCandidates = "Collapsed";
                item.ShowStatus     = "Visible";
            }

            CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh();

            _searchThd = new Thread(() =>
                {
                    foreach (var item in _list.Where(x => x.Candidates.Count != 0 && x.SelectedCandidate != x.Candidates.Count))
                    {
                        var err = false;

                        Dispatcher.Invoke(() => listView.ScrollIntoView(item));

                        item.Status = "Downloading from " + item.Candidates[item.SelectedCandidate].Guide.Name + "...";

                        Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());

                        item.Show = Database.Add(item.Candidates[item.SelectedCandidate], (i, s) =>
                            {
                                item.Status = s;

                                switch (i)
                                {
                                    case 1:
                                        _addedOne = true;
                                        _successful.Add(item.Token);
                                        item.Group = "Added";
                                        break;

                                    case -1:
                                        err = true;
                                        item.Group = "Failed";
                                        break;
                                }

                                Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());
                            });

                        if (err)
                        {
                            continue;
                        }

                        Dispatcher.Invoke(() =>
                            {
                                var gotLast = false;
                                
                                {
                                    var sp = new StackPanel
                                        {
                                            Orientation = Orientation.Horizontal
                                        };

                                    sp.Children.Add(new Image
                                        {
                                            Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/checked.png", UriKind.Absolute)),
                                            Height = 16,
                                            Width  = 16,
                                            Margin = new Thickness(0, 0, 0, 0)
                                        });

                                    sp.Children.Add(new Label
                                        {
                                            Content = " Mark episodes as watched until...",
                                            Padding = new Thickness(0)
                                        });

                                    item.EpisodeSP.Add(sp);
                                }

                                foreach (var ep in item.Show.Episodes.OrderByDescending(ep => ep.ID))
                                {
                                    var sp = new StackPanel
                                        {
                                            Orientation = Orientation.Horizontal,
                                            Tag         = ep.ID
                                        };

                                    sp.Children.Add(new Label
                                        {
                                            Content    = string.Format("S{0:00}E{1:00} ", ep.Season, ep.Number),
                                            FontWeight = FontWeights.Bold,
                                            Padding    = new Thickness(0)
                                        });

                                    sp.Children.Add(new Label
                                        {
                                            Content = ep.Title,
                                            Padding = new Thickness(0)
                                        });
                                    
                                    if (!gotLast && ep.Airdate < DateTime.Now && ep.Airdate != Utils.UnixEpoch)
                                    {
                                        gotLast = true;

                                        sp.Children.Add(new Label
                                            {
                                                Content = " [last aired episode]",
                                                Opacity = 0.5,
                                                Padding = new Thickness(0)
                                            });
                                    }

                                    item.EpisodeSP.Add(sp);
                                }
                            });

                        item.SelectedEpisode = 0;

                        item.ShowStatus   = "Collapsed";
                        item.ShowEpisodes = "Visible";

                        Dispatcher.Invoke(() => CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh());
                    }

                    Dispatcher.Invoke(() =>
                        {
                            cancelButton.Visibility = Visibility.Collapsed;
                            restartButton.IsEnabled = true;
                        });

                    if (_addedOne)
                    {
                        MainWindow.Active.DataChanged();
                        Task.Factory.StartNew(Library.SearchForFiles);
                    }
                });
            _searchThd.Start();
        }
    }
}
