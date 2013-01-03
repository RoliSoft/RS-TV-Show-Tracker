namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    using Parsers.Social.Engines;

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

        private Twitter _twitter;
        private Identica _identica;
        private Facebook _facebook;

        private bool _loaded;

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

            _twitter = new Twitter
                {
                    Tokens = Settings.Get("Twitter OAuth", new List<string>())
                };

            postToTwitter.IsChecked = Settings.Get<bool>("Post to Twitter");

            if (_twitter.Tokens.Count == 4)
            {
                twitterUserName.Text = _twitter.Tokens[1];
                twitterUserLink.NavigateUri = new Uri("http://twitter.com/" + _twitter.Tokens[1]);
                twitterNoAuthMsg.Visibility = Visibility.Collapsed;
                twitterOkAuthMsg.Visibility = Visibility.Visible;
                twitterAuthStackPanel.Effect = new BlurEffect();
                twitterAuthStackPanel.IsHitTestVisible = twitterPinTextBox.IsTabStop = twitterFinishAuthButton.IsTabStop = false;
            }

            twitterStatusFormat.Text = Settings.Get("Twitter Status Format", _twitter.DefaultStatusFormat);

            // Identi.ca

            _identica = new Identica
                {
                    Tokens = Settings.Get("Identi.ca OAuth", new List<string>())
                };

            postToIdentica.IsChecked = Settings.Get<bool>("Post to Identi.ca");

            if (_identica.Tokens.Count == 4)
            {
                identicaUserName.Text = _identica.Tokens[1];
                identicaUserLink.NavigateUri = new Uri("http://identi.ca/" + _identica.Tokens[1]);
                identicaNoAuthMsg.Visibility = Visibility.Collapsed;
                identicaOkAuthMsg.Visibility = Visibility.Visible;
                identicaAuthStackPanel.Effect = new BlurEffect();
                identicaAuthStackPanel.IsHitTestVisible = identicaPinTextBox.IsTabStop = identicaFinishAuthButton.IsTabStop = false;
            }

            identicaStatusFormat.Text = Settings.Get("Identi.ca Status Format", _identica.DefaultStatusFormat);

            // Facebook

            _facebook = new Facebook
                {
                    Tokens = Settings.Get("Facebook OAuth", new List<string>())
                };

            postToFacebook.IsChecked = Settings.Get<bool>("Post to Facebook");

            if (_facebook.Tokens.Count == 4)
            {
                facebookUserName.Text = _facebook.Tokens[1];
                facebookUserLink.NavigateUri = new Uri("http://facebook.com/profile.php?id=" + _facebook.Tokens[0]);
                facebookNoAuthMsg.Visibility = Visibility.Collapsed;
                facebookOkAuthMsg.Visibility = Visibility.Visible;
            }

            facebookStatusFormat.Text = Settings.Get("Facebook Status Format", _facebook.DefaultStatusFormat);

            // Settings

            onlyNew.IsChecked = Settings.Get("Post only recent", true);

            switch (Settings.Get("Post restrictions list type", "black"))
            {
                case "black":
                    blackListRadioButton.IsChecked = true;
                    listTypeText.Text = "Specify TV shows to block from being posted:";
                    break;

                case "white":
                    whiteListRadioButton.IsChecked = true;
                    listTypeText.Text = "Specify TV shows to allow to be posted:";
                    break;
            }

            foreach (var show in Settings.Get("Post restrictions list", new List<int>()))
            {
                if (Database.TVShows.ContainsKey(show))
                {
                    listBox.Items.Add(Database.TVShows[show].Name);
                }
                else
                {
                    listBox.Items.Add("Unknown show #" + show);
                }
            }

            _loaded = true;

            ListBoxSelectionChanged();

            foreach (var show in Database.TVShows.Values.OrderBy(x => x.Name))
            {
                listComboBox.Items.Add(show.Name);
            }

            ListComboBoxSelectionChanged();
        }

        /// <summary>
        /// Handles the Click event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            Utils.Run((sender as Hyperlink).NavigateUri.ToString());
        }

        #region Twitter
        /// <summary>
        /// Handles the Checked event of the postToTwitter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToTwitterChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Post to Twitter", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the postToTwitter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToTwitterUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

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
                Utils.Run(_twitter.GenerateAuthorizationLink());
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
                var tokens = _twitter.FinishAuthorizationWithPin(twitterPinTextBox.Text.Trim());

                Settings.Set("Twitter OAuth", _twitter.Tokens = tokens);

                if (_twitter.Tokens.Count == 4)
                {
                    twitterUserName.Text = _twitter.Tokens[1];
                    twitterUserLink.NavigateUri = new Uri("http://twitter.com/" + _twitter.Tokens[1]);
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
            if (!_loaded) return;

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
            if (_loaded)
            {
                Settings.Set("Twitter Status Format", twitterStatusFormat.Text);
            }

            twitterStatusFormatExample.Text = FileNames.Parser.FormatFileName(twitterStatusFormat.Text, RenamerWindow.SampleInfo).CutIfLonger(140);
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
            if (!_loaded) return;

            Settings.Set("Post to Identi.ca", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the postToIdentica control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToIdenticaUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

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
                Utils.Run(_identica.GenerateAuthorizationLink());
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
                var tokens = _identica.FinishAuthorizationWithPin(identicaPinTextBox.Text.Trim());

                Settings.Set("Identi.ca OAuth", _identica.Tokens = tokens);

                if (_identica.Tokens.Count == 4)
                {
                    identicaUserName.Text = _identica.Tokens[1];
                    identicaUserLink.NavigateUri = new Uri("http://identi.ca/" + _identica.Tokens[1]);
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
            if (!_loaded) return;

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
            if (_loaded)
            {
                Settings.Set("Identi.ca Status Format", identicaStatusFormat.Text);
            }

            identicaStatusFormatExample.Text = FileNames.Parser.FormatFileName(identicaStatusFormat.Text, RenamerWindow.SampleInfo).CutIfLonger(140);
        }
        #endregion

        #region Facebook
        /// <summary>
        /// Handles the Checked event of the postToFacebook control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToFacebookChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Post to Facebook", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the postToFacebook control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void PostToFacebookUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Post to Facebook", false);
        }

        /// <summary>
        /// Handles the Click event of the facebookAuthInBrowserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FacebookAuthInBrowserButtonClick(object sender, RoutedEventArgs e)
        {
            string code;

            try
            {
                var faw = new FacebookAuthWindow(_facebook);
                faw.ShowDialog();

                if (string.IsNullOrWhiteSpace(faw.Code))
                {
                    throw new Exception("Invalid response from server. (No tokens were returned.)");
                }
                else
                {
                    code = faw.Code;
                }
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        CommonIcon          = TaskDialogIcon.Stop,
                        Title               = "Facebook OAuth",
                        Instruction         = "Error",
                        Content             = "An error occured while generating an authorization link.",
                        ExpandedControlText = "Show exception message",
                        ExpandedInformation = ex.Message
                    }.Show();
                return;
            }

            try
            {
                var tokens = _facebook.FinishAuthorizationWithPin(code);

                Settings.Set("Facebook OAuth", _facebook.Tokens = tokens);

                if (_facebook.Tokens.Count == 4)
                {
                    facebookUserName.Text = _facebook.Tokens[1];
                    facebookUserLink.NavigateUri = new Uri("http://facebook.com/profile.php?id=" + _facebook.Tokens[0]);
                    facebookNoAuthMsg.Visibility = Visibility.Collapsed;
                    facebookOkAuthMsg.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                new TaskDialog
                    {
                        CommonIcon          = TaskDialogIcon.Stop,
                        Title               = "Facebook OAuth",
                        Instruction         = "Error",
                        Content             = "An error occured while getting the tokens for the specified PIN.",
                        ExpandedControlText = "Show exception message",
                        ExpandedInformation = ex.Message
                    }.Show();
            }
        }
        /// <summary>
        /// Handles the TextChanged event of the facebookStatusFormat control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void FacebookStatusFormatTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_loaded)
            {
                Settings.Set("Facebook Status Format", facebookStatusFormat.Text);
            }

            facebookStatusFormatExample.Text = FileNames.Parser.FormatFileName(facebookStatusFormat.Text, RenamerWindow.SampleInfo).CutIfLonger(140);
        }
        #endregion

        #region Settings
        /// <summary>
        /// Handles the SelectionChanged event of the listBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ListBoxSelectionChanged(object sender = null, System.Windows.Controls.SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            listRemoveButton.IsEnabled = listBox.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the listComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ListComboBoxSelectionChanged(object sender = null, System.Windows.Controls.SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            listAddButton.IsEnabled = listComboBox.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the Checked event of the onlyNew control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnlyNewChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Post only recent", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the onlyNew control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnlyNewUnchecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            Settings.Set("Post only recent", false);
        }

        /// <summary>
        /// Handles the Click event of the whiteListRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WhiteListRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            listTypeText.Text = "Specify TV shows to allow to be posted:";
            Settings.Set("Post restrictions list type", "white");
        }

        /// <summary>
        /// Handles the Click event of the blackListRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void BlackListRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            listTypeText.Text = "Specify TV shows to block from being posted:";
            Settings.Set("Post restrictions list type", "black");
        }

        /// <summary>
        /// Handles the Click event of the listAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ListAddButtonClick(object sender, RoutedEventArgs e)
        {
            if (listComboBox.SelectedIndex == -1) return;

            var list = Settings.Get("Post restrictions list", new List<int>());
            var shid = Database.TVShows.Values.First(x => x.Name == (string)listComboBox.SelectedValue).ID;

            if (!list.Contains(shid))
            {
                listBox.Items.Add(listComboBox.SelectedValue);
                list.Add(shid);
                Settings.Set("Post restrictions list", list);
            }

            listComboBox.SelectedIndex = -1;
        }

        /// <summary>
        /// Handles the Click event of the listRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ListRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedIndex == -1) return;

            var list = Settings.Get("Post restrictions list", new List<int>());
            var shid = Database.TVShows.Values.First(x => x.Name == (string)listBox.SelectedValue).ID;

            if (list.Contains(shid))
            {
                listBox.Items.RemoveAt(listBox.SelectedIndex);
                list.Remove(shid);
                Settings.Set("Post restrictions list", list);
            }
        }
        #endregion
    }
}
