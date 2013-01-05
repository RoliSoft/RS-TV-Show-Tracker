namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;

    /// <summary>
    /// Interaction logic for AddNewWindow.xaml
    /// </summary>
    public partial class AddNewWindow
    {
        private Guide _guide;
        private List<ShowID> _shows;
        private Thread _worker;
        private int _dbid;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        /// <param name="name">The name of the show to put into the textbox.</param>
        public AddNewWindow(string name = null)
        {
            InitializeComponent();

            if (name != null)
            {
                textBox.Text = name;
            }
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

            DatabaseSelectionChanged(null, null);
            ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
        }

        /// <summary>
        /// Handles the KeyUp event of the textBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void TextBoxKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButtonClick(null, null);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the database control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void DatabaseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((database.SelectedValue as ComboBoxItem).Content == null) return;
            _guide = CreateGuide((((database.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as Label).Content.ToString().Trim());

            language.Items.Clear();

            foreach (var lang in _guide.SupportedLanguages)
            {
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

            language.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text)) return;

            working.Content           = "Searching on {0}...".FormatWith(_guide.Name);
            subworking.Content        = textBox.Text.ToUppercaseWords();
            addTabItem.Visibility     = Visibility.Collapsed;
            workingTabItem.Visibility = Visibility.Visible;
            tabControl.SelectedIndex  = 1;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);

            var show = textBox.Text;
            var lang = (language.SelectedValue as StackPanel).Tag.ToString();

            _worker = new Thread(() =>
                {
                    try
                    {
                        _shows = _guide.GetID(show, lang).ToList();

                        if (_shows.Count == 0)
                        {
                            var up = new Exception();

                            throw up; // hehe
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is ThreadAbortException)
                        {
                            return;
                        }

                        Dispatcher.Invoke((Action)(() =>
                            {
                                workingTabItem.Visibility = Visibility.Collapsed;
                                addTabItem.Visibility     = Visibility.Visible;
                                tabControl.SelectedIndex  = 0;

                                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                            }));

                        new TaskDialog
                            {
                                Icon                = TaskDialogStandardIcon.Error,
                                Caption             = "Couldn't find TV show",
                                InstructionText     = show.ToUppercaseWords(),
                                Text                = "Couldn't find the specified TV show on this database.",
                                DetailsExpandedText = "If this is a popular TV show, then you maybe just misspelled it or didn't enter the full official name.\r\nIf this is a rare or foreign TV show, try using another database.",
                                Cancelable          = true
                            }.Show();

                        return;
                    }

                    Dispatcher.Invoke((Action)(() =>
                        {
                            listBox.Items.Clear();

                            foreach (var id in _shows)
                            {
                                listBox.Items.Add(id.Title);
                            }

                            listBox.SelectedIndex = 0;

                            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                            workingTabItem.Visibility = Visibility.Collapsed;
                            selectTabItem.Visibility  = Visibility.Visible;
                            tabControl.SelectedIndex  = 2;
                        }));
                });
            _worker.Start();
        }

        /// <summary>
        /// Handles the Click event of the searchCancelButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchCancelButtonClick(object sender, RoutedEventArgs e)
        {
            if (_worker != null && _worker.IsAlive)
            {
                _worker.Abort();
            }

            workingTabItem.Visibility = Visibility.Collapsed;
            addTabItem.Visibility     = Visibility.Visible;
            tabControl.SelectedIndex  = 0;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
        }

        /// <summary>
        /// Handles the Click event of the selectBackButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectBackButtonClick(object sender, RoutedEventArgs e)
        {
            selectTabItem.Visibility = Visibility.Collapsed;
            addTabItem.Visibility    = Visibility.Visible;
            tabControl.SelectedIndex = 0;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
        }

        /// <summary>
        /// Handles the Click event of the addButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            AddShow(_shows.First(show => show.Title == (string)listBox.SelectedValue));
        }

        /// <summary>
        /// Downloads and inserts the specified show into the database.
        /// </summary>
        /// <param name="show">The show.</param>
        private void AddShow(ShowID show)
        {
            working.Content           = "Downloading guide...";
            subworking.Content        = show.Title;
            selectTabItem.Visibility  = Visibility.Collapsed;
            workingTabItem.Visibility = Visibility.Visible;
            tabControl.SelectedIndex  = 1;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);

            var lang = (language.SelectedValue as StackPanel).Tag.ToString();

            _worker = new Thread(() =>
                {
                    // get data from guide
                    TVShow tv;
                    try
                    {
                        tv = _guide.GetData(show.ID, lang);

                        if (tv.Episodes.Count == 0)
                        {
                            throw new Exception("There aren't any episodes associated to this TV show on this database.");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is ThreadAbortException)
                        {
                            return;
                        }

                        Dispatcher.Invoke((Action)(() =>
                            {
                                workingTabItem.Visibility = Visibility.Collapsed;
                                addTabItem.Visibility     = Visibility.Visible;
                                tabControl.SelectedIndex  = 0;

                                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                            }));

                        new TaskDialog
                            {
                                Icon                = TaskDialogStandardIcon.Error,
                                Caption             = "Couldn't grab TV show",
                                InstructionText     = show.Title,
                                Text                = "Couldn't download the episode listing and associated informations due to an unexpected error.",
                                DetailsExpandedText = ex.Message,
                                Cancelable          = true
                            }.Show();

                        return;
                    }

                    // try to see if duplicate
                    if (Database.TVShows.Values.FirstOrDefault(x => x.Title == tv.Title) != null)
                    {
                        Dispatcher.Invoke((Action)(() =>
                            {
                                workingTabItem.Visibility = Visibility.Collapsed;
                                addTabItem.Visibility     = Visibility.Visible;
                                tabControl.SelectedIndex  = 0;

                                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                            }));

                        new TaskDialog
                            {
                                Icon            = TaskDialogStandardIcon.Error,
                                Caption         = "Duplicate entry",
                                InstructionText = tv.Title,
                                Text            = "This TV show is already on your list.",
                                Cancelable      = true
                            }.Show();

                        return;
                    }

                    // increment each rowid
                    foreach (var tvs in Database.TVShows.Values)
                    {
                        tvs.RowID++;
                        tvs.SaveData();
                    }

                    // generate showid
                    tv.RowID = 0;
                    _dbid = tv.ID = Database.TVShows.Values.Max(x => x.ID) + 1;
                    tv.Data = new Dictionary<string, string>();
                    tv.Directory = Path.Combine(Database.DataPath, Utils.CreateSlug(tv.Title, false));

                    if (Directory.Exists(tv.Directory))
                    {
                        tv.Directory += "-" + tv.Source.ToLower();
                    }

                    if (Directory.Exists(tv.Directory))
                    {
                        tv.Directory += "-" + Utils.Rand.Next();
                    }

                    Directory.CreateDirectory(tv.Directory);

                    // apply timezone corrections
                    foreach (var ep in tv.Episodes)
                    {
                        if (!string.IsNullOrWhiteSpace(tv.AirTime) && ep.Airdate != Utils.UnixEpoch)
                        {
                            ep.Airdate = DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone(tv.TimeZone);
                        }
                    }

                    // save
                    tv.Save();
                    tv.SaveTracking();

                    // fire data change event
                    Database.LoadDatabase();
                    MainWindow.Active.DataChanged();

                    // asynchronously update lab.rolisoft.net's cache
                    if (tv.Language == "en")
                    {
                        Updater.UpdateRemoteCache(tv);
                    }
                    
                    // show finish page
                    Dispatcher.Invoke((Action)(() =>
                        {
                            finishTitle.Content       = tv.Title;
                            workingTabItem.Visibility = Visibility.Collapsed;
                            finishTabItem.Visibility  = Visibility.Visible;
                            tabControl.SelectedIndex  = 3;

                            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                            var shows = Database.TVShows[_dbid].Episodes.OrderByDescending(ep => ep.ID);

                            markUntil.Items.Clear();

                            var gotLast = false;

                            foreach (var item in shows)
                            {
                                markUntil.Items.Add("S{0:00}E{1:00} - {2}".FormatWith(item.Season, item.Number, item.Title));

                                if (!gotLast && item.Airdate < DateTime.Now && item.Airdate != Utils.UnixEpoch)
                                {
                                    gotLast = true;
                                    markUntil.Items[markUntil.Items.Count - 1] += " [last aired episode]";
                                }
                            }
                        }));
                });
            _worker.Start();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the markUntil control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void MarkUntilSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (markUntil.SelectedValue == null) return;

            var season  = ((string)markUntil.SelectedValue).Substring(1, 2).ToInteger();
            var episode = ((string)markUntil.SelectedValue).Substring(4, 2).ToInteger();

            Database.TVShows[_dbid].Episodes.ForEach(ep => ep.Watched = false);

            foreach (var ep in Database.TVShows[_dbid].Episodes.Where(ep => ep.ID <= episode + (season * 1000) + (_dbid * 1000 * 1000)))
            {
                ep.Watched = true;
            }

            Database.TVShows[_dbid].SaveTracking();
            MainWindow.Active.DataChanged();
        }

        /// <summary>
        /// Handles the Click event of the closeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the Click event of the addAnotherButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddAnotherButtonClick(object sender, RoutedEventArgs e)
        {
            finishTabItem.Visibility = Visibility.Collapsed;
            addTabItem.Visibility    = Visibility.Visible;
            tabControl.SelectedIndex = 0;
            textBox.Text             = string.Empty;
            textBox.Focus();
        }

        /// <summary>
        /// Creates the guide based on the ComboBox item name.
        /// </summary>
        /// <param name="grabber">The grabber.</param>
        /// <returns>The guide.</returns>
        public static Guide CreateGuide(string grabber)
        {
            switch (grabber)
            {
                default:
                case "TVRage":
                    return new TVRage();

                case "The TVDB":
                    return new TVDB();

                case "TV.com":
                    return new TVcom();

                case "EPisodeWorld":
                    return new EPisodeWorld();

                case "IMDb":
                    return new IMDb();

                case "EPGuides - TVRage":
                    return new EPGuides("tvrage.com");

                case "EPGuides - TV.com":
                    return new EPGuides("tv.com");

                case "AniDB":
                    return new AniDB();

                case "Anime News Network":
                    return new AnimeNewsNetwork();
            }
        }
    }
}
