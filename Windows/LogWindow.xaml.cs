namespace RoliSoft.TVShowTracker
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogWindow"/> class.
        /// </summary>
        public LogWindow()
        {
            InitializeComponent();

            foreach (var entry in Log.Messages.ToArray().Reverse().OrderBy(x => x.Time))
            {
                logListView.Items.Add(new LogListViewItem(entry));
            }

            logListView.ScrollIntoView(logListView.Items[logListView.Items.Count - 1]);

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
    }
}
