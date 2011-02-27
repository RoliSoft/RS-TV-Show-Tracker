namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Forms;

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.Parsers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    using CheckBox       = System.Windows.Controls.CheckBox;
    using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        /// <summary>
        /// Gets or sets the downloads list view item collection.
        /// </summary>
        /// <value>The downloads list view item collection.</value>
        public ObservableCollection<DownloadsListViewItem> DownloadsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private List<DownloadSearchEngine> _engines;
        private List<string> _trackers, _excludes;

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

            // general

            dlPathTextBox.Text      = Settings.Get("Download Path");
            torrentPathTextBox.Text = Settings.Get("Torrent Downloader");
            processTextBox.Text     = string.Join(",", Settings.GetList("Processes to Monitor"));

            using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                startAtStartup.IsChecked = rk.GetValue("RS TV Show Tracker") != null;
            }

            convertTimezone.IsChecked = Settings.Get("Convert Timezone", true);
            currentTimezone.Text      = "Your current timezone is " + TimeZoneInfo.Local.DisplayName + ".\r\n"
                                      + "Your difference from Central Standard Time is {0} hours.".FormatWith(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").BaseUtcOffset.TotalHours * -1)).TotalHours);

            showUnhandledErrors.IsChecked = Settings.Get<bool>("Show Unhandled Errors");
            
            // downloads

            DownloadsListViewItemCollection = new ObservableCollection<DownloadsListViewItem>();
            listView.ItemsSource            = DownloadsListViewItemCollection;

            _engines = typeof(DownloadSearchEngine)
                       .GetDerivedTypes()
                       .Select(type => Activator.CreateInstance(type) as DownloadSearchEngine)
                       .ToList();

            _trackers = Settings.GetList("Tracker Order").ToList();
            _trackers.AddRange(_engines
                               .Where(engine => _trackers.IndexOf(engine.Name) == -1)
                               .Select(engine => engine.Name));

            _excludes = Settings.GetList("Tracker Exclusions").ToList();

            foreach (var engine in _engines.OrderBy(engine => _trackers.IndexOf(engine.Name)))
            {
                DownloadsListViewItemCollection.Add(new DownloadsListViewItem
                    {
                        Enabled         = !_excludes.Contains(engine.Name),
                        Icon            = engine.Icon,
                        Site            = engine.Name,
                        Type            = engine.Type.ToString(),
                        RequiresCookies = engine.Private ? "Yes" : "No",
                        Developer       = engine.GetAttribute<ParserAttribute>().Developer,
                        Revision        = engine.GetAttribute<ParserAttribute>().Revision.ToRelativeDate(),
                        Assembly        = engine.GetType().Assembly.GetName().Name
                    });
            }

            listView.SelectedIndex = 0;
        }

        #region General
        /// <summary>
        /// Handles the Click event of the dlPathBrowseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog
                {
                    Description         = "Select the directory where you download your TV shows:",
                    ShowNewFolderButton = false
                };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dlPathTextBox.Text = fbd.SelectedPath + Path.DirectorySeparatorChar;
            }
        }

        /// <summary>
        /// Handles the Click event of the torrentPathBrowseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TorrentPathBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    Title           = "Select the alternative torrent downloader",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect     = false,
                    Filter          = "Executable|*.exe"
                };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                torrentPathTextBox.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the dlPathTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void DlPathTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Directory.Exists(dlPathTextBox.Text))
            {
                Settings.Set("Download Path", dlPathTextBox.Text);
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the torrentPathTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TorrentPathTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (File.Exists(torrentPathTextBox.Text))
            {
                Settings.Set("Torrent Downloader", torrentPathTextBox.Text);
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the processTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void ProcessTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Settings.Set("Processes to Monitor", processTextBox.Text.Trim(',').Split(','));
        }

        /// <summary>
        /// Handles the Checked event of the startAtStartup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StartAtStartupChecked(object sender, RoutedEventArgs e)
        {
            using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                rk.SetValue("RS TV Show Tracker", "\"" + Assembly.GetExecutingAssembly().Location + "\" -hide");
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the startAtStartup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StartAtStartupUnchecked(object sender, RoutedEventArgs e)
        {
            using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                rk.DeleteValue("RS TVShow Tracker", false);
            }
        }

        /// <summary>
        /// Handles the Checked event of the convertTimezone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ConvertTimezoneChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Convert Timezone", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the convertTimezone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ConvertTimezoneUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Convert Timezone", false);
        }

        /// <summary>
        /// Handles the Checked event of the showUnhandledErrors control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowUnhandledErrorsChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Show Unhandled Errors", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the showUnhandledErrors control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowUnhandledErrorsUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Show Unhandled Errors", false);
        }
        #endregion

        #region Downloads
        /// <summary>
        /// Handles the SelectionChanged event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ListViewSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var sel    = listView.SelectedItem as DownloadsListViewItem;
            var engine = _engines.Single(en => en.Name == sel.Site);

            engineTitle.Content = sel.Site;

            if (engine.Private)
            {
                cookiesLabel.IsEnabled = cookiesTextBox.IsEnabled = grabCookiesButton.IsEnabled = true;
                cookiesTextBox.Text = Settings.Get(engine.Name + " Cookies");
            }
            else
            {
                cookiesLabel.IsEnabled = cookiesTextBox.IsEnabled = grabCookiesButton.IsEnabled = false;
                cookiesTextBox.Text = "not required";
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the cookiesTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void CookiesTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var sel    = listView.SelectedItem as DownloadsListViewItem;
            var engine = _engines.Single(en => en.Name == sel.Site);

            if (engine.Private && !string.IsNullOrWhiteSpace(cookiesTextBox.Text))
            {
                Settings.Set(engine.Name + " Cookies", cookiesTextBox.Text.Trim());
            }
        }

        /// <summary>
        /// Handles the Checked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledChecked(object sender, RoutedEventArgs e)
        {
            if (_excludes.Contains((sender as CheckBox).Tag as string))
            {
                _excludes.Remove((sender as CheckBox).Tag as string);

                SaveExclusions();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_excludes.Contains((sender as CheckBox).Tag as string))
            {
                _excludes.Add((sender as CheckBox).Tag as string);

                SaveExclusions();
            }
        }

        /// <summary>
        /// Saves the exclusions to the XML settings file.
        /// </summary>
        public void SaveExclusions()
        {
            Settings.Set("Tracker Exclusions", _excludes);
        }

        /// <summary>
        /// Handles the Click event of the moveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex != 0)
            {
                _trackers.MoveUp(listView.SelectedIndex);
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex - 1);

                SaveOrder();
            }
        }

        /// <summary>
        /// Handles the Click event of the moveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex != _trackers.Count - 1)
            {
                _trackers.MoveDown(listView.SelectedIndex);
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex + 1);

                SaveOrder();
            }
        }

        /// <summary>
        /// Saves the order to the XML settings file.
        /// </summary>
        public void SaveOrder()
        {
            Settings.Set("Tracker Order", _trackers);
        }

        /// <summary>
        /// Handles the Click event of the grabCookiesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GrabCookiesButtonClick(object sender, RoutedEventArgs e)
        {
            var sel    = listView.SelectedItem as DownloadsListViewItem;
            var engine = _engines.Single(en => en.Name == sel.Site);

            if (engine.Private && new CookieCatcherWindow(engine).ShowDialog() == true) // yes, == true, because ShowDialog() returns Nullable<bool> not bool directly
            {
                cookiesTextBox.Text = Settings.Get(engine.Name + " Cookies");
            }
        }
        #endregion

        /// <summary>
        /// Handles the Closing event of the GlassWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void GlassWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Dispatcher.Invoke((Action)(() => MainWindow.Active.activeDownloadLinksPage.LoadEngines(true)));
        }
    }
}
