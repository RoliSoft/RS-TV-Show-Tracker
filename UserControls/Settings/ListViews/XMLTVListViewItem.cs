namespace RoliSoft.TVShowTracker
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an XMLTV on the list view.
    /// </summary>
    public class XMLTVListViewItem
    {
        /// <summary>
        /// Gets or sets the config object.
        /// </summary>
        /// <value>
        /// The config object.
        /// </value>
        public Dictionary<string, object> Config { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>
        /// The file.
        /// </value>
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the date of the file's last modification.
        /// </summary>
        /// <value>
        /// The date of the file's last modification.
        /// </value>
        public string Update { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>
        /// The icon.
        /// </value>
        public string Icon { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XMLTVListViewItem"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        public XMLTVListViewItem(Dictionary<string, object> config)
        {
            Config = config;
            Name   = (string)config["Name"];
            File   = System.IO.Path.GetFileName((string)config["File"]);
            Update = System.IO.File.Exists((string)config["File"]) ? System.IO.File.GetLastWriteTime((string)config["File"]).ToString("yyyy'-'MM'-'dd HH':'mm':'ss") : "File not found!";
            Icon   = config.ContainsKey("Language") && config["Language"] is string ? "pack://application:,,,/RSTVShowTracker;component/Images/flag-" + (string)config["Language"] + ".png" : "pack://application:,,,/RSTVShowTracker;component/Images/guides.png";
        }
    }
}
