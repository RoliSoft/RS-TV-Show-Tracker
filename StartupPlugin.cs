namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Represents a plugin which will be run when the software starts and will have access to
    /// any internal object instances, including the User Interface, Database, Settings, etc...
    /// </summary>
    public abstract class StartupPlugin : IPlugin
    {
        /// <summary>
        /// Gets or sets the name of the plugin.
        /// </summary>
        /// <value>The name of the plugin.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the URL to the plugin's icon.
        /// </summary>
        /// <value>The location of the plugin's icon.</value>
        public virtual string Icon
        {
            get
            {
                return "pack://application:,,,/RSTVShowTracker;component/Images/dll.gif";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public virtual string Developer
        {
            get
            {
                var company = GetType().Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);

                if (company.Length != 0)
                {
                    return ((AssemblyCompanyAttribute)company[0]).Company;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public virtual Version Version
        {
            get
            {
                var version = GetType().Assembly.GetCustomAttributes(typeof(AssemblyVersionAttribute), true);

                if (version.Length != 0)
                {
                    return Version.Parse(((AssemblyVersionAttribute)version[0]).Version);
                }

                return new Version(1, 0);
            }
        }

        /// <summary>
        /// Called in the <c>WindowLoaded</c> event of the active <c>MainWindow</c>.
        /// </summary>
        public abstract void Run();
    }
}
