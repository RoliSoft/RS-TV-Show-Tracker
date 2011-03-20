namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    using RoliSoft.TVShowTracker.Remote.Objects;

    /// <summary>
    /// Provides methods to synchronize the database with a remote server.
    /// </summary>
    public static class Synchronization
    {
        /// <summary>
        /// Gets or sets a value indicating whether synchronization is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to send the full database instead of just the differences on the next synchronization.
        /// </summary>
        /// <value><c>true</c> if full synchronization is required on the next request; otherwise, <c>false</c>.</value>
        public static bool SendFull { get; set; }

        /// <summary>
        /// Gets or sets the change list.
        /// </summary>
        /// <value>The change list.</value>
        public static Dictionary<string, SerializedShowInfoDiff> ChangeList = new Dictionary<string, SerializedShowInfoDiff>();

        /// <summary>
        /// Add a database change to the list of operations to be sent to the remote server.
        /// </summary>
        /// <param name="showid">The ID of the show.</param>
        /// <param name="type">The type of the change.</param>
        public static void AddChange(string showid, SerializedShowInfoDiff.ChangeType type)
        {
            //if (!Enabled) return;
            if (SendFull) return;

            if (!ChangeList.ContainsKey(showid))
            {
                ChangeList[showid] = new SerializedShowInfoDiff { Title = Database.GetShowTitle(showid) };
            }

            switch (type)
            {
                case SerializedShowInfoDiff.ChangeType.MarkedEpisodesModified:
                    if (ChangeList[showid].Changes != SerializedShowInfoDiff.ChangeType.ShowModified)
                    {
                        ChangeList[showid].Changes = SerializedShowInfoDiff.ChangeType.MarkedEpisodesModified;
                    }

                    ChangeList[showid].MarkedEpisodes = SerializeMarkedEpisodes(showid);
                    break;

                case SerializedShowInfoDiff.ChangeType.ShowModified:
                    var show = Database.Query("select rowid, name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where showid = ? limit 1", showid)[0];
                    var info = new SerializedShowInfoDiff
                        {
                            RowID          = show["rowid"].ToInteger(),
                            Title          = show["name"],
                            Source         = show["grabber"],
                            SourceID       = Database.ShowData(showid, show["grabber"] + ".id"),
                            SourceLanguage = Database.ShowData(showid, show["grabber"] + ".lang"),
                            Changes        = SerializedShowInfoDiff.ChangeType.ShowModified,
                            MarkedEpisodes = SerializeMarkedEpisodes(showid)
                        };

                    if (string.IsNullOrWhiteSpace(info.SourceLanguage))
                    {
                        info.SourceLanguage = "en";
                    }

                    ChangeList[showid] = info;
                    break;

                case SerializedShowInfoDiff.ChangeType.ShowAdded:
                case SerializedShowInfoDiff.ChangeType.ShowRemoved:
                case SerializedShowInfoDiff.ChangeType.RowIdModified:
                    SendFull = true;
                    ChangeList.Clear();
                    break;
            }
        }

        /// <summary>
        /// Sends the full database to the remote server.
        /// </summary>
        public static void SendDatabase()
        {
            var sync = Remote.API.SendDatabase(SerializeDatabase());
            if (sync.Success)
            {
                Settings.Set("Last Sync", sync.Result);
            }
        }

        /// <summary>
        /// Sends the database changes to the remote server.
        /// </summary>
        public static void SendDatabaseChanges()
        {
            if (SendFull)
            {
                ChangeList.Clear();
                SendFull = false;

                SendDatabase();
            }
            else
            {
                var changes = ChangeList.Select(kv => kv.Value).ToList();
                ChangeList.Clear();
                
                var sync = Remote.API.SendDatabaseChanges(changes);
                if (sync.Success)
                {
                    Settings.Set("Last Sync", sync.Result);
                }
            }
        }

        /// <summary>
        /// Checks if the databases on both sides are equal, and if not, initiates synchronization.
        /// </summary>
        public static void FirstRunSynchronize()
        {
            var localhash = GetDatabaseChecksum();
            var rsnethash = Remote.API.GetDatabaseChecksum();

            if (!rsnethash.Success)
            {
                return;
            }

            if (localhash.Checksum != rsnethash.Checksum)
            {
                if (localhash.LastSync >= rsnethash.LastSync)
                {
                    SendDatabase();
                }

                if (localhash.LastSync < rsnethash.LastSync)
                {
                    // get database changes
                }
            }
        }

        /// <summary>
        /// Serializes the list of followed TV shows and their marked episodes.
        /// </summary>
        /// <returns>List of serialized TV show states.</returns>
        public static List<SerializedShowInfo> SerializeDatabase()
        {
            var list  = new List<SerializedShowInfo>();
            var shows = Database.Query("select rowid, showid, name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows order by rowid asc");

            foreach (var show in shows)
            {
                var info = new SerializedShowInfo
                    {
                        RowID          = show["rowid"].ToInteger(),
                        Title          = show["name"],
                        Source         = show["grabber"],
                        SourceID       = Database.ShowData(show["showid"], show["grabber"] + ".id"),
                        SourceLanguage = Database.ShowData(show["showid"], show["grabber"] + ".lang")
                    };

                if (string.IsNullOrWhiteSpace(info.SourceLanguage))
                {
                    info.SourceLanguage = "en";
                }

                info.MarkedEpisodes = SerializeMarkedEpisodes(show["showid"]);

                list.Add(info);
            }

            return list;
        }

        /// <summary>
        /// Serializes the list of marked episodes for the specified TV show.
        /// </summary>
        /// <param name="showid">The ID of the show.</param>
        /// <returns>List of marked episode ranges.</returns>
        public static List<int[]> SerializeMarkedEpisodes(string showid)
        {
            var sint   = showid.ToInteger() * 100000;
            var marked = Database.Query("select distinct episodeid from tracking where showid = ? order by episodeid asc", showid);
            var list   = new List<int[]>();
            var start  = 0;
            var prev   = 0;

            foreach (var ep in marked.Select(ep => ep["episodeid"].ToInteger() - sint).Where(ep => ep >= 0))
            {
                if (prev == ep - 1)
                {
                    prev = ep;
                }
                else
                {
                    if (start != 0 && prev != 0)
                    {
                        list.Add(new[] { start, prev });
                    }

                    start = prev = ep;
                }
            }

            if ((list.Count == 0 && start != 0 && prev != 0) || (list.Count != 0 && list.Last() != new[] { start, prev }))
            {
                list.Add(new[] { start, prev });
            }

            return list;
        }

        /// <summary>
        /// Serializes the database and computes its SHA-256 checksum.
        /// </summary>
        /// <returns>The last modification time and SHA-256 hash.</returns>
        public static DatabaseChecksum GetDatabaseChecksum()
        {
            var db = SerializeDatabase();
            var sb = new StringBuilder();

            foreach (var show in db)
            {
                sb.Append(show.RowID.Value + "\0" + show.Title + "\0" + show.Source + "\0" + show.SourceID + "\0" + show.SourceLanguage + "\0");

                foreach (var eps in show.MarkedEpisodes)
                {
                    sb.Append(eps[0] + "\0" + eps[1] + "\0");
                }
            }

            return new DatabaseChecksum
                {
                    LastSync = Settings.Get<int>("Last Sync"),
                    Checksum = BitConverter.ToString(new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-", string.Empty).ToLower()
                };
        }

        /// <summary>
        /// Inserts the TV shows in the specified list and marks the specified episodes.
        /// </summary>
        /// <param name="shows">The list of serialized TV show states.</param>
        public static void UpdateDatabase(List<SerializedShowInfo> shows)
        {
            foreach (var show in shows)
            {
                var id = Database.GetShowID(show.Title);
                if (string.IsNullOrWhiteSpace(id))
                {
                    Database.Execute("update tvshows set rowid = rowid + 1");
                    Database.Execute("insert into tvshows values (1, null, ?)", show.Title);

                    id = Database.GetShowID(show.Title);

                    Database.ShowData(id, "grabber",             show.Source);
                    Database.ShowData(id, show.Source + ".id",   show.SourceID);
                    Database.ShowData(id, show.Source + ".lang", show.SourceLanguage);
                }

                var sint = id.ToInteger() * 100000;
                foreach (var range in show.MarkedEpisodes)
                {
                    foreach (var ep in Enumerable.Range(range[0], range[1]))
                    {
                        if (Database.Query("select * from tracking where showid = ? and episodeid = ?", id, ep + sint).Count == 0)
                        {
                            Database.Execute("insert into tracking values (?, ?)", id, ep + sint);
                        }
                    }
                }
            }

            MainWindow.Active.DataChanged();
        }
    }
}
