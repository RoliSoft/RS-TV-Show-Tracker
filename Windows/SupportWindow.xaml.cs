namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Remote;
    using RoliSoft.TVShowTracker.Remote.Objects;

    /// <summary>
    /// Interaction logic for SupportWindow.xaml
    /// </summary>
    public partial class SupportWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SupportWindow"/> class.
        /// </summary>
        public SupportWindow()
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

            SetStatus(true);
        }

        /// <summary>
        /// Sets the status.
        /// </summary>
        /// <param name="force">The value indicating whether to replace values from the license.</param>
        private void SetStatus(bool force = false)
        {
            if (force || string.IsNullOrWhiteSpace(emailTextBox.Text))
            {
                emailTextBox.Text = Signature.ActivationUser;
            }

            if (force || string.IsNullOrWhiteSpace(keyTextBox.Text))
            {
                keyTextBox.Text = Signature.ActivationKey;
            }

            emailTextBox.IsReadOnly = keyTextBox.IsReadOnly = Signature.IsActivated;

            SetStatus2(Signature.ActivationStatus);
        }

        /// <summary>
        /// Sets the status.
        /// </summary>
        /// <param name="status">The status to set.</param>
        private void SetStatus2(Signature.LicenseStatus status)
        {
            switch (status)
            {
                case Signature.LicenseStatus.Aborted:
                case Signature.LicenseStatus.Uninitialized:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-gray.png"));
                    statusText.Text = "Licensing system is uninitialized.";
                    activateButton.IsEnabled = false;
                    break;

                case Signature.LicenseStatus.Valid:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-green.png"));
                    statusText.Text = "The software is activated!";
                    activateButton.IsEnabled = false;
                    break;

                case Signature.LicenseStatus.NotAvailable:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-yellow.png"));
                    statusText.Text = "The software is not activated.";
                    activateButton.IsEnabled = false;
                    break;

                case Signature.LicenseStatus.KeyCryptoError:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-red.png"));
                    statusText.Text = "The specified key is invalid for the specified email address.";
                    activateButton.IsEnabled = false;
                    break;

                case Signature.LicenseStatus.KeyStatusError:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-red.png"));
                    statusText.Text = "The specified key has been rejected by the server.";
                    activateButton.IsEnabled = true;
                    break;

                case Signature.LicenseStatus.LicenseDecryptError:
                case Signature.LicenseStatus.LicenseException:
                case Signature.LicenseStatus.LicenseInvalid:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-red.png"));
                    statusText.Text = "Failed to decrypt existing license file. Please inspect the software logs.";
                    activateButton.IsEnabled = !string.IsNullOrWhiteSpace(emailTextBox.Text) && !string.IsNullOrWhiteSpace(keyTextBox.Text) && Signature.VerifyKey(emailTextBox.Text, keyTextBox.Text);
                    break;
            }
        }

        /// <summary>
        /// Sets the status.
        /// </summary>
        /// <param name="status">The status to set.</param>
        /// <param name="icon">The icon to set.</param>
        /// <param name="btn">if set to <c>true</c> the activate button will be enabled, disabled or left alone.</param>
        private void SetStatus2(string status, int icon, bool? btn = null)
        {
            statusText.Text = status;

            if (btn.HasValue)
            {
                activateButton.IsEnabled = btn.Value;
            }
            
            switch (icon)
            {
                default:
                case 0:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-gray.png"));
                    break;

                case 1:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-green.png"));
                    break;

                case 2:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-yellow.png"));
                    break;

                case 3:
                    statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/status-red.png"));
                    break;
            }
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the TabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void TabControlOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (tabControl.SelectedIndex)
            {
                case 0:
                    Height = 240;
                    Width  = 362;
                    break;

                case 1:
                    Height = 450;
                    Width  = 540;
                    break;
            }
        }

        /// <summary>
        /// Handles the OnTextChanged event of the KeyTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void KeyTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (keyTextBox.Text.Contains("_")) return;

            if (Signature.VerifyKey(emailTextBox.Text, keyTextBox.Text))
            {
                SetStatus2("The specified key is valid for the specified email address.", 1, true);
            }
            else
            {
                SetStatus2(Signature.LicenseStatus.KeyCryptoError);
            }
        }

        /// <summary>
        /// Handles the OnClick event of the ActivateButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ActivateButtonOnClick(object sender, RoutedEventArgs e)
        {
            activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = false;

            Log.Info("Activating the software...");
            SetStatus2("Activating software...", 1);

            var name = emailTextBox.Text.ToLower().Trim();
            var key  = keyTextBox.Text.Trim();

            Task.Factory.StartNew(() =>
                {
                    string hash;
                    object identity;
                    Generic<string> resp;
                    byte[] license;

                    try
                    {
                        hash = BitConverter.ToString(new HMACSHA384(SHA384.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Truncate(16)).ComputeHash(Encoding.UTF8.GetBytes(key))).ToLower().Replace("-", string.Empty);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error while generating license hash from the specified email address and donation key.", ex);
                                SetStatus2("Error while preparing to activate. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    try
                    {
                        identity = Signature.GetComputerInfo();
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error while generating machine identity from WMI.", ex);
                                SetStatus2("Error while preparing to activate. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    try
                    {
                        resp = API.GetMachineKey(hash, identity);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error while sending request to the server.", ex);
                                SetStatus2("Error while contacting server. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    int code;
                    if (!string.IsNullOrWhiteSpace(resp.Result) && Regex.IsMatch(resp.Result, @"^\d+$") && int.TryParse(resp.Result, out code))
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("The activation server returned KeyStatus=" + ((Signature.KeyStatus)code) + (!string.IsNullOrWhiteSpace(resp.Error) ? " and error message:" + Environment.NewLine + resp.Error : "."));
                                SetStatus2("The specified key has been rejected by the server: " + ((Signature.KeyStatus)code), 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(resp.Error))
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error received from server while activating:" + Environment.NewLine + resp.Error);
                                SetStatus2("Error received from server. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    try
                    {
                        license = Convert.FromBase64String(resp.Result);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error while decrypting server response:" + Environment.NewLine + resp.Result, ex);
                                SetStatus2("Error while decrypting license. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    try
                    {
                        Signature.SaveLicense(name, key, license);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error while saving license to disk.", ex);
                                SetStatus2("Error while saving license. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    try
                    {
                        Signature.InitLicense();
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                            {
                                Log.Error("Error while loading license into software.", ex);
                                SetStatus2("Error while loading license. Please inspect the software logs.", 3, true);
                                activateButton.IsEnabled = closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            });
                        return;
                    }

                    Dispatcher.Invoke(() =>
                        {
                            SetStatus(true);
                            closeButton.IsEnabled = emailTextBox.IsEnabled = keyTextBox.IsEnabled = true;
                            MainWindow.Active.ReindexDownloadPaths.IsEnabled = true;
                            MainWindow.Active.ReindexDownloadPathsClick();
                        });
                });
        }

        /// <summary>
        /// Handles the OnClick event of the CloseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void CloseButtonOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the OnClick event of the DonateButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void DonateButtonOnClick(object sender, RoutedEventArgs e)
        {
            Utils.Run("http://lab.rolisoft.net/tvshowtracker/donate.html");
        }
    }
}
