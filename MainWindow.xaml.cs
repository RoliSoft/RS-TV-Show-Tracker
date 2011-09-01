namespace RoliSoft.TVShowTracker
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using Microsoft.Windows.Shell;
    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.Remote;

    using VistaControls.TaskDialog;

    using Drawing     = System.Drawing;
    using NotifyIcon  = System.Windows.Forms.NotifyIcon;
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using WinMenuItem = System.Windows.Forms.MenuItem;
    using Timer       = System.Timers.Timer;
    using Application = System.Windows.Application;
    using TaskDialog  = Microsoft.WindowsAPICodePack.Dialogs.TaskDialog;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
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
        private bool _hideOnStart;
        private bool _askUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture   = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            if (!Utils.Is7)
            {
                MessageBox.Show("This software doesn't support " + Utils.OS + ", only Windows 7 or newer.", Utils.OS + " is not supported", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
            
            var args = Environment.GetCommandLineArgs();

            if (args.Length != 1)
            {
                if (args.Contains("-hide"))
                {
                    _hideOnStart = true;
                }

                if (args.Length == 2 && args[1][0] != '-' && File.Exists(args[1]))
                {
                    var fid = FileNames.Parser.ParseFile(Path.GetFileName(args[1]), Path.GetDirectoryName(args[1]).Split(Path.DirectorySeparatorChar));

                    if (fid.Success)
                    {
                        new VistaControls.TaskDialog.TaskDialog
                            {
                                CommonIcon  = TaskDialogIcon.Information,
                                Title       = Signature.Software + " " + Signature.Version,
                                Instruction = Path.GetFileNameWithoutExtension(args[1]),
                                Content     = fid + " – " + ShowNames.Regexes.PartText.Replace(fid.Title, string.Empty) + " – " + fid.Quality
                            }.Show();
                    }
                    else
                    {
                        new VistaControls.TaskDialog.TaskDialog
                            {
                                CommonIcon  = TaskDialogIcon.Stop,
                                Title       = Signature.Software + " " + Signature.Version,
                                Instruction = Path.GetFileNameWithoutExtension(args[1]),
                                Content     = "Couldn't identify the specified file."
                            }.Show();
                    }
                    
                    Process.GetCurrentProcess().Kill();
                }
            }


            InitializeComponent();

            Dispatcher.UnhandledException              += (s, e) => { HandleUnexpectedException(e.Exception); e.Handled = true; };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnexpectedException(e.ExceptionObject as Exception, e.IsTerminating);
        }

        #region Window events
        /// <summary>
        /// Handles the SourceInitialized event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WindowSourceInitialized(object sender, EventArgs e)
        {
            if (SystemParameters2.Current.IsGlassEnabled)
            {
                ActivateAero();
            }
            else
            {
                ActivateNonAero();
            }

            SystemParameters2.Current.PropertyChanged += AeroChanged;
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

            if (_hideOnStart)
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

            new Task(() =>
                {
                    var uf = Directory.GetFiles(Signature.FullPath).Where(f => Path.GetFileName(f).StartsWith("update_") && f.EndsWith(".exe")).ToList();

                    if (uf.Count != 0)
                    {
                        var ver = Path.GetFileNameWithoutExtension(uf[0]).Replace("update_", string.Empty);

                        if (Version.Parse(ver) <= Version.Parse(Signature.Version))
                        {
                            try { File.Delete(Path.Combine(Signature.FullPath, uf[0])); } catch { }
                        }
                        else
                        {
                            Dispatcher.Invoke((Action)(() => UpdateDownloaded(ver, true)));
                            return;
                        }
                    }

                    Thread.Sleep(5000);
                    CheckForUpdate();
                }).Start();
        }

        /// <summary>
        /// This method is called when a system parameter from <c>SystemParameters2</c> is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        public void AeroChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsGlassEnabled")
            {
                Dispatcher.Invoke((Action)(() =>
                    {
                        if (SystemParameters2.Current.IsGlassEnabled)
                        {
                            ActivateAero();
                        }
                        else
                        {
                            ActivateNonAero();
                        }
                    }));
            }
        }

        /// <summary>
        /// Activates the aero interface.
        /// </summary>
        public void ActivateAero()
        {
            WindowChrome.SetWindowChrome(this, new WindowChrome { GlassFrameThickness = new Thickness(-1) });

            Background         = Brushes.Transparent;
            mainBorder.Padding = new Thickness(5);
            logoMenu.Margin    = new Thickness(6, -1, 0, 0);
            logoMenu.Width     = 157;
            logo.Visibility    = lastUpdatedLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Activates the non-aero interface.
        /// </summary>
        public void ActivateNonAero()
        {
            WindowChrome.SetWindowChrome(this, null);

            Background         = new SolidColorBrush(Color.FromArgb(Drawing.SystemColors.ControlDark.A, Drawing.SystemColors.ControlDark.R, Drawing.SystemColors.ControlDark.G, Drawing.SystemColors.ControlDark.B));
            mainBorder.Padding = logoMenu.Margin = new Thickness(0);
            logoMenu.Width     = SystemParameters.PrimaryScreenWidth;
            logo.Visibility    = lastUpdatedLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Handles the KeyUp event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            // on F5 refresh the current user control
            if (e.Key == Key.F5)
            {
                DataChanged();
            }

            // if the overview page is selected, send any keys to the listview
            if (tabControl.SelectedIndex == 0)
            {
                activeOverviewPage.ListViewKeyUp(sender, e);
            }
        }

        /// <summary>
        /// Handles the IsVisibleChanged event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void WindowIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && _askUpdate && updateOuter.Visibility == Visibility.Visible)
            {
                _askUpdate = false;
                UpdateDownloaded((string)update.Tag, true);
            }
        }

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            ShowMenuClick(null, e);
        }
        #endregion

        #region Miscellaneous
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
        /// Restarts the application. Microsoft forgot to implement <c>Application.Restart()</c> for WPF...
        /// </summary>
        public void Restart()
        {
            Application.Current.Exit += (sender, e) => Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
        #endregion

        #region Notify icon
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
                Hide();
                NotifyIcon.ContextMenu.MenuItems[0].Text = "Show";
            }
            else if (NotifyIcon.Visible && !(e is CancelEventArgs))
            {
                Show();
                Activate();
                NotifyIcon.ContextMenu.MenuItems[0].Text = "Hide";
            }
        }
        #endregion

        #region Logo
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

            var last = Database.Setting("last update");
            if (string.IsNullOrEmpty(last))
            {
                Dispatcher.Invoke((Action)(() => { lastUpdatedLabel.Content = string.Empty; }));
                return;
            }

            var ts = DateTime.Now - last.ToDouble().GetUnixTimestamp();

            Dispatcher.Invoke((Action)(() => { lastUpdatedLabel.Content = "last updated " + ts.ToShortRelativeTime() + " ago"; }));

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
        /// Handles the MouseEnter event of the logo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void LogoMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            logo.Background  = (Brush)FindResource("HeadGradientHover");
            logo.BorderBrush = Brushes.Gray;
        }

        /// <summary>
        /// Handles the MouseLeave event of the logo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void LogoMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            logo.Background  = (Brush)FindResource("HeadGradient" + (logoMenuItem.IsSubmenuOpen ? "Hover" : string.Empty));
            logo.BorderBrush = logoMenuItem.IsSubmenuOpen ? Brushes.Gray : Brushes.DimGray;
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
            logo.Background  = (Brush)FindResource("HeadGradient");
            logo.BorderBrush = Brushes.DimGray;
        }
        #endregion

        #region Main menu
        /// <summary>
        /// Handles the Click event of the UpdateDatabase control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void UpdateDatabaseClick(object sender = null, RoutedEventArgs e = null)
        {
            var updater = new Updater();

            updater.UpdateProgressChanged += UpdateProgressChanged;
            updater.UpdateDone            += UpdateDone;
            updater.UpdateError           += UpdateError;

            updater.UpdateAsync();
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
        /// Handles the Click event of the AddNewTVShow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddNewTVShowClick(object sender, RoutedEventArgs e)
        {
            new AddNewWindow().ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the RenamerSoftware control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void RenameVideoFilesClick(object sender, RoutedEventArgs e)
        {
            new RenamerWindow().Show();
        }

        /// <summary>
        /// Handles the Click event of the SendFeedback control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendFeedbackClick(object sender, RoutedEventArgs e)
        {
            new SendFeedbackWindow().ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the ExitSoftware control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ExitSoftwareClick(object sender, RoutedEventArgs e)
        {
            NotifyIcon.ContextMenu.MenuItems[1].PerformClick();
        }
        #endregion

        #region Database update
        /// <summary>
        /// Called when the update is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void UpdateDone(object sender, EventArgs e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

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
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void UpdateError(object sender, EventArgs<string, Exception, bool, bool> e)
        {
            if (e.Fourth) // fatal to whole update
            {
                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

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
            Utils.Win7Taskbar((int)e.Second, TaskbarProgressBarState.Normal);

            Dispatcher.Invoke((Action)(() =>
                {
                    lastUpdatedLabel.Content = "updating " + e.First + " (" + e.Second.ToString("0.00") + "%)";
                    SetHeaderProgress(e.Second);
                }));
        }
        #endregion

        #region Software update
        /// <summary>
        /// Checks for software update.
        /// </summary>
        public void CheckForUpdate()
        {
            var upd = API.CheckForUpdate();
            if (upd.Success && upd.New)
            {
                Dispatcher.Invoke((Action)(() =>
                    {
                        update.Tag              = upd.Version;
                        updateOuter.Visibility  = Visibility.Visible;
                        updateToolTipTitle.Text = "v" + upd.Version + " is available";
                        updateToolTipText.Text  = "Downloading update...";
                    }));

                if (File.Exists(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe")))
                {
                    return;
                }

                if (File.Exists(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp")))
                {
                    try { File.Delete(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp")); } catch { }
                }

                var wc = new WebClient();

                wc.DownloadProgressChanged += (s, e) => Dispatcher.Invoke((Action)(() => updateToolTipText.Text = "Downloading update... (" + e.ProgressPercentage + "%)"));
                wc.DownloadFileCompleted   += (s, e) =>
                    {
                        File.Move(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp"), Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe"));
                        Dispatcher.Invoke((Action)(() => UpdateDownloaded(upd.Version, true)));
                    };

                wc.DownloadFileAsync(new Uri(upd.URL), Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp"));
            }
        }

        /// <summary>
        /// Called when an update was downloaded.
        /// </summary>
        /// <param name="version">The new version.</param>
        /// <param name="ask">if set to <c>true</c> a TaskDialog will be displayed asking the user to update.</param>
        public void UpdateDownloaded(string version, bool ask)
        {
            update.Tag              = version;
            updateOuter.Visibility  = Visibility.Visible;
            updateToolTipTitle.Text = "v" + version + " is available";
            updateToolTipText.Text  = "Click here to install update!";

            if (ask && IsVisible && Top != -999)
            {
                var td = new VistaControls.TaskDialog.TaskDialog
                    {
                        CommonIcon      = TaskDialogIcon.SecurityWarning,
                        UseCommandLinks = true,
                        Title           = Signature.Software,
                        Instruction     = "Update available",
                        Content         = "Version " + version + " has been downloaded and is ready to be installed!",
                        CustomButtons   = new[]
                            {
                                new CustomButton(Result.Yes, "Install now\nIt won't take more than 10 seconds."),
                                new CustomButton(Result.No,  "Remind me later")
                            }
                    };

                if (td.Show().CommonButton == Result.Yes)
                {
                    UpdateMouseLeftButtonUp();
                }
            }
            else if (ask && (!IsVisible || Top == -999))
            {
                _askUpdate = true;
            }
        }

        /// <summary>
        /// Handles the MouseEnter event of the Update control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void UpdateMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            update.Background = (Brush)FindResource("UpdateGradientHover");
        }

        /// <summary>
        /// Handles the MouseLeave event of the Update control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void UpdateMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            update.Background = (Brush)FindResource("UpdateGradient");
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the Update control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void UpdateMouseLeftButtonUp(object sender = null, System.Windows.Input.MouseButtonEventArgs e = null)
        {
            if (File.Exists(Path.Combine(Signature.FullPath, "update_" + update.Tag + ".exe")))
            {
                Utils.Run(Path.Combine(Signature.FullPath, "update_" + update.Tag + ".exe"), "/S /AR /D=" + Signature.FullPath);
                NotifyIcon.ContextMenu.MenuItems[1].PerformClick();
            }
        }
        #endregion

        #region Exceptions
        /// <summary>
        /// Handles the unexpected exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="isTerminating">if set to <c>true</c> the exception will terminate the execution.</param>
        public void HandleUnexpectedException(Exception ex, bool isTerminating = false)
        {
            if (ex is ThreadAbortException)
            {
                return;
            }

            var show = Settings.Get<bool>("Show Unhandled Errors");
            var sb   = new StringBuilder();

        parseException:
            sb.AppendLine(ex.GetType() + ": " + ex.Message);
            sb.AppendLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                ex = ex.InnerException;
                goto parseException;
            }

            if (show)
            {
                var mc  = Regex.Matches(sb.ToString(), @"\\(?<file>[^\\]+\.cs)(?::lig?ne|, sor:) (?<ln>[0-9]+)");
                var loc = "at a location where it was not expected";

                if (mc.Count != 0)
                {
                    loc = "in file {0} at line {1}".FormatWith(mc[0].Groups["file"].Value, mc[0].Groups["ln"].Value);
                }

                var td = new TaskDialog
                    {
                        Icon                  = TaskDialogStandardIcon.Error,
                        Caption               = "An unexpected error occurred",
                        InstructionText       = "An unexpected error occurred",
                        Text                  = "An exception of type {0} was thrown {1}.".FormatWith(ex.GetType().ToString().Replace("System.", string.Empty), loc) + (isTerminating ? "\r\n\r\nUnfortunately this exception occured at a crucial part of the code and the execution of the software will be terminated." : string.Empty),
                        DetailsExpandedText   = sb.ToString(),
                        DetailsExpandedLabel  = "Hide stacktrace",
                        DetailsCollapsedLabel = "Show stacktrace",
                        Cancelable            = true,
                        StandardButtons       = TaskDialogStandardButtons.None
                    };

                if ((bool)Dispatcher.Invoke((Func<bool>)(() => IsVisible)))
                {
                    td.Opened  += (s, r) => Utils.Win7Taskbar(100, TaskbarProgressBarState.Error);
                    td.Closing += (s, r) => Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                }

                var fd = new TaskDialogCommandLink { Text = "Submit bug report" };
                fd.Click += (s, r) =>
                    {
                        td.Close();
                        ReportException(sb.ToString());
                    };

                var ig = new TaskDialogCommandLink { Text = "Ignore exception" };
                ig.Click += (s, r) => td.Close();

                td.Controls.Add(fd);
                td.Controls.Add(ig);
                td.Show();
            }
            else
            {
                ReportException(sb.ToString());
            }
        }

        /// <summary>
        /// Reports the parsed exception silently and asynchronously to lab.rolisoft.net.
        /// </summary>
        /// <param name="ex">The exception text parsed by <c>HandleUnexpectedException()</c>.</param>
        private static void ReportException(string ex)
        {
            new Task(() => { try { API.ReportError(ex); } catch { } }).Start();
        }
        #endregion
    }
}
