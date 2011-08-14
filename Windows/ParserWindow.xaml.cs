namespace RoliSoft.TVShowTracker
{
    using System.Windows;
    using System.Windows.Documents;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Interaction logic for ParserWindow.xaml
    /// </summary>
    public partial class ParserWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParserWindow"/> class.
        /// </summary>
        /// <param name="parser">The parser.</param>
        public ParserWindow(DownloadSearchEngine parser)
        {
            InitializeComponent();

            editTabItem.Header = parser.Name;
            Parser = parser;
        }

        /// <summary>
        /// Gets or sets the parser.
        /// </summary>
        /// <value>
        /// The parser.
        /// </value>
        public DownloadSearchEngine Parser { get; set; }

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

            cookiesTextBox.Text = Settings.Get(Parser.Name + " Cookies");
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
            Settings.Set(Parser.Name + " Cookies", cookiesTextBox.Text);

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
    }
}
