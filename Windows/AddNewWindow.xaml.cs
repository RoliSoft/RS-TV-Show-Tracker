namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
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
        private string _dbid;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        public AddNewWindow()
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

            working.Content           = "Searching for {0}...".FormatWith(textBox.Text.ToUppercaseWords());
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

                    // test if the application's engine sees the entered and the returned first result as an exact match
                    var enterroot = ShowNames.Tools.Normalize(show);
                    var matchroot = ShowNames.Tools.Normalize(_shows[0].Title);
                    var exact     = enterroot == matchroot;

                    if (exact && _shows.Count != 1)
                    {
                        // test if the second result is an exact match too
                        // if it is, then selection is required by the user
                        exact = enterroot != ShowNames.Tools.Normalize(_shows[1].Title);

                        if (exact && _shows.Count != 2)
                        {
                            exact = enterroot != ShowNames.Tools.Normalize(_shows[2].Title);
                        }
                    }

                    Dispatcher.Invoke((Action)(() =>
                        {
                            if (exact)
                            {
                                AddShow(_shows[0]);
                            }
                            else
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
                            }
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
            AddShow(_shows.Where(show => show.Title == (string)listBox.SelectedValue).First());
        }

        /// <summary>
        /// Downloads and inserts the specified show into the database.
        /// </summary>
        /// <param name="show">The show.</param>
        private void AddShow(ShowID show)
        {
            working.Content           = "Downloading guide for {0}...".FormatWith(show.Title);
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
                    if (!string.IsNullOrWhiteSpace(Database.GetShowID(tv.Title)))
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

                    // create transaction
                    SQLiteTransaction tr;
                    try
                    {
                        tr = Database.Connection.BeginTransaction();
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
                                Caption             = "Couldn't create transaction",
                                InstructionText     = tv.Title,
                                Text                = "Couldn't create transaction on the database to insert the episodes. The TV show is added to the list, however, there aren't any episodes associated to it. Run an update to add the episodes.",
                                DetailsExpandedText = ex.Message,
                                Cancelable          = true
                            }.Show();

                        return;
                    }


                    // insert into tvshows and let the autoincrementing field assign a showid
                    Database.Execute("update tvshows set rowid = rowid + 1");
                    Database.Execute("insert into tvshows values (1, null, ?)", tv.Title);

                    // then get that showid
                    _dbid = Database.GetShowID(tv.Title);

                    // insert guide fields
                    var gname = _guide.GetType().Name;

                    Database.ShowData(_dbid, "grabber",       gname);
                    Database.ShowData(_dbid, gname + ".id",   show.ID);
                    Database.ShowData(_dbid, gname + ".lang", lang);

                    // insert showdata fields
                    Database.ShowData(_dbid, "genre",   tv.Genre);
                    Database.ShowData(_dbid, "descr",   tv.Description);
                    Database.ShowData(_dbid, "cover",   tv.Cover);
                    Database.ShowData(_dbid, "airing",  tv.Airing.ToString());
                    Database.ShowData(_dbid, "airtime", tv.AirTime);
                    Database.ShowData(_dbid, "airday",  tv.AirDay);
                    Database.ShowData(_dbid, "network", tv.Network);
                    Database.ShowData(_dbid, "runtime", tv.Runtime.ToString());
                    Database.ShowData(_dbid, "url",     tv.URL);

                    // insert episodes
                    foreach (var ep in tv.Episodes)
                    {
                        try
                        {
                            Database.ExecuteOnTransaction(tr, "insert into episodes values (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                                          ep.Number + (ep.Season * 1000) + (_dbid.ToInteger() * 100 * 1000),
                                                          _dbid,
                                                          ep.Season,
                                                          ep.Number,
                                                          tv.AirTime == String.Empty || ep.Airdate == Utils.UnixEpoch
                                                            ? ep.Airdate.ToUnixTimestamp()
                                                            : DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone().ToUnixTimestamp(),
                                                          ep.Title,
                                                          ep.Summary,
                                                          ep.Picture,
                                                          ep.URL);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    // commit the changes
                    tr.Commit();

                    // fire data change event
                    MainWindow.Active.DataChanged();

                    // asynchronously update lab.rolisoft.net's cache
                    Updater.UpdateRemoteCache(new Tuple<string, string>(_guide.GetType().Name, show.ID), tv);

                    // show finish page
                    Dispatcher.Invoke((Action)(() =>
                        {
                            finishTitle.Content       = "{0} has been added to your list!".FormatWith(tv.Title);
                            workingTabItem.Visibility = Visibility.Collapsed;
                            finishTabItem.Visibility  = Visibility.Visible;
                            tabControl.SelectedIndex  = 3;

                            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                            var shows = Database.Query("select season, episode, name, airdate from episodes where showid = ? order by (season * 1000 + episode) desc", _dbid);

                            markUntil.Items.Clear();

                            var gotLast = false;

                            foreach (var item in shows)
                            {
                                markUntil.Items.Add("S{0:00}E{1:00} - {2}".FormatWith(item["season"].ToInteger(), item["episode"].ToInteger(), item["name"]));

                                if (!gotLast && item["airdate"].ToInteger() < DateTime.Now.ToUnixTimestamp() && item["airdate"] != "0")
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

            Database.Execute("delete from tracking where showid = ?", _dbid);
            Database.Execute("insert into tracking select showid, episodeid from episodes where showid = ? and episodeid <= ?", _dbid, episode + (season * 1000) + (_dbid.ToInteger() * 100 * 1000));
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

                case "EPGuides - TVRage":
                    return new EPGuides("tvrage.com");

                case "EPGuides - TV.com":
                    return new EPGuides("tv.com");

                case "AniDB":
                    return new AniDB();

                case "Generate list based on download links":
                    return new Guess();
            }
        }
    }
}
