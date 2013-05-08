namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for GeneralSettings.xaml
    /// </summary>
    public partial class GeneralSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralSettings"/> class.
        /// </summary>
        public GeneralSettings()
        {
            InitializeComponent();
        }

        private bool _loaded;
        internal bool _reindex;

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
                foreach (var path in Settings.Get<List<string>>("Download Paths"))
                {
                    dlPathsListBox.Items.Add(path);
                }

                using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    startAtStartup.IsChecked = rk.GetValue("RS TV Show Tracker") != null;
                }

                convertTimezone.IsChecked = Settings.Get("Convert Timezone", true);

                var tzinfo = "Your current timezone is " + TimeZoneInfo.Local.DisplayName + ".\r\n"
                           + "Your difference from Central Standard Time is {0} hours.".FormatWith(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").BaseUtcOffset.TotalHours * -1)).TotalHours);

                currentTimezone.ContentEnd.InsertTextInRun(tzinfo);

                searchNtfsMft.IsChecked       = Settings.Get<bool>("Search NTFS MFT records");
                disableAero.IsChecked         = !Settings.Get("Enable Aero", true);
                disableAnimations.IsChecked   = !Settings.Get("Enable Animations", true);
                showUnhandledErrors.IsChecked = Settings.Get<bool>("Show Unhandled Errors");

                if (Utils.IsAdmin)
                {
                    uacIcon.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/uac-tick.png"));
                    uacIcon.ToolTip += Environment.NewLine + "The software is currently running with administrator rights.";
                }
                else
                {
                    uacIcon.ToolTip += Environment.NewLine + "The software is currently running without administrator rights.";
                }
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;

            DlPathsListBoxSelectionChanged();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the dlPathsListBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void DlPathsListBoxSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            dlPathRemoveButton.IsEnabled   = dlPathsListBox.SelectedIndex != -1;
            dlPathMoveUpButton.IsEnabled   = dlPathsListBox.SelectedIndex > 0;
            dlPathMoveDownButton.IsEnabled = dlPathsListBox.SelectedIndex != -1 && dlPathsListBox.SelectedIndex < dlPathsListBox.Items.Count - 1;
        }

        /// <summary>
        /// Handles the Click event of the dlPathAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathAddButtonClick(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description         = "Select the directory where you download your TV shows:",
                    ShowNewFolderButton = false
                };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dlPathsListBox.Items.Add(fbd.SelectedPath + Path.DirectorySeparatorChar);
            }

            Library.AddPath(fbd.SelectedPath + Path.DirectorySeparatorChar);

            _reindex = true;

            SaveDlPaths();
        }

        /// <summary>
        /// Handles the Click event of the dlPathMoveUpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathMoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (dlPathsListBox.SelectedIndex == -1) return;

            var idx = dlPathsListBox.SelectedIndex;
            var sel = dlPathsListBox.Items[idx];

            if (idx > 0)
            {
                dlPathsListBox.Items.Remove(sel);
                dlPathsListBox.Items.Insert(idx - 1, sel);
                dlPathsListBox.SelectedItem = sel;

                SaveDlPaths();
            }
        }

        /// <summary>
        /// Handles the Click event of the dlPathMoveDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathMoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (dlPathsListBox.SelectedIndex == -1) return;

            var idx = dlPathsListBox.SelectedIndex;
            var sel = dlPathsListBox.Items[idx];

            if (idx < dlPathsListBox.Items.Count - 1)
            {
                dlPathsListBox.Items.Remove(sel);
                dlPathsListBox.Items.Insert(idx + 1, sel);
                dlPathsListBox.SelectedItem = sel;

                SaveDlPaths();
            }
        }

        /// <summary>
        /// Handles the Click event of the dlPathRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DlPathRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (dlPathsListBox.SelectedIndex == -1) return;

            Library.RemovePath(dlPathsListBox.Items[dlPathsListBox.SelectedIndex].ToString());
            dlPathsListBox.Items.RemoveAt(dlPathsListBox.SelectedIndex);

            SaveDlPaths();
        }

        /// <summary>
        /// Saves the download paths to the XML settings file.
        /// </summary>
        public void SaveDlPaths()
        {
            Settings.Set("Download Paths", dlPathsListBox.Items.Cast<string>().ToList());
        }

        /// <summary>
        /// Handles the Checked event of the searchNtfsMft control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchNtfsMftChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Search NTFS MFT records", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the searchNtfsMft control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchNtfsMftUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Search NTFS MFT records", false);
        }

        /// <summary>
        /// Handles the Checked event of the startAtStartup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StartAtStartupChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                rk.SetValue("RS TV Show Tracker", "\"" + Assembly.GetExecutingAssembly().Location + "\" -hide");
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the startAtStartup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StartAtStartupUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                rk.DeleteValue("RS TV Show Tracker", false);
            }
        }

        /// <summary>
        /// Handles the Checked event of the convertTimezone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ConvertTimezoneChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Convert Timezone", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the convertTimezone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ConvertTimezoneUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Convert Timezone", false);
        }

        /// <summary>
        /// Handles the Checked event of the disableAero control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAeroChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable Aero", false);

            MainWindow.Active.ActivateNonAero();
        }

        /// <summary>
        /// Handles the Unchecked event of the disableAero control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAeroUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable Aero", true);

            MainWindow.Active.AeroChanged(sender, new PropertyChangedEventArgs("IsGlassEnabled"));
        }

        /// <summary>
        /// Handles the Checked event of the disableAnimations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAnimationsChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable Animations", false);

            MainWindow.Active.DeactivateAnimation();
        }

        /// <summary>
        /// Handles the Unchecked event of the disableAnimations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableAnimationsUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable Animations", true);

            MainWindow.Active.ActivateAnimation();
        }

        /// <summary>
        /// Handles the Checked event of the showUnhandledErrors control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowUnhandledErrorsChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Show Unhandled Errors", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the showUnhandledErrors control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShowUnhandledErrorsUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Show Unhandled Errors", false);
        }
    }
}
