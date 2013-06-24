namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Text;

    using Newtonsoft.Json;

    using SharpCompress.Archive.Zip;
    using SharpCompress.Common;
    using SharpCompress.Writer;

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
        public static readonly string DataPath = Path.Combine(Signature.InstallPath, "db");

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Database()
        {
            if (string.IsNullOrWhiteSpace(Signature.InstallPath))
            {
                Log.Fatal("Stopping database initialization because $InstallPath is null or empty.");
                return;
            }

            if (!Directory.Exists(DataPath))
            {
                Log.Info("Creating database folder: " + DataPath);
                Directory.CreateDirectory(DataPath);
            }

            var tmp = Path.Combine(Signature.UACVirtualPath, "db");

            if (Directory.Exists(tmp))
            {
                Log.Info("$UACVirtualPath\\db exists, initiating migration to $DataPath...");

                foreach (var dir in Directory.EnumerateDirectories(tmp))
                {
                    var fn  = Path.GetFileName(dir);
                    var nir = Path.Combine(DataPath, fn);

                    Log.Info("Migrating " + fn + "...");

                    if (Directory.Exists(nir) || !File.Exists(Path.Combine(dir, "info")) || !File.Exists(Path.Combine(dir, "conf")))
                    {
                        Log.Info(fn + " already exists in the target directory or is not a valid TV show directory.");
                        continue;
                    }

                    try
                    {
                        var show = TVShow.Load(dir);

                        show.ID += 1000;
                        show.Directory = nir;

                        Directory.CreateDirectory(show.Directory);

                        show.Save();
                        show.SaveTracking();

                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Exception while migrating data.", ex);
                        continue;
                    }
                }

                Log.Info("Migration finished.");
            }

            LoadDatabase();
        }

        /// <summary>
        /// Loads the database files.
        /// </summary>
        public static void LoadDatabase()
        {
            Log.Info("Loading database...");

            DataChange = DateTime.Now;
            TVShows    = new Dictionary<int, TVShow>();

            foreach (var dir in Directory.EnumerateDirectories(DataPath))
            {
                var fn = Path.GetFileName(dir);

                if (Log.IsTraceEnabled) Log.Trace("Reading " + fn + "...");

                if (!File.Exists(Path.Combine(dir, "info")) || !File.Exists(Path.Combine(dir, "conf")))
                {
                    Log.Warn(fn + " is not a valid TV show directory.");
                    continue;
                }

                try
                {
                    var show = TVShow.Load(dir);

                    TVShows[show.ID] = show;
                }
                catch (Exception ex)
                {
                    Log.Error("Error while loading TV show data " + fn + ".", ex);
                    continue;
                    //MainWindow.HandleUnexpectedException(new Exception("Couldn't load db\\" + fn + " due to an error.", ex));
                }
            }

            Log.Debug("Database loaded in " + (DateTime.Now - DataChange).TotalSeconds + "s, containing " + TVShows.Count + " shows.");
            Log.Info("UUID is " + Utils.GetUUID());
        }

        /// <summary>
        /// Loads the data from the database.
        /// </summary>
        /// <returns>
        ///   <c>true</c> on success; otherwise, <c>false</c>.
        /// </returns>
        public static bool LoadData()
        {
            if (!File.Exists(Path.Combine(DataPath, "conf")))
            {
                Log.Debug("$DataPath\\conf doesn't exist.");
                return false;
            }

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

            return true;
        }

        /// <summary>
        /// Retrieves the key from the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Setting(string key)
        {
            if (Data == null && !LoadData())
            {
                return string.Empty;
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
            if (Data == null && !LoadData())
            {
                Data = new Dictionary<string, string>();
            }

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
            var path = Path.Combine(Signature.InstallPath, name);

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
            var path = Path.Combine(Signature.InstallPath, name);

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
            var path = Path.Combine(Signature.InstallPath, name);

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
        /// <param name="sid">The show ID to add to the database.</param>
        /// <param name="callback">The status callback.</param>
        /// <returns>Added TV show or <c>null</c>.</returns>
        public static TVShow Add(ShowID sid, Action<int, string> callback = null)
        {
            Log.Info("Adding " + sid.Guide.Name + "/" + sid.ID + "...");

            var st = DateTime.Now;
            
            if (callback != null)
            {
                callback(0, "Downloading guide from " + sid.Guide.Name + "...");
            }

            TVShow tv;
            try
            {
                tv = sid.Guide.GetData(sid.ID, sid.Language);

                if (tv.Episodes.Count == 0)
                {
                    Log.Error("Downloaded guide for " + tv.Title + " (" + sid.Guide.Name + "/" + sid.ID + ") has no episodes.");

                    if (callback != null)
                    {
                        callback(-1, "No episodes listed for this show on " + sid.Guide.Name + ".");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    Log.Warn("Thread aborted while downloading data from guide for " + sid.Guide.Name + "/" + sid.ID + ".", ex);
                    return null;
                }

                Log.Error("Error while downloading data from guide for " + sid.Guide.Name + "/" + sid.ID + ".", ex);

                if (callback != null)
                {
                    callback(-1, "Error while downloading data: " + ex.Message);
                }

                return null;
            }

            if (TVShows.Values.FirstOrDefault(x => x.Title == tv.Title) != null)
            {
                Log.Error("Duplicate entry detected for " + tv.Title + " (" + sid.Guide.Name + "/" + sid.ID + ").");

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
            tv.ID          = TVShows.Values.Count > 0 ? TVShows.Values.Max(x => x.ID) + 1 : 1;
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
                Log.Error("Error while creating directory db\\" + Path.GetFileName(tv.Directory) + " for " + tv.Title + " (" + sid.Guide.Name + "/" + sid.ID + ").", ex);

                if (callback != null)
                {
                    callback(-1, "Error while creating database.");
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
                    try { ep.Airdate = DateTime.Parse(ep.Airdate.ToString("yyyy-MM-dd ") + tv.AirTime).ToLocalTimeZone(tv.TimeZone); } catch { }
                }
            }

            try
            {
                tv.Save();
                tv.SaveTracking();
            }
            catch (Exception ex)
            {
                Log.Error("Error while saving database to db\\" + Path.GetFileName(tv.Directory) + " for " + tv.Title + " (" + sid.Guide.Name + "/" + sid.ID + ").", ex);

                if (callback != null)
                {
                    callback(-1, "Error while saving to database.");
                }

                return null;
            }

            TVShows[tv.ID] = tv;
            DataChange = DateTime.Now;

            if (tv.Language == "en")
            {
                Updater.UpdateRemoteCache(tv);
            }
            
            if (callback != null)
            {
                callback(1, "Show added successfully.");
            }

            Log.Debug("Successfully added " + tv.Title + " (" + sid.Guide.Name + "/" + sid.ID + ") in " + (DateTime.Now - st).TotalSeconds + "s.");

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
            Log.Info("Updating " + show.Title + "...");

            var st = DateTime.Now;

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
                Log.Error("Error while creating guide object for " + show.Title + ".", ex);

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
                Log.Error("Error while downloading data from guide for " + show.Title + ".", ex);

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
                Log.Error("Error while saving updated database for " + show.Title + ".", ex);

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

            Log.Debug("Successfully updated " + show.Title + " in " + (DateTime.Now - st).TotalSeconds + "s.");

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
            Log.Info("Removing " + show.Title + "...");

            if (callback != null)
            {
                callback(0, "Removing " + show.Title + "...");
            }

            try
            {
                Directory.Delete(show.Directory, true);
            }
            catch (Exception ex)
            {
                Log.Error("Error while removing " + show.Title + ".", ex);

                if (callback != null)
                {
                    callback(-1, "Could not remove database for " + show.Title + ".");
                }

                return false;
            }

            TVShows.Remove(show.ID);
            DataChange = DateTime.Now;

            if (Library.Files != null && Library.Files.Count != 0)
            {
                foreach (var ep in Library.Files)
                {
                    if (Math.Floor((double)ep.Key / 1000 / 1000) == show.ID)
                    {
                        ep.Value.Clear();
                    }
                }

                Library.SaveList();
            }

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
            var release = TVShows.Values.Where(s => s.Title == show).Take(1).ToList();

            if (release.Count != 0 && !string.IsNullOrWhiteSpace(release[0].Data.Get("regex")))
            {
                return new Regex(release[0].Data["regex"]);
            }

            return ShowNames.Parser.GenerateTitleRegex(show, release.FirstOrDefault());
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
                statusCallback("Searching for the " + Languages.List[language] + " title of " + TVShows[id].Title +" on lab.rolisoft.net...");
            }

            var api = Remote.API.GetForeignTitle(TVShows[id].Title, language);

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
                    statusCallback("Searching for the " + Languages.List[language] + " title of " + TVShows[id].Title + " on " + engine.Name + "...");
                }

                var search = engine.Search(TVShows[id].Title);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    TVShows[id].Data["title." + language] = search;
                    TVShows[id].SaveData();

                    new Thread(() => Remote.API.SetForeignTitle(TVShows[id].Title, search, language)).Start();

                    return search;
                }
            }

            TVShows[id].Data["title." + language] = "!" + DateTime.Now.ToUnixTimestamp();
            TVShows[id].SaveData();

            new Thread(() => Remote.API.SetForeignTitle(TVShows[id].Title, "!", language)).Start();

            return null;
        }

        /// <summary>
        /// Backups the database to the specified file.
        /// </summary>
        /// <param name="archive">The archive.</param>
        /// <param name="callback">The status callback.</param>
        /// <param name="covers">if set to <c>true</c> covers will be saved as well.</param>
        /// <param name="settings">if set to <c>true</c> settings will be saved as well.</param>
        public static void Backup(string archive, Action<int, string> callback = null, bool covers = false, bool settings = false)
        {
            if (File.Exists(archive))
            {
                try
                {
                    File.Delete(archive);
                }
                catch (Exception ex)
                {
                    if (callback != null)
                    {
                        callback(-1, "Unable to delete existing destination file.");
                    }

                    Log.Error("Error while deleting existing archive file.", ex);
                    return;
                }
            }

            if (callback != null)
            {
                callback(0, "Creating archive...");
            }

            try
            {
                using (var fs = File.OpenWrite(archive))
                using (var zip = WriterFactory.Open(fs, ArchiveType.Zip, CompressionType.Deflate))
                {
                    if (File.Exists(Path.Combine(DataPath, "conf")))
                    {
                        zip.Write("conf", Path.Combine(DataPath, "conf"));
                    }

                    foreach (var show in TVShows.Values)
                    {
                        if (callback != null)
                        {
                            callback(0, "Saving " + show.Title + "...");
                        }

                        var dir = Path.GetFileName(show.Directory);

                        zip.Write(Path.Combine(dir, "info"), Path.Combine(show.Directory, "info"));
                        zip.Write(Path.Combine(dir, "conf"), Path.Combine(show.Directory, "conf"));

                        if (File.Exists(Path.Combine(show.Directory, "seen")))
                        {
                            zip.Write(Path.Combine(dir, "seen"), Path.Combine(show.Directory, "seen"));
                        }

                        if (covers)
                        {
                            var cover = CoverManager.GetCoverLocation(show.Title);

                            if (File.Exists(cover))
                            {
                                zip.Write(Path.Combine(dir, "cover"), cover);
                            }
                        }
                    }

                    if (settings)
                    {
                        if (callback != null)
                        {
                            callback(0, "Saving settings...");
                        }

                        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Settings.Keys, Formatting.None))))
                        {
                            ms.Position = 0;
                            zip.Write("json", ms);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(-1, "Error while archiving database.");
                }

                Log.Error("Error while writing files into archive.", ex);
                return;
            }

            if (callback != null)
            {
                callback(1, "Database successfully backed up.");
            }
        }

        /// <summary>
        /// Inspects the specified backup archive.
        /// </summary>
        /// <param name="archive">The archive.</param>
        /// <returns>Summary of the archive.</returns>
        public static ArchiveInfo Inspect(string archive)
        {
            try
            {
                var arc = new ArchiveInfo();
                var zip = ZipArchive.Open(archive);
                
                if (zip.Entries.Any(e => e.FilePath == "json"))
                {
                    arc.Settings = true;
                }

                if (zip.Entries.Any(e => e.FilePath.EndsWith("/cover")))
                {
                    arc.Covers = true;
                }

                foreach (var file in zip.Entries.Where(e => e.FilePath.EndsWith("/info")))
                {
                    var dir = Path.GetFileName(Path.GetDirectoryName(file.FilePath));

                    try
                    {
                        using (var fs = file.OpenEntryStream())
                        using (var br = new BinaryReader(fs))
                        {
                            var ver = br.ReadByte();
                            var upd = br.ReadUInt32();

                            var title = br.ReadString();
                            var guide = br.ReadString();

                            arc.Shows.Add(new[] { dir, title, guide });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Unable to process " + dir + "/info in backup due to error.", ex);
                    }
                }

                return arc;
            }
            catch (Exception ex)
            {
                Log.Error("Error while processing archive.", ex);
            }

            return null;
        }

        /// <summary>
        /// Represents a database backup archive's attributes.
        /// </summary>
        public class ArchiveInfo
        {
            /// <summary>
            /// Gets or sets a value indicating whether this archive has settings.
            /// </summary>
            /// <value>
            ///   <c>true</c> if has settings; otherwise, <c>false</c>.
            /// </value>
            public bool Settings { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this archive has covers.
            /// </summary>
            /// <value>
            ///   <c>true</c> if has covers; otherwise, <c>false</c>.
            /// </value>
            public bool Covers { get; set; }

            /// <summary>
            /// Gets or sets the list of archived shows.
            /// </summary>
            /// <value>
            /// The list of archived shows.
            /// </value>
            public List<string[]> Shows { get; set; }
        }
    }
}
