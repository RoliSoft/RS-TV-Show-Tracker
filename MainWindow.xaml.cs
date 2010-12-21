namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using Drawing     = System.Drawing;
    using NotifyIcon  = System.Windows.Forms.NotifyIcon;
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using WinMenuItem = System.Windows.Forms.MenuItem;
    using Timer       = System.Timers.Timer;
    using Application = System.Windows.Application;

    using Microsoft.Windows.Shell;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Gets or sets the active main window.
        /// </summary>
        /// <value>The active main window.</value>
        public static MainWindow Active { get; set; }

        /// <summary>
        /// Gets or sets the notify icon.
        /// </summary>
        /// <value>The notify icon.</value>
        public static NotifyIcon NotifyIcon { get; set; }

        private Timer _statusTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the SourceInitialized event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WindowSourceInitialized(object sender, EventArgs e)
        {
            if (SystemParameters2.Current.IsGlassEnabled)
            {
                WindowChrome.SetWindowChrome(this, new WindowChrome { GlassFrameThickness = new Thickness(-1) });
                Background = Brushes.Transparent;
            }
            else
            {
                Background         = new SolidColorBrush(Color.FromArgb(Drawing.SystemColors.ControlDark.A, Drawing.SystemColors.ControlDark.R, Drawing.SystemColors.ControlDark.G, Drawing.SystemColors.ControlDark.B));
                mainBorder.Padding = logoMenu.Margin = new Thickness(0);
                logoMenu.Width     = SystemParameters.PrimaryScreenWidth;
                logo.Visibility    = lastUpdatedLabel.Visibility = Visibility.Collapsed;
            }

            SystemParameters2.Current.PropertyChanged += AeroChanged;
        }

        /// <summary>
        /// This method is called when a system parameter from <c>SystemParameters2</c> is changed.
        /// </summary>
        public void AeroChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsGlassEnabled")
            {
                Dispatcher.Invoke((Action)(() =>
                    {
                        if (SystemParameters2.Current.IsGlassEnabled)
                        {
                            WindowChrome.SetWindowChrome(this, new WindowChrome { GlassFrameThickness = new Thickness(-1) });
                            Background = Brushes.Transparent;

                            mainBorder.Padding = new Thickness(5);
                            logoMenu.Margin    = new Thickness(6, -1, 0, 0);
                            logoMenu.Width     = 157;
                            logo.Visibility    = lastUpdatedLabel.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            // I couldn't figure out how to remove the WindowChrome from the window,
                            // so we'll just restart the application for that. (TODO)
                            Restart();
                        }
                    }));
            }
        }

        /// <summary>
        /// Restarts the application. Microsoft forgot to implement <c>Application.Restart()</c> for WPF...
        /// </summary>
        public void Restart()
        {
            Application.Current.Exit += (sender, e) => Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Active = this;
            BackgroundTasks.Start();
            SetLastUpdated();
            LoadNotifyIcon();

            if (Environment.GetCommandLineArgs().Contains("-hide"))
            {
                // hiding the window when it isn't even visible is a tricky thing :]
                // we wait for the next event, until then we make it inactive and -999px off the screen

                var top = Top;
                Top = -999;
                ShowInTaskbar = ShowActivated = false;

                ContentRendered += (s, r) =>
                    {
                        ShowMenuClick(s, r);
                        ShowInTaskbar = ShowActivated = true;

                        var t = new Timer { Interval = 1000, AutoReset = false, Enabled = true };
                        t.Elapsed += (d, y) => Dispatcher.Invoke((Action)(() => { Top = top; }));
                    };
            }
        }

        /// <summary>
        /// Sets the status to the last updated time.
        /// </summary>
        public void SetLastUpdated(object sender = null, System.Timers.ElapsedEventArgs e = null)
        {
            if (_statusTimer == null)
            {
                _statusTimer = new Timer();
                _statusTimer.Elapsed += SetLastUpdated;
            }
            else
            {
                _statusTimer.Stop();
            }

            var last = 0d;
            double.TryParse(Database.Setting("last update"), out last);
            var ts = DateTime.Now - Utils.DateTimeFromUnix(last);

            Dispatcher.Invoke((Action)(() =>
                {
                    lastUpdatedLabel.Content = "last updated " + ts.ToRelativeTime() + " ago";
                }));

            if (ts.TotalMinutes < 1) // if under a minute, update by seconds
            {
                _statusTimer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            }
            else if (ts.TotalHours < 1) // if under an hour, update by minutes
            {
                _statusTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            }
            else // if more than an hour, update by hours
            {
                _statusTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
            }

            _statusTimer.Start();
        }

        /// <summary>
        /// Sets the width of the progress bar in the header.
        /// </summary>
        /// <param name="value">The value. If -1, the progress bar will be hidden.</param>
        public void SetHeaderProgress(double value)
        {
            if (value == -1)
            {
                progressRectangle.BeginAnimation(OpacityProperty, new DoubleAnimation
                    {
                        To                = 0,
                        Duration          = TimeSpan.FromMilliseconds(500),
                        AccelerationRatio = 1
                    });

                return;
            }

            if (progressRectangle.Opacity == 0)
            {
                progressRectangle.Width   = 0;
                progressRectangle.Opacity = 1;
            }

            progressRectangle.BeginAnimation(WidthProperty, new DoubleAnimation
                {
                    To                = (value / 100) * logo.RenderSize.Width,
                    Duration          = TimeSpan.FromMilliseconds(500),
                    AccelerationRatio = 1,
                    EasingFunction    = new ElasticEase
                        {
                            Springiness = 10
                        }
                });
        }

        /// <summary>
        /// Loads the notify icon.
        /// </summary>
        private void LoadNotifyIcon()
        {
            var menu = new ContextMenu();

            NotifyIcon = new NotifyIcon
                {
                    Text        = "RS TV Show Tracker",
                    Icon        = new Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/RSTVShowTracker;component/tv.ico")).Stream),
                    Visible     = true,
                    ContextMenu = menu
                };

            var showMenu    = new WinMenuItem { Text = "Hide" };
            showMenu.Click += ShowMenuClick;
            
            var exitMenu    = new WinMenuItem { Text = "Exit" };
            exitMenu.Click += (s, r) =>
                {
                    NotifyIcon.Visible = false;
                    //Application.Current.Shutdown();
                    Process.GetCurrentProcess().Kill(); // this would be more *aggressive* I guess
                };

            menu.MenuItems.Add(showMenu);
            menu.MenuItems.Add(exitMenu);

            NotifyIcon.DoubleClick += (s, e) => showMenu.PerformClick();
        }

        /// <summary>
        /// Handles the Click event of the showMenu control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void ShowMenuClick(object sender, EventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                NotifyIcon.ContextMenu.MenuItems[0].Text = "Show";
                Hide();
            }
            else if (NotifyIcon.Visible)
            {
                NotifyIcon.ContextMenu.MenuItems[0].Text = "Hide";
                Show();
                Activate();
            }
        }

        /// <summary>
        /// Called when data is changed in the database.
        /// </summary>
        /// <param name="invokeRefresh">if set to <c>true</c> it will try to invoke the <c>Refresh()</c> method of the active user control.</param>
        public void DataChanged(bool invokeRefresh = true)
        {
            Database.DataChange = DateTime.Now;

            if (invokeRefresh)
            {
                Dispatcher.Invoke((Action)(() =>
                    {
                        if (tabControl.SelectedContent is IRefreshable)
                        {
                            (tabControl.SelectedContent as IRefreshable).Refresh();
                        }
                    }));
            }
        }

        /// <summary>
        /// Handles the KeyUp event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void WindowKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // if the overview page is selected, send any keys to the listview
            if (tabControl.SelectedIndex == 0)
            {
                activeOverviewPage.ListViewKeyUp(sender, e);
            }
        }

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ShowMenuClick(null, null);
        }

        /// <summary>
        /// Handles the MouseEnter event of the logo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void LogoMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            logo.Background = (Brush)FindResource("HeadGradientHover");
        }

        /// <summary>
        /// Handles the MouseLeave event of the logo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void LogoMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            logo.Background = (Brush)FindResource("HeadGradient" + (logoMenuItem.IsSubmenuOpen ? "Hover" : string.Empty));
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the logo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void LogoMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            logoMenuItem.IsSubmenuOpen = true;
        }

        /// <summary>
        /// Handles the SubmenuClosed event of the logoMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LogoMenuItemSubmenuClosed(object sender, RoutedEventArgs e)
        {
            logo.Background = (Brush)FindResource("HeadGradient");
        }

        #region Main menu
        /// <summary>
        /// Handles the Click event of the UpdateDatabase control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void UpdateDatabaseClick(object sender = null, RoutedEventArgs e = null)
        {
            var update = new Updater();

            update.UpdateProgressChanged += UpdateProgressChanged;
            update.UpdateDone            += UpdateDone;
            update.UpdateError           += UpdateError;

            update.UpdateAsync();
        }

        /// <summary>
        /// Handles the Click event of the MinimizeToTray control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void MinimizeToTrayClick(object sender, RoutedEventArgs e)
        {
            ShowMenuClick(null, null);
        }

        /// <summary>
        /// Handles the Click event of the OpenHelpPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OpenHelpPageClick(object sender, RoutedEventArgs e)
        {
            Utils.Run("http://lab.rolisoft.net/tvshowtracker/help.html");
        }

        /// <summary>
        /// Handles the Click event of the AboutSoftware control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AboutSoftwareClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the ConfigureSoftware control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ConfigureSoftwareClick(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the ActivateBetaFeatures control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ActivateBetaFeaturesClick(object sender, RoutedEventArgs e)
        {
            new ActivateBetaWindow().ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the AddNewTVShow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddNewTVShowClick(object sender, RoutedEventArgs e)
        {
            new AddNewWindow().ShowDialog();
        }
        #endregion

        #region Update
        /// <summary>
        /// Called when the update is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void UpdateDone(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    SetLastUpdated();
                    SetHeaderProgress(-1);
                }));
        }

        /// <summary>
        /// Called when the update has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.String,System.Exception,System.Boolean,System.Boolean&gt;"/> instance containing the event data.</param>
        public void UpdateError(object sender, EventArgs<string, Exception, bool, bool> e)
        {
            if (e.Fourth) // fatal to whole update
            {
                Dispatcher.Invoke((Action)(() =>
                    {
                        lastUpdatedLabel.Content = "update failed";
                        SetHeaderProgress(-1);
                    }));
            }
        }

        /// <summary>
        /// Called when the progress has changed on the update.
        /// </summary>
        public void UpdateProgressChanged(object sender, EventArgs<string, double> e)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    lastUpdatedLabel.Content = "updating " + e.First + " (" + e.Second.ToString("0.00") + "%)";
                    SetHeaderProgress(e.Second);
                }));
        }
        #endregion
    }
}
