namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Interaction logic for EditShowWindow.xaml
    /// </summary>
    public partial class EditShowWindow
    {
        private Guide _guide;
        private string _show, _id;
        private Thread _worker;
        private List<ShowID> _shows;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewWindow"/> class.
        /// </summary>
        public EditShowWindow(string id, string show)
        {
            InitializeComponent();

            _id   = id;
            _show = show;
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

            editTabItem.Header = "Edit " + _show;

            var grabber = Database.ShowData(_id, "grabber");

            switch (grabber)
            {
                case "TVRage":
                    database.SelectedIndex = 1;
                    break;

                case "TVDB":
                    database.SelectedIndex = 2;
                    break;

                case "TVcom":
                    database.SelectedIndex = 3;
                    break;

                case "IMDb":
                    database.SelectedIndex = 4;
                    break;

                case "AniDB":
                    database.SelectedIndex = 5;
                    break;

                case "EPGuides":
                    database.SelectedIndex = 7;
                    break;
            }

            DatabaseSelectionChanged(null, null);

            var lang = Database.ShowData(_id, grabber + ".lang");

            for (int i = 0; i < language.Items.Count; i++)
            {
                if ((language.Items[i] as StackPanel).Tag.ToString() == lang)
                {
                    language.SelectedIndex = i;
                    break;
                }
            }

            ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the database control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void DatabaseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((database.SelectedValue as ComboBoxItem).Content == null) return;
            _guide = AddNewWindow.CreateGuide((((database.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as Label).Content.ToString().Trim());

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
            working.Content           = "Searching on {0}...".FormatWith(_guide.Name);
            subworking.Content        = _show;
            editTabItem.Visibility    = Visibility.Collapsed;
            workingTabItem.Visibility = Visibility.Visible;
            tabControl.SelectedIndex  = 1;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);

            var lang = (language.SelectedValue as StackPanel).Tag.ToString();

            _worker = new Thread(() =>
                {
                    try
                    {
                        _shows = _guide.GetID(_show, lang).ToList();

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
                                editTabItem.Visibility    = Visibility.Visible;
                                tabControl.SelectedIndex  = 0;

                                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                            }));

                        new TaskDialog
                            {
                                Icon                = TaskDialogStandardIcon.Error,
                                Caption             = "Couldn't find TV show",
                                InstructionText     = _show,
                                Text                = "Couldn't find the specified TV show on this database.",
                                DetailsExpandedText = "If this is a popular TV show, then you maybe just misspelled it or didn't enter the full official name.\r\nIf this is a rare or foreign TV show, try using another database.",
                                Cancelable          = true
                            }.Show();

                        return;
                    }

                    // test if the application's engine sees the entered and the returned first result as an exact match
                    var enterroot = ShowNames.Parser.Normalize(_show);
                    var matchroot = ShowNames.Parser.Normalize(_shows[0].Title);
                    var exact = enterroot == matchroot;

                    if (exact && _shows.Count != 1)
                    {
                        // test if the second result is an exact match too
                        // if it is, then selection is required by the user
                        exact = enterroot != ShowNames.Parser.Normalize(_shows[1].Title);

                        if (exact && _shows.Count != 2)
                        {
                            exact = enterroot != ShowNames.Parser.Normalize(_shows[2].Title);
                        }
                    }

                    Dispatcher.Invoke((Action)(() =>
                        {
                            if (exact)
                            {
                                SetShow(_shows[0]);
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
            editTabItem.Visibility    = Visibility.Visible;
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
            editTabItem.Visibility   = Visibility.Visible;
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
            SetShow(_shows.Where(show => show.Title == (string)listBox.SelectedValue).First());
        }

        /// <summary>
        /// Sets the new show ID.
        /// </summary>
        /// <param name="show">The show.</param>
        private void SetShow(ShowID show)
        {
            var gname = _guide.GetType().Name;

            if (Synchronization.Status.Enabled)
            {
                Synchronization.Status.Engine.ModifyShow(_id, new[] { _show, gname, show.ID, show.Language });
            }

            Database.ShowData(_id, "grabber",       gname);
            Database.ShowData(_id, gname + ".id",   show.ID);
            Database.ShowData(_id, gname + ".lang", show.Language);

            Hide();

            new TaskDialog
                {
                    Icon                = TaskDialogStandardIcon.Information,
                    Caption             = "Show information updated",
                    InstructionText     = show.Title,
                    Text                = "The guide of this TV show was changed. Initiate a database update to get the new episode listing and show informations.",
                    Cancelable          = true
                }.Show();

            Close();
        }
    }
}
