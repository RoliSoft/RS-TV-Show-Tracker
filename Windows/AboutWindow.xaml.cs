namespace RoliSoft.TVShowTracker
{
    using System;

    using Microsoft.WindowsAPICodePack.Shell;
    
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : GlassWindow
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
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GlassWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (AeroGlassCompositionEnabled)
            {
                SetAeroGlassTransparency();
            }

            site.Content = string.Format(site.Content.ToString(), DateTime.Now.Year);

            info.Text = Signature.CompileTime.ToString("yyyy-MM-dd H:mm:ss")
                        + "\r\nv" + Signature.Version + (Signature.IsDebug ? "-debug" : string.Empty);
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the Label control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void LabelMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Utils.Run("http://lab.rolisoft.net/tvshowtracker.html");
        }
    }
}
