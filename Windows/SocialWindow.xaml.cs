namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    using RoliSoft.TVShowTracker.Social;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Interaction logic for SocialWindow.xaml
    /// </summary>
    public partial class SocialWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SocialWindow"/> class.
        /// </summary>
        public SocialWindow()
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

            // Twitter

            postToTwitter.IsChecked  = Settings.Get<bool>("Post to Twitter");
            twitterOnlyNew.IsChecked = Settings.Get("Post to Twitter only new", true);

            if (Twitter.OAuthTokensAvailable())
            {
                twitterNoAuthMsg.Visibility = Visibility.Collapsed;
                twitterOkAuthMsg.Visibility = Visibility.Visible;
                twitterAuthStackPanel.Effect = new BlurEffect();
                twitterAuthStackPanel.IsHitTestVisible = twitterPinTextBox.IsTabStop = twitterFinishAuthButton.IsTabStop = false;
            }

            twitterStatusFormat.Text = Settings.Get("Twitter Status Format", Twitter.DefaultStatusFormat);

            // Identi.ca

            postToIdentica.IsChecked = Settings.Get<bool>("Post to Identi.ca");
            identicaOnlyNew.IsChecked = Settings.Get("Post to Identi.ca only new", true);

            if (Identica.OAuthTokensAvailable())
            {
                identicaNoAuthMsg.Visibility = Visibility.Collapsed;
                identicaOkAuthMsg.Visibility = Visibility.Visible;
                identicaAuthStackPanel.Effect = new BlurEffect();
                identicaAuthStackPanel.IsHitTestVisible = identicaPinTextBox.IsTabStop = identicaFinishAuthButton.IsTabStop = false;
            }

            identicaStatusFormat.Text = Settings.Get("Identi.ca Status Format", Identica.DefaultStatusFormat);
        }

        #region Twitter
        /// <summary>
        /// Handles the Checked event of the postToTwitter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToTwitterChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Twitter", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the postToTwitter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToTwitterUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Twitter", false);
        }

        /// <summary>
        /// Handles the Click event of the twitterAuthInBrowserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TwitterAuthInBrowserButtonClick(object sender, RoutedEventArgs e)
        {
            twitterAuthStackPanel.Effect = null;
            twitterAuthStackPanel.IsHitTestVisible = twitterPinTextBox.IsTabStop = twitterFinishAuthButton.IsTabStop = true;

            try
            {
                Utils.Run(Twitter.GenerateAuthorizationLink());
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        CommonIcon          = TaskDialogIcon.Stop,
                        Title               = "Twitter OAuth",
                        Instruction         = "Error",
                        Content             = "An error occured while generating an authorization link.",
                        ExpandedControlText = "Show exception message",
                        ExpandedInformation = ex.Message
                    }.Show();
            }
        }

        /// <summary>
        /// Handles the Click event of the twitterFinishAuthButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TwitterFinishAuthButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Twitter.FinishAuthorizationWithPin(twitterPinTextBox.Text.Trim());

                if (Twitter.OAuthTokensAvailable())
                {
                    twitterNoAuthMsg.Visibility = Visibility.Collapsed;
                    twitterOkAuthMsg.Visibility = Visibility.Visible;
                    twitterAuthStackPanel.Effect = new BlurEffect();
                    twitterAuthStackPanel.IsHitTestVisible = twitterPinTextBox.IsTabStop = twitterFinishAuthButton.IsTabStop = false;
                    twitterPinTextBox.Foreground = Brushes.Gray;
                    twitterPinTextBox.Text = "Enter PIN here";
                    twitterFinishAuthButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        CommonIcon          = TaskDialogIcon.Stop,
                        Title               = "Twitter OAuth",
                        Instruction         = "Error",
                        Content             = "An error occured while getting the tokens for the specified PIN.",
                        ExpandedControlText = "Show exception message",
                        ExpandedInformation = ex.Message
                    }.Show();
            }
        }

        /// <summary>
        /// Handles the GotFocus event of the twitterPinTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TwitterPinTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (twitterPinTextBox.Text == "Enter PIN here")
            {
                twitterPinTextBox.Text = string.Empty;
                twitterPinTextBox.Foreground = Brushes.Black;
            }
        }

        /// <summary>
        /// Handles the LostFocus event of the twitterPinTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TwitterPinTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(twitterPinTextBox.Text))
            {
                twitterPinTextBox.Foreground = Brushes.Gray;
                twitterPinTextBox.Text = "Enter PIN here";
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the twitterPinTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TwitterPinTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (twitterFinishAuthButton != null)
            {
                twitterFinishAuthButton.IsEnabled = !string.IsNullOrWhiteSpace(twitterPinTextBox.Text) && Regex.IsMatch(twitterPinTextBox.Text.Trim(), @"^\d+$");
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the twitterStatusFormat control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TwitterStatusFormatTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Settings.Set("Twitter Status Format", twitterStatusFormat.Text);
            twitterStatusFormatExample.Text = FileNames.Parser.FormatFileName(twitterStatusFormat.Text, RenamerWindow.SampleInfo).CutIfLonger(140);
        }

        /// <summary>
        /// Handles the Checked event of the twitterOnlyNew control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TwitterOnlyNewChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Twitter only new", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the twitterOnlyNew control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TwitterOnlyNewUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Twitter only new", false);
        }
        #endregion

        #region Identica
        /// <summary>
        /// Handles the Checked event of the postToIdentica control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToIdenticaChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Identi.ca", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the postToIdentica control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToIdenticaUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Identi.ca", false);
        }

        /// <summary>
        /// Handles the Click event of the identicaAuthInBrowserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IdenticaAuthInBrowserButtonClick(object sender, RoutedEventArgs e)
        {
            identicaAuthStackPanel.Effect = null;
            identicaAuthStackPanel.IsHitTestVisible = identicaPinTextBox.IsTabStop = identicaFinishAuthButton.IsTabStop = true;

            try
            {
                Utils.Run(Identica.GenerateAuthorizationLink());
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        CommonIcon          = TaskDialogIcon.Stop,
                        Title               = "Identi.ca OAuth",
                        Instruction         = "Error",
                        Content             = "An error occured while generating an authorization link.",
                        ExpandedControlText = "Show exception message",
                        ExpandedInformation = ex.Message
                    }.Show();
            }
        }

        /// <summary>
        /// Handles the Click event of the identicaFinishAuthButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IdenticaFinishAuthButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Identica.FinishAuthorizationWithPin(identicaPinTextBox.Text.Trim());

                if (Identica.OAuthTokensAvailable())
                {
                    identicaNoAuthMsg.Visibility = Visibility.Collapsed;
                    identicaOkAuthMsg.Visibility = Visibility.Visible;
                    identicaAuthStackPanel.Effect = new BlurEffect();
                    identicaAuthStackPanel.IsHitTestVisible = identicaPinTextBox.IsTabStop = identicaFinishAuthButton.IsTabStop = false;
                    identicaPinTextBox.Foreground = Brushes.Gray;
                    identicaPinTextBox.Text = "Enter PIN here";
                    identicaFinishAuthButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        CommonIcon          = TaskDialogIcon.Stop,
                        Title               = "Identi.ca OAuth",
                        Instruction         = "Error",
                        Content             = "An error occured while getting the tokens for the specified PIN.",
                        ExpandedControlText = "Show exception message",
                        ExpandedInformation = ex.Message
                    }.Show();
            }
        }

        /// <summary>
        /// Handles the GotFocus event of the identicaPinTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IdenticaPinTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (identicaPinTextBox.Text == "Enter PIN here")
            {
                identicaPinTextBox.Text = string.Empty;
                identicaPinTextBox.Foreground = Brushes.Black;
            }
        }

        /// <summary>
        /// Handles the LostFocus event of the identicaPinTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IdenticaPinTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(identicaPinTextBox.Text))
            {
                identicaPinTextBox.Foreground = Brushes.Gray;
                identicaPinTextBox.Text = "Enter PIN here";
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the identicaPinTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void IdenticaPinTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (identicaFinishAuthButton != null)
            {
                identicaFinishAuthButton.IsEnabled = !string.IsNullOrWhiteSpace(identicaPinTextBox.Text) && Regex.IsMatch(identicaPinTextBox.Text.Trim(), @"^\d+$");
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the identicaStatusFormat control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void IdenticaStatusFormatTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Settings.Set("Identi.ca Status Format", identicaStatusFormat.Text);
            identicaStatusFormatExample.Text = FileNames.Parser.FormatFileName(identicaStatusFormat.Text, RenamerWindow.SampleInfo).CutIfLonger(140);
        }

        /// <summary>
        /// Handles the Checked event of the identicaOnlyNew control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IdenticaOnlyNewChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Identi.ca only new", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the identicaOnlyNew control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void IdenticaOnlyNewUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Post to Identi.ca only new", false);
        }
        #endregion
    }
}
