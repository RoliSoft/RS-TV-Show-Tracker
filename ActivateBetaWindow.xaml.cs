namespace RoliSoft.TVShowTracker
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Interaction logic for ActivateBetaWindow.xaml
    /// </summary>
    public partial class ActivateBetaWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivateBetaWindow"/> class.
        /// </summary>
        public ActivateBetaWindow()
        {
            InitializeComponent();
        }

        private Dictionary<string, string> _keys = new Dictionary<string, string>
            {
                { "538f04c5-f503-4745-9d16-dd4e59640e5d", "Search for Download Links" },
                { "c66c389b-4112-4cd3-bca9-5b9d21afeade", "RSS Generator for Automatic Downloaders" }
            };

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Handles the TextChanged event of the textBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void TextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_keys.ContainsKey(textBox.Text))
            {
                button.IsEnabled   = true;
                descr.Effect       = new BlurEffect();
                feature.Content    = _keys[textBox.Text];
                feature.Visibility = Visibility.Visible;

                button.Focus();
            }
            else
            {
                button.IsEnabled   = false;
                descr.Effect       = null;
                feature.Visibility = Visibility.Collapsed;
            }
        }
    }
}
