namespace RoliSoft.TVShowTracker
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.IO;

    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow
    {
        private bool _loaded;
        private volatile int _count, _size;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWindow"/> class.
        /// </summary>
        public LogWindow()
        {
            InitializeComponent();

            _count = _size = 0;
            foreach (var entry in Log.Messages.ToArray().Reverse().OrderBy(x => x.Time))
            {
                logListView.Items.Add(new LogListViewItem(entry));
                _count++;
                _size += entry.ToString().Length;
            }

            if (logListView.Items.Count != 0) logListView.ScrollIntoView(logListView.Items[logListView.Items.Count - 1]);

            msgCount.Text = _count.ToString();
            var fs = Utils.GetFileSize(_size).Split(' ');
            msgSize.Text = fs[0];
            msgUnit.Text = fs[1];

            Log.NewMessage += AddMessage;
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

            var log = Settings.Get("Logging Level", Log.Level.Info);

            switch (log)
            {
                case Log.Level.Trace: level.SelectedIndex = 0; break;
                case Log.Level.Debug: level.SelectedIndex = 1; break;
                case Log.Level.Info:  level.SelectedIndex = 2; break;
                case Log.Level.Warn:  level.SelectedIndex = 3; break;
                case Log.Level.Error: level.SelectedIndex = 4; break;
                case Log.Level.Fatal: level.SelectedIndex = 5; break;
                case Log.Level.None:  level.SelectedIndex = 6; break;
            }

            _loaded = true;
        }

        /// <summary>
        /// Adds the message to the listview.
        /// </summary>
        /// <param name="item">The item.</param>
        private void AddMessage(object item)
        {
            lock (logListView)
            {
                Dispatcher.Invoke((Action)(() =>
                    {
                        logListView.Items.Add(new LogListViewItem(item as Log.Entry));
                        logListView.ScrollIntoView(logListView.Items[logListView.Items.Count - 1]);

                        _count++;
                        _size += (item as Log.Entry).ToString().Length;

                        msgCount.Text = _count.ToString();
                        var fs = Utils.GetFileSize(_size).Split(' ');
                        msgSize.Text = fs[0];
                        msgUnit.Text = fs[1];
                    }));
            }
        }

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Log.NewMessage -= AddMessage;
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the Level control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void LevelOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;

            var log = Log.Level.None;

            switch (level.SelectedIndex)
            {
                case 0: log = Log.Level.Trace; break;
                case 1: log = Log.Level.Debug; break;
                case 2: log = Log.Level.Info;  break;
                case 3: log = Log.Level.Warn;  break;
                case 4: log = Log.Level.Error; break;
                case 5: log = Log.Level.Fatal; break;
                case 6: log = Log.Level.None;  break;
            }

            Log.SetLevel(log);
            Log.Trace("Setting logging level to " + log + "...");
            Settings.Set("Logging Level", log);
        }

        /// <summary>
        /// Handles the OnClick event of the ClearButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void ClearButtonOnClick(object sender, RoutedEventArgs e)
        {
            level.IsEnabled = saveButton.IsEnabled = clearButton.IsEnabled = false;

            lock (Log.Messages)
                lock (logListView)
            {
                Log.Entry ent;

                while (!Log.Messages.IsEmpty)
                {
                    Log.Messages.TryTake(out ent);
                }

                logListView.Items.Clear();
            }

            _count = _size = 0;

            msgCount.Text = _count.ToString();
            var fs = Utils.GetFileSize(_size).Split(' ');
            msgSize.Text = fs[0];
            msgUnit.Text = fs[1];

            level.IsEnabled = saveButton.IsEnabled = clearButton.IsEnabled = true;
        }

        /// <summary>
        /// Handles the OnClick event of the SaveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void SaveButtonOnClick(object sender, RoutedEventArgs e)
        {
            level.IsEnabled = saveButton.IsEnabled = clearButton.IsEnabled = false;

            var sfd = new SaveFileDialog
                {
                    Title           = "Specify file to save the log to",
                    CheckPathExists = true,
                    Filter          = "Log files|*.log|All files|*.*",
                    FileName        = DateTime.Now.ToString("s").Replace('T', '-').Replace(':', '-') + ".log"
                };

            if (sfd.ShowDialog(this).HasValue)
            {
                try
                {
                    using (var fs = sfd.OpenFile())
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (var entry in Log.Messages.ToArray().Reverse().OrderBy(x => x.Time))
                        {
                            if (Log.LoggingLevel >= entry.Level)
                            {
                                sw.WriteLine(entry);
                            }
                        }

                        sw.Flush();
                    }

                    Log.Info("Exported log to " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error("Error while exporting the log to the specified file.", ex);
                }
            }

            level.IsEnabled = saveButton.IsEnabled = clearButton.IsEnabled = true;
        }

        /// <summary>
        /// Handles the OnClick event of the RunBTButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void RunBTButtonOnClick(object sender, RoutedEventArgs e)
        {
            BackgroundTasks.TaskTimer.Stop();
            BackgroundTasks.TaskTimer.Start();
            BackgroundTasks.Tasks();
        }
    }
}
