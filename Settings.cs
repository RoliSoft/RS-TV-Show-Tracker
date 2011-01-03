namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    /// <summary>
    /// Provides access to the default JSON settings file.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Gets or sets the key-value container.
        /// </summary>
        /// <value>The key-value container.</value>
        public static Dictionary<string, string> Keys { get; set; }

        private static readonly string _jsFile;

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Settings()
        {
            if (string.IsNullOrWhiteSpace(Signature.FullPath))
            {
                return;
            }

            _jsFile = Path.Combine(Signature.FullPath, "Settings.json");

            // hackity-hack :)
            if (Environment.MachineName == "ROLISOFT-PC" && File.Exists(Path.Combine(Signature.FullPath, ".hack")))
            {
                _jsFile = @"C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\Settings.json";
            }

            try
            {
                Keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    File.ReadAllText(_jsFile)
                );
            }
            catch
            {
                Keys = new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// Retrieves the key from the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Get(string key)
        {
            string value;
            return Keys.TryGetValue(key, out value)
                   ? value
                   : string.Empty;
        }

        /// <summary>
        /// Stores the key and value into the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Set(string key, string value)
        {
            Keys[key] = value;
            Save();
        }

        /// <summary>
        /// Saves the XML settings into the file.
        /// </summary>
        public static void Save()
        {
            File.WriteAllText(
                _jsFile,
                JsonConvert.SerializeObject(Keys, Formatting.Indented)
            );
        }
    }
}
