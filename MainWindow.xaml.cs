namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shell;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.Remote;

    using Drawing     = System.Drawing;
    using NotifyIcon  = System.Windows.Forms.NotifyIcon;
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using WinMenuItem = System.Windows.Forms.MenuItem;
    using Timer       = System.Timers.Timer;
    using Application = System.Windows.Application;

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

        public static readonly int WM_SHOWFIRSTINSTANCE = Utils.Interop.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", Signature.Software);

        private Timer _statusTimer;
        private static bool _initialized;
        private bool _hideOnStart, _dieOnStart, _askUpdate, _askErrorUpdate;
        private Mutex _mutex;
        private static ConcurrentDictionary<string, int> _exCnt = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            // set global language and unhandled exception handler

            Thread.CurrentThread.CurrentCulture   = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            Dispatcher.UnhandledException              += (s, e) => { HandleUnexpectedException(e.Exception); e.Handled = true; };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnexpectedException(e.ExceptionObject as Exception, e.IsTerminating);

            // set up mutex so only one instance will run

            var uniq = false;
            _mutex = new Mutex(true, "Local\\" + Signature.Software, out uniq);

            if (!uniq)
            {
                Utils.Interop.PostMessage((IntPtr)Utils.Interop.HWND_BROADCAST, WM_SHOWFIRSTINSTANCE, IntPtr.Zero, IntPtr.Zero);
                Process.GetCurrentProcess().Kill();
                return;
            }

            // check if the database is user writable

            if (!Utils.IsUserWritable(Signature.FullPath))
            {
                if (!Utils.IsAdmin)
                {
                    // if not admin, elevate

                    _mutex.ReleaseMutex();
                    Utils.RunElevated(Assembly.GetExecutingAssembly().Location);
                    Process.GetCurrentProcess().Kill();
                    return;
                }
                else
                {
                    // if admin, add permissions

                    if (!Utils.MakeUserWritable(Signature.FullPath) || !Utils.IsUserWritable(Signature.FullPath))
                    {
                        MessageBox.Show("Failed to add permissions to the database. You will most likely experience issues later during the execution of the software.", "Permission error", MessageBoxButton.OK, MessageBoxImage.Stop);
                    }
                }
            }

            // if old database exists somewhere, update

            if (File.Exists(Path.Combine(Signature.FullPath, "TVShows.db3")) || File.Exists(Path.Combine(Signature.UACVirtualizedPath, "TVShows.db3")))
            {
                new TaskDialogs.DatabaseUpdateTaskDialog().Ask();
                _dieOnStart = true;
            }

            // live a long and fulfilling life! unless told not to.

            if (_dieOnStart)
            {
                Visibility = Visibility.Hidden;
                return;
            }
            
            // handle command line arguments, if there are any

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
                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Information,
                                Title           = Signature.Software + " " + Signature.Version,
                                MainInstruction = Path.GetFileNameWithoutExtension(args[1]),
                                Content         = fid + " – " + ShowNames.Regexes.PartText.Replace(fid.Title, string.Empty) + " – " + fid.Quality,
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                    else
                    {
                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Error,
                                Title           = Signature.Software + " " + Signature.Version,
                                MainInstruction = Path.GetFileNameWithoutExtension(args[1]),
                                Content         = "Couldn't identify the specified file.",
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                    
                    Process.GetCurrentProcess().Kill();
                }
            }

            // init interface

            InitializeComponent();
        }

        /// <summary>
        /// Runs the specified action in the UI thread.
        /// </summary>
        /// <param name="func">The action.</param>
        public void Run(Action func)
        {
            Dispatcher.Invoke(func);
        }

        #region Window events
        /// <summary>
        /// Handles the SourceInitialized event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WindowSourceInitialized(object sender, EventArgs e)
        {
            HwndSource.FromHwnd((new WindowInteropHelper(this)).Handle).AddHook(new HwndSourceHook(WndProc));

            if (_dieOnStart)
            {
                Visibility = Visibility.Hidden;
                return;
            }

            if (Settings.Get("Enable Aero", true) && SystemParameters.IsGlassEnabled)
            {
                ActivateAero();
            }
            else
            {
                ActivateNonAero();
            }

            SystemParameters.StaticPropertyChanged += AeroChanged;

            if (Settings.Get("Enable Animations", true))
            {
                ActivateAnimation();
            }
            else
            {
                DeactivateAnimation();
            }
        }

        /// <summary>
        /// Processes Windows messages.
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWFIRSTINSTANCE)
            {
                ShowMenuClick();
                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (_dieOnStart)
            {
                Visibility = Visibility.Hidden;
                return;
            }

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
                        t.Elapsed += (d, y) => Run(() => { Top = top; });
                    };
            }

            new Task(() =>
                {
                    var uf = Directory.GetFiles(Signature.FullPath).Where(f => Path.GetFileName(f).StartsWith("update_") && f.EndsWith(".exe")).ToList();

                    if (uf.Count != 0)
                    {
                        var ver = Path.GetFileNameWithoutExtension(uf[0]).Replace("update_", string.Empty);

                        if (Version.Parse(ver) <= Version.Parse(Signature.Version) || new FileInfo(Path.Combine(Signature.FullPath, uf[0])).Length == 0)
                        {
                            try { File.Delete(Path.Combine(Signature.FullPath, uf[0])); } catch { }
                        }
                        else
                        {
                            Run(() => UpdateDownloaded(ver, true));
                            return;
                        }
                    }

                    Thread.Sleep(5000);
                    CheckForUpdate();
                }).Start();

            new Task(() =>
                {
                    Thread.Sleep(5000);

                    Run(() =>
                        {
                            ReindexDownloadPaths.IsEnabled = false;

                            if (_statusTimer != null)
                            {
                                _statusTimer.Stop();
                            }

                            lastUpdatedLabel.Content = "indexing download paths";
                        });

                    Library.Initialize();

                    Run(() =>
                            {
                                ReindexDownloadPaths.IsEnabled = true;

                                SetLastUpdated();
                            });
                }).Start();

            _initialized = true;

            foreach (var plugin in Extensibility.GetNewInstances<StartupPlugin>())
            {
                plugin.Run();
            }
        }

        /// <summary>
        /// This method is called when a system parameter from <c>SystemParameters2</c> is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        public void AeroChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsGlassEnabled" && Settings.Get("Enable Aero", true))
            {
                Run(() =>
                    {
                        if (SystemParameters.IsGlassEnabled)
                        {
                            ActivateAero();
                        }
                        else
                        {
                            ActivateNonAero();
                        }
                    });
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
        /// Activates the animation of tab controls.
        /// </summary>
        public void ActivateAnimation()
        {
            tabControl.ContentTemplate = activeGuidesPage.tabControl.ContentTemplate = (DataTemplate)FindResource("TabTemplate");
        }

        /// <summary>
        /// Deactivates the animation of tab controls.
        /// </summary>
        public void DeactivateAnimation()
        {
            tabControl.ContentTemplate = activeGuidesPage.tabControl.ContentTemplate = null;
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
            else if (IsVisible && _askErrorUpdate && updateOuter.Visibility == Visibility.Visible)
            {
                _askErrorUpdate = false;
                UpdateIOError((string)update.Tag);
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
                Run(() =>
                    {
                        if (tabControl.SelectedContent is IRefreshable)
                        {
                            (tabControl.SelectedContent as IRefreshable).Refresh();
                        }
                    });
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
        public void ShowMenuClick(object sender = null, EventArgs e = null)
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

            var last = Database.Setting("update");

            if (string.IsNullOrEmpty(last))
            {
                Run(() => { lastUpdatedLabel.Content = string.Empty; });
                return;
            }

            var ts = DateTime.Now - last.ToDouble().GetUnixTimestamp();

            Run(() => { lastUpdatedLabel.Content = "last updated " + ts.ToShortRelativeTime() + " ago"; });

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
        /// <param name="value">The value.</param>
        public void SetHeaderProgress(double value)
        {
            progressRectangle.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    To                = value / 100,
                    Duration          = TimeSpan.FromMilliseconds(500),
                    AccelerationRatio = 1
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
        /// Handles the Click event of the ReindexDownloadPaths control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void ReindexDownloadPathsClick(object sender = null, RoutedEventArgs e = null)
        {
            if (!ReindexDownloadPaths.IsEnabled)
            {
                return;
            }

            if (_statusTimer != null)
            {
                _statusTimer.Stop();
            }

            lastUpdatedLabel.Content = "indexing download paths";

            ReindexDownloadPaths.IsEnabled = false;

            new Task(() =>
                {
                    Library.Initialize();

                    Run(() =>
                            {
                                ReindexDownloadPaths.IsEnabled = true;

                                SetLastUpdated();
                            });
                }).Start();
        }

        /// <summary>
        /// Handles the Click event of the UpdateDatabase control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void UpdateDatabaseClick(object sender = null, RoutedEventArgs e = null)
        {
            _statusTimer.Stop();

            UpdateDatabase.IsEnabled = false;

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
        /// Handles the Click event of the SupportSoftware control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SupportSoftwareClick(object sender, RoutedEventArgs e)
        {
            Utils.Run("http://lab.rolisoft.net/tvshowtracker/donate.html");
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
        /// Handles the Click event of the SocialNetworks control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SocialNetworksClick(object sender, RoutedEventArgs e)
        {
            new SocialWindow().Show();
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

            Run(() =>
                {
                    UpdateDatabase.IsEnabled = true;
                    SetLastUpdated();
                    SetHeaderProgress(0);
                });
        }

        /// <summary>
        /// Called when the update has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void UpdateError(object sender, EventArgs<string, Exception, bool, bool> e)
        {
            if (e.Second != null && !(e.Second is WebException))
            {
                HandleUnexpectedException(e.Second);
            }

            if (e.Fourth) // fatal to whole update
            {
                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                Run(() =>
                    {
                        UpdateDatabase.IsEnabled = true;
                        lastUpdatedLabel.Content = "update failed";
                        SetHeaderProgress(0);
                    });
            }
        }

        /// <summary>
        /// Called when the progress has changed on the update.
        /// </summary>
        public void UpdateProgressChanged(object sender, EventArgs<string, double> e)
        {
            Utils.Win7Taskbar((int)e.Second, TaskbarProgressBarState.Normal);

            Run(() =>
                {
                    lastUpdatedLabel.Content = "updating " + e.First + " (" + e.Second.ToString("0.00") + "%)";
                    SetHeaderProgress(e.Second);
                });
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
                Run(() =>
                    {
                        update.Tag              = upd.Version;
                        updateOuter.Visibility  = Visibility.Visible;
                        updateToolTipTitle.Text = "v" + upd.Version + " is available";
                        updateToolTipText.Text  = "Downloading update...";
                    });

                if (File.Exists(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe")) && new FileInfo(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe")).Length != 0)
                {
                    return;
                }

                if (File.Exists(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe")))
                {
                    try
                    {
                        File.Delete(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe"));
                    }
                    catch (Exception ex)
                    {
                        Run(() => UpdateIOError(ex.Message));
                        return;
                    }
                }

                if (File.Exists(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp")))
                {
                    try
                    {
                        File.Delete(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp"));
                    }
                    catch (Exception ex)
                    {
                        Run(() => UpdateIOError(ex.Message));
                        return;
                    }
                }

                var wc = new WebClient();

                wc.DownloadProgressChanged += (s, e) => Run(() => updateToolTipText.Text = "Downloading update... (" + e.ProgressPercentage + "%)");
                wc.DownloadFileCompleted   += (s, e) =>
                    {
                        try
                        {
                            File.Move(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".tmp"), Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe"));

                            if (new FileInfo(Path.Combine(Signature.FullPath, "update_" + upd.Version + ".exe")).Length == 0)
                            {
                                throw new Exception("The downloaded binary is 0 bytes long.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Run(() => UpdateIOError(ex.Message));
                            return;
                        }

                        Run(() => UpdateDownloaded(upd.Version, true));
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
                var res = TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon        = VistaTaskDialogIcon.Information,
                        Title           = Signature.Software,
                        MainInstruction = "Update available",
                        Content         = "Version " + version + " has been downloaded and is ready to be installed!",
                        CommandButtons  = new[] { "Install now", "Postpone" }
                    });

                if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value == 0)
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
        /// Called when an update failed to download.
        /// </summary>
        /// <param name="error">The error message.</param>
        public void UpdateIOError(string error)
        {
            update.Tag             = error;
            updateToolTipText.Text = "There was an error while downloading the update:\r\n" + error + "\r\nClick here to update manually!";

            if (IsVisible && Top != -999)
            {
                var res = TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon        = VistaTaskDialogIcon.Error,
                        Title           = Signature.Software,
                        MainInstruction = "Update error",
                        Content         = "There was an error while downloading the latest update for the software.",
                        ExpandedInfo    = error,
                        CommandButtons  = new[] { "Update manually", "Postpone" }
                    });

                if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value == 0)
                {
                    Process.Start("http://lab.rolisoft.net/tvshowtracker/downloads.html");
                }
            }
            else if (!IsVisible || Top == -999)
            {
                _askErrorUpdate = true;
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
            if (_askErrorUpdate)
            {
                Process.Start("http://lab.rolisoft.net/tvshowtracker/downloads.html");
            }
            else if (File.Exists(Path.Combine(Signature.FullPath, "update_" + update.Tag + ".exe")) && new FileInfo(Path.Combine(Signature.FullPath, "update_" + update.Tag + ".exe")).Length != 0)
            {
                Utils.Run(Path.Combine(Signature.FullPath, "update_" + update.Tag + ".exe"), "/D=" + Signature.FullPath);
                NotifyIcon.ContextMenu.MenuItems[1].PerformClick();
            }
            else
            {
                Process.Start("http://lab.rolisoft.net/tvshowtracker/downloads.html");
            }
        }
        #endregion

        #region Exceptions
        /// <summary>
        /// Handles the unexpected exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="isTerminating">if set to <c>true</c> the exception will terminate the execution.</param>
        public static void HandleUnexpectedException(Exception ex, bool isTerminating = false)
        {
            if (ex is ThreadAbortException)
            {
                return;
            }

            var type = ex.GetType().Name;
            var count = 0;

            _exCnt.AddOrUpdate(type, 1, (t, i) => count = i + 1);

            if (count > 3)
            {
                return;
            }

            var show = !_initialized || Settings.Get<bool>("Show Unhandled Errors");
            var sb   = new StringBuilder();
            var sbtd = new StringBuilder();
            var ecnt = 0;

            while (ex != null)
            {
                sb.AppendLine(ex.GetType() + ": " + ex.Message);

                var st = new StackTrace(ex, true);

                if (st.FrameCount != 0)
                {
                    // TODO: StackTrace.ToString(TraceFormat.NoResourceLookup)
                    sb.Append(st);
                }
                else
                {
                    sb.AppendLine("   no stacktrace");
                }

                sb.AppendLine();

                if (show)
                {
                    if (sbtd.Length == 0)
                    {
                        sbtd.AppendFormat("An exception of type <a href=\"https://www.google.com/search?q={0}\">{1}</a> was thrown", Utils.EncodeURL(ex.GetType() + " " + ex.Message), ex.GetType().ToString().Split(".".ToCharArray()).Last());
                    }
                    else
                    {
                        sbtd.AppendFormat("\r\n\r\nWith inner exception of type <a href=\"https://www.google.com/search?q={0}\">{1}</a>", Utils.EncodeURL(ex.GetType() + " " + ex.Message), ex.GetType().ToString().Split(".".ToCharArray()).Last());
                    }

                    var fs = st.GetFrames();

                    if (fs != null)
                    {
                        foreach (var fr in fs)
                        {
                            var fn = fr.GetFileName();

                            if (string.IsNullOrWhiteSpace(fn))
                            {
                                continue;
                            }

                            var pr = fn.Replace('\\', '/');
                            var sp = Signature.BuildDirectory.Replace('\\', '/');

                            if (pr.StartsWith(sp, true, CultureInfo.InvariantCulture))
                            {
                                pr = pr.Substring(sp.Length).Trim("/".ToCharArray());
                            }

                            if (ecnt == 0)
                            {
                                sbtd.Append(" in ");
                            }
                            else
                            {
                                sbtd.Append(" originating from ");
                            }

                            sbtd.AppendFormat("<a href=\"https://github.com/RoliSoft/RS-TV-Show-Tracker/blob/{0}/{1}#L{2}\">{3}:{2}</a>", Signature.GitRevision, pr, fr.GetFileLineNumber(), Path.GetFileName(fn));
                            break;
                        }
                    }

                    sbtd.AppendFormat(":\r\n\r\n   {0}", ex.Message);
                }

                ex = ex.InnerException;
                ecnt++;
            }

            if (show)
            {
                if (Active != null && (bool)Active.Dispatcher.Invoke((Func<bool>)(() => Active.IsVisible)))
                {
                    Utils.Win7Taskbar(100, TaskbarProgressBarState.Error);
                }

                var res = TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "An unexpected error occurred",
                        MainInstruction         = "An unexpected error occurred",
                        Content                 = sbtd.ToString() + (count == 3 ? "\r\n\r\nFuture exceptions of this type will be ignored automatically." : string.Empty) + (isTerminating ? "\r\n\r\nUnfortunately this exception occurred at a crucial part of the code and the execution of the software will be terminated." : string.Empty),
                        ExpandedInfo            = sb.ToString().TrimEnd(),
                        CommandButtons          = new[] { "Submit bug report", "Ignore exception" },
                        AllowDialogCancellation = true,
                        Callback                = (dialog, args, data) =>
                            {
                                if (!string.IsNullOrWhiteSpace(args.Hyperlink))
                                {
                                    Utils.Run(args.Hyperlink);
                                }

                                if (args.ButtonId != 0)
                                {
                                    return false;
                                }

                                return true;
                            }
                    });

                if (Active != null &&(bool)Active.Dispatcher.Invoke((Func<bool>)(() => Active.IsVisible)))
                {
                    Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
                }

                if (res.CommandButtonResult.HasValue &&res.CommandButtonResult.Value == 0)
                {
                    ReportException(sb.ToString());
                }
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
