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
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.FileNames;
    using RoliSoft.TVShowTracker.ShowNames;

    using DataFormats    = System.Windows.DataFormats;
    using DragEventArgs  = System.Windows.DragEventArgs;
    using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
    using Orientation    = System.Windows.Controls.Orientation;
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
        /// Gets or sets the worker thread.
        /// </summary>
        /// <value>The worker thread.</value>
        public Thread WorkerThread { get; set; }

        /// <summary>
        /// Gets or sets the rename format.
        /// </summary>
        /// <value>The rename format.</value>
        public static string Format { get; set; }

        /// <summary>
        /// Gets or sets the rename file operation.
        /// </summary>
        /// <value>The rename file operation.</value>
        public static string Operation { get; set; }

        private static string _operationVerb
        {
            get
            {
                switch (Operation)
                {
                    default:
                    case "rename":
                        return "Renaming";

                    case "copy":
                        return "Copying";

                    case "move":
                        return "Moving";

                    case "symlink":
                        return "Symlinking";
                }
            }
        }


        private static string _operationPast
        {
            get
            {
                switch (Operation)
                {
                    default:
                    case "rename":
                        return "Renamed";

                    case "copy":
                        return "Copied";

                    case "move":
                        return "Moved";

                    case "symlink":
                        return "Symlinked";
                }
            }
        }

        /// <summary>
        /// Gets or sets the target directory of the file operation.
        /// </summary>
        /// <value>The target directory of the file operation.</value>
        public static string TargetDir { get; set; }

        private volatile bool _parsing, _renaming;

        /// <summary>
        /// Contains a sample <c>ShowFile</c> object.
        /// </summary>
        public static readonly ShowFile SampleInfo = new ShowFile("House.M.D.S06E02.Epic.Fail.720p.WEB-DL.h.264.DD5.1-LP.mkv", "House, M.D.", new ShowEpisode(6, 2), "Epic Fail", "Web-DL 720p", "LP", new DateTime(2009, 9, 28));

        /// <summary>
        /// Contains the parsed name for the sample file.
        /// </summary>
        public static readonly Regex SampleTitleRegex = ShowNames.Parser.GenerateTitleRegex(SampleInfo.Show);

        /// <summary>
        /// Contains a regular expression which matches the episode parts for the sample file.
        /// </summary>
        public static readonly Regex SampleEpisodeRegex = ShowNames.Parser.GenerateEpisodeRegexes(new ShowEpisode(SampleInfo.Episode.Season, SampleInfo.Episode.Episode));

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

            Format = renameFormatTextBox.Text = Settings.Get("Rename Format", @"$show\Season $seasonz\$show S$seasonE$episode - $title$ext");
            RenameFormatTextBoxTextChanged(null, null);

            switch (Operation = Settings.Get("Rename File Operation", "rename"))
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

            TargetDir = targetDirTextBox.Text = Settings.Get("Rename Target Directory");
        }

        /// <summary>
        /// Handles the Closing event of the GlassWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void GlassWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _parsing = true;
            ParserTimer.Stop();

            if (WorkerThread != null && WorkerThread.IsAlive)
            {
                try { WorkerThread.Abort(); } catch { }
            }
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

            var files = FilesListViewItemCollection.Where(f => !f.Processed).ToList();
            if (files.Count == 0)
            {
                goto end;
            }

            foreach (var file in files)
            {
                SetStatus("Identifying " + file.Information.Name + "...", true);

                try
                {
                    file.Information = FileNames.Parser.ParseFile(file.Information.Name, Path.GetDirectoryName(file.Location).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
                    file.Checked     = file.Recognized = file.Enabled = file.Information.Success;
                    file.Processed   = true;

                    if (file.Information.Success)
                    {
                        file.ShowStatusImage = "Collapsed";
                        file.ShowCheckBox    = "Visible";
                    }
                    else
                    {
                        file.StatusImage = "/RSTVShowTracker;component/Images/exclamation-red.png";
                    }
                }
                catch
                {
                    file.Information.ParseError = ShowFile.FailureReasons.ExceptionOccurred;
                    file.Checked     = file.Recognized = file.Enabled = false;
                    file.Processed   = true;
                    file.StatusImage = "/RSTVShowTracker;component/Images/exclamation-red.png";
                }

                file.RefreshEnabled();
            }

            SetStatus();

          end:
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
                    var id = FilesListViewItemCollection.Count(f => f.Enabled && f.Checked);
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

            if (WorkerThread != null && WorkerThread.IsAlive)
            {
                return;
            }

            WorkerThread = new Thread(() => AddFilesInternal(files, first));
            WorkerThread.Start();
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
                else if (Regex.IsMatch(file, @"\.(avi|mkv|mp4|ts|wmv|srt|sub|ass|smi)$", RegexOptions.IgnoreCase))
                {
                    var tmp = file;
                    Dispatcher.Invoke((Action)(() => FilesListViewItemCollection.Add(new FileListViewItem
                        {
                            Location        = tmp,
                            ShowCheckBox    = "Collapsed",
                            ShowStatusImage = "Visible",
                            StatusImage     = "/RSTVShowTracker;component/Images/hourglass.png",
                            Information     = new ShowFile(tmp)
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
            if (listView.ContextMenu.IsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop))
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
                if (item.Enabled)
                {
                    item.Checked = true;
                }
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
                item.Checked = false;
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

            Settings.Set("Rename Format", Format = renameFormatTextBox.Text);
            resultingNameTextBox.Text = FileNames.Parser.FormatFileName(Format, SampleInfo);

            if (ShowNames.Parser.IsMatch(Path.GetFileName(resultingNameTextBox.Text), SampleTitleRegex, SampleEpisodeRegex))
            {
                resultingDetected.Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/tick.png", UriKind.Relative));
                resultingDetected.ToolTip = "The software recognizes this format.\r\nThis means you will be able to find the episode using the 'Play episode' context menu\r\nand the software can automatically mark the episode as watched when you're playing it.";
            }
            else
            {
                resultingDetected.Source  = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cross.png", UriKind.Relative));
                resultingDetected.ToolTip = "The software doesn't recognize this format.\r\nThis means you won't be able to find the episode using the 'Play episode' context menu\r\nand the software can't automatically mark the episode as watched when you're playing it.";
            }
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
            Settings.Set("Rename File Operation", Operation = "rename");
        }

        /// <summary>
        /// Handles the Checked event of the copyRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CopyRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", Operation = "copy");
        }

        /// <summary>
        /// Handles the Checked event of the moveRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MoveRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", Operation = "move");
        }

        /// <summary>
        /// Handles the Checked event of the symLinkRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SymLinkRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Rename File Operation", Operation = "symlink");
        }

        /// <summary>
        /// Handles the TextChanged event of the targetDirTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TargetDirTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Settings.Set("Rename Target Directory", TargetDir = targetDirTextBox.Text);
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

        #region Renaming
        /// <summary>
        /// Handles the Click event of the startRenamingButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StartRenamingButtonClick(object sender, RoutedEventArgs e)
        {
            if (_renaming)
            {
                if (WorkerThread != null && WorkerThread.IsAlive)
                {
                    try { WorkerThread.Abort(); } catch { }
                }

                startRenamingButton.Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(3,1,3,1)
                    };
                (startRenamingButton.Content as StackPanel).Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/pencil.png", UriKind.Relative)),
                        Width  = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 5, 0),
                    });
                (startRenamingButton.Content as StackPanel).Children.Add(new TextBlock
                    {
                        Text   = "Start renaming",
                        Margin = new Thickness(0, 0, 3, 0),
                    });

                startRenamingButton.IsEnabled = FilesListViewItemCollection.Count(f => f.Enabled && f.Checked) != 0;
                settingsTabItem.IsEnabled = listView.ContextMenu.IsEnabled = true;
                _parsing = _renaming = false;

                SetStatus("Canceled " + _operationVerb.ToLower() + " operation.");
            }
            else
            {
                startRenamingButton.Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin      = new Thickness(3,1,3,1)
                    };
                (startRenamingButton.Content as StackPanel).Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/cross.png", UriKind.Relative)),
                        Width  = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 5, 0),
                    });
                (startRenamingButton.Content as StackPanel).Children.Add(new TextBlock
                    {
                        Text   = "Stop renaming",
                        Margin = new Thickness(0, 0, 3, 0),
                    });

                settingsTabItem.IsEnabled = listView.ContextMenu.IsEnabled = false;
                _parsing = _renaming = true;

                if (WorkerThread != null && WorkerThread.IsAlive)
                {
                    try { WorkerThread.Abort(); } catch { }
                }

                WorkerThread = new Thread(RenameRecognizedFiles);
                WorkerThread.Start();
            }
        }

        /// <summary>
        /// Renames the recognized files.
        /// </summary>
        private void RenameRecognizedFiles()
        {
            var i = 0;
            foreach (var file in FilesListViewItemCollection.Where(f => f.Enabled && f.Checked).ToList())
            {
                var name = Utils.SanitizeFileName(FileNames.Parser.FormatFileName(Format, file.Information));
                SetStatus(_operationVerb + " " + name + "...", true);

                try { ProcessFile(name, file.Location, Path.Combine(TargetDir, name)); } catch { }

                file.Enabled = file.Checked = false;
                i++;
            }

            Dispatcher.Invoke((Action)(() =>
                {
                    startRenamingButton.Content   = "Start renaming";
                    startRenamingButton.IsEnabled = FilesListViewItemCollection.Count(f => f.Enabled && f.Checked) != 0;
                    settingsTabItem.IsEnabled = listView.ContextMenu.IsEnabled = true;
                }));
            SetStatus(_operationPast + " " + Utils.FormatNumber(i, "file") + "!");
            _parsing = _renaming = false;
        }

        /// <summary>
        /// Processes the specified file.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="source">The source file.</param>
        /// <param name="target">The target file.</param>
        private void ProcessFile(string name, string source, string target)
        {
            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }

            switch (Operation)
            {
                default:
                case "rename":
                    File.Move(source, Path.Combine(Path.GetDirectoryName(source), name));
                    break;

                case "copy":
                    int pbCancel = 0, lastCopyPerc = 0;
                    Utils.Interop.CopyFileEx(source, target, (totalSize, transferred, streamSize, streamTransferred, streamNumber, callbackReason, sourceFile, destinationFile, lpData) =>
                        {
                            var perc = (int)Math.Round((double)transferred / totalSize * 100);
                            if (perc != lastCopyPerc)
                            {
                                lastCopyPerc = perc;
                                SetStatus(_operationVerb + " " + name + "... (" + perc + "%)", true);
                            }

                            return _renaming ? Utils.Interop.CopyProgressResult.PROGRESS_CONTINUE : Utils.Interop.CopyProgressResult.PROGRESS_CANCEL;
                        }, IntPtr.Zero, ref pbCancel, 0);
                    break;

                case "move":
                    var lastMovePerc = 0;
                    Utils.Interop.MoveFileWithProgress(source, target, (totalSize, transferred, streamSize, streamTransferred, streamNumber, callbackReason, sourceFile, destinationFile, lpData) =>
                        {
                            var perc = (int)Math.Round((double)transferred / totalSize * 100);
                            if (perc != lastMovePerc)
                            {
                                lastMovePerc = perc;
                                SetStatus(_operationVerb + " " + name + "... (" + perc + "%)", true);
                            }

                            return _renaming ? Utils.Interop.CopyProgressResult.PROGRESS_CONTINUE : Utils.Interop.CopyProgressResult.PROGRESS_CANCEL;
                        }, IntPtr.Zero, Utils.Interop.MoveFileFlags.MOVE_FILE_COPY_ALLOWED);
                    break;

                case "symlink":
                    Utils.Interop.CreateSymbolicLink(target, source, Utils.Interop.SymbolicLinkFlags.SYMBLOC_LINK_FLAG_FILE);
                    break;
            }
        }
        #endregion
    }
}
