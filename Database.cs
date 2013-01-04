namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Parsers.ForeignTitles;
    using Parsers.Guides;

    /// <summary>
    /// Provides access to the default database.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Gets or sets the date when the data was last changed. This field is used for caching purposes, and it's not automatically updated by <c>Execute()</c>.
        /// </summary>
        /// <value>The date of last change.</value>
        public static DateTime DataChange { get; set; }

        /// <summary>
        /// Gets or sets the contents of the TV shows table in the database.
        /// </summary>
        /// <value>
        /// The contents of the TV shows table in the database.
        /// </value>
        public static Dictionary<int, TVShow> TVShows { get; set; }

        /// <summary>
        /// Gets or sets the contents of the episodes table in the database.
        /// </summary>
        /// <value>
        /// The contents of the episodes table in the database.
        /// </value>
        public static List<Episode> Episodes { get; set; }

        /// <summary>
        /// Gets or sets the contents of the episodes table in the database.
        /// </summary>
        /// <value>
        /// The contents of the episodes table in the database.
        /// </value>
        public static Dictionary<int, Episode> EpisodeByID { get; set; }

        private static string _dbPath = Path.Combine(Signature.FullPath, "db");

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Database()
        {
            if (string.IsNullOrWhiteSpace(Signature.FullPath))
            {
                return;
            }

            if (!Directory.Exists(_dbPath))
            {
                Directory.CreateDirectory(_dbPath);
            }

            LoadDatabase();
        }

        /// <summary>
        /// Loads the database files.
        /// </summary>
        public static void LoadDatabase()
        {
            DataChange = DateTime.Now;

            TVShows   = new Dictionary<int, TVShow>();
            Episodes  = new List<Episode>();

            foreach (var dir in Directory.EnumerateDirectories(_dbPath))
            {
                if (!Regex.IsMatch(Path.GetDirectoryName(dir), @"^\d+\-") || !File.Exists(Path.Combine(dir, "info")) || !File.Exists(Path.Combine(dir, "conf")))
                {
                    continue;
                }

                var show = TVShow.Load(dir);

                TVShows[show.ID] = show;
                Episodes.AddRange(show.Episodes);
            }
        }

        /// <summary>
        /// Retrieves the key from the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Setting(string key)
        {
            // TODO
            return string.Empty;
        }

        /// <summary>
        /// Retrieves the key from the show data table.
        /// </summary>
        /// <param name="id">The id of the show.</param>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string ShowData(int id, string key)
        {
            string value;

            if (TVShows[id].Data.TryGetValue(key, out value))
            {
                return value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Stores the key and value into the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Setting(string key, string value)
        {
            // TODO
        }

        /// <summary>
        /// Stores the key and value into the show data table.
        /// </summary>
        /// <param name="id">The id of the show.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void ShowData(int id, string key, string value)
        {
            // TODO commit
            TVShows[id].Data[key] = value;
        }

        /// <summary>
        /// Gets the ID of a show in the database.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <returns>ID of the show or -2^31.</returns>
        public static int GetShowID(string show)
        {
            var showid = TVShows.Values.Where(s => s.Name == show).Take(1).ToList();

            if (showid.Count != 0)
            {
                return showid[0].ID;
            }

            return int.MinValue;
        }
        
        /// <summary>
        /// Gets the ID of an episode in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The number of the episode.</param>
        /// <returns>ID of the show or -2^31.</returns>
        public static int GetEpisodeID(int id, int season, int episode)
        {
            var episodeid = Episodes.Where(ep => ep.ID == id && ep.Season == season && ep.Number == episode).Take(1).ToList();

            if (episodeid.Count != 0)
            {
                return episodeid[0].ID;
            }

            return int.MinValue;
        }

        /// <summary>
        /// Gets the name of the show used in scene releases.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <returns>Name of the show used in scene releases.</returns>
        public static Regex GetReleaseName(string show)
        {
            var release = TVShows.Values.Where(s => s.Name == show).Take(1).ToList();

            if (release.Count != 0 && !string.IsNullOrWhiteSpace(release[0].Release))
            {
                return new Regex(release[0].Release);
            }

            return ShowNames.Parser.GenerateTitleRegex(show);
        }

        /// <summary>
        /// Gets the foreign title of the specified show.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The ISO 639-1 code of the language.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked then a foreign title provider engine.</param>
        /// <param name="statusCallback">The method to call to report a status change.</param>
        /// <returns>Foreign title or <c>null</c>.</returns>
        public static string GetForeignTitle(int id, string language, bool askRemote = false, Action<string> statusCallback = null)
        {
            var title = ShowData(id, "title." + language);

            if (!string.IsNullOrWhiteSpace(title))
            {
                if (Regex.IsMatch(title, @"^!\d{10}$"))
                {
                    if ((DateTime.Now.ToUnixTimestamp() - int.Parse(title.Substring(1))) < 2629743)
                    {
                        // don't search again if the not-found-tag is not older than a month

                        return null;
                    }
                }
                else
                {
                    return title;
                }
            }

            if (!askRemote)
            {
                return null;
            }

            if (statusCallback != null)
            {
                statusCallback("Searching for the " + Languages.List[language] + " title of " + TVShows[id].Name +" on lab.rolisoft.net...");
            }

            var api = Remote.API.GetForeignTitle(TVShows[id].Name, language);

            if (api.Success && !string.IsNullOrWhiteSpace(api.Result))
            {
                if (api.Result == "!")
                {
                    ShowData(id, "title." + language, "!" + DateTime.Now.ToUnixTimestamp());

                    return null;
                }

                ShowData(id, "title." + language, api.Result);

                return api.Result;
            }

            var engine = Extensibility.GetNewInstances<ForeignTitleEngine>().FirstOrDefault(x => x.Language == language);

            if (engine != null)
            {
                if (statusCallback != null)
                {
                    statusCallback("Searching for the " + Languages.List[language] + " title of " + TVShows[id].Name + " on " + engine.Name + "...");
                }

                var search = engine.Search(TVShows[id].Name);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    ShowData(id, "title." + language, search);

                    new Thread(() => Remote.API.SetForeignTitle(TVShows[id].Name, search, language)).Start();

                    return search;
                }
            }

            ShowData(id, "title." + language, "!" + DateTime.Now.ToUnixTimestamp());

            new Thread(() => Remote.API.SetForeignTitle(TVShows[id].Name, "!", language)).Start();

            return null;
        }
    }
}
