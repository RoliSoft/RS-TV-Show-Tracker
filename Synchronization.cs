namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            //if (!Enabled) return;

            var show   = Database.Query("select name, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where showid = ? limit 1", showid)[0];
            var change = new ShowInfoChange
                {
                    Time   = DateTime.UtcNow.ToUnixTimestamp(),
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
                        Time   = DateTime.UtcNow.ToUnixTimestamp(),
                        Change = ShowInfoChange.ChangeType.AddShow,
                        Show   = showinf
                    };

                list.Add(addchg);

                var markchg = new ShowInfoChange
                    {
                        Time   = DateTime.UtcNow.ToUnixTimestamp(),
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
