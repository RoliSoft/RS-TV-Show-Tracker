namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.Parsers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

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
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private List<DownloadSearchEngine> _engines;
        private List<string> _trackers, _includes;

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

            foreach (var path in Settings.Get<IEnumerable<string>>("Download Paths"))
            {
                dlPathsListBox.Items.Add(path);
            }

            using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                startAtStartup.IsChecked = rk.GetValue("RS TV Show Tracker") != null;
            }

            convertTimezone.IsChecked = Settings.Get("Convert Timezone", true);

            var tzinfo = "Your current timezone is " + TimeZoneInfo.Local.DisplayName + ".\r\n"
                       + "Your difference from Central Standard Time is {0} hours.".FormatWith(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").BaseUtcOffset.TotalHours * -1)).TotalHours);

            currentTimezone.ContentEnd.InsertTextInRun(tzinfo);

            showUnhandledErrors.IsChecked = Settings.Get<bool>("Show Unhandled Errors");
            
            // associations

            torrentPathTextBox.Text = Settings.Get("Torrent Downloader");
            usenetPathTextBox.Text  = Settings.Get("Usenet Downloader");
            jDlPathTextBox.Text     = Settings.Get("JDownloader");

            var atr = Utils.GetApplicationForExtension(".torrent");
            if (!string.IsNullOrWhiteSpace(atr))
            {
                var tri = Utils.GetExecutableInfo(atr);

                torrentAssociationName.Text   = tri.Item1;
                torrentAssociationIcon.Source = tri.Item2;
            }
            else
            {
                torrentAssociationName.Text = "[no software associated with .torrent files]";
            }

            var anz = Utils.GetApplicationForExtension(".nzb");
            if (!string.IsNullOrWhiteSpace(anz))
            {
                var nzi = Utils.GetExecutableInfo(anz);

                usenetAssociationName.Text   = nzi.Item1;
                usenetAssociationIcon.Source = nzi.Item2;
            }
            else
            {
                usenetAssociationName.Text = "[no software associated with .nzb files]";
            }

            var htz = Utils.GetApplicationForExtension(".htm");
            if (!string.IsNullOrWhiteSpace(htz))
            {
                var hti = Utils.GetExecutableInfo(htz);

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

            processTextBox.Text = string.Join(",", Settings.Get<IEnumerable<string>>("Processes to Monitor"));

            // parsers

            DownloadsListViewItemCollection = new ObservableCollection<DownloadsListViewItem>();
            listView.ItemsSource            = DownloadsListViewItemCollection;

            _engines = typeof(DownloadSearchEngine)
                       .GetDerivedTypes()
                       .Select(type => Activator.CreateInstance(type) as DownloadSearchEngine)
                       .ToList();

            _trackers = Settings.Get<IEnumerable<string>>("Tracker Order").ToList();
            _trackers.AddRange(_engines
                               .Where(engine => _trackers.IndexOf(engine.Name) == -1)
                               .Select(engine => engine.Name));

            _includes = Settings.Get<IEnumerable<string>>("Active Trackers").ToList();

            ReloadParsers();

            // proxies

            ProxiesListViewItemCollection = new ObservableCollection<ProxiesListViewItem>();
            proxiesListView.ItemsSource   = ProxiesListViewItemCollection;
            
            ProxiedDomainsListViewItemCollection = new ObservableCollection<ProxiedDomainsListViewItem>();
            proxiedDomainsListView.ItemsSource   = ProxiedDomainsListViewItemCollection;

            ReloadProxies();
        }

        #region General
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
            Settings.Set("Download Paths", dlPathsListBox.Items.Cast<string>());
        }

        /// <summary>
        /// Handles the TextChanged event of the processTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void ProcessTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var proc = processTextBox.Text.Trim(',').Split(',');

            if (proc.Length == 1 && string.IsNullOrWhiteSpace(proc[0]))
            {
                proc = new string[0];
            }

            Settings.Set("Processes to Monitor", proc);
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

        #region Downloaders
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
        /// Handles the Click event of the JDownloaderHyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void JDownloaderHyperlinkClick(object sender, RoutedEventArgs e)
        {
            Utils.Run((sender as Hyperlink).NavigateUri.ToString());
        }
        #endregion

        #region Parsers
        /// <summary>
        /// Reloads the parsers list view.
        /// </summary>
        private void ReloadParsers()
        {
            foreach (var engine in _engines.OrderBy(engine => _trackers.IndexOf(engine.Name)))
            {
                var revdiff = engine.GetAttribute<ParserAttribute>().Revision - new DateTime(2000, 1, 1, 1, 0, 0);

                DownloadsListViewItemCollection.Add(new DownloadsListViewItem
                    {
                        Enabled = _includes.Contains(engine.Name),
                        Icon    = engine.Icon,
                        Site    = engine.Name,
                        Login   = engine.Private
                                  ? Settings.Get(engine.Name + " Cookies") != null
                                    ? "/RSTVShowTracker;component/Images/tick.png"
                                    : "/RSTVShowTracker;component/Images/cross.png"
                                  : "/RSTVShowTracker;component/Images/na.png",
                        Version = "2.0." + Math.Floor(revdiff.TotalDays).ToString("0000") + "." + ((revdiff.Subtract(TimeSpan.FromDays(Math.Floor(revdiff.TotalDays)))).TotalSeconds / 2).ToString("00000")
                    });
            }

            listView.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles the Checked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledChecked(object sender, RoutedEventArgs e)
        {
            if (!_includes.Contains((sender as CheckBox).Tag as string))
            {
                _includes.Add((sender as CheckBox).Tag as string);

                SaveInclusions();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the Enabled control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnabledUnchecked(object sender, RoutedEventArgs e)
        {
            if (_includes.Contains((sender as CheckBox).Tag as string))
            {
                _includes.Remove((sender as CheckBox).Tag as string);

                SaveInclusions();
            }
        }

        /// <summary>
        /// Saves the active trackers to the XML settings file.
        /// </summary>
        public void SaveInclusions()
        {
            Settings.Set("Active Trackers", _includes);
        }

        /// <summary>
        /// Handles the Click event of the parserEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ParserEditButtonClick(object sender, RoutedEventArgs e)
        {
            var sel = listView.SelectedItem as DownloadsListViewItem;
            var prs = _engines.Single(en => en.Name == sel.Site);

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

        #region Proxies
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
    }
}
