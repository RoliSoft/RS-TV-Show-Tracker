namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Newtonsoft.Json.Linq;

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
        /// Sends a database change to the remote server.
        /// </summary>
        /// <param name="showid">The ID of the show.</param>
        /// <param name="type">The type of the change.</param>
        /// <param name="data">The changed information.</param>
        public static void SendChange(string showid, ShowInfoChange.ChangeType type, object data = null)
        {
            if (!Enabled) return;

            new Thread(() =>
                {
                    var show   = Database.Query("select name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where showid = ? limit 1", showid)[0];
                    var change = new ShowInfoChange
                        {
                            Time   = (long)DateTime.UtcNow.ToUnixTimestamp(),
                            Change = type,
                            Data   = data,
                            Show   = new[]
                                {
                                    show["name"],
                                    show["grabber"],
                                    Database.ShowData(showid, show["grabber"] + ".id"),
                                    Database.ShowData(showid, show["grabber"] + ".lang")
                                }
                        };

                    Remote.API.SendDatabaseChange(change);
                }).Start();
        }

        /// <summary>
        /// Sends the full database to the remote server.
        /// </summary>
        public static void SendDatabase()
        {
            Remote.API.SendDatabaseChanges(SerializeDatabase());
        }

        /// <summary>
        /// Serializes the list of followed TV shows and their marked episodes.
        /// </summary>
        /// <returns>List of serialized TV show states.</returns>
        public static List<ShowInfoChange> SerializeDatabase()
        {
            var list  = new List<ShowInfoChange>();
            var shows = Database.Query("select showid, name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows order by rowid asc");

            foreach (var show in shows)
            {
                var showinf = new[]
                    {
                        show["name"],
                        show["grabber"],
                        Database.ShowData(show["showid"], show["grabber"] + ".id"),
                        Database.ShowData(show["showid"], show["grabber"] + ".lang")
                    };

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
        /// Gets the changes on the remote server.
        /// </summary>
        public static void GetChanges()
        {
            var last = Database.Setting("Last Sync");
            if (string.IsNullOrWhiteSpace(last))
            {
                last = "0";
            }

            var changes = Remote.API.GetDatabaseChanges(long.Parse(last));
            if (changes.Success && changes.Changes.Count != 0)
            {
                ApplyRemoteChanges(changes);
            }
        }

        /// <summary>
        /// Applies the changes retrieved from the remote database.
        /// </summary>
        /// <param name="changes">The list of serialized TV show states.</param>
        public static void ApplyRemoteChanges(ShowInfoChangeList changes)
        {
            foreach (var change in changes.Changes)
            {
                var id = Database.GetShowID(change.Show[0], change.Show[1], change.Show[2], change.Show[3]);
                if (string.IsNullOrWhiteSpace(id))
                {
                    if (change.Change == ShowInfoChange.ChangeType.AddShow || change.Change == ShowInfoChange.ChangeType.MarkEpisode || change.Change == ShowInfoChange.ChangeType.UnmarkEpisode)
                    {
                        Database.Execute("update tvshows set rowid = rowid + 1");
                        Database.Execute("insert into tvshows values (1, null, ?)", change.Show[0]);

                        id = Database.GetShowID(change.Show[0]);

                        Database.ShowData(id, "grabber", change.Show[1]);
                        Database.ShowData(id, change.Show[1] + ".id", change.Show[2]);
                        Database.ShowData(id, change.Show[1] + ".lang", change.Show[3]);
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
                }
            }

            Database.Setting("Last Sync", changes.LastSync.ToString());
            Database.Execute("vacuum;");
            MainWindow.Active.DataChanged();
        }
    }
}
