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
    }
}
