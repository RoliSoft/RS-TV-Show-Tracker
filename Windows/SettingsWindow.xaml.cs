namespace RoliSoft.TVShowTracker
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Windows;

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
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
        }

        /// <summary>
        /// Handles the Closing event of the GlassWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void GlassWindowClosing(object sender, CancelEventArgs e)
        {
            if (activeGeneralPage._reindex || activeListingPage._reindex)
            {
                Dispatcher.Invoke(() => MainWindow.Active.ReindexDownloadPathsClick());
            }

            new Thread(() => Dispatcher.Invoke(() =>
                {
                    MainWindow.Active.activeDownloadLinksPage.LoadEngines(true);
                    MainWindow.Active.activeSubtitlesPage.LoadEngines(true);
                })).Start();
        }
    }
}
