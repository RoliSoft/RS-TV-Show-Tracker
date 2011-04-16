namespace RoliSoft.TVShowTracker.Synchronization.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using System.Timers;

    using Newtonsoft.Json.Linq;

    using RoliSoft.TVShowTracker.Remote.Objects;

    using Timer = System.Timers.Timer;

    /// <summary>
    /// Provides synchronization with the API located at lab.rolisoft.net/api.
    /// </summary>
    public class RoliSoftDotNetAPI : SyncEngine
    {
        /// <summary>
        /// The list of supported engines and their ID on the remote database.
        /// </summary>
        public static Dictionary<string, int> Engines = new Dictionary<string, int>
            {
                { "TVRage",   0 },
                { "TVDB",     1 },
                { "TVcom",    2 },
                { "IMDb",     3 },
                { "AniDB",    4 },
                { "EPGuides", 5 }
            };

        /// <summary>
        /// The list of supported engines and their ID on the remote database in reverse order.
        /// </summary>
        public static Dictionary<int, string> EnginesReverse;

        /// <summary>
        /// The list of supported languages by ISO 639-1 code and their ID on the remote database.
        /// </summary>
        public static Dictionary<string, int> Languages = new Dictionary<string, int>
            {
                { "en", 0  },
                { "hu", 1  },
                { "ro", 2  },
                { "de", 3  },
                { "fr", 4  },
                { "es", 5  },
                { "sv", 6  },
                { "it", 7  },
                { "nl", 8  },
                { "da", 9  },
                { "no", 10 },
                { "et", 11 },
                { "fi", 12 },
                { "pl", 13 },
                { "is", 14 },
                { "cs", 15 },
                { "hr", 16 },
                { "sr", 17 },
                { "sk", 18 },
                { "sl", 19 },
                { "ru", 20 },
                { "br", 21 },
                { "pt", 22 },
                { "el", 23 },
                { "tr", 24 },
                { "zh", 25 },
                { "ja", 26 },
                { "ko", 27 },
                { "ar", 28 },
                { "he", 29 },
                { "id", 30 },
                { "fa", 31 },
            };

        /// <summary>
        /// The list of supported languages by ISO 639-1 code and their ID on the remote database in reverse order.
        /// </summary>
        public static Dictionary<int, string> LanguagesReverse;

        #region Queue
        /// <summary>
        /// Gets or sets the list of delayed changes.
        /// </summary>
        /// <value>The list of delayed changes.</value>
        public List<ShowInfoChange> DelayedChanges { get; set; }

        /// <summary>
        /// Gets or sets the delayed changes timer.
        /// </summary>
        /// <value>The delayed changes timer.</value>
        public Timer DelayedChangesTimer { get; set; }

        /// <summary>
        /// Gets or sets the list of pending changes.
        /// </summary>
        /// <value>The list of pending changes.</value>
        public List<ShowInfoChange> PendingChanges { get; set; }

        /// <summary>
        /// Gets or sets the pending changes timer.
        /// </summary>
        /// <value>The pending changes timer.</value>
        public Timer PendingChangesTimer { get; set; }

        private readonly string _delayedDataPath = Path.Combine(Signature.FullPath, ".sync-delayed-data");
        private readonly string _pendingDataPath = Path.Combine(Signature.FullPath, ".sync-pending-data");

        private readonly string _user, _pass;

        /// <summary>
        /// Initializes the <see cref="RoliSoftDotNetAPI"/> class.
        /// </summary>
        static RoliSoftDotNetAPI()
        {
            EnginesReverse   = Engines.ToDictionary(x => x.Value, y => y.Key);
            LanguagesReverse = Languages.ToDictionary(x => x.Value, y => y.Key);

            Languages.Add(string.Empty, 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoliSoftDotNetAPI"/> class.
        /// </summary>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        public RoliSoftDotNetAPI(string user, string pass)
        {
            _user = user;
            _pass = pass;

            DelayedChanges = new List<ShowInfoChange>();
            PendingChanges = new List<ShowInfoChange>();

            DelayedChangesTimer = new Timer(30 * 1000) { AutoReset = false };
            PendingChangesTimer = new Timer(120 * 1000) { AutoReset = false };

            DelayedChangesTimer.Elapsed += DelayedChangesTimerElapsed;
            PendingChangesTimer.Elapsed += PendingChangesTimerElapsed;

            if (File.Exists(_delayedDataPath))
            {
                var bf = new BinaryFormatter();

                using (var fs = new FileStream(_delayedDataPath, FileMode.Open))
                {
                    DelayedChanges = (List<ShowInfoChange>)bf.Deserialize(fs);
                }

                DelayedChangesTimer.Start();
            }

            if (File.Exists(_pendingDataPath))
            {
                var bf = new BinaryFormatter();

                using (var fs = new FileStream(_pendingDataPath, FileMode.Open))
                {
                    PendingChanges = (List<ShowInfoChange>)bf.Deserialize(fs);
                }

                PendingChangesTimer.Start();
            }
        }

        /// <summary>
        /// Handles the Elapsed event of the DelayedChangesTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        public void DelayedChangesTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DelayedChanges.Count == 0)
            {
                return;
            }

            var changes = DelayedChanges.ToList();
            DelayedChanges.Clear();

            if (File.Exists(_delayedDataPath))
            {
                try { File.Delete(_delayedDataPath); } catch { }
            }

            var req = Remote.API.SendDatabaseChanges(changes, _user, _pass);
            if (!req.Success || !req.OK)
            {
                PendingChanges.AddRange(changes);
                PendingChangesTimer.Start();
                SavePendingChanges();
            }
        }

        /// <summary>
        /// Handles the Elapsed event of the PendingChangesTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        public void PendingChangesTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (PendingChanges.Count == 0)
            {
                return;
            }

            var changes = PendingChanges.ToList();
            PendingChanges.Clear();

            if (File.Exists(_pendingDataPath))
            {
                try { File.Delete(_pendingDataPath); } catch { }
            }

            var req = Remote.API.SendDatabaseChanges(changes, _user, _pass);
            if (!req.Success || !req.OK)
            {
                PendingChanges.AddRange(changes);
                PendingChangesTimer.Start();
                SavePendingChanges();
            }
        }

        /// <summary>
        /// Saves the list of delayed changes.
        /// </summary>
        public void SaveDelayedChanges()
        {
            new Thread(() =>
                {
                    try
                    {
                        var bf = new BinaryFormatter();

                        using (var fs = new FileStream(_delayedDataPath, FileMode.Create))
                        {
                            bf.Serialize(fs, DelayedChanges);
                            fs.Flush();
                        }

                        File.SetAttributes(_delayedDataPath, FileAttributes.Hidden);
                    }
                    catch { }
                }).Start();
        }

        /// <summary>
        /// Saves the list of pending changes.
        /// </summary>
        public void SavePendingChanges()
        {
            new Thread(() =>
                {
                    try
                    {
                        var bf = new BinaryFormatter();

                        using (var fs = new FileStream(_pendingDataPath, FileMode.Create))
                        {
                            bf.Serialize(fs, PendingChanges);
                            fs.Flush();
                        }

                        File.SetAttributes(_pendingDataPath, FileAttributes.Hidden);
                    }
                    catch { }
                }).Start();
        }
        #endregion

        #region Implementation of SyncEngine
        /// <summary>
        /// Adds a new TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        public override void AddShow(string showid)
        {
            SendChange(InitChange(showid, ShowInfoChange.ChangeType.AddShow));
        }

        /// <summary>
        /// Modified an existing TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="modification">The array of modified parameters.</param>
        public override void ModifyShow(string showid, string[] modification)
        {
            SendChange(InitChange(showid, ShowInfoChange.ChangeType.ModifyShow, modification));
        }

        /// <summary>
        /// Removes an existing TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        public override void RemoveShow(string showid)
        {
            SendChange(InitChange(showid, ShowInfoChange.ChangeType.RemoveShow));
        }

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public override void MarkEpisodes(string showid, IEnumerable<int> episodes)
        {
            var change = InitChange(showid, ShowInfoChange.ChangeType.MarkEpisode, episodes);

            if (DelayedChanges.Any(x => x.Change == ShowInfoChange.ChangeType.MarkEpisode && x.Show == change.Show))
            {
                var prevmark = DelayedChanges.First(x => x.Change == ShowInfoChange.ChangeType.MarkEpisode && x.Show == change.Show);

                if (prevmark.Data is List<int> && change.Data is List<int>)
                {
                    (prevmark.Data as List<int>).AddRange(change.Data as List<int>);
                }
                else
                {
                    DelayedChanges.Add(change);
                }
            }
            else
            {
                DelayedChanges.Add(change);
            }

            DelayedChangesTimer.Start();
            SaveDelayedChanges();
        }

        /// <summary>
        /// Marks one or more episodes as seen.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public override void MarkEpisodes(string showid, IEnumerable<int[]> episodes)
        {
            var change = InitChange(showid, ShowInfoChange.ChangeType.MarkEpisode, episodes);

            if (DelayedChanges.Any(x => x.Change == ShowInfoChange.ChangeType.MarkEpisode && x.Show == change.Show))
            {
                var prevmark = DelayedChanges.First(x => x.Change == ShowInfoChange.ChangeType.MarkEpisode && x.Show == change.Show);

                if (prevmark.Data is List<int[]> && change.Data is List<int[]>)
                {
                    (prevmark.Data as List<int[]>).AddRange(change.Data as List<int[]>);
                }
                else
                {
                    DelayedChanges.Add(change);
                }
            }
            else
            {
                DelayedChanges.Add(change);
            }

            DelayedChangesTimer.Start();
            SaveDelayedChanges();
        }

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episodes.</param>
        public override void UnmarkEpisodes(string showid, IEnumerable<int> episodes)
        {
            var change = InitChange(showid, ShowInfoChange.ChangeType.UnmarkEpisode, episodes);

            if (DelayedChanges.Any(x => x.Change == ShowInfoChange.ChangeType.UnmarkEpisode && x.Show == change.Show))
            {
                var prevmark = DelayedChanges.First(x => x.Change == ShowInfoChange.ChangeType.UnmarkEpisode && x.Show == change.Show);

                if (prevmark.Data is List<int> && change.Data is List<int>)
                {
                    (prevmark.Data as List<int>).AddRange(change.Data as List<int>);
                }
                else
                {
                    DelayedChanges.Add(change);
                }
            }
            else
            {
                DelayedChanges.Add(change);
            }

            DelayedChangesTimer.Start();
            SaveDelayedChanges();
        }

        /// <summary>
        /// Unmarks one or more episodes.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="episodes">The list of episode ranges. A range consists of two numbers from the same season.</param>
        public override void UnmarkEpisodes(string showid, IEnumerable<int[]> episodes)
        {
            var change = InitChange(showid, ShowInfoChange.ChangeType.UnmarkEpisode, episodes);

            if (DelayedChanges.Any(x => x.Change == ShowInfoChange.ChangeType.UnmarkEpisode && x.Show == change.Show))
            {
                var prevmark = DelayedChanges.First(x => x.Change == ShowInfoChange.ChangeType.UnmarkEpisode && x.Show == change.Show);

                if (prevmark.Data is List<int[]> && change.Data is List<int[]>)
                {
                    (prevmark.Data as List<int[]>).AddRange(change.Data as List<int[]>);
                }
                else
                {
                    DelayedChanges.Add(change);
                }
            }
            else
            {
                DelayedChanges.Add(change);
            }

            DelayedChangesTimer.Start();
            SaveDelayedChanges();
        }

        /// <summary>
        /// Sends the reordered TV show list.
        /// </summary>
        public override void ReorderList()
        {
            var change = InitChange(null, ShowInfoChange.ChangeType.AddShow, Database.Query("select name from tvshows order by rowid asc").Select(dict => dict["name"]).ToArray());

            DelayedChanges.RemoveAll(x => x.Change == ShowInfoChange.ChangeType.ReorderList);
            DelayedChanges.Add(change);

            DelayedChangesTimer.Start();
            SaveDelayedChanges();
        }

        /// <summary>
        /// Serializes and sends the full database.
        /// </summary>
        /// <returns><c>true</c> if sent successfully.</returns>
        public override bool SendDatabase()
        {
            var req = Remote.API.SendDatabaseChanges(SerializeDatabase(), _user, _pass);
            return req.Success && req.OK;
        }

        /// <summary>
        /// Retrieves and applies the changes which have been made to the remote database.
        /// </summary>
        /// <returns>Number of changes since last synchronization or -1 on sync failure.</returns>
        public override int GetRemoteChanges()
        {
            var changes = Remote.API.GetDatabaseChanges((Database.Setting("Last Sync") ?? "0").ToLong(), _user, _pass);
            if (changes.Success && changes.Changes.Count != 0)
            {
                ApplyRemoteChanges(changes);
                return changes.Changes.Count;
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region Helper methods
        /// <summary>
        /// Builds an array which identifies the TV show on the remote server.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <returns>Basic TV show information.</returns>
        public static string GetShowData(string showid)
        {
            var show = Database.Query("select name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where showid = ? limit 1", showid)[0];

            return "{0}\0{1}\0{2}\0{3}".FormatWith(
                    show["name"],
                    Engines[show["grabber"]],
                    Languages[Database.ShowData(showid, show["grabber"] + ".lang")],
                    Database.ShowData(showid, show["grabber"] + ".id")
                );
        }

        /// <summary>
        /// Creates a new object which will be sent to the remote server.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
        /// <param name="type">The type of the change.</param>
        /// <param name="data">The data of the change.</param>
        /// <returns>Simple JSON object.</returns>
        public static ShowInfoChange InitChange(string showid, ShowInfoChange.ChangeType type, object data = null)
        {
            return new ShowInfoChange
                {
                    Time   = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000d,
                    Change = type,
                    Data   = data,
                    Show   = type != ShowInfoChange.ChangeType.ReorderList
                             ? GetShowData(showid)
                             : null
                };
        }

        /// <summary>
        /// Sends the specified change to the remote database.
        /// </summary>
        /// <param name="change">The object containing the changed information.</param>
        public void SendChange(ShowInfoChange change)
        {
            new Thread(() =>
                {
                    var req = Remote.API.SendDatabaseChange(change, _user, _pass);
                    if (!req.Success || !req.OK)
                    {
                        PendingChanges.Add(change);
                        PendingChangesTimer.Start();
                    }
                }).Start();
        }

        /// <summary>
        /// Serializes the list of followed TV shows and their marked episodes.
        /// </summary>
        /// <returns>List of serialized TV show states.</returns>
        public static List<ShowInfoChange> SerializeDatabase()
        {
            var list  = new List<ShowInfoChange>();
            var shows = Database.Query("select showid, name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows order by name asc");

            foreach (var show in shows)
            {
                var showinf = "{0}\0{1}\0{2}\0{3}".FormatWith(
                        show["name"],
                        Engines[show["grabber"]],
                        Languages[Database.ShowData(show["showid"], show["grabber"] + ".lang")],
                        Database.ShowData(show["showid"], show["grabber"] + ".id")
                    );

                var addchg = new ShowInfoChange
                    {
                        Time   = (long)DateTime.UtcNow.ToUnixTimestamp(),
                        Change = ShowInfoChange.ChangeType.AddShow,
                        Show   = showinf
                    };

                list.Add(addchg);

                var markchg = new ShowInfoChange
                    {
                        Time   = (long)DateTime.UtcNow.ToUnixTimestamp(),
                        Change = ShowInfoChange.ChangeType.MarkEpisode,
                        Show   = showinf,
                        Data   = SerializeMarkedEpisodes(show["showid"])
                    };

                list.Add(markchg);
            }

            var orderchg = new ShowInfoChange
                {
                    Time   = (long)DateTime.UtcNow.ToUnixTimestamp(),
                    Change = ShowInfoChange.ChangeType.ReorderList,
                    Data   = Database.Query("select name from tvshows order by rowid asc").Select(dict => dict["name"]).ToArray()
                };

            list.Add(orderchg);

            return list;
        }

        /// <summary>
        /// Serializes the list of marked episodes for the specified TV show.
        /// </summary>
        /// <param name="showid">The ID of the TV show in the database.</param>
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
        /// Applies the changes retrieved from the remote database.
        /// </summary>
        /// <param name="changes">The list of serialized TV show states.</param>
        public static void ApplyRemoteChanges(ShowInfoChangeList changes)
        {
            foreach (var change in changes.Changes)
            {
                var chg = change.Show.Split("\0".ToCharArray(), 4);

                var id = Database.GetShowID(chg[0], EnginesReverse[chg[1].ToInteger()], LanguagesReverse[chg[2].ToInteger()], chg[3]);
                if (string.IsNullOrWhiteSpace(id))
                {
                    if (change.Change == ShowInfoChange.ChangeType.AddShow || change.Change == ShowInfoChange.ChangeType.MarkEpisode || change.Change == ShowInfoChange.ChangeType.UnmarkEpisode)
                    {
                        Database.Execute("update tvshows set rowid = rowid + 1");
                        Database.Execute("insert into tvshows values (1, null, ?)", chg[0]);

                        id = Database.GetShowID(chg[0]);

                        Database.ShowData(id, "grabber", EnginesReverse[chg[1].ToInteger()]);
                        Database.ShowData(id, change.Show[1] + ".id", chg[3]);
                        Database.ShowData(id, change.Show[1] + ".lang", LanguagesReverse[chg[2].ToInteger()]);
                    }
                    else
                    {
                        continue;
                    }
                }

                var sint = id.ToInteger() * 100000;

                switch (change.Change)
                {
                    case ShowInfoChange.ChangeType.RemoveShow:
                        Database.Execute("delete from tvshows where showid = ?", id);
                        Database.Execute("delete from showdata where showid = ?", id);
                        Database.Execute("delete from episodes where showid = ?", id);
                        Database.Execute("delete from tracking where showid = ?", id);

                        Database.Execute("update tvshows set rowid = rowid * -1");

                        var tr = Database.Connection.BeginTransaction();

                        var shows = Database.Query("select showid from tvshows order by rowid desc");
                        var i = 1;
                        foreach (var show in shows)
                        {
                            Database.ExecuteOnTransaction(tr, "update tvshows set rowid = ? where showid = ?", i, show["showid"]);
                            i++;
                        }

                        tr.Commit();

                        Database.Execute("vacuum;");
                        break;

                    case ShowInfoChange.ChangeType.MarkEpisode:
                        var tr2 = Database.Connection.BeginTransaction();

                        foreach (var ep in (JArray)change.Data)
                        {
                            if (Database.Query("select * from tracking where showid = ? and episodeid = ?", id, (int)ep + sint).Count == 0)
                            {
                                Database.ExecuteOnTransaction(tr2, "insert into tracking values (?, ?)", id, (int)ep + sint);
                            }
                        }

                        tr2.Commit();
                        break;

                    case ShowInfoChange.ChangeType.UnmarkEpisode:
                        foreach (var ep in (JArray)change.Data)
                        {
                            Database.Execute("delete from tracking where showid = ? and episodeid = ?", id, (int)ep + sint);
                        }
                        break;

                    case ShowInfoChange.ChangeType.ReorderList:
                        // TODO
                        break;
                }
            }

            Database.Setting("Last Sync", changes.LastSync.ToString());
            Database.Execute("vacuum;");
            MainWindow.Active.DataChanged();
        }
        #endregion
    }
}
