namespace RoliSoft.TVShowTracker.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Represents a plugin which provides support for one or more scripting languages.
    /// </summary>
    public abstract class ScriptingPlugin : IPlugin
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
        /// Gets the extension of the files handled by this plugin.
        /// </summary>
        /// <value>The extension of the files handled by this plugin.</value>
        public abstract string Extension { get; }

        /// <summary>
        /// Loads the specified script and extracts the exposed types.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        /// A list of types.
        /// </returns>
        public abstract List<ExternalType> LoadScript(string file);

        /// <summary>
        /// Determines whether the exposed type by the specified script derives from the specified type.
        /// </summary>
        /// <param name="externalType">The type exposed by the script.</param>
        /// <param name="internalType">The internal abstract type which the external type derives from.</param>
        /// <returns>
        ///   <c>true</c> if it is a subclass of the specified type; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsCompatible(ExternalType externalType, Type internalType);

        /// <summary>
        /// Creates a new instance from the specified type.
        /// </summary>
        /// <typeparam name="T">The internal abstract type to cast the new instance to.</typeparam>
        /// <param name="type">The type exposed by the script.</param>
        /// <returns>
        /// A new instance from the specified type.
        /// </returns>
        public abstract T CreateInstance<T>(ExternalType type);
    }
}
