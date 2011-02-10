namespace RoliSoft.TVShowTracker
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Forms;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">More than one instance of the <see cref="T:System.Windows.Application"/> class is created per <see cref="T:System.AppDomain"/>.</exception>
        public App()
        {
            Thread.CurrentThread.CurrentCulture   = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            if (!Utils.Is7)
            {
                MessageBox.Show("This software currently doesn't support " + Utils.OS + ", only Windows 7 or newer. Because the underlying code would run on any operating system which can run CLI code, a universal interface will be developed sometime in the future.", Utils.OS + " is not supported", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}