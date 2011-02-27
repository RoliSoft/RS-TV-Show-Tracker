namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Timers;
    using System.Windows;
    using System.Collections.ObjectModel;
    using System.Windows.Media.Animation;

    using RoliSoft.TVShowTracker.FileNames;

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.ShowNames;

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

            RenameFormatTextBoxTextChanged(null, null);

            ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
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
            if (files.Count == 0) goto end;

            Dispatcher.Invoke((Action)(() => { statusThrobber.Visibility = Visibility.Visible; }));

            foreach (var file in files)
            {
                file.Parsed = true;

                try   { file.Information = Parser.ParseFile(file.Information.Name, false); }
                catch { file.Enabled = false; }
                
            }

            Dispatcher.Invoke((Action)(() => { statusThrobber.Visibility = Visibility.Hidden; }));

          end:
            _parsing = false;
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
        private void AddFiles(IEnumerable<string> files)
        {
            _parsing = true;

            foreach (var file in files)
            {
                if (!Regex.IsMatch(file, @"\.(avi|mkv|mp4|wmv|srt|sub|ass|smi)$", RegexOptions.IgnoreCase)) continue;

                FilesListViewItemCollection.Add(new FileListViewItem
                    {
                        Enabled     = true,
                        Location    = file,
                        Information = new ShowFile(file)
                    });
            }

            _parsing = false;
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
            var fbd = new System.Windows.Forms.FolderBrowserDialog { ShowNewFolderButton = false };

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
        #endregion

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
    }
}
