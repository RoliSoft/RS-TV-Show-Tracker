namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindow"/> class.
        /// </summary>
        public AboutWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the GlassWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private unsafe void GlassWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (AeroGlassCompositionEnabled)
            {
                SetAeroGlassTransparency();
            }

            version.Text        = Signature.VersionFormatted;
            compile.Text        = Signature.CompileTime.ToString("yyyy-MM-dd H:mm:ss");
            github1.NavigateUri = new Uri("https://github.com/RoliSoft/RS-TV-Show-Tracker/commits/v" + Signature.Version);

            if (Properties.Resources.GitRevision.Length > 7)
            {
                revision.Text       = Properties.Resources.GitRevision.Substring(0, 8);
                revision.ToolTip    = Properties.Resources.GitRevision.Trim();
                github2.NavigateUri = new Uri("https://github.com/RoliSoft/RS-TV-Show-Tracker/tree/" + revision.ToolTip);
            }

            var α = (UInt64)2.9555336418361426e16m;
            var β = (UInt64)3.2651535392374867e16m;
            var γ = new string((char*)&α, (int)(α%2), (int)(α%6));
            var δ = new string((char*)&β, (int)(β%1), (int)(β%17));
            var ε = γ + δ;

            site.Content = "© " + DateTime.Now.Year + " " + ε + " – lab." + ε.ToLower() + ".net";
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the Label control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void LabelMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Utils.Run("http://lab.rolisoft.net/tvshowtracker.html");
        }

        /// <summary>
        /// Handles the Click event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            Utils.Run(((Hyperlink)sender).NavigateUri.ToString());
        }

        /// <summary>
        /// Handles the MouseEnter event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void HyperlinkMouseEnter(object sender, MouseEventArgs e)
        {
            ((Hyperlink)sender).TextDecorations = TextDecorations.Underline;
        }

        /// <summary>
        /// Handles the MouseLeave event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void HyperlinkMouseLeave(object sender, MouseEventArgs e)
        {
            ((Hyperlink)sender).TextDecorations = null;
        }
    }
}
