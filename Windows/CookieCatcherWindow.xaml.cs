namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    using WebBrowser = System.Windows.Forms.WebBrowser;

    /// <summary>
    /// Interaction logic for CookieCatcherWindow.xaml
    /// </summary>
    public partial class CookieCatcherWindow
    {
        /// <summary>
        /// Gets or sets the engine edited on this window.
        /// </summary>
        /// <value>
        /// The engine.
        /// </value>
        public dynamic Engine { get; set; }

        /// <summary>
        /// Gets or sets the cookies.
        /// </summary>
        /// <value>
        /// The cookies.
        /// </value>
        public string Cookies { get; set; }

        private WebBrowser _webBrowser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="engine">The engine to grab cookies for.</param>
        public CookieCatcherWindow(dynamic engine)
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

            Title           = "Login to {0}".FormatWith((string)Engine.Name);
            urlTextBox.Text = Engine.Site;
            favicon.Source  = new BitmapImage(new Uri("http://getfavicon.appspot.com/http://{0}/".FormatWith(new Uri(Engine.Site).DnsSafeHost)));

            _webBrowser = new WebBrowser { ScriptErrorsSuppressed = true };
            _webBrowser.DocumentCompleted += WebBrowserDocumentCompleted;
            winFormsHost.Child = _webBrowser;

            _webBrowser.Navigate(Engine.Site);

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the DocumentCompleted event of the _webBrowser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WebBrowserDocumentCompleted(object sender, EventArgs e)
        {
            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (urlTextBox.Text != _webBrowser.Url.ToString())
            {
                favicon.Source = new BitmapImage(new Uri("http://getfavicon.appspot.com/http://{0}/".FormatWith(_webBrowser.Url.DnsSafeHost)));
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
            Cookies = _webBrowser.Document.Cookie ?? string.Empty;

            if (Engine.RequiredCookies != null && Engine.RequiredCookies.Length != 0 && !((string[])Engine.RequiredCookies).All(req => Regex.IsMatch(Cookies, @"(?:^|[\s;]){0}=".FormatWith(req), RegexOptions.IgnoreCase)))
            {
                var td = new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Required cookies not found",
                        MainInstruction         = Engine.Name,
                        Content                 = "Couldn't catch the required cookies for authentication.\r\nYou need to manually extract the following cookies from your browser:\r\n\r\n-> " + string.Join("\r\n-> ", Engine.RequiredCookies) + "\r\n\r\nAnd enter them in this way:\r\n\r\n" + string.Join("=VALUE; ", Engine.RequiredCookies) + "=VALUE",
                        AllowDialogCancellation = true,
                        CommandButtons          = new[] { "How to extract cookies manually", "Close" }
                    };

                var res = TaskDialog.Show(td);

                if (res.CommandButtonResult.HasValue && res.CommandButtonResult == 0)
                {
                    Utils.Run("http://lab.rolisoft.net/tvshowtracker/extract-cookies.html");
                }
            }

            DialogResult = true;
        }
    }
}
