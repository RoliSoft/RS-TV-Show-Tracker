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
        /// Serializes the list of followed TV shows and their marked episodes.
        /// </summary>
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
    }
}
