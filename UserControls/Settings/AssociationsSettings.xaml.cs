namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
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

                monitorProcesses.IsChecked = Settings.Get("Monitor Processes", true);
                processTextBox.Text = string.Join(",", Settings.Get<List<string>>("Processes to Monitor"));

                switch (Settings.Get("Process Monitoring Method", "Internal"))
                {
                    default:
                    case "Internal":
                        methodComboBox.SelectedIndex = 0;
                        break;

                    case "WindowTitle":
                        methodComboBox.SelectedIndex = 1;
                        break;

                    case "Sysinternals":
                        methodComboBox.SelectedIndex = 2;
                        break;

                    case "NirSoft":
                        methodComboBox.SelectedIndex = 3;
                        break;
                }

                monitorNetworkShare.IsChecked = Settings.Get<bool>("Monitor Network Shares");
                upnpShare.IsChecked           = Settings.Get<bool>("Enable UPnP AV Media Server");

                if (!Signature.IsActivated)
                {
                    upnpShare.IsEnabled = monitorNetworkShare.IsEnabled = false;
                }

                memLimit.Value = Settings.Get("Memory Usage Limit", 512);

                if (Utils.IsAdmin)
                {
                    uacIcon.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/uac-tick.png"));
                    uacIcon.ToolTip += Environment.NewLine + "The software is currently running with administrator rights.";
                }
                else
                {
                    uacIcon.ToolTip += Environment.NewLine + "The software is currently running without administrator rights.";
                }

                if (Signature.IsActivated)
                {
                    cupIcon1.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cup-tick.png"));
                    cupIcon2.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cup-tick.png"));
                    cupIcon1.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                    cupIcon2.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                }
                else
                {
                    cupIcon1.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                    cupIcon2.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                }
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
        /// Handles the Unchecked event of the monitorProcesses control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorNetworkShareUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Monitor Network Shares", false);
        }

        /// <summary>
        /// Handles the Checked event of the monitorProcesses control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorProcessesChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Monitor Processes", true);

            monitorNetworkShare.IsEnabled = true;
            monitorNetworkShare.IsChecked = monitorNetworkShare.Tag is bool && (bool)monitorNetworkShare.Tag;
        }

        /// <summary>
        /// Handles the Unchecked event of the monitorNetworkShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MonitorProcessesUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Monitor Processes", false);

            monitorNetworkShare.IsEnabled = false;
            monitorNetworkShare.Tag = monitorNetworkShare.IsChecked;
            monitorNetworkShare.IsChecked = false;
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the MethodComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void MethodComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (methodComboBox.SelectedIndex)
            {
                case 0:
                    titleInfo.Visibility = sysinternalsInfo.Visibility = nirsoftInfo.Visibility = Visibility.Collapsed;
                    internalInfo.Visibility = Visibility.Visible;
                    break;

                case 1:
                    internalInfo.Visibility = sysinternalsInfo.Visibility = nirsoftInfo.Visibility = Visibility.Collapsed;
                    titleInfo.Visibility = Visibility.Visible;
                    break;

                case 2:
                    internalInfo.Visibility = titleInfo.Visibility = nirsoftInfo.Visibility = Visibility.Collapsed;
                    sysinternalsInfo.Visibility = Visibility.Visible;
                    break;

                case 3:
                    sysinternalsInfo.Visibility = internalInfo.Visibility = titleInfo.Visibility = Visibility.Collapsed;
                    nirsoftInfo.Visibility = Visibility.Visible;
                    break;
            }

            if (!_loaded) return;

            switch (methodComboBox.SelectedIndex)
            {
                case 0:
                    Settings.Set("Process Monitoring Method", "Internal");
                    break;

                case 1:
                    Settings.Set("Process Monitoring Method", "WindowTitle");
                    break;

                case 2:
                    Settings.Set("Process Monitoring Method", "Sysinternals");
                    break;

                case 3:
                    Settings.Set("Process Monitoring Method", "NirSoft");
                    break;
            }
        }

        /// <summary>
        /// Handles the Checked event of the upnpShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UpnpShareChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable UPnP AV Media Server", true);

            if (Signature.IsActivated)
            {
                UPnP.Start();
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the upnpShare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UpnpShareUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable UPnP AV Media Server", false);

            UPnP.Stop();
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
        /// Handles the OnLostFocus event of the memLimit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void MemLimitOnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Memory Usage Limit", memLimit.Value.GetValueOrDefault(512));
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
