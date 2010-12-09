namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        public SettingsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Click event of the UpdateDatabaseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void UpdateDatabaseButtonClick(object sender, RoutedEventArgs e)
        {
            var update = new Updater();

            update.UpdateProgressChanged += UpdateProgressChanged;
            update.UpdateDone            += UpdateDone;
            update.UpdateError           += UpdateError;

            update.UpdateAsync();
        }

        /// <summary>
        /// Called when the update is done.
        /// </summary>
        public void UpdateDone()
        {
            Dispatcher.Invoke((Func<bool>)delegate
                {
                    MainWindow.Active.SetLastUpdated();
                    MainWindow.Active.SetHeaderProgress(-1);
                    return true;
                });
        }

        /// <summary>
        /// Called when the update has encountered an error.
        /// </summary>
        public void UpdateError(string message, Exception exception, bool fatalToShow, bool fatalToWholeUpdate)
        {
            if (fatalToWholeUpdate)
            {
                Dispatcher.Invoke((Func<bool>)delegate
                    {
                        MainWindow.Active.lastUpdatedLabel.Content = "update failed";
                        MainWindow.Active.SetHeaderProgress(-1);
                        return true;
                    });
            }
        }

        /// <summary>
        /// Called when the progress has changed on the update.
        /// </summary>
        public void UpdateProgressChanged(string show, double percentage)
        {
            Dispatcher.Invoke((Func<bool>)delegate
                {
                    MainWindow.Active.lastUpdatedLabel.Content = "updating " + show + " (" + percentage.ToString("0.00") + "%)";
                    MainWindow.Active.SetHeaderProgress(percentage);
                    return true;
                });
        }
    }
}
