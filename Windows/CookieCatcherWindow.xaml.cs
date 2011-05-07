namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using WebBrowser = System.Windows.Forms.WebBrowser;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Interaction logic for CookieCatcherWindow.xaml
    /// </summary>
    public partial class CookieCatcherWindow
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
            var cookies = _webBrowser.Document.Cookie ?? string.Empty;

            if (Engine.RequiredCookies != null && Engine.RequiredCookies.Length != 0 && !Engine.RequiredCookies.All(req => Regex.IsMatch(cookies, @"(?:^|[\s;]){0}=".FormatWith(req), RegexOptions.IgnoreCase)))
            {
                var td = new TaskDialog
                    {
                        Icon            = TaskDialogStandardIcon.Error,
                        Caption         = "Required cookies not found",
                        InstructionText = Engine.Name,
                        Text            = "Couldn't catch the required cookies for authentication.\r\nYou need to manually extract the following cookies from your browser:\r\n\r\n-> " + string.Join("\r\n-> ", Engine.RequiredCookies) + "\r\n\r\nAnd enter them in this way:\r\n\r\n" + string.Join("=VALUE; ", Engine.RequiredCookies) + "=VALUE",
                        Cancelable      = true,
                        StandardButtons = TaskDialogStandardButtons.Ok
                    };

                var fd = new TaskDialogCommandLink { Text = "How to extract cookies manually" };
                fd.Click += (s, r) =>
                    {
                        td.Close();
                        Utils.Run("http://lab.rolisoft.net/tvshowtracker/extract-cookies.html");
                    };

                td.Controls.Add(fd);
                td.Show();
            }

            Settings.Set(Engine.Name + " Cookies", cookies);
            DialogResult = true;
        }
    }
}
