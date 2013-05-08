namespace RoliSoft.TVShowTracker
{
    using System.Windows;

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

            emailTextBox.Text = Signature.ActivationUser;
            keyTextBox.Text   = Signature.ActivationKey;

            if (Signature.IsActivated)
            {
                emailTextBox.IsEnabled = keyTextBox.IsEnabled = false;
            }
        }
    }
}
