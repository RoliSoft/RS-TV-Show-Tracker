namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using System.Linq;

    /// <summary>
    /// Provides access to the default XML settings file.
    /// </summary>
    public static class Settings
    {
        private static readonly List<Setting> Keys = new List<Setting>();
        private static readonly XmlSerializer Serializer = new XmlSerializer(Keys.GetType(), new XmlRootAttribute("Settings"));

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Settings()
        {
            try
            {
                using (var xml = File.OpenRead(@"C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\Settings.xml"))
                {
                    Keys = Serializer.Deserialize(xml) as List<Setting>;
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Retrieves the key from the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Get(string key)
        {
            var setting = Keys.Where(s => s.Key == key);

            return setting.Count() != 0
                   ? setting.First().Value
                   : string.Empty;
        }

        /// <summary>
        /// Stores the key and value into the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Set(string key, string value)
        {
            var setting = Keys.Where(s => s.Key == key);
            
            if (setting.Count() != 0)
            {
                setting.First().Value = value;
            }
            else
            {
                Keys.Add(new Setting { Key = key, Value = value });
            }

            Save();
        }

        /// <summary>
        /// Saves the XML settings into the file.
        /// </summary>
        public static void Save()
        {
            using (var xml = File.Open(@"C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\Settings.xml", FileMode.Create))
            {
                Serializer.Serialize(xml, Keys);
            }
        }
    }

    /// <summary>
    /// Represents a serializable setting.
    /// </summary>
    [Serializable]
    public class Setting
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        [XmlAttribute]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }
    }
}
