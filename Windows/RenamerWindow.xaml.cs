namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Timers;
    using System.Windows;
    using System.Collections.ObjectModel;
    using System.Windows.Forms;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.FileNames;
    using RoliSoft.TVShowTracker.ShowNames;

    using DataFormats    = System.Windows.DataFormats;
    using DragEventArgs  = System.Windows.DragEventArgs;
    using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
    using Timer          = System.Timers.Timer;

    /// <summary>
    /// Interaction logic for RenamerWindow.xaml
    /// </summary>
    public partial class RenamerWindow
    {
        /// <summary>
        /// Gets or sets the files list view item collection.
        /// </summary>
        /// <value>The files list view item collection.</value>
        public ObservableCollection<FileListViewItem> FilesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the parser timer.
        /// </summary>
        /// <value>The parser timer.</value>
        public Timer ParserTimer { get; set; }

        /// <summary>
        /// Gets or sets the rename format.
        /// </summary>
        /// <value>The rename format.</value>
        public static string Format { get; set; }

        private volatile bool _parsing;

        /// <summary>
        /// Contains a sample <c>ShowFile</c> object.
        /// </summary>
        public static readonly ShowFile SampleInfo = new ShowFile("House.M.D.S06E02.Epic.Fail.720p.WEB-DL.h.264.DD5.1-LP.mkv", "House, M.D.", new ShowEpisode(6, 2), "Epic Fail", "Web-DL 720p");

        /// <summary>
        /// Contains the parsed name for the sample file.
        /// </summary>
        public static readonly string[] SampleTitleParts = ShowNames.Parser.GetRoot(SampleInfo.Show);

        /// <summary>
        /// Contains the matching episode parts for the sample file.
        /// </summary>
        public static readonly string[] SampleEpisodeParts = new[]
            {
                "S06E02",
                "S06E02".Replace("E", ".E"),
                Regex.Replace("S06E02", "S0?([0-9]{1,2})E([0-9]{1,2})", "$1X$2", RegexOptions.IgnoreCase),
                Regex.Replace("S06E02", "S0?([0-9]{1,2})E([0-9]{1,2})", "$1$2", RegexOptions.IgnoreCase)
            };

        /// <summary>
        /// Contains a regular expression which matches for video file extensions.
        /// </summary>
        public static readonly Regex SampleKnownVideoRegex  = new Regex(@"\.(avi|mkv|mp4|wmv)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Contains a regular expression which matches for sample files.
        /// </summary>
        public static readonly Regex SampleSampleVideoRegex = new Regex(@"(^|[\.\-\s])sample[\.\-\s]", RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="RenamerWindow"/> class.
        /// </summary>
        public RenamerWindow()
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

            FilesListViewItemCollection = new ObservableCollection<FileListViewItem>();
            listView.ItemsSource        = FilesListViewItemCollection;

            ParserTimer = new Timer(250);
            ParserTimer.Elapsed += ParserTimerElapsed;
            ParserTimer.Start();

            renameFormatTextBox.Text = Settings.Get("Rename Format", "$show S$seasonE$episode - $title - $quality$ext");
            RenameFormatTextBoxTextChanged(null, null);

            switch (Settings.Get("Rename File Operation", "rename"))
            {
                case "rename":
                    renameRadioButton.IsChecked = true;
                    break;

                case "copy":
                    copyRadioButton.IsChecked = true;
                    break;

                case "move":
                    moveRadioButton.IsChecked = true;
                    break;

                case "symlink":
                    symLinkRadioButton.IsChecked = true;
                    break;
            }

            targetDirTextBox.Text = Settings.Get("Rename Target Directory");
        }

        /// <summary>
        /// Handles the Elapsed event of the ParserTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        private void ParserTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_parsing)
            {
                return;
            }

            _parsing = true;

            var files = FilesListViewItemCollection.Where(f => !f.Parsed).ToList();
            if (files.Count == 0)
            {
                goto end;
            }

            foreach (var file in files)
            {
                SetStatus("Identifying " + file.Information.Name + "...", true);

                try   { file.Information = FileNames.Parser.ParseFile(file.Information.Name); }
                catch { file.Enabled = false; }

                file.Parsed = true;
            }

          end:
            SetStatus();
            _parsing = false;
        }

        /// <summary>
        /// Sets the status message to the specified message or to the number of files if the message is null.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="activity">if set to <c>true</c> an animating spinner will be displayed.</param>
        public void SetStatus(string message = null, bool activity = false)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    var id = FilesListViewItemCollection.Count(f => f.Information.Show != null);
                    statusLabel.Content = message ?? Utils.FormatNumber(FilesListViewItemCollection.Count, "file") + " added; " + Utils.FormatNumber(id, "file") + " identified.";
                    startRenamingButton.IsEnabled = id != 0;

                    if (activity && statusThrobber.Visibility != Visibility.Visible)
                    {
                        statusImage.Visibility    = Visibility.Hidden;
                        statusThrobber.Visibility = Visibility.Visible;
                        ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
                    }
                    else if (!activity && statusThrobber.Visibility != Visibility.Hidden)
                    {
                        ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Stop();
                        statusThrobber.Visibility = Visibility.Hidden;
                        statusImage.Visibility    = Visibility.Visible;
                    }
                }));
        }

        /// <summary>
        /// Handles the SelectionChanged event of the tabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void TabControlSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
            {
                foreach (FileListViewItem item in listView.Items)
                {
                    item.RefreshTarget();
                }
            }
        }

        #region Adding files
        /// <summary>
        /// Adds the specified list of files to the list view.
        /// </summary>
        /// <param name="files">The list of files.</param>
        /// <param name="first">if set to <c>true</c> the <c>_parsing</c> variable will be modified accordingly.</param>
        private void AddFiles(IEnumerable<string> files, bool first = true)
        {
            if (first)
            {
                _parsing = true;
                SetStatus("Adding files...", true);
            }

            new Thread(() => AddFilesInternal(files, first)).Start();
        }

        /// <summary>
        /// Adds the specified list of files to the list view.
        /// </summary>
        /// <param name="files">The list of files.</param>
        /// <param name="first">if set to <c>true</c> the <c>_parsing</c> variable will be modified accordingly.</param>
        private void AddFilesInternal(IEnumerable<string> files, bool first = true)
        {
            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    AddFilesInternal(Directory.EnumerateFiles(file, "*.*", SearchOption.AllDirectories), false);
                }
                else if (Regex.IsMatch(file, @"\.(avi|mkv|mp4|wmv|srt|sub|ass|smi)$", RegexOptions.IgnoreCase))
                {
                    Dispatcher.Invoke((Action)(() => FilesListViewItemCollection.Add(new FileListViewItem
                        {
                            Enabled     = true,
                            Location    = file,
                            Information = new ShowFile(file)
                        })));
                }
            }

            if (first)
            {
                SetStatus();
                _parsing = false;
            }
        }

        /// <summary>
        /// Handles the Drop event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DragEventArgs"/> instance containing the event data.</param>
        private void ListViewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                AddFiles(e.Data.GetData(DataFormats.FileDrop, true) as string[]);
            }
        }

        /// <summary>
        /// Handles the Click event of the addFilesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddFilesButtonClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    Multiselect     = true,
                    CheckFileExists = true
                };

            if (ofd.ShowDialog().Value)
            {
                AddFiles(ofd.FileNames);
            }
        }

        /// <summary>
        /// Handles the Click event of the addFoldersButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddFoldersButtonClick(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog
                {
                    Description         = "Select the directory where the files you want to rename are located:",
                    ShowNewFolderButton = false
                };

            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            AddFiles(Directory.EnumerateFiles(fbd.SelectedPath, "*.*", SearchOption.AllDirectories));
        }
        #endregion

        #region List view buttons
        /// <summary>
        /// Handles the Click event of the removeSelectedButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void RemoveSelectedButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in listView.SelectedItems.Cast<FileListViewItem>().ToList())
            {
                FilesListViewItemCollection.Remove(item);
            }

            SetStatus();
        }

        /// <summary>
        /// Handles the Click event of the markSelectedButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MarkSelectedButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (FileListViewItem item in listView.SelectedItems)
            {
                item.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the unmarkSelectedButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UnmarkSelectedButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (FileListViewItem item in listView.SelectedItems)
            {
                item.Enabled = false;
            }
        }

        /// <summary>
        /// Handles the Click event of the selectAllButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectAllButtonClick(object sender, RoutedEventArgs e)
        {
            SelectNoneButtonClick(sender, e);

            foreach (var item in listView.Items)
            {
                listView.SelectedItems.Add(item);
            }
        }

        /// <summary>
        /// Handles the Click event of the selectNoneButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectNoneButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in listView.SelectedItems.Cast<FileListViewItem>().ToList())
            {
                listView.SelectedItems.Remove(item);
            }
        }
        #endregion

        #region Format tab
        /// <summary>
        /// Handles the TextChanged event of the renameFormatTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void RenameFormatTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (resultingNameTextBox == null) return;

            Format = renameFormatTextBox.Text;
            resultingNameTextBox.Text = GenerateName(SampleInfo);

            if (TestName(resultingNameTextBox.Text))
            {
                resultingDetected.Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/tick.png", UriKind.Relative));
                resultingDetected.ToolTip = "The software recognizes this format.\r\nThis means you will be able to find the episode using the 'Play episode' context menu\r\nand the software can automatically mark the episode as watched when you're playing it.";
            }
            else
            {
                resultingDetected.Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cross.png", UriKind.Relative));
                resultingDetected.ToolTip = "The software doesn't recognize this format.\r\nThis means you won't be able to find the episode using the 'Play episode' context menu\r\nand the software can't automatically mark the episode as watched when you're playing it.";
            }

            Settings.Set("Rename Format", Format);
        }

        /// <summary>
        /// Generates a new name.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>New file name.</returns>
        public static string GenerateName(ShowFile file)
        {
            return Format.Replace("$show",     file.Show)
                         .Replace("$seasonz",  file.Season.ToString("0"))
                         .Replace("$season",   file.Season.ToString("00"))
                         .Replace("$episodez", file.Episode.ToString("0"))
                         .Replace("$episode",  file.Episode.ToString("00"))
                         .Replace("$title",    file.Title)
                         .Replace("$quality",  file.Quality)
                         .Replace("$ext",      file.Extension);
        }

        /// <summary>
        /// Tests the new format to see if the <c>FileSearch</c> class will find it.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the software can find the new name.</returns>
        public static bool TestName(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                && SampleTitleParts.All(part => Regex.IsMatch(name, @"\b" + part + @"\b", RegexOptions.IgnoreCase)) // does it have all the title words?
                && SampleKnownVideoRegex.IsMatch(name) // is it a known video file extension?
                && !SampleSampleVideoRegex.IsMatch(name) // is it not a sample?
                && SampleEpisodeParts.Any(ep => Regex.IsMatch(name, @"\b" + ep + @"\b", RegexOptions.IgnoreCase)); // is it the episode we want?
        }
        #endregion

        #region Settings tab
        /// <summary>
        /// Handles the Checked event of the renameRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void RenameRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", "rename");
        }

        /// <summary>
        /// Handles the Checked event of the copyRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CopyRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", "copy");
        }

        /// <summary>
        /// Handles the Checked event of the moveRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", "move");
        }

        /// <summary>
        /// Handles the Checked event of the symLinkRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SymLinkRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", "symlink");
        }

        /// <summary>
        /// Handles the TextChanged event of the targetDirTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TargetDirTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Settings.Set("Rename Target Directory", targetDirTextBox.Text);
        }

        /// <summary>
        /// Handles the Click event of the targetDirBrowseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TargetDirBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog
                {
                    Description         = "Select the directory where you want to copy/move/symlink the files:",
                    ShowNewFolderButton = true
                };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                targetDirTextBox.Text = fbd.SelectedPath + Path.DirectorySeparatorChar;
            }
        }
        #endregion
    }
}
