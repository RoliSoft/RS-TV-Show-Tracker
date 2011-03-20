namespace RoliSoft.TVShowTracker
{
    using System.Collections.Generic;
    using System.Linq;

    using RoliSoft.TVShowTracker.Remote.Objects;

    /// <summary>
    /// Provides methods to synchronize the database with a remote server.
    /// </summary>
    public static class Synchronization
    {
        /// <summary>
        /// Gets or sets the change list.
        /// </summary>
        /// <value>The change list.</value>
        public static List<ChangeOperation> ChangeList = new List<ChangeOperation>();

        /// <summary>
        /// Sends the full database to the remote server.
        /// </summary>
        public static void SendDatabase()
        {
            Remote.API.SendDatabase(SerializeDatabase());
        }

        /// <summary>
        /// Sends the database changes to the remote server.
        /// </summary>
        public static void SendDatabaseChanges()
        {
            var changes = ChangeList.ToArray();
            ChangeList.Clear();

            Remote.API.SendDatabaseChanges(changes);
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

                var sint   = show["showid"].ToInteger() * 100000;
                var marked = Database.Query("select distinct episodeid from tracking where showid = ? order by episodeid asc", show["showid"]);

                // the marked episodes member is a list of integer array with two numbers
                // the first number represents the start of the range, while the second one represents the end of the range
                // by storing the whole list of marked episodes, my database would be 32 kB
                // by storing just the ranges, my database serializes to 8 kB
                info.MarkedEpisodes = new List<int[]>();

                var start = 0;
                var prev  = 0;
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
                            info.MarkedEpisodes.Add(new[] { start, prev });
                        }

                        start = prev = ep;
                    }
                }

                if ((info.MarkedEpisodes.Count == 0 && start != 0 && prev != 0) || info.MarkedEpisodes.Last() != new[] { start, prev })
                {
                    info.MarkedEpisodes.Add(new[] { start, prev });
                }

                list.Add(info);
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

        /// <summary>
        /// Represents a small database change.
        /// </summary>
        public class ChangeOperation
        {
            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>The title.</value>
            public string Title { get; set; }

            /// <summary>
            /// Gets or sets the change type.
            /// </summary>
            /// <value>The change type.</value>
            public ChangeType Type { get; set; }

            /// <summary>
            /// Gets or sets the diff data.
            /// </summary>
            /// <value>The diff data.</value>
            public object Data { get; set; }

            /// <summary>
            /// Describes the type of the change.
            /// </summary>
            public enum ChangeType
            {
                /// <summary>
                /// An episode was marked as watched.
                /// </summary>
                EpisodeMarked,
                /// <summary>
                /// An episode was unmarked.
                /// </summary>
                EpisodeUnmarked,
                /// <summary>
                /// A show was added to the list.
                /// </summary>
                ShowAdded,
                /// <summary>
                /// A show was removed from the list.
                /// </summary>
                ShowRemoved,
                /// <summary>
                /// The grabber and/or the language of a show was modified.
                /// </summary>
                ShowModified,
                /// <summary>
                /// The list of the shows was reordered.
                /// </summary>
                RowIdUpdated
            }
        }
    }
}
