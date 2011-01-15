namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides access to the default JSON settings file.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Gets or sets the key-value container.
        /// </summary>
        /// <value>The key-value container.</value>
        public static Dictionary<string, object> Keys { get; set; }

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
                Keys = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    File.ReadAllText(_jsFile)
                );
            }
            catch
            {
                Keys = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Retrieves the key from the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Get(string key)
        {
            return Get<string>(key);
        }

        /// <summary>
        /// Retrieves the key from the XML settings casting it to type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The type in which to return the setting's value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value to return if key was not found.</param>
        /// <returns>Stored value or default type value.</returns>
        public static T Get<T>(string key, T defaultValue = default(T))
        {
            object value;
            return Keys.TryGetValue(key, out value)
                   ? (T)value
                   : defaultValue;
        }

        /// <summary>
        /// Retrieves the list from the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or list with 0 items.</returns>
        public static string[] GetList(string key)
        {
            return GetList<string>(key);
        }

        /// <summary>
        /// Retrieves the list from the XML settings casting it to type <c>T[]</c>.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or list with 0 items.</returns>
        public static T[] GetList<T>(string key)
        {
            // This is tricky, because the object reference is returned from the array for
            // the specified key, so when ToList() or similar functions are called later in the
            // code, the object will change in the Keys dictionary too! This makes the type of
            // the array dynamic and hilarity ensues: we'll have to detect the type and
            // cast it back to T[] every. fucking. time.

            object value;
            return Keys.TryGetValue(key, out value)
                   ? value is T[]
                     ? (T[])value
                     : value is List<T>
                       ? ((List<T>)value).ToArray()
                       : value is JArray
                         ? ((JArray)value).Values<T>().ToArray()
                         : null
                   : new T[0];
        }

        /// <summary>
        /// Stores the key and value into the XML settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Set<T>(string key, T value)
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
