namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// Interaction logic for ProxyWindow.xaml
    /// </summary>
    public partial class ProxyWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyWindow"/> class.
        /// </summary>
        public ProxyWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyWindow"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="address">The address.</param>
        public ProxyWindow(string name, string address)
        {
            InitializeComponent();

            Title = "Edit proxy";
            ProxyName = name;

            nameTextBox.Text    = name;
            addressTextBox.Text = address;
        }

        /// <summary>
        /// Gets or sets the name of the proxy.
        /// </summary>
        /// <value>
        /// The name of the proxy.
        /// </value>
        public string ProxyName { get; set; }

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
            if (string.IsNullOrWhiteSpace(nameTextBox.Text) || string.IsNullOrWhiteSpace(addressTextBox.Text)) return;

            try
            {
                new Uri(addressTextBox.Text);
            }
            catch
            {
                MessageBox.Show("The specified address is not a valid URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dict = Settings.Get<Dictionary<string, object>>("Proxies");

            if (ProxyName != null)
            {
                if (ProxyName != nameTextBox.Text && dict.ContainsKey(nameTextBox.Text))
                {
                    MessageBox.Show("A proxy by this name already exists.", "Name collision", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dict2 = Settings.Get<Dictionary<string, object>>("Proxied Domains");
                foreach (var prdmn in dict2.ToDictionary(k => k.Key, v => v.Value))
                {
                    if ((string)prdmn.Value == ProxyName)
                    {
                        dict2[prdmn.Key] = nameTextBox.Text;
                    }
                }

                dict.Remove(ProxyName);
            }
            else if (dict.ContainsKey(nameTextBox.Text))
            {
                MessageBox.Show("A proxy by this name already exists.", "Name collision", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            dict.Add(nameTextBox.Text, addressTextBox.Text);
            Settings.Set("Proxies", dict);

            DialogResult = true;
        }
    }
}
