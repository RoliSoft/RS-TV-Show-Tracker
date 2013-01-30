namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using RoliSoft.TVShowTracker.Parsers.Senders;

    /// <summary>
    /// Interaction logic for SenderSettingsWindow.xaml
    /// </summary>
    public partial class SenderSettingsWindow
    {
        private string _id;
        private bool _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="SenderSettingsWindow"/> class.
        /// </summary>
        public SenderSettingsWindow()
        {
            InitializeComponent();

            foreach (var se in Extensibility.GetNewInstances<SenderEngine>())
            {
                var sp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Tag         = se
                    };

                sp.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri(se.Icon)),
                        Height = 16,
                        Width  = 16,
                        Margin = new Thickness(0, 0, 0, 0)
                    });

                sp.Children.Add(new Label
                    {
                        Content = " " + se.Name,
                        Padding = new Thickness(0)
                    });

                senderComboBox.Items.Add(sp);
            }

            senderComboBox.SelectedIndex = 0;
            nameTextBox.Watermark = GenerateDefaultName();
            _loaded = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxiedDomainWindow" /> class.
        /// </summary>
        /// <param name="id">The id.</param>
        public SenderSettingsWindow(string id)
        {
            InitializeComponent();

            _id = id;
            Title = "Edit a sender";

            var sobj = (Dictionary<string, object>)Settings.Get<Dictionary<string, object>>("Sender Destinations")[id];

            nameTextBox.Text = (string)sobj["Name"];
            locationTextBox.Text = (string)sobj["Location"];
            
            var i = 0;
            var s = 0;
            var e = default(SenderEngine);
            foreach (var se in Extensibility.GetNewInstances<SenderEngine>())
            {
                var sp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Tag         = se
                    };

                sp.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri(se.Icon)),
                        Height = 16,
                        Width  = 16,
                        Margin = new Thickness(0, 0, 0, 0)
                    });

                sp.Children.Add(new Label
                    {
                        Content = " " + se.Name,
                        Padding = new Thickness(0)
                    });

                senderComboBox.Items.Add(sp);

                if (se.Name == (string)sobj["Sender"])
                {
                    s = i;
                    e = se;
                }

                i++;
            }

            senderComboBox.SelectedIndex = s;

            if (e != null && sobj.ContainsKey("Login"))
            {
                var login = Utils.Decrypt(e, (string)sobj["Login"]);
                usernameTextBox.Text = login[0];
                passwordTextBox.Password = login[1];
            }

            _loaded = true;
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
            if (string.IsNullOrWhiteSpace(locationTextBox.Text)) return;

            var dict = Settings.Get<Dictionary<string, object>>("Sender Destinations");

            if (string.IsNullOrWhiteSpace(_id))
            {
                _id = Guid.NewGuid().ToString();
            }

            object obj;
            if (!dict.TryGetValue(_id, out obj))
            {
                obj = new Dictionary<string, object>();
            }

            var sdic = (Dictionary<string, object>)obj;

            sdic["Name"]     = string.IsNullOrWhiteSpace(nameTextBox.Text) ? GenerateDefaultName() : nameTextBox.Text.Trim();
            sdic["Sender"]   = ((SenderEngine)(((StackPanel)senderComboBox.SelectedItem).Tag)).Name;
            sdic["Location"] = locationTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(usernameTextBox.Text) || !string.IsNullOrEmpty(passwordTextBox.Password))
            {
                sdic["Login"] = Utils.Encrypt((SenderEngine)(((StackPanel)senderComboBox.SelectedItem).Tag), usernameTextBox.Text, passwordTextBox.Password);
            }
            else if (sdic.ContainsKey("Login"))
            {
                sdic.Remove("Login");
            }

            dict[_id] = sdic;

            Settings.Set("Sender Destinations", dict);

            var inst = (SenderEngine)Activator.CreateInstance(((SenderEngine)(((StackPanel)senderComboBox.SelectedItem).Tag)).GetType());

            inst.Title = (string)sdic["Name"];
            inst.Location = (string)sdic["Location"];

            if (sdic.ContainsKey("Login"))
            {
                inst.Login = new NetworkCredential(usernameTextBox.Text, passwordTextBox.Password);
            }

            MainWindow.Active.activeDownloadLinksPage.Senders[_id] = inst;

            DialogResult = true;
        }

        /// <summary>
        /// Handles the OnSelectionChanged event of the SenderComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void SenderComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;

            nameTextBox.Watermark = GenerateDefaultName();
        }

        /// <summary>
        /// Handles the OnTextChanged event of the LocationTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs" /> instance containing the event data.</param>
        private void LocationTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_loaded) return;

            nameTextBox.Watermark = GenerateDefaultName();
        }

        /// <summary>
        /// Generates the default name.
        /// </summary>
        /// <returns>
        /// Default name.
        /// </returns>
        private string GenerateDefaultName()
        {
            var str = Regex.Replace(((SenderEngine)(((StackPanel)senderComboBox.SelectedItem).Tag)).Name, @"\s(Web|Remote).+", string.Empty);

            Uri uri;
            if (!string.IsNullOrWhiteSpace(locationTextBox.Text) && Uri.TryCreate(locationTextBox.Text, UriKind.Absolute, out uri))
            {
                str += " at " + uri.DnsSafeHost;
            }

            return str;
        }
    }
}
