namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Interaction logic for ListingSettings.xaml
    /// </summary>
    public partial class ListingSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListingSettings"/> class.
        /// </summary>
        public ListingSettings()
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
                nzbDays.Value              = Settings.Get<int>("Usenet Retention");
                filterUrlTextBox.Text      = string.Join(",", Settings.Get<List<string>>("One-Click Hoster List"));
                highlightFree.IsChecked    = Settings.Get<bool>("Highlight Free Torrents");
                fadeDead.IsChecked         = Settings.Get("Fade Dead Torrents", true);
                linkChecker.IsChecked      = Settings.Get<bool>("One-Click Hoster Link Checker");
                shortestNotation.IsChecked = Settings.Get<bool>("Enable Shortest Notation");
                updateToNightly.IsChecked  = Settings.Get<bool>("Update to Nightly Builds");

                switch (Settings.Get("One-Click Hoster List Type", "white"))
                {
                    case "black":
                        blackListRadioButton.IsChecked = true;
                        listTypeText.Text = "Enter a comma-separated list of domains or partial URLs to filter links if they match:";
                        break;

                    case "white":
                        whiteListRadioButton.IsChecked = true;
                        listTypeText.Text = "Enter a comma-separated list of domains or partial URLs to filter links if they don't match:";
                        break;
                }

                if (!Signature.IsActivated)
                {
                    nzbDays.IsEnabled = highlightFree.IsEnabled = fadeDead.IsEnabled = blackListRadioButton.IsEnabled = whiteListRadioButton.IsEnabled = filterUrlTextBox.IsEnabled = linkChecker.IsEnabled = false;
                }

                var fn = Path.Combine(Signature.InstallPath, @"misc\linkchecker");

                if (!File.Exists(fn))
                {
                    linkCheck.Text = "The link checker definitions file hasn't been downloaded yet.";
                }
                else if (Parsers.LinkCheckers.Engines.UniversalEngine.Definitions == null || Parsers.LinkCheckers.Engines.UniversalEngine.Definitions.Count == 0)
                {
                    linkCheck.Text = "The link checker definitions file hasn't been loaded yet.";
                }
                else
                {
                    linkCheck.Text = "The link checker definitions file has " + Parsers.LinkCheckers.Engines.UniversalEngine.Definitions.Count + " sites; last updated " + File.GetLastWriteTime(fn).ToRelativeDate() + ".";
                }

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
                    cupIcon3.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cup-tick.png"));
                    cupIcon4.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cup-tick.png"));
                    cupIcon5.Source   = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/cup-tick.png"));
                    cupIcon1.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                    cupIcon2.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                    cupIcon3.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                    cupIcon4.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                    cupIcon5.ToolTip += Environment.NewLine + "The software is activated, thank you for your support!";
                }
                else
                {
                    cupIcon1.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                    cupIcon2.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                    cupIcon3.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                    cupIcon4.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                    cupIcon5.ToolTip += Environment.NewLine + "For more information, click on 'Support the software' in the main menu.";
                }
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;
        }

        /// <summary>
        /// Handles the OnLostFocus event of the nzbDays control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void NzbDaysOnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Usenet Retention", nzbDays.Value);
        }
        /// <summary>
        /// Handles the Checked event of the hightlightFree control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HighlightFreeChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Highlight Free Torrents", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the highlightFree control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HighlightFreeUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Highlight Free Torrents", false);
        }

        /// <summary>
        /// Handles the Checked event of the fadeDead control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FadeDeadChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Fade Dead Torrents", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the fadeDead control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FadeDeadUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Fade Dead Torrents", false);
        }

        /// <summary>
        /// Handles the Click event of the whiteListRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WhiteListRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            listTypeText.Text = "Enter a comma-separated list of domains or partial URLs to filter links if they don't match:";
            Settings.Set("One-Click Hoster List Type", "white");
        }

        /// <summary>
        /// Handles the Click event of the blackListRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void BlackListRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            listTypeText.Text = "Enter a comma-separated list of domains or partial URLs to filter links if they match:";
            Settings.Set("One-Click Hoster List Type", "black");
        }

        /// <summary>
        /// Handles the TextChanged event of the filterUrlTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void FilterUrlTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_loaded) return;

            var urls = filterUrlTextBox.Text.Trim(',').Split(',').ToList();

            if (urls.Count == 1 && string.IsNullOrWhiteSpace(urls[0]))
            {
                urls.RemoveAt(0);
            }

            Settings.Set("One-Click Hoster List", urls);
        }

        /// <summary>
        /// Handles the Checked event of the linkChecker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LinkCheckerChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("One-Click Hoster Link Checker", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the linkChecker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LinkCheckerUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("One-Click Hoster Link Checker", false);
        }

        /// <summary>
        /// Handles the Checked event of the shortestNotation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShortestNotationChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable Shortest Notation", true);

            _reindex = true;
            ShowNames.Regexes.AdvNumbering = ShowNames.Parser.GenerateEpisodeRegexes(generateExtractor: true);
        }

        /// <summary>
        /// Handles the Unchecked event of the shortestNotation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ShortestNotationUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Enable Shortest Notation", false);

            _reindex = true;
            ShowNames.Regexes.AdvNumbering = ShowNames.Parser.GenerateEpisodeRegexes(generateExtractor: true);
        }

        /// <summary>
        /// Handles the Checked event of the updateToNightly control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UpdateToNightlyChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Update to Nightly Builds", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the updateToNightly control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UpdateToNightlyUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Update to Nightly Builds", false);
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
