namespace RoliSoft.TVShowTracker.FileNames
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;
    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Provides support for parsing scene release file names.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// A list of uppercase keywords which will be removed if the file name starts with them.
        /// </summary>
        public static readonly string[] Keywords = new[] { "AAF-" };

        /// <summary>
        /// A regular expression dynamically generated from the list of keywords above.
        /// </summary>
        public static readonly Regex RemoveKeywords = new Regex("^(" + Keywords.Aggregate((str, keyword) => Regex.Escape(keyword) + "|").TrimEnd('|') + ")");

        /// <summary>
        /// Contains a list of previously seen names associated to their <c>ShowID</c> information.
        /// </summary>
        public static readonly Dictionary<string, ShowID> ShowIDCache = new Dictionary<string, ShowID>();

        /// <summary>
        /// Contains a list of previously seen names associated to their <c>TVShow</c> information.
        /// </summary>
        public static readonly Dictionary<string, TVShow> TVShowCache = new Dictionary<string, TVShow>();

        /// <summary>
        /// Parses the name of the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Parsed file information.</returns>
        public static ShowFile ParseFile(string file)
        {
            // split the name into two parts: before and after the episode numbering

            var fi = Regexes.AdvNumbering.Split(file);
            var ep = Tools.ExtractEpisode(fi[1]);

            // clean name

            var name  = Regexes.SpecialChars.Replace(RemoveKeywords.Replace(fi[0].ToUpper(), string.Empty).Trim(), " ").Trim();
            var title = string.Empty;

            // try to find show in local database

            var match = false;
            var shows = Database.Query("select showid, name from tvshows");

            foreach (var show in shows)
            {
                var titleParts = Tools.GetRoot(show["name"]);
                if (titleParts.All(part => Regex.IsMatch(name, @"\b" + part + @"\b", RegexOptions.IgnoreCase)))
                {
                    var episode = Database.Query("select name from episodes where episodeid = ?", ep.Episode + (ep.Season * 1000) + (show["showid"].ToInteger() * 100 * 1000));
                    if (episode.Count != 0)
                    {
                        match = true;
                        name  = show["name"];
                        title = episode[0]["name"];

                        break;
                    }
                }
            }

            // try to find show in cache

            if (!match && ShowIDCache.ContainsKey(name))
            {
                match = true;
                name  = ShowIDCache[name].Title;

                var eps = TVShowCache[name].Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                if (eps.Count() != 0)
                {
                    title = eps[0].Title;
                }
            }

            // try to identify show using TVRage's API

            if (!match)
            {
                var guide = new TVRage();
                var ids   = guide.GetID(name).ToList();

                if (ids.Count != 0)
                {
                    ShowIDCache[name] = ids[0];

                    match = true;
                    name  = ids[0].Title;

                    var tvshow = guide.GetData(ids[0].ID);
                    TVShowCache[name] = tvshow;

                    var eps = tvshow.Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                    if (eps.Count() != 0)
                    {
                        title = eps[0].Title;
                    }
                }
            }

            // if name or title was not found, try to improvise

            if (!match)
            {
                name = name.ToLower().ToUppercaseWords();
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Season {0}, Episode {1}".FormatWith(ep.Season, ep.Episode);
            }

            return new ShowFile(file, name, ep, title);
        }
    }
}
