namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media.Effects;

    using TaskDialogInterop;

    /// <summary>
    /// Interaction logic for ParserWindow.xaml
    /// </summary>
    public partial class ParserWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParserWindow"/> class.
        /// </summary>
        /// <param name="parser">The parser.</param>
        public ParserWindow(dynamic parser)
        {
            InitializeComponent();

            Title = "Configure " + parser.Name;
            Parser = parser;

            if (!parser.CanLogin)
            {
                uaGrid.IsHitTestVisible = false;
                uaGrid.Effect = new BlurEffect();

                usernameTextBox.IsTabStop = passwordTextBox.IsTabStop = testLoginButton.IsHitTestVisible = false;
            }
        }

        /// <summary>
        /// Gets or sets the parser.
        /// </summary>
        /// <value>
        /// The parser.
        /// </value>
        public dynamic Parser { get; set; }

        private Dictionary<string, string> _original;

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

            _original = new Dictionary<string, string>();

            if (Parser.CanLogin)
            {
                var login = Settings.Get(Parser.Name + " Login");

                if (!string.IsNullOrWhiteSpace(login))
                {
                    try
                    {
                        var ua = Utils.Decrypt(login, Parser.GetType().FullName + Environment.NewLine + Utils.GetUUID()).Split(new[] { '\0' }, 2);

                        usernameTextBox.Text     = _original["user"] = ua[0];
                        passwordTextBox.Password = _original["pass"] = ua[1];
                    }
                    catch { }
                }
            }

            if (Parser.RequiredCookies.Length != 0)
            {
                for (var i = 0; i < Parser.RequiredCookies.Length; i++)
                {
                    var b = new Bold();
                    b.Inlines.Add(Parser.RequiredCookies[i]);

                    requiredCookies.Inlines.Add(b);

                    if (i != Parser.RequiredCookies.Length - 1)
                    {
                        requiredCookies.Inlines.Add(", ");
                    }
                }
            }

            cookiesTextBox.Text = _original["cookies"] = Settings.Get(Parser.Name + " Cookies");
        }

        /// <summary>
        /// Handles the Click event of the testLoginButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TestLoginButtonClick(object sender, RoutedEventArgs e)
        {
            Thread action = null;
            var done = false;

            var user = usernameTextBox.Text;
            var pass = passwordTextBox.Password;

            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = Parser.Name,
                    MainInstruction         = "Logging in",
                    Content                 = "Logging in to " + Parser.Name + " as " + user + "...",
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp)
                            {
                                dialog.SetProgressBarMarquee(true, 0);
                                showmbp = true;
                            }

                            if (args.ButtonId != 0)
                            {
                                if (!done)
                                {
                                    try { action.Abort(); } catch { }
                                }

                                return false;
                            }

                            if (done)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();
            
            action = new Thread(() =>
                {
                    try
                    {
                        var cookies = Parser.Login(user, pass);

                        done = true;

                        if (string.IsNullOrWhiteSpace(cookies))
                        {
                            TaskDialog.Show(new TaskDialogOptions
                                {
                                    MainIcon        = VistaTaskDialogIcon.Error,
                                    Title           = Parser.Name,
                                    MainInstruction = "Login error",
                                    Content         = "The site didn't return any cookies. The username and password combination is most likely wrong.",
                                    CustomButtons   = new[] { "OK" }
                                });
                            return;
                        }

                        Dispatcher.Invoke((Action)(() => cookiesTextBox.Text = cookies));
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Error,
                                Title           = Parser.Name,
                                MainInstruction = "Login error",
                                Content         = "An error occured while logging in to the site.",
                                ExpandedInfo    = ex.Message,
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the grabCookiesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GrabCookiesButtonClick(object sender, RoutedEventArgs e)
        {
            var ccw = new CookieCatcherWindow(Parser);

            if (ccw.ShowDialog() == true) // Nullable<bool> == true
            {
                cookiesTextBox.Text = ccw.Cookies;
            }
        }

        /// <summary>
        /// Handles the Click event of the cancelButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the Click event of the saveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (_original.Get("user", string.Empty) != usernameTextBox.Text || _original.Get("pass", string.Empty) != passwordTextBox.Password)
            {
                if (string.IsNullOrWhiteSpace(usernameTextBox.Text) && string.IsNullOrWhiteSpace(passwordTextBox.Password))
                {
                    Settings.Remove(Parser.Name + " Login");
                }
                else
                {
                    Settings.Set(Parser.Name + " Login", Utils.Encrypt(usernameTextBox.Text + '\0' + passwordTextBox.Password, Parser.GetType().FullName + Environment.NewLine + Utils.GetUUID()));
                }
            }

            if (_original.Get("cookies") != cookiesTextBox.Text)
            {
                Settings.Set(Parser.Name + " Cookies", cookiesTextBox.Text);
            }

            DialogResult = true;
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

        /// <summary>
        /// Handles the TextChanged event of the usernameTextBox and passwordTextBox controls.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void UsernamePasswordTextBoxTextChanged(object sender, EventArgs e)
        {
            testLoginButton.IsEnabled = !string.IsNullOrWhiteSpace(usernameTextBox.Text) && !string.IsNullOrWhiteSpace(passwordTextBox.Password);
        }
    }
}
