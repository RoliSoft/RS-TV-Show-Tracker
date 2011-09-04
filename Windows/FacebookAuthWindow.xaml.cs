namespace RoliSoft.TVShowTracker
{
    using System.Windows;
    using System.Windows.Navigation;

    using RoliSoft.TVShowTracker.Parsers.Social.Engines;

    /// <summary>
    /// Interaction logic for FacebookAuthWindow.xaml
    /// </summary>
    public partial class FacebookAuthWindow
    {
        private Facebook _facebook;

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public string Code { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookAuthWindow"/> class.
        /// </summary>
        /// <param name="facebook">The Facebook engine instance.</param>
        public FacebookAuthWindow(Facebook facebook)
        {
            InitializeComponent();

            _facebook = facebook;
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigate(_facebook.GenerateAuthorizationLink() + "&display=popup");
        }

        /// <summary>
        /// Handles the Navigating event of the webBrowser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigatingCancelEventArgs"/> instance containing the event data.</param>
        private void WebBrowserNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri.ToString().StartsWith("https://www.facebook.com/connect/login_success.html?"))
            {
                var parsed = Utils.ParseQueryString(e.Uri.Query.Substring(1));

                if (parsed.ContainsKey("code"))
                {
                    Code = parsed["code"];
                }

                e.Cancel = true;
                Close();
            }
        }
    }
}
