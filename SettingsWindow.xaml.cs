namespace RoliSoft.TVShowTracker
{
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;

    using Microsoft.WindowsAPICodePack.Shell;

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : GlassWindow
    {
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

            dlPathTextBox.Text      = Settings.Get("Download Path");
            torrentPathTextBox.Text = Settings.Get("Torrent Downloader");
            processTextBox.Text     = Settings.Get("Processes to Monitor");
        }

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
            Settings.Set("Processes to Monitor", processTextBox.Text);
        }
    }
}
