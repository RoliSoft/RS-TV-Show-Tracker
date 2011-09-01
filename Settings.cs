namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using RoliSoft.TVShowTracker.Parsers.Subtitles;

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
                Keys = (Dictionary<string, object>)ConvertJToNet(
                    JObject.Parse(
                        File.ReadAllText(_jsFile)
                    )
                );
            }
            catch
            {
                Keys = new Dictionary<string, object>();
            }

            // set defaults, if they're missing

            if (!Keys.ContainsKey("Active Trackers"))
            {
                Keys["Active Trackers"] = new List<string>
                    {
                        "The Pirate Bay", "DirectDownload.tv", "Zunox", "BinSearch", "NZBClub"
                    };
            }

            if (!Keys.ContainsKey("Tracker Order"))
            {
                Keys["Tracker Order"] = new List<string>
                    {
                        "BroadcasTheNet", "Tv Torrents Ro", "TvStore", "BitMeTV", "TvTorrents", "FileList",
                        "nCore", "bitHUmen", "The Pirate Bay", "DirectDownload.tv", "Zunox", "SceneReleases",
                        "ReleaseLog", "BinSearch", "NZBClub", "Twilight", "PhazeDDL"
                    };
            }

            if (!Keys.ContainsKey("Active Subtitle Sites"))
            {
                Keys["Active Subtitle Sites"] = typeof(SubtitleSearchEngine)
                                                .GetDerivedTypes()
                                                .Select(type => (Activator.CreateInstance(type) as SubtitleSearchEngine).Name)
                                                .ToList();
            }

            if (!Keys.ContainsKey("Active Subtitle Languages"))
            {
                Keys["Active Subtitle Languages"] = new List<string> { "en" };
            }

            // update download path to an array
            // -> revision bd869ed9836a6d1c846a17586855d51b5ce092c2

            if (Keys.ContainsKey("Download Path"))
            {
                Keys["Download Paths"] = new List<string>
                    {
                        (string)Keys["Download Path"]
                    };

                Keys.Remove("Download Path");
            }
        }

        /// <summary>
        /// Converts a <c>JObject</c> to a CLR object.
        /// Consult the source code for the list of supported <c>JTokenType</c>s.
        /// </summary>
        /// <param name="obj">The deserialized JavaScript object.</param>
        /// <returns>
        /// Matching CLR object.
        /// </returns>
        private static object ConvertJToNet(JToken obj)
        {
            switch (obj.Type)
            {
                case JTokenType.String:
                    return (string)obj;

                case JTokenType.Boolean:
                    return (bool)obj;

                case JTokenType.Integer:
                    return (int)obj;

                case JTokenType.Float:
                    return (double)obj;

                case JTokenType.Array:
                    Type lastType = null;
                    bool typeDiff = false;

                    var array = new List<object>();

                    foreach (var item in (JArray)obj)
                    {
                        var nitem = ConvertJToNet(item);
                        var ntype = nitem.GetType();
                        array.Add(nitem);

                        if (lastType != null && lastType != ntype)
                        {
                            typeDiff = true;
                        }

                        lastType = ntype;
                    }

                    if (array.Count == 0)
                    {
                        return new List<string>();
                    }

                    if (!typeDiff && lastType != null)
                    {
                        switch (lastType.FullName)
                        {
                            case "System.String":
                                return array.Cast<string>().ToList();

                            case "System.Integer":
                                return array.Cast<int>().ToList();

                            case "System.Double":
                                return array.Cast<double>().ToList();
                        }
                    }
                    
                    return array;

                case JTokenType.Object:
                    var dict = new Dictionary<string, object>();

                    foreach (var item in (JObject)obj)
                    {
                        dict.Add(item.Key, ConvertJToNet(item.Value));
                    }

                    return dict;

                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Retrieves the key from the JSON settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Get(string key)
        {
            return Get<string>(key);
        }

        /// <summary>
        /// Retrieves the key from the JSON settings casting it to type <c>T</c>,
        /// or if the key doesn't exist, the default value will be returned for value types
        /// and a new instance will be returned for reference types.
        /// </summary>
        /// <typeparam name="T">The type in which to return the setting's value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value to return if key was not found.</param>
        /// <returns>
        /// Stored value or default value/new instance.
        /// </returns>
        public static T Get<T>(string key, T defaultValue = default(T))
        {
            object value;

            if (Keys.TryGetValue(key, out value) && value != null)
            {
                return (T)value;
            }

            if (!typeof(T).IsValueType && !(defaultValue is string) && defaultValue != null)
            {
                return Activator.CreateInstance<T>();
            }

            return defaultValue;
        }

        /// <summary>
        /// Stores the key and value into the JSON settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Set<T>(string key, T value)
        {
            if (value is string && string.IsNullOrWhiteSpace(value as string))
            {
                Keys.Remove(key);
            }
            else
            {
                Keys[key] = value;
            }

            Save();
        }

        /// <summary>
        /// Toggles the key's current boolean value in the JSON settings.
        /// </summary>
        /// <param name="key">The key.</param>
        public static void Toggle(string key)
        {
            Keys[key] = !Get<bool>(key);
            Save();
        }

        /// <summary>
        /// Removes the key and value from the JSON settings.
        /// </summary>
        /// <param name="key">The key.</param>
        public static void Remove(string key)
        {
            Keys.Remove(key);
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
