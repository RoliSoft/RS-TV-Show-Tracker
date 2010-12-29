namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Windows;
    using System.Windows.Media.Imaging;

    using WebBrowser = System.Windows.Forms.WebBrowser;

    using Microsoft.WindowsAPICodePack.Shell;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Interaction logic for CookieCatcherWindow.xaml
    /// </summary>
    public partial class CookieCatcherWindow : GlassWindow
    {
        /// <summary>
        /// Gets or sets the engine edited on this window.
        /// </summary>
        /// <value>The engine.</value>
        public DownloadSearchEngine Engine { get; set; }

        private WebBrowser _webBrowser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="engine">The engine to grab cookies for.</param>
        public CookieCatcherWindow(DownloadSearchEngine engine)
        {
            InitializeComponent();

            Engine = engine;
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
                ExcludeElementFromAeroGlass(border);
                SetAeroGlassTransparency();
            }

            Title           = "Login to {0}".FormatWith(Engine.Name);
            urlTextBox.Text = Engine.Site;
            favicon.Source  = new BitmapImage(new Uri("http://www.google.com/s2/favicons?domain={0}".FormatWith(new Uri(Engine.Site).DnsSafeHost)));

            _webBrowser = new WebBrowser { ScriptErrorsSuppressed = true };
            _webBrowser.DocumentCompleted += WebBrowserDocumentCompleted;
            winFormsHost.Child = _webBrowser;

            _webBrowser.Navigate(Engine.Site);
        }

        /// <summary>
        /// Handles the DocumentCompleted event of the _webBrowser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WebBrowserDocumentCompleted(object sender, EventArgs e)
        {
            if (urlTextBox.Text != _webBrowser.Url.ToString())
            {
                favicon.Source = new BitmapImage(new Uri("http://www.google.com/s2/favicons?domain={0}".FormatWith(_webBrowser.Url.DnsSafeHost)));
            }

            urlTextBox.Text = _webBrowser.Url.ToString();
        }

        /// <summary>
        /// Handles the Click event of the grabButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GrabButtonClick(object sender, RoutedEventArgs e)
        {
            Settings.Set(Engine.Name + " Cookies", _webBrowser.Document.Cookie.Trim());
            DialogResult = true;
        }
    }
}
