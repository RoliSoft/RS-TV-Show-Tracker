namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using RoliSoft.TVShowTracker.Parsers.ForeignTitles;
    using RoliSoft.TVShowTracker.Parsers.Guides;

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
        /// Gets or sets the key-value store associated with this database.
        /// </summary>
        /// <value>The data.</value>
        public static Dictionary<string, string> Data { get; set; }

        /// <summary>
        /// Gets or sets the database directory.
        /// </summary>
        /// <value>
        /// The database directory.
        /// </value>
        public static readonly string DataPath = Path.Combine(Signature.FullPath, "db");

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Database()
        {
            if (string.IsNullOrWhiteSpace(Signature.FullPath))
            {
                return;
            }

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            LoadDatabase();
        }

        /// <summary>
        /// Loads the database files.
        /// </summary>
        public static void LoadDatabase()
        {
            DataChange = DateTime.Now;
            TVShows    = new Dictionary<int, TVShow>();

            foreach (var dir in Directory.EnumerateDirectories(DataPath))
            {
                if (!File.Exists(Path.Combine(dir, "info")) || !File.Exists(Path.Combine(dir, "conf")))
                {
                    continue;
                }

                try
                {
                    var show = TVShow.Load(dir);

                    TVShows[show.ID] = show;
                }
                catch (Exception ex)
                {
                    MainWindow.HandleUnexpectedException(new Exception("Couldn't load db\\" + Path.GetFileName(dir) + " due to an error.", ex));
                }
            }
        }

        /// <summary>
        /// Retrieves the key from the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Setting(string key)
        {
            if (Data == null)
            {
                using (var fs = File.OpenRead(Path.Combine(DataPath, "conf")))
                using (var br = new BinaryReader(fs))
                {
                    var dver = br.ReadByte();
                    var dupd = br.ReadUInt32();
                    var dcnt = br.ReadUInt16();

                    Data = new Dictionary<string, string>();

                    for (var i = 0; i < dcnt; i++)
                    {
                        Data[br.ReadString()] = br.ReadString();
                    }
                }
            }

            string value;
            if (Data.TryGetValue(key, out value))
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
            Data[key] = value;

            using (var fs = File.OpenWrite(Path.Combine(DataPath, "conf")))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((ushort)Data.Count);

                foreach (var kv in Data)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value);
                }
            }
        }

        /// <summary>
        /// Saves a custom dictionary to the specified file in the database.
        /// </summary>
        /// <param name="name">The name in the database.</param>
        /// <param name="dict">The dictionary to serialize.</param>
        public static void SaveDict(string name, Dictionary<int, string> dict)
        {
            var path = Path.Combine(Signature.FullPath, name);

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)dict.Count);

                foreach (var kv in dict)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Saves a custom dictionary to the specified file in the database.
        /// </summary>
        /// <param name="name">The name in the database.</param>
        /// <param name="dict">The dictionary to serialize.</param>
        public static void SaveDict(string name, Dictionary<string, int> dict)
        {
            var path = Path.Combine(Signature.FullPath, name);

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)dict.Count);

                foreach (var kv in dict)
                {
                    bw.Write(kv.Key ?? string.Empty);
                    bw.Write(kv.Value);
                }
            }
        }
        
        /// <summary>
        /// Saves a custom dictionary to the specified file in the database.
        /// </summary>
        /// <param name="name">The name in the database.</param>
        /// <param name="dict">The dictionary to serialize.</param>
        public static void SaveDict(string name, Dictionary<string, string> dict)
        {
            var path = Path.Combine(Signature.FullPath, name);

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)dict.Count);

                foreach (var kv in dict)
                {
                    bw.Write(kv.Key ?? string.Empty);
                    bw.Write(kv.Value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Loads a custom dictionary from the specified file in the database.
        /// </summary>
        /// <param name="name">The name in the database.</param>
        /// <returns>
        /// Deserialized dictionary.
        /// </returns>
        public static Dictionary<string, string> LoadDictStrStr(string name)
        {
            using (var fs = File.OpenRead(Path.Combine(Path.GetDirectoryName(DataPath), name)))
            using (var br = new BinaryReader(fs))
            {
                var ver = br.ReadByte();
                var upd = br.ReadUInt32();
                var cnt = br.ReadUInt32();
                var dat = new Dictionary<string, string>();

                for (var i = 0; i < cnt; i++)
                {
                    dat[br.ReadString()] = br.ReadString();
                }

                return dat;
            }
        }

        /// <summary>
        /// Loads a custom dictionary from the specified file in the database.
        /// </summary>
        /// <param name="name">The name in the database.</param>
        /// <returns>
        /// Deserialized dictionary.
        /// </returns>
        public static Dictionary<int, string> LoadDictIntStr(string name)
        {
            using (var fs = File.OpenRead(Path.Combine(Path.GetDirectoryName(DataPath), name)))
            using (var br = new BinaryReader(fs))
            {
                var ver = br.ReadByte();
                var upd = br.ReadUInt32();
                var cnt = br.ReadUInt32();
                var dat = new Dictionary<int, string>();

                for (var i = 0; i < cnt; i++)
                {
                    dat[br.ReadInt32()] = br.ReadString();
                }

                return dat;
            }
        }

        /// <summary>
        /// Loads a custom dictionary from the specified file in the database.
        /// </summary>
        /// <param name="name">The name in the database.</param>
        /// <returns>
        /// Deserialized dictionary.
        /// </returns>
        public static Dictionary<string, int> LoadDictStrInt(string name)
        {
            using (var fs = File.OpenRead(Path.Combine(Path.GetDirectoryName(DataPath), name)))
            using (var br = new BinaryReader(fs))
            {
                var ver = br.ReadByte();
                var upd = br.ReadUInt32();
                var cnt = br.ReadUInt32();
                var dat = new Dictionary<string, int>();

                for (var i = 0; i < cnt; i++)
                {
                    dat[br.ReadString()] = br.ReadInt32();
                }

                return dat;
            }
        }

        /// <summary>
        /// Adds the specified TV show to the database.
        /// </summary>
        /// <param name="gname">The guide to get the information from.</param>
        /// <param name="guuid">The ID on the guide.</param>
        /// <param name="glang">The language on the guide.</param>
        /// <param name="callback">The status callback.</param>
        /// <returns>
        /// Added TV show or <c>null</c>.
        /// </returns>
        public static TVShow Add(string gname, string guuid, string glang, Action<int, string> callback = null)
        {
            if (callback != null)
            {
                callback(0, "Downloading guide...");
            }

            Guide guide;
            try
            {
                guide = Updater.CreateGuide(gname);
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not get guide object of type " + gname + ".");
                }

                return null;
            }

            TVShow tv;
            try
            {
                tv = guide.GetData(guuid, glang);

                if (tv.Episodes.Count == 0)
                {
                    if (callback != null)
                    {
                        callback(-1, "There aren't any episodes associated to " + tv.Title + " on this guide.");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    return null;
                }
                
                if (callback != null)
                {
                    callback(-1, "Couldn't download the episode listing and associated informations due to an unexpected error:" + Environment.NewLine + ex.Message);
                }

                return null;
            }

            if (TVShows.Values.FirstOrDefault(x => x.Title == tv.Title) != null)
            {
                if (callback != null)
                {
                    callback(-1, tv.Title + " is already in your database.");
                }

                return null;
            }

            foreach (var tvs in TVShows.Values)
            {
                tvs.RowID++;
                tvs.SaveData();
            }

            tv.RowID       = 0;
            tv.ID          = TVShows.Values.Max(x => x.ID) + 1;
            tv.Data        = new Dictionary<string, string>();
            tv.Directory   = Path.Combine(DataPath, Utils.CreateSlug(tv.Title, false));
            tv.EpisodeByID = new Dictionary<int, Episode>();

            if (Directory.Exists(tv.Directory))
            {
                tv.Directory += "-" + tv.Source.ToLower();
            }

            if (Directory.Exists(tv.Directory))
            {
                tv.Directory += "-" + Utils.Rand.Next();
            }

            try
            {
                Directory.CreateDirectory(tv.Directory);
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not create database for " + tv.Title + ".");
                }

                return null;
            }

            foreach (var ep in tv.Episodes)
            {
                ep.Show = tv;
                ep.ID   = ep.Number + (ep.Season * 1000) + (tv.ID * 1000 * 1000);

                tv.EpisodeByID[ep.Number + (ep.Season * 1000)] = ep;

                if (!string.IsNullOrWhiteSpace(tv.AirTime) && ep.Airdate != Utils.UnixEpoch)
                {
                    ep.Airdate = DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone(tv.TimeZone);
                }
            }

            try
            {
                tv.Save();
                tv.SaveTracking();
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not save database for " + tv.Title + ".");
                }

                return null;
            }

            TVShows[tv.ID] = tv;
            DataChange = DateTime.Now;

            if (callback != null)
            {
                callback(1, "Added " + tv.Title + ".");
            }

            return tv;
        }

        /// <summary>
        /// Updates the specified TV show in the database.
        /// </summary>
        /// <param name="show">The TV show to update.</param>
        /// <param name="callback">The status callback.</param>
        /// <returns>
        /// Updated TV show or <c>null</c>.
        /// </returns>
        public static TVShow Update(TVShow show, Action<int, string> callback = null)
        {
            if (callback != null)
            {
                callback(0, "Updating " + show.Title + "...");
            }

            Guide guide;
            try
            {
                guide = Updater.CreateGuide(show.Source);
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not get guide object of type " + show.Source + " for " + show.Title + ".");
                }

                return null;
            }

            TVShow tv;
            try
            {
                tv = guide.GetData(show.SourceID, show.Language);
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not get guide data for " + show.Source + "#" + show.SourceID + ".");
                }

                return null;
            }

            tv.ID          = show.ID;
            tv.Data        = show.Data;
            tv.Directory   = show.Directory;
            tv.EpisodeByID = new Dictionary<int, Episode>();

            if (tv.Title != show.Title)
            {
                tv.Title = show.Title;
            }

            foreach (var ep in tv.Episodes)
            {
                ep.Show = tv;
                ep.ID   = ep.Number + (ep.Season * 1000) + (tv.ID * 1000 * 1000);

                tv.EpisodeByID[ep.Number + (ep.Season * 1000)] = ep;

                Episode op;
                if (show.EpisodeByID.TryGetValue(ep.Number + (ep.Season * 1000), out op) && op.Watched)
                {
                    ep.Watched = true;
                }

                if (!string.IsNullOrWhiteSpace(tv.AirTime) && ep.Airdate != Utils.UnixEpoch)
                {
                    ep.Airdate = DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone(tv.TimeZone);
                }
            }

            try
            {
                tv.Save();
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not save database for " + show.Title + ".");
                }

                return null;
            }

            TVShows[tv.ID] = tv;
            DataChange = DateTime.Now;

            if (callback != null)
            {
                callback(1, "Updated " + show.Title + ".");
            }

            return tv;
        }

        /// <summary>
        /// Removes the specified TV show from the database.
        /// </summary>
        /// <param name="show">The TV show to remove.</param>
        /// <param name="callback">The status callback.</param>
        /// <returns>
        ///   <c>true</c> on success; otherwise, <c>false</c>.
        /// </returns>
        public static bool Remove(TVShow show, Action<int, string> callback = null)
        {
            if (callback != null)
            {
                callback(0, "Updating " + show.Title + "...");
            }

            try
            {
                Directory.Delete(show.Directory, true);
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Could not remove database for " + show.Title + ".");
                }

                return false;
            }

            TVShows.Remove(show.ID);
            DataChange = DateTime.Now;

            if (callback != null)
            {
                callback(1, "Removed " + show.Title + ".");
            }

            return true;
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
            string title;
            if (TVShows[id].Data.TryGetValue("title." + language, out title))
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
                    TVShows[id].Data["title." + language] = "!" + DateTime.Now.ToUnixTimestamp();
                    TVShows[id].SaveData();
                    return null;
                }

                TVShows[id].Data["title." + language] = api.Result;
                TVShows[id].SaveData();
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
                    TVShows[id].Data["title." + language] = search;
                    TVShows[id].SaveData();

                    new Thread(() => Remote.API.SetForeignTitle(TVShows[id].Name, search, language)).Start();

                    return search;
                }
            }

            TVShows[id].Data["title." + language] = "!" + DateTime.Now.ToUnixTimestamp();
            TVShows[id].SaveData();

            new Thread(() => Remote.API.SetForeignTitle(TVShows[id].Name, "!", language)).Start();

            return null;
        }
    }
}
