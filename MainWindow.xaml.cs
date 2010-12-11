namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using Drawing  = System.Drawing;
    using WinForms = System.Windows.Forms;

    using Microsoft.Windows.Shell;

    using RoliSoft.TVShowTracker.Helpers;

    using Application = System.Windows.Application;

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
        public static WinForms.NotifyIcon NotifyIcon { get; set; }

        private Thread _statusThread;

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
            if (GlassHelper.IsCompositionEnabled)
            {
                WindowChrome.SetWindowChrome(this, new WindowChrome());
            }

            GlassHelper.ExtendGlassFrameComplete(this);

            if (!GlassHelper.IsCompositionEnabled)
            {
                Background         = new SolidColorBrush(Color.FromArgb(Drawing.SystemColors.ControlDark.A, Drawing.SystemColors.ControlDark.R, Drawing.SystemColors.ControlDark.G, Drawing.SystemColors.ControlDark.B));
                mainBorder.Padding = logoMenu.Margin = new Thickness(0);
                logoMenu.Width     = SystemParameters.PrimaryScreenWidth;
                logo.Visibility    = lastUpdatedLabel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// This method is called from GlassHelper.cs when a DWMCOMPOSITIONCHANGED message is received.
        /// </summary>
        public void AeroChanged()
        {
            Dispatcher.Invoke((Func<bool>)delegate
                {
                    if (GlassHelper.IsCompositionEnabled)
                    {
                        WindowChrome.SetWindowChrome(this, new WindowChrome());
                        GlassHelper.ExtendGlassFrameComplete(this);

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
                    return true;
                });
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
        }

        /// <summary>
        /// Sets the status to the last updated time.
        /// </summary>
        public void SetLastUpdated()
        {
            var last = 0d;
            double.TryParse(Database.Setting("last update"), out last);
            var ts = DateTime.Now - Utils.DateTimeFromUnix(last);

            Dispatcher.Invoke((Func<bool>)delegate
                {
                    lastUpdatedLabel.Content = "last updated " + ts.ToRelativeTime() + " ago";
                    return true;
                });

            /*if (_statusThread != null && _statusThread.IsAlive)
            {
                _statusThread.Abort();
            }*/

            if (ts.TotalMinutes < 1) // if under a minute, update by seconds
            {
                _statusThread = new Thread(() =>
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        SetLastUpdated();
                    });
            }
            else if (ts.TotalHours < 1) // if under an hour, update by minutes
            {
                _statusThread = new Thread(() =>
                    {
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        SetLastUpdated();
                    });
            }
            else // if more than an hour, update by hours
            {
                _statusThread = new Thread(() =>
                    {
                        Thread.Sleep(TimeSpan.FromHours(1));
                        SetLastUpdated();
                    });
            }

            _statusThread.Start();
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
            var menu = new WinForms.ContextMenu();

            NotifyIcon = new WinForms.NotifyIcon
                {
                    Text        = "RS TV Show Tracker",
                    Icon        = new Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/RSTVShowTracker;component/tv.ico")).Stream),
                    Visible     = true,
                    ContextMenu = menu
                };

            var showMenu    = new WinForms.MenuItem { Text = "Hide" };
            showMenu.Click += ShowMenuClick;
            
            var exitMenu    = new WinForms.MenuItem { Text = "Exit" };
            exitMenu.Click += (s, r) =>
                {
                    NotifyIcon.Visible = false;
                    Application.Current.Shutdown();
                    //Process.GetCurrentProcess().Kill(); // this would be more *aggressive* I guess
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
                Dispatcher.Invoke((Func<bool>)delegate
                    {
                        var tc = tabControl.SelectedContent;

                        if (tc != null && tc is UserControl)
                        {
                            var rf = tc.GetType().GetMethod("Refresh");

                            if (rf != null)
                            {
                                rf.Invoke(tc, new object[] { });
                            }
                        }

                        return true;
                    });
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
        /// Handles the Closed event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WindowClosed(object sender, EventArgs e)
        {
            BackgroundTasks.TaskThread.Abort();

            if (_statusThread != null)
            {
                _statusThread.Abort();
            }
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
        private void UpdateDatabaseClick(object sender, RoutedEventArgs e)
        {
            activeSettingsPage.UpdateDatabaseButtonClick(null, null);
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
            new AboutWindow().Show();
        }
        #endregion
    }
}
