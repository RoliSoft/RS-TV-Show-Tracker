namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ProtoBuf;

    /// <summary>
    /// Represents a plugin which maps your TV shows to your local programming
    /// and provides a listing of upcoming episodes in your area.
    /// </summary>
    public abstract class LocalProgrammingPlugin : IPlugin
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/table-select-row.png";
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
        /// Gets a list of available programming configurations.
        /// </summary>
        /// <returns>The list of available programming configurations.</returns>
        public abstract IEnumerable<Configuration> GetConfigurations(); 

        /// <summary>
        /// Gets a list of upcoming episodes in your area ordered by airdate.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns>List of upcoming episodes in your area.</returns>
        public abstract IEnumerable<Programme> GetListing(Configuration config);

        /// <summary>
        /// Represents a programming configuration.
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the plugin.
            /// </summary>
            /// <value>
            /// The plugin.
            /// </value>
            public LocalProgrammingPlugin Plugin { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Configuration"/> class.
            /// </summary>
            /// <param name="plugin">The plugin.</param>
            public Configuration(LocalProgrammingPlugin plugin)
            {
                Plugin = plugin;
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return "Upcoming episodes in " + Name;
            }
        }

        /// <summary>
        /// Represents a programming in the listing.
        /// </summary>
        [ProtoContract]
        public class Programme
        {
            /// <summary>
            /// Gets or sets the show.
            /// </summary>
            /// <value>
            /// The show.
            /// </value>
            [ProtoMember(1)]
            public string Show { get; set; }

            /// <summary>
            /// Gets or sets the episode number.
            /// </summary>
            /// <value>
            /// The episode number.
            /// </value>
            [ProtoMember(2)]
            public string Number { get; set; }

            /// <summary>
            /// Gets or sets the description.
            /// </summary>
            /// <value>
            /// The description.
            /// </value>
            [ProtoMember(3)]
            public string Description { get; set; }

            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            /// <value>
            /// The channel.
            /// </value>
            [ProtoMember(4)]
            public string Channel { get; set; }

            /// <summary>
            /// Gets or sets the airdate.
            /// </summary>
            /// <value>
            /// The airdate.
            /// </value>
            [ProtoMember(5)]
            public DateTime Airdate { get; set; }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return Show + " [" + Channel + "]";
            }
        }
    }
}
