namespace RoliSoft.TVShowTracker
{
    using System;
    using System.IO;
    using System.Net.Cache;
    using System.Windows;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Interaction logic for CaptchaWindow.xaml
    /// </summary>
    public partial class CaptchaWindow
    {
        /// <summary>
        /// Gets the solution entered by the user.
        /// </summary>
        public string Solution { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CaptchaWindow"/> class.
        /// </summary>
        /// <param name="site">The site which thinks they've solved all their bot problems.</param>
        /// <param name="url">The URL to the captcha image.</param>
        /// <param name="width">The width of the captcha image.</param>
        /// <param name="height">The height of the captcha image.</param>
        public CaptchaWindow(string site, string url, int width, int height)
        {
            InitializeComponent();

            Title              += " for " + site;
            captchaImage.Source = new BitmapImage(new Uri(url), new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore));
            captchaImage.Width  = width;
            captchaImage.Height = height;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CaptchaWindow"/> class.
        /// </summary>
        /// <param name="site">The site which thinks they've solved all their bot problems.</param>
        /// <param name="image">The byte array of the captcha image.</param>
        /// <param name="width">The width of the captcha image.</param>
        /// <param name="height">The height of the captcha image.</param>
        public CaptchaWindow(string site, byte[] image, int width, int height)
        {
            InitializeComponent();

            var ms  = new MemoryStream(image);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.EndInit();

            Title              += " for " + site;
            captchaImage.Source = bmp;
            captchaImage.Width  = width;
            captchaImage.Height = height;
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Activate();
        }

        /// <summary>
        /// Handles the TextChanged event of the captchaTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void CaptchaTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            sendButton.IsEnabled = !string.IsNullOrWhiteSpace(captchaTextBox.Text);
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
        /// Handles the Click event of the sendButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            Solution = captchaTextBox.Text;
            DialogResult = true;
        }
    }
}
