namespace RoliSoft.TVShowTracker
{
    using System.Collections.Generic;
    using System.Windows;

    /// <summary>
    /// Interaction logic for ProxiedDomainWindow.xaml
    /// </summary>
    public partial class ProxiedDomainWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxiedDomainWindow"/> class.
        /// </summary>
        public ProxiedDomainWindow()
        {
            InitializeComponent();

            foreach (var pr in Settings.Get<Dictionary<string, object>>("Proxies"))
            {
                proxyComboBox.Items.Add(pr.Key);
            }

            proxyComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxiedDomainWindow"/> class.
        /// </summary>
        /// <param name="domain">The domain.</param>
        /// <param name="proxy">The proxy.</param>
        public ProxiedDomainWindow(string domain, string proxy)
        {
            InitializeComponent();

            Title = "Edit proxied domain";
            DomainName = domain;

            nameTextBox.Text = domain;

            var i = 0;
            var s = 0;
            foreach (var pr in Settings.Get<Dictionary<string, object>>("Proxies"))
            {
                proxyComboBox.Items.Add(pr.Key);

                if (pr.Key == proxy)
                {
                    s = i;
                }

                i++;
            }

            proxyComboBox.SelectedIndex = s;
        }

        /// <summary>
        /// Gets or sets the name of the domain.
        /// </summary>
        /// <value>
        /// The name of the domain.
        /// </value>
        public string DomainName { get; set; }

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
            if (string.IsNullOrWhiteSpace(nameTextBox.Text)) return;

            var dict = Settings.Get<Dictionary<string, object>>("Proxied Domains");

            if (DomainName != null)
            {
                if (DomainName != nameTextBox.Text && dict.ContainsKey(nameTextBox.Text))
                {
                    MessageBox.Show("A proxied domain by this name already exists.", "Domain collision", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                dict.Remove(DomainName);
            }
            else if (dict.ContainsKey(nameTextBox.Text))
            {
                MessageBox.Show("A proxied domain by this name already exists.", "Domain collision", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            dict.Add(nameTextBox.Text, (string)proxyComboBox.SelectedItem);
            Settings.Save();

            DialogResult = true;
        }
    }
}
