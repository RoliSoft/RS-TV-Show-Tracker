namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
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
        /// Contains a list of show names and their IDs from the local database.
        /// </summary>
        public static List<Dictionary<string, string>> LocalTVShows;

        /// <summary>
        /// Contains the date when the <c>LocalTVShows</c> list was loaded.
        /// </summary>
        public static DateTime LoadDate;

        /// <summary>
        /// Parses the name of the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="askExternalGuide">if set to <c>true</c> TVRage's API will be asked to identify a show after the local database failed.</param>
        /// <returns>Parsed file information.</returns>
        public static ShowFile ParseFile(string file, bool askExternalGuide = true)
        {
            // split the name into two parts: before and after the episode numbering

            var fi = Regexes.AdvNumbering.Split(file);

            if (fi.Length < 2)
            {
                return new ShowFile();
            }

            var ep = ShowNames.Parser.ExtractEpisode(fi[1]);

            if (ep == null)
            {
                return new ShowFile();
            }

            // clean name

            var name  = Regexes.SpecialChars.Replace(RemoveKeywords.Replace(fi[0].ToUpper(), string.Empty).Trim(), " ").Trim();
            var title = string.Empty;

            // try to find show in local database

            var match = false;

            if (LocalTVShows == null || LoadDate < Database.DataChange)
            {
                LoadDate     = DateTime.Now;
                LocalTVShows = Database.Query("select showid, name from tvshows");
            }

            foreach (var show in LocalTVShows)
            {
                var titleParts = ShowNames.Parser.GetRoot(show["name"]);
                var fileParts  = ShowNames.Parser.GetRoot(name);

                if (titleParts.SequenceEqual(fileParts))
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

            if (!match && askExternalGuide && ShowIDCache.ContainsKey(name))
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

            if (!match && askExternalGuide)
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

            var quality = ParseQuality(file).GetAttribute<DescriptionAttribute>().Description;

            return new ShowFile(file, name, ep, title, quality, match);
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            release = release.Replace((char)160, '.').Replace((char)32, '.');

            if (IsMatch(release, @"\b1080(i|p)\b", @"\bWEB[_\-\.]?DL\b"))
            {
                return Qualities.WebDL1080p;
            }
            if (IsMatch(release, @"\b1080(i|p)\b", @"\bBlu[_\-]?Ray\b"))
            {
                return Qualities.BluRay1080p;
            }
            if (IsMatch(release, @"\b1080(i|p)\b", @"\bHDTV\b"))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch(release, @"\b720p\b", @"\bWEB[_\-\.]?DL\b"))
            {
                return Qualities.WebDL720p;
            }
            if (IsMatch(release, @"\b720p\b", @"\bBlu[_\-]?Ray\b"))
            {
                return Qualities.BluRay720p;
            }
            if (IsMatch(release, @"\b720p\b", @"\bHDTV\b"))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch(release, @"\b((HR|HiRes|High[_\-\.]?Resolution)\b|x264\-|H264)"))
            {
                return Qualities.HRx264;
            }
            if (IsMatch(release, @"\b(HDTV|PDTV|DVBRip|DVDRip)\b"))
            {
                return Qualities.HDTVXviD;
            }
            if (IsMatch(release, @"\bTV[_\-\.]?Rip\b"))
            {
                return Qualities.TVRip;
            }

            // if quality can't be determined based on the release name,
            // try to make wild guesses based on the extension

            if (IsMatch(release, @"\.ts$"))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch(release, @"\.mkv$"))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch(release, @"\.avi$"))
            {
                return Qualities.HDTVXviD;
            }
            if (IsMatch(release, @"\.m(ov|pg)$"))
            {
                return Qualities.TVRip;
            }

            return Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified input is matches all the specified regexes.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="regexes">The regexes.</param>
        /// <returns>
        /// 	<c>true</c> if the specified input matches all the specified regexes; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsMatch(string input, params string[] regexes)
        {
            return regexes.All(regex => Regex.IsMatch(input, regex, RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Generates a new name.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="file">The file.</param>
        /// <returns>New file name.</returns>
        public static string FormatFileName(string format, ShowFile file)
        {
            return format.Replace("$show", file.Show)
                         .Replace("$seasonz", file.Season.ToString("0"))
                         .Replace("$season", file.Season.ToString("00"))
                         .Replace("$episodez", file.SecondEpisode.HasValue ? file.Episode.ToString("0") + "-" + file.SecondEpisode.Value.ToString("0") : file.Episode.ToString("0"))
                         .Replace("$episode", file.SecondEpisode.HasValue ? file.Episode.ToString("00") + "-" + file.SecondEpisode.Value.ToString("00") : file.Episode.ToString("00"))
                         .Replace("$title", file.SecondEpisode.HasValue ? Regexes.PartText.Replace(file.Title, string.Empty) : file.Title)
                         .Replace("$quality", file.Quality)
                         .Replace("$ext", file.Extension);
        }
    }
}
