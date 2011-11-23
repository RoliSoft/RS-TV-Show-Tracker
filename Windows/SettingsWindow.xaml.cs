namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.ContextMenus.Menus;
    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.LinkCheckers;
    using RoliSoft.TVShowTracker.Parsers.OnlineVideos;
    using RoliSoft.TVShowTracker.Parsers.Recommendations;
    using RoliSoft.TVShowTracker.Parsers.Social;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;
    using RoliSoft.TVShowTracker.Parsers.WebSearch;
    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;

    using VistaControls.TaskDialog;

    using CheckBox       = System.Windows.Controls.CheckBox;
    using Label          = System.Windows.Controls.Label;
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
        /// Gets or sets the proxies list view item collection.
        /// </summary>
        /// <value>The proxies list view item collection.</value>
        public ObservableCollection<ProxiesListViewItem> ProxiesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the proxied domains list view item collection.
        /// </summary>
        /// <value>The proxied domains list view item collection.</value>
        public ObservableCollection<ProxiedDomainsListViewItem> ProxiedDomainsListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the plugins list view item collection.
        /// </summary>
        /// <value>The plugins list view item collection.</value>
        public ObservableCollection<PluginsListViewItem> PluginsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
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

            // general

            try
            { 
                foreach (var path in Settings.Get<List<string>>("Download Paths"))
                {
                    dlPathsListBox.Items.Add(path);
                }

                DlPathsListBoxSelectionChanged();

                using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    startAtStartup.IsChecked = rk.GetValue("RS TV Show Tracker") != null;
                }

                convertTimezone.IsChecked = Settings.Get("Convert Timezone", true);

                var tzinfo = "Your current timezone is " + TimeZoneInfo.Local.DisplayName + ".\r\n"
                           + "Your difference from Central Standard Time is {0} hours.".FormatWith(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").BaseUtcOffset.TotalHours * -1)).TotalHours);

                currentTimezone.ContentEnd.InsertTextInRun(tzinfo);

                searchNtfsMft.IsChecked       = Settings.Get<bool>("Search NTFS MFT records");
                disableAero.IsChecked         = !Settings.Get("Enable Aero", true);
                disableAnimations.IsChecked   = !Settings.Get("Enable Animations", true);
                showUnhandledErrors.IsChecked = Settings.Get<bool>("Show Unhandled Errors");
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            // associations

            try
            {
                torrentPathTextBox.Text = Settings.Get("Torrent Downloader");
                usenetPathTextBox.Text  = Settings.Get("Usenet Downloader");
                jDlPathTextBox.Text     = Settings.Get("JDownloader");

                var atr = Utils.GetApplicationForExtension(".torrent");
                Tuple<string, BitmapSource> tri;
                if (!string.IsNullOrWhiteSpace(atr) && (tri = Utils.GetExecutableInfo(atr)) != null)
                {
                    torrentAssociationName.Text   = tri.Item1;
                    torrentAssociationIcon.Source = tri.Item2;
                }
                else
                {
                    torrentAssociationName.Text = "[no software associated with .torrent files]";
                }

                var anz = Utils.GetApplicationForExtension(".nzb");
                Tuple<string, BitmapSource> nzi;
                if (!string.IsNullOrWhiteSpace(anz) && (nzi = Utils.GetExecutableInfo(anz)) != null)
                {
                    usenetAssociationName.Text   = nzi.Item1;
                    usenetAssociationIcon.Source = nzi.Item2;
                }
                else
                {
                    usenetAssociationName.Text = "[no software associated with .nzb files]";
                }

                var htz = Utils.GetApplicationForExtension(".htm");
                Tuple<string, BitmapSource> hti;
                if (!string.IsNullOrWhiteSpace(htz) && (hti = Utils.GetExecutableInfo(htz)) != null)
                {
                    httpAssociationName.Text   = hti.Item1;
                    httpAssociationIcon.Source = hti.Item2;
                }
                else
                {
                    httpAssociationName.Text = "[no software associated with .htm files]";
                }

                var viddef = Utils.GetDefaultVideoPlayers();
                foreach (var app in viddef)
                {
                    var info = Utils.GetExecutableInfo(app);

                    if (info == null || string.IsNullOrWhiteSpace(info.Item1)) continue;

                    if (info.Item2 != null)
                    {
                        processesStackPanel.Children.Add(new Image
                            {
                                Source = info.Item2,
                                Width  = 16,
                                Height = 16,
                                Margin = new Thickness(0, 0, 4, 0),
                            });
                    }

                    processesStackPanel.Children.Add(new Label
                        {
                            Content = info.Item1,
                            Margin  = new Thickness(0, 0, 7, 0),
                            Padding = new Thickness(0)
                        });
                }

                processTextBox.Text = string.Join(",", Settings.Get<List<string>>("Processes to Monitor"));

                monitorNetworkShare.IsChecked = Settings.Get<bool>("Monitor Network Shares");
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            // parsers

            try
            {
                DownloadsListViewItemCollection = new ObservableCollection<DownloadsListViewItem>();
                listView.ItemsSource = DownloadsListViewItemCollection;

                ReloadParsers();
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            // proxies

            try
            { 
                ProxiesListViewItemCollection = new ObservableCollection<ProxiesListViewItem>();
                proxiesListView.ItemsSource   = ProxiesListViewItemCollection;
            
                ProxiedDomainsListViewItemCollection = new ObservableCollection<ProxiedDomainsListViewItem>();
                proxiedDomainsListView.ItemsSource   = ProxiedDomainsListViewItemCollection;

                ReloadProxies();
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            // plugins

            try
            {
                PluginsListViewItemCollection = new ObservableCollection<PluginsListViewItem>();
                pluginsListView.ItemsSource   = PluginsListViewItemCollection;

                ReloadPlugins();
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }
        }

        /// <summary>
        /// Handles the Closing event of the GlassWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void GlassWindowClosing(object sender, CancelEventArgs e)
        {
            Dispatcher.Invoke((Action)(() => MainWindow.Active.activeDownloadLinksPage.LoadEngines(true)));
        }

        #region General
        /// <summary>
        /// Handles the SelectionChanged event of the dlPathsListBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void DlPathsListBoxSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            dlPathRemoveButton.IsEnabled   = dlPathsListBox.SelectedIndex != -1;
            dlPathMoveUpButton.IsEnabled   = dlPathsListBox.SelectedIndex > 0;
            dlPathMoveDownButton.IsEnabled = dlPathsListBox.SelectedIndex != -1 && dlPathsListBox.SelectedIndex < dlPathsListBox.Items.Count - 1;
        }

        /// <summary>
        /// Handles the Click event of the dlPathAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathAddButtonClick(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description         = "Select the directory where you download your TV shows:",
                    ShowNewFolderButton = false
                };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dlPathsListBox.Items.Add(fbd.SelectedPath + Path.DirectorySeparatorChar);
            }

            SaveDlPaths();
        }

        /// <summary>
        /// Handles the Click event of the dlPathMoveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathMoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (dlPathsListBox.SelectedIndex == -1) return;

            var idx = dlPathsListBox.SelectedIndex;
            var sel = dlPathsListBox.Items[idx];

            if (idx > 0)
            {
                dlPathsListBox.Items.Remove(sel);
                dlPathsListBox.Items.Insert(idx - 1, sel);
                dlPathsListBox.SelectedItem = sel;

                SaveDlPaths();
            }
        }

        /// <summary>
        /// Handles the Click event of the dlPathMoveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathMoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (dlPathsListBox.SelectedIndex == -1) return;

            var idx = dlPathsListBox.SelectedIndex;
            var sel = dlPathsListBox.Items[idx];

            if (idx < dlPathsListBox.Items.Count - 1)
            {
                dlPathsListBox.Items.Remove(sel);
                dlPathsListBox.Items.Insert(idx + 1, sel);
                dlPathsListBox.SelectedItem = sel;

                SaveDlPaths();
            }
        }

        /// <summary>
        /// Handles the Click event of the dlPathRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (dlPathsListBox.SelectedIndex == -1) return;

            dlPathsListBox.Items.RemoveAt(dlPathsListBox.SelectedIndex);

            SaveDlPaths();
        }

        /// <summary>
        /// Saves the download paths to the XML settings file.
        /// </summary>
        public void SaveDlPaths()
        {
            Settings.Set("Download Paths", dlPathsListBox.Items.Cast<string>().ToList());
        }

        /// <summary>
        /// Handles the TextChanged event of the processTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void ProcessTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var proc = processTextBox.Text.Trim(',').Split(',').ToList();

            if (proc.Count == 1 && string.IsNullOrWhiteSpace(proc[0]))
            {
                proc.RemoveAt(0);
            }

            Settings.Set("Processes to Monitor", proc);
        }

        /// <summary>
        /// Handles the Checked event of the searchNtfsMft control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchNtfsMftChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Search NTFS MFT records", true);

            if (!Utils.IsAdmin)
            {
                new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.Warning,
                        Title       = "Administrator right required",
                        Instruction = "Administrator right required",
                        Content     = "The software doesn't have administrator rights, which means it won't be able to access the MFT records on your NTFS partitions. Please restart the software by right-clicking on the executable and selecting \"Run as administrator\" from the menu."
                    }.Show();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the searchNtfsMft control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchNtfsMftUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Search NTFS MFT records", false);
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
        /// Handles the Checked event of the disableAero control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAeroChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Enable Aero", false);

            MainWindow.Active.ActivateNonAero();
        }

        /// <summary>
        /// Handles the Unchecked event of the disableAero control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAeroUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Enable Aero", true);

            MainWindow.Active.AeroChanged(sender, new PropertyChangedEventArgs("IsGlassEnabled"));
        }

        /// <summary>
        /// Handles the Checked event of the disableAnimations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAnimationsChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Enable Animations", false);

            MainWindow.Active.DeactivateAnimation();
        }

        /// <summary>
        /// Handles the Unchecked event of the disableAnimations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAnimationsUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Enable Animations", true);

            MainWindow.Active.ActivateAnimation();
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

        #region Downloaders
        /// <summary>
        /// Handles the Checked event of the monitorNetworkShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorNetworkShareChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Monitor Network Shares", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the monitorNetworkShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorNetworkShareUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Monitor Network Shares", false);
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
        /// Handles the TextChanged event of the torrentPathTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TorrentPathTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (torrentPathTextBox.Text.Length == 0 || File.Exists(torrentPathTextBox.Text))
            {
                Settings.Set("Torrent Downloader", torrentPathTextBox.Text);
            }
        }

        /// <summary>
        /// Handles the Click event of the usenetPathBrowseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UsenetPathBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    Title           = "Select the alternative usenet downloader",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect     = false,
                    Filter          = "Executable|*.exe"
                };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                usenetPathTextBox.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the usenetPathTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void UsenetPathTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (usenetPathTextBox.Text.Length == 0 || File.Exists(usenetPathTextBox.Text))
            {
                Settings.Set("Usenet Downloader", usenetPathTextBox.Text);
            }
        }

        /// <summary>
        /// Handles the Click event of the jDlPathBrowseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void JDlPathBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    Title           = "Select the path to JDownloader",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect     = false,
                    Filter          = "JDownloader executable|JDownloader.exe"
                };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                jDlPathTextBox.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the jDlPathTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void JDlPathTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (jDlPathTextBox.Text.Length == 0 || File.Exists(jDlPathTextBox.Text))
            {
                Settings.Set("JDownloader", jDlPathTextBox.Text);
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
        #endregion

        #region Parsers
        /// <summary>
        /// Handles the SelectionChanged event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            parserEditButton.IsEnabled = listView.SelectedIndex != -1;
            moveUpButton.IsEnabled     = listView.SelectedIndex > 0;
            moveDownButton.IsEnabled   = listView.SelectedIndex != -1 && listView.SelectedIndex < listView.Items.Count - 1;
        }

        /// <summary>
        /// Reloads the parsers list view.
        /// </summary>
        private void ReloadParsers()
        {
            var idx = listView.SelectedIndex;
            DownloadsListViewItemCollection.Clear();

            foreach (var engine in AutoDownloader.SearchEngines.OrderBy(engine => AutoDownloader.Parsers.IndexOf(engine.Name)))
            {
                DownloadsListViewItemCollection.Add(new DownloadsListViewItem
                    {
                        Enabled = AutoDownloader.Actives.Contains(engine.Name),
                        Icon    = engine.Icon,
                        Site    = engine.Name,
                        Login   = engine.Private
                                  ? !string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Login"))
                                    ? "/RSTVShowTracker;component/Images/tick.png"
                                    : !string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Cookies"))
                                      ? "/RSTVShowTracker;component/Images/cookie.png"
                                      : "/RSTVShowTracker;component/Images/cross.png"
                                  : "/RSTVShowTracker;component/Images/na.png",
                        Version = engine.Version.ToString().PadRight(14, '0')
                    });
            }

            listView.SelectedIndex = idx;
            ListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the Checked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledChecked(object sender, RoutedEventArgs e)
        {
            if (!AutoDownloader.Actives.Contains((sender as CheckBox).Tag as string))
            {
                AutoDownloader.Actives.Add((sender as CheckBox).Tag as string);

                Settings.Set("Active Trackers", AutoDownloader.Actives);
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledUnchecked(object sender, RoutedEventArgs e)
        {
            if (AutoDownloader.Actives.Contains((sender as CheckBox).Tag as string))
            {
                AutoDownloader.Actives.Remove((sender as CheckBox).Tag as string);

                Settings.Set("Active Trackers", AutoDownloader.Actives);
            }
        }

        /// <summary>
        /// Handles the Click event of the parserEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ParserEditButtonClick(object sender, RoutedEventArgs e)
        {
            var sel = listView.SelectedItem as DownloadsListViewItem;
            var prs = AutoDownloader.SearchEngines.Single(en => en.Name == sel.Site);

            if (prs.Private)
            {
                if (new ParserWindow(prs).ShowDialog() == true) // Nullable<bool> vs true
                {
                    ReloadParsers();
                }
            }
            else
            {
                MessageBox.Show(prs.Name + " does not require authentication to search the site.", "Login not required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Click event of the moveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var idx = AutoDownloader.Parsers.IndexOf((listView.SelectedItem as DownloadsListViewItem).Site);

            if (idx != 0)
            {
                AutoDownloader.Parsers.MoveUp(idx);
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex - 1);
                ListViewSelectionChanged();

                Settings.Set("Tracker Order", AutoDownloader.Parsers);
            }
        }

        /// <summary>
        /// Handles the Click event of the moveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var idx = AutoDownloader.Parsers.IndexOf((listView.SelectedItem as DownloadsListViewItem).Site);

            if (idx != AutoDownloader.Parsers.Count - 1)
            {
                AutoDownloader.Parsers.MoveDown(idx);
                DownloadsListViewItemCollection.Move(listView.SelectedIndex, listView.SelectedIndex + 1);
                ListViewSelectionChanged();

                Settings.Set("Tracker Order", AutoDownloader.Parsers);
            }
        }
        #endregion

        #region Proxies
        /// <summary>
        /// Handles the SelectionChanged event of the proxiesListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ProxiesListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            proxyEditButton.IsEnabled = proxySearchButton.IsEnabled = proxyTestButton.IsEnabled = proxyRemoveButton.IsEnabled = proxiesListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the proxiedDomainsListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ProxiedDomainsListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            proxyDomainEditButton.IsEnabled = proxyDomainRemoveButton.IsEnabled = proxiedDomainsListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Reloads the proxy-related list views.
        /// </summary>
        private void ReloadProxies()
        {
            ProxiesListViewItemCollection.Clear();

            foreach (var proxy in Settings.Get<Dictionary<string, object>>("Proxies"))
            {
                ProxiesListViewItemCollection.Add(new ProxiesListViewItem
                    {
                        Name    = proxy.Key,
                        Address = (string)proxy.Value
                    });
            }

            ProxiesListViewSelectionChanged();

            ProxiedDomainsListViewItemCollection.Clear();

            foreach (var proxy in Settings.Get<Dictionary<string, object>>("Proxied Domains"))
            {
                ProxiedDomainsListViewItemCollection.Add(new ProxiedDomainsListViewItem
                    {
                        Icon   = "http://g.etfv.co/http://www." + proxy.Key + "?defaulticon=lightpng",
                        Domain = proxy.Key,
                        Proxy  = (string)proxy.Value
                    });
            }

            ProxiedDomainsListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the Click event of the proxyAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyAddButtonClick(object sender, RoutedEventArgs e)
        {
            new ProxyWindow().ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;

            new ProxyWindow(sel.Name, sel.Address).ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxySearchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxySearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;
            
            Thread action = null;
            var done = false;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;
            var uri = new Uri(sel.Address);

            if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
            {
                var app = "a local application";

                try
                {
                    var tcpRows = Utils.GetExtendedTCPTable();
                    foreach (var row in tcpRows)
                    {
                        if (((row.localPort1 << 8) + (row.localPort2) + (row.localPort3 << 24) + (row.localPort4 << 16)) == uri.Port)
                        {
                            app = "PID " + row.owningPid + " (" + Process.GetProcessById(row.owningPid).Modules[0].FileName + ")";
                            break;
                        }
                    }
                }
                catch { }

                new Thread(() => new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.SecurityWarning,
                        Title       = sel.Name,
                        Instruction = "Potentially dangerous",
                        Content     = "This proxy points to a local loopback address on port " + uri.Port + ".\r\nYour requests will go to " + app + ", which will most likely forward them to an external server."
                    }.Show()).Start();
                return;
            }

            var td  = new TaskDialog
                {
                    Title           = sel.Name,
                    Instruction     = "Testing proxy",
                    Content         = "Testing whether " + uri.Host + " is a known proxy...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };
            td.ButtonClick += (s, v) =>
                {
                    if (!done)
                    {
                        try { action.Abort(); } catch { }
                    }
                };
            td.SetMarqueeProgressBar(true);
            new Thread(() => td.Show()).Start();

            action = new Thread(() =>
                {
                    try
                    {
                        var src = new Bing();
                        var res = new List<SearchResult>();
                        res.AddRange(src.Search(uri.Host + " intitle:proxy"));

                        if (res.Count == 0)
                        {
                            res.AddRange(src.Search(uri.Host + " intitle:proxies"));
                        }

                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        if (res.Count == 0)
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.SecuritySuccess,
                                    Title       = sel.Name,
                                    Instruction = "Not a known public proxy",
                                    Content     = uri.Host + " does not seem to be a known public proxy." + Environment.NewLine + Environment.NewLine +
                                                  "If your goal is to trick proxy detectors, you're probably safe for now. However, you shouldn't use public proxies if you don't want to potentially compromise your account."
                                }.Show();
                            return;
                        }
                        else
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.SecurityError,
                                    Title       = sel.Name,
                                    Instruction = "Known public proxy",
                                    Content     = uri.Host + " is a known public proxy according to " + new Uri(res[0].URL).Host.Replace("www.", string.Empty) + Environment.NewLine + Environment.NewLine +
                                                  "If the site you're trying to access through this proxy forbids proxy usage, they're most likely use a detection mechanism too, which will trigger an alert when it sees this IP address. Your requests will be denied and your account might also get banned. Even if the site's detector won't recognize it, using a public proxy is not such a good idea, because you could compromise your account as public proxy operators are known to be evil sometimes."
                                }.Show();
                            return;
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        new TaskDialog
                            {
                                CommonIcon          = TaskDialogIcon.Stop,
                                Title               = sel.Name,
                                Instruction         = "Connection error",
                                Content             = "An error occured while checking the proxy.",
                                ExpandedControlText = "Show exception message",
                                ExpandedInformation = ex.Message
                            }.Show();
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the proxyTestButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyTestButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            Thread action = null;
            var done = false;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;
            var uri = new Uri(sel.Address);
            var td  = new TaskDialog
                {
                    Title           = sel.Name,
                    Instruction     = "Testing proxy",
                    Content         = "Testing connection through " + uri.Host + ":" + uri.Port + "...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };
            td.ButtonClick += (s, v) =>
                {
                    if (!done)
                    {
                        try { action.Abort(); } catch { }
                    }
                };
            td.SetMarqueeProgressBar(true);
            new Thread(() => td.Show()).Start();

            action = new Thread(() =>
                {
                    var s = Stopwatch.StartNew();

                    try
                    {
                        var b = Utils.GetHTML("http://rolisoft.net/b", proxy: sel.Address);
                        s.Stop();

                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        var tor  = b.DocumentNode.SelectSingleNode("//img[@class='tor']");
                        var ip   = b.DocumentNode.GetTextValue("//span[@class='ip'][1]");
                        var host = b.DocumentNode.GetTextValue("//span[@class='host'][1]");
                        var geo  = b.DocumentNode.GetTextValue("//span[@class='geoip'][1]");

                        if (tor != null)
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.SecurityError,
                                    Title       = sel.Name,
                                    Instruction = "TOR detected",
                                    Content     = ip + " is a TOR exit node." + Environment.NewLine + Environment.NewLine +
                                                  "If the site you're trying to access through this proxy forbids proxy usage, they're most likely use a detection mechanism too, which will trigger an alert when it sees this IP address. Your requests will be denied and your account might also get banned. Even if the site's detector won't recognize it, using TOR is not such a good idea, because you could compromise your account as TOR exit node operators are known to be evil sometimes."
                                }.Show();
                        }

                        if (ip == null)
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.Stop,
                                    Title       = sel.Name,
                                    Instruction = "Proxy error",
                                    Content     = "The proxy did not return the requested resource, or greatly modified the structure of the page. Either way, it is not suitable for use with this software.",
                                }.Show();
                            return;
                        }

                        new TaskDialog
                            {
                                CommonIcon  = TaskDialogIcon.Information,
                                Title       = sel.Name,
                                Instruction = "Test results",
                                Content     = "Total time to get rolisoft.net/b: " + s.Elapsed + "\r\n\r\nIP address: " + ip + "\r\nHost name: " + host + "\r\nGeoIP lookup: " + geo,
                            }.Show();
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        new TaskDialog
                            {
                                CommonIcon          = TaskDialogIcon.Stop,
                                Title               = sel.Name,
                                Instruction         = "Connection error",
                                Content             = "An error occured while connecting to the proxy.",
                                ExpandedControlText = "Show exception message",
                                ExpandedInformation = ex.Message
                            }.Show();
                    }
                    finally
                    {
                        if (s.IsRunning)
                        {
                            s.Stop();
                        }
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the proxyRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Name + " and all the proxied domains associated with it?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var dict = Settings.Get<Dictionary<string, object>>("Proxied Domains");

                foreach (var prdmn in dict.ToDictionary(k => k.Key, v => v.Value))
                {
                    if ((string)prdmn.Value == sel.Name)
                    {
                        dict.Remove(prdmn.Key);
                    }
                }

                Settings.Get<Dictionary<string, object>>("Proxies").Remove(sel.Name);
                Settings.Save();

                ReloadProxies();
            }
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainAddButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.Items.Count == 0)
            {
                MessageBox.Show("You need to add a new proxy before adding domains.", "No proxies", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            new ProxiedDomainWindow().ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiedDomainsListView.SelectedIndex == -1) return;

            if (proxiesListView.Items.Count == 0)
            {
                MessageBox.Show("You need to add a new proxy before adding domains.", "No proxies", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sel = (ProxiedDomainsListViewItem)proxiedDomainsListView.SelectedItem;

            new ProxiedDomainWindow(sel.Domain, sel.Proxy).ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiedDomainsListView.SelectedIndex == -1) return;

            var sel = (ProxiedDomainsListViewItem)proxiedDomainsListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Domain + "?", "Remove " + sel.Domain, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Settings.Get<Dictionary<string, object>>("Proxied Domains").Remove(sel.Domain);
                Settings.Save();

                ReloadProxies();
            }
        }
        #endregion

        #region Plugins
        /// <summary>
        /// Reloads the plugins list view.
        /// </summary>
        /// <param name="inclInternal">if set to <c>true</c> internal classes will be included too.</param>
        private void ReloadPlugins(bool inclInternal = false)
        {
            PluginsListViewItemCollection.Clear();

            var types = new[]
                {
                    typeof(Guide),
                    typeof(DownloadSearchEngine),
                    typeof(SubtitleSearchEngine),
                    typeof(LinkCheckerEngine),
                    typeof(OnlineVideoSearchEngine),
                    typeof(RecommendationEngine), 
                    typeof(SocialEngine),
                    typeof(WebSearchEngine),
                    typeof(OverviewContextMenu),
                    typeof(UpcomingListingContextMenu),
                    typeof(EpisodeListingContextMenu),
                    typeof(DownloadLinkContextMenu),
                    typeof(SubtitleContextMenu),
                    typeof(StatisticsContextMenu),
                    typeof(RecommendationContextMenu),
                    typeof(StartupPlugin),
                    typeof(IPlugin)
                };

            var icons = new[]
                {
                    "/RSTVShowTracker;component/Images/guides.png",
                    "/RSTVShowTracker;component/Images/torrents.png",
                    "/RSTVShowTracker;component/Images/subtitles.png",
                    "/RSTVShowTracker;component/Images/tick.png",
                    "/RSTVShowTracker;component/Images/monitor.png",
                    "/RSTVShowTracker;component/Images/information.png",
                    "/RSTVShowTracker;component/Images/bird.png",
                    "/RSTVShowTracker;component/Images/magnifier.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/menu.png",
                    "/RSTVShowTracker;component/Images/document-insert.png",
                    "/RSTVShowTracker;component/Images/dll.gif"
                };

            foreach (var engine in Extensibility.GetNewInstances<IPlugin>(inclInternal: inclInternal).OrderBy(engine => engine.Name))
            {
                var type   = engine.GetType();
                var parent = string.Empty;
                var picon  = string.Empty;
                var i      = 0;

                foreach (var ptype in types)
                {
                    if (type.IsSubclassOf(ptype))
                    {
                        parent = ptype.Name;
                        picon  = icons[i];
                        break;
                    }

                    i++;
                }

                var file = type.Assembly.ManifestModule.Name;

                if (file == "<In Memory Module>")
                {
                    var script = Extensibility.Scripts.FirstOrDefault(s => s.Type == type);

                    if (script != null)
                    {
                        file = Path.GetFileName(script.File);
                    }
                }

                PluginsListViewItemCollection.Add(new PluginsListViewItem
                    {
                        Icon    = engine.Icon,
                        Name    = engine.Name,
                        Type    = parent,
                        Icon2   = picon,
                        Version = engine.Version.ToString().PadRight(14, '0'),
                        File    = file
                    });
            }
        }

        /// <summary>
        /// Handles the Checked event of the showInternal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowInternalChecked(object sender, RoutedEventArgs e)
        {
            ReloadPlugins(true);
        }

        /// <summary>
        /// Handles the Unchecked event of the showInternal control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowInternalUnchecked(object sender, RoutedEventArgs e)
        {
            ReloadPlugins();
        }
        #endregion
    }
}
