namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Forms;
    using System.Windows.Media.Imaging;

    using Label = System.Windows.Controls.Label;

    /// <summary>
    /// Interaction logic for AssociationsSettings.xaml
    /// </summary>
    public partial class AssociationsSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationsSettings"/> class.
        /// </summary>
        public AssociationsSettings()
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
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;
        }
        
        /// <summary>
        /// Handles the Checked event of the monitorNetworkShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorNetworkShareChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Monitor Network Shares", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the monitorNetworkShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorNetworkShareUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Monitor Network Shares", false);
        }

        /// <summary>
        /// Handles the TextChanged event of the processTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void ProcessTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_loaded) return;

            var proc = processTextBox.Text.Trim(',').Split(',').ToList();

            if (proc.Count == 1 && string.IsNullOrWhiteSpace(proc[0]))
            {
                proc.RemoveAt(0);
            }

            Settings.Set("Processes to Monitor", proc);
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
            if (!_loaded) return;

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
            if (!_loaded) return;

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

            if (ofd.ShowDialog() == DialogResult.OK)
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
            if (!_loaded) return;

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
    }
}
