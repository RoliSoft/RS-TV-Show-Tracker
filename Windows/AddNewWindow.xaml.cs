namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Data.SQLite;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;

    /// <summary>
    /// Interaction logic for AddNewWindow.xaml
    /// </summary>
    public partial class AddNewWindow
    {
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
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text)) return;

            textBox.IsEnabled = comboBox.IsEnabled = searchButton.IsEnabled = markCheckBox.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);

            new Task(() =>
                {
                    string show = string.Empty, grabber = string.Empty;
                    var mark = true;

                    Dispatcher.Invoke((Action)delegate
                        {
                            show    = textBox.Text;
                            grabber = (((comboBox.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as Label).Content.ToString().Trim();
                            mark    = markCheckBox.IsChecked ?? mark;
                        });

                    var res = false;

                    try
                    {
                        res = Add(show, grabber, mark);
                    }
                    catch (Exception ex)
                    {
                        new TaskDialog
                            {
                                Icon                = TaskDialogStandardIcon.Error,
                                Caption             = "Couldn't add TV show",
                                InstructionText     = show.ToUppercaseFirst(),
                                Text                = "Couldn't add the specified TV show to the database due to an unexpected error.",
                                DetailsExpandedText = ex.Message,
                                Cancelable          = true
                            }.Show();
                    }

                    Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                    Dispatcher.Invoke((Action)(() =>
                        {
                            textBox.IsEnabled = comboBox.IsEnabled = searchButton.IsEnabled = markCheckBox.IsEnabled = true;
                            progressBar.Visibility = Visibility.Collapsed;

                            if (res)
                            {
                                textBox.Text = string.Empty;
                            }
                        }));
                }).Start();
        }

        /// <summary>
        /// Searches for the TV show and inserts it into the database.
        /// </summary>
        /// <param name="show">The show name.</param>
        /// <param name="grabber">The grabber name.</param>
        /// <param name="markAsSeen">if set to <c>true</c> all aired episodes will be marked as seen.</param>
        /// <returns>
        /// 	<c>true</c> if the show was added successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="Exception"><c>Exception</c>.</exception>
        public static bool Add(string show, string grabber, bool markAsSeen)
        {
            Guide guide;
            switch(grabber)
            {
                default:
                case "TVRage":
                    guide = new TVRage();
                    break;

                case "The TVDB":
                    guide = new TVDB();
                    break;

                case "TV.com":
                    guide = new TVcom();
                    break;

                case "EPGuides - TVRage":
                    guide = new EPGuides("tvrage.com");
                    break;

                case "EPGuides - TV.com":
                    guide = new EPGuides("tv.com");
                    break;

                case "AniDB":
                    guide = new AniDB();
                    break;

                case "Generate list based on download links":
                    guide = new Guess();
                    break;
            }

            // get ID on guide
            string id;
            try
            {
                id = guide.GetID(show);

                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new Exception();
                }
            }
            catch
            {
                new TaskDialog
                    {
                        Icon                = TaskDialogStandardIcon.Error,
                        Caption             = "Couldn't find TV show",
                        InstructionText     = show.ToUppercaseFirst(),
                        Text                = "Couldn't find the specified TV show on this database.",
                        DetailsExpandedText = "If this is a popular TV show, then you maybe just misspelled it or didn't enter the full official name.\r\nIf this is a rare or foreign TV show, try using another database.",
                        Cancelable          = true
                    }.Show();
                return false;
            }

            // get data from guide
            TVShow tv;
            try
            {
                tv = guide.GetData(id);

                if (tv.Episodes.Count == 0)
                {
                    throw new Exception("There aren't any episodes associated to this TV show on this database.");
                }
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        Icon                = TaskDialogStandardIcon.Error,
                        Caption             = "Couldn't grab TV show",
                        InstructionText     = show.ToUppercaseFirst(),
                        Text                = "Couldn't download the episode listing and associated informations due to an unexpected error.",
                        DetailsExpandedText = ex.Message,
                        Cancelable          = true
                    }.Show();
                return false;
            }

            // try to see if duplicate
            if (!string.IsNullOrWhiteSpace(Database.GetShowID(tv.Title)))
            {
                new TaskDialog
                    {
                        Icon                = TaskDialogStandardIcon.Error,
                        Caption             = "Duplicate entry",
                        InstructionText     = tv.Title,
                        Text                = "This TV show is already on your list.",
                        Cancelable          = true
                    }.Show();
                return false;
            }

            // insert into tvshows and let the autoincrementing field assign a showid
            Database.Execute("update tvshows set rowid = rowid + 1");
            Database.Execute("insert into tvshows values (1, null, ?)", tv.Title);

            // then get that showid
            var showid = Database.GetShowID(tv.Title);

            // insert showdata fields
            Database.ShowData(showid, "grabber", guide.GetType().Name);
            Database.ShowData(showid, "genre",   tv.Genre);
            Database.ShowData(showid, "descr",   tv.Description);
            Database.ShowData(showid, "cover",   tv.Cover);
            Database.ShowData(showid, "airing",  tv.Airing.ToString());
            Database.ShowData(showid, "airtime", tv.AirTime);
            Database.ShowData(showid, "airday",  tv.AirDay);
            Database.ShowData(showid, "network", tv.Network);
            Database.ShowData(showid, "runtime", tv.Runtime.ToString());

            // create transaction
            SQLiteTransaction tr;
            try
            {
                tr = Database.Connection.BeginTransaction();
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        Icon                = TaskDialogStandardIcon.Error,
                        Caption             = "Couldn't create transaction",
                        InstructionText     = tv.Title,
                        Text                = "Couldn't create transaction on the database to insert the episodes. The TV show is added to the list, however, there aren't any episodes associated to it. Run an update to add the episodes.",
                        DetailsExpandedText = ex.Message,
                        Cancelable          = true
                    }.Show();
                return false;
            }

            // insert episodes
            foreach (var ep in tv.Episodes)
            {
                try
                {
                    Database.ExecuteOnTransaction(tr, "insert into episodes values (?, ?, ?, ?, ?, ?, ?, ?)",
                                                  ep.Number + (ep.Season * 1000) + (showid.ToInteger() * 100 * 1000),
                                                  showid,
                                                  ep.Season,
                                                  ep.Number,
                                                  tv.AirTime == String.Empty || ep.Airdate == Utils.UnixEpoch
                                                   ? ep.Airdate.ToUnixTimestamp()
                                                   : DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone().ToUnixTimestamp(),
                                                  ep.Title,
                                                  ep.Summary,
                                                  ep.Picture);
                }
                catch
                {
                    continue;
                }
            }

            // commit the changes
            tr.Commit();

            // mark all aired episodes as seen
            if (markAsSeen)
            {
                Database.Execute("insert into tracking select showid, episodeid from episodes where showid = ? and airdate < ? and airdate != 0", showid, DateTime.Now.ToUnixTimestamp());
            }

            // fire data change event
            MainWindow.Active.DataChanged();

            // asynchronously update lab.rolisoft.net's cache
            Updater.UpdateRemoteCache(new Tuple<string, string>(guide.GetType().Name, id), tv);

            // show this on another thread so the control enabler can run
            new Task(() => new TaskDialog
                {
                    Icon            = TaskDialogStandardIcon.Information,
                    Caption         = "Added successfully",
                    InstructionText = tv.Title,
                    Text            = "The TV show has been successfully added to your list!",
                    Cancelable      = true
                }.Show()).Start();

            return true;
        }
    }
}
