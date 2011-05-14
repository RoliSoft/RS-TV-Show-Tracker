namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Provides support for parsing scene release file names.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// A list of uppercase keywords which will be removed if the file name starts with them.
        /// </summary>
        public static readonly string[] Keywords = new[] { "AAF-", "MED-" };

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
        /// <param name="parents">The name of the parent directories.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked to identify a show after the local database failed.</param>
        /// <returns>Parsed file information.</returns>
        public static ShowFile ParseFile(string file, string[] parents = null, bool askRemote = true)
        {
            // split the name into two parts: before and after the episode numbering

            var fi = Regexes.AdvNumbering.Split(file);

            if (fi.Length < 2)
            {
                return new ShowFile(file, ShowFile.FailureReasons.EpisodeNumberingNotFound);
            }

            var ep = ShowNames.Parser.ExtractEpisode(fi[1]);

            if (ep == null)
            {
                return new ShowFile(file, ShowFile.FailureReasons.EpisodeNumberingNotFound);
            }

            // clean name

            var name  = Regexes.SpecialChars.Replace(RemoveKeywords.Replace(fi[0].ToUpper(), string.Empty).Trim(), " ").Trim();
            var title = string.Empty;
            var date  = DateTime.MinValue;
            var match = false;

            // try to identify show by file name

            if (!string.IsNullOrWhiteSpace(name))
            {
                var info = IdentifyShow(name, ep, askRemote);

                if (info != null)
                {
                    match = true;
                    name  = info.Item1;
                    title = info.Item2;
                    date  = info.Item3;
                }
            }

            // try to identify show by the name of the parent directories

            if (!match && parents != null)
            {
                for (var i = 1; i < 6; i++) // limit traversal up to 5 directory names, because identification can be expensive
                {
                    if ((parents.Length - i) >= 0) break;

                    var dir = Regexes.VolNumbering.Replace(Regexes.SpecialChars.Replace(RemoveKeywords.Replace(parents[parents.Length - i].ToUpper(), string.Empty).Trim(), " ").Trim(), string.Empty);
                    var dirinfo = IdentifyShow(dir, ep);

                    if (dirinfo != null)
                    {
                        match = true;
                        name  = dirinfo.Item1;
                        title = dirinfo.Item2;
                        date  = dirinfo.Item3;

                        break;
                    }
                }
            }

            // if no name was found and none of the directories match stop the identification

            if (!match && string.IsNullOrWhiteSpace(name))
            {
                return new ShowFile(file, ShowFile.FailureReasons.ShowNameNotFound);
            }

            // extract quality and group

            var path    = parents != null ? string.Join(" ", parents) + " " + file : file; 
            var quality = ParseQuality(path).GetAttribute<DescriptionAttribute>().Description;
            var groupm  = Regexes.Group.Match(path);
            var group   = groupm.Success
                          ? groupm.Groups[1].Value
                          : string.Empty;

            // if name or title was not found, try to improvise

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Season {0}, Episode {1}".FormatWith(ep.Season, ep.Episode);
            }

            if (!match)
            {
                return new ShowFile(file, name.ToLower().ToUppercaseWords(), ep, title, quality, group, date, false)
                    {
                        ParseError = ShowFile.FailureReasons.ShowNotIdentified
                    };
            }

            return new ShowFile(file, name, ep, title, quality, group, date);
        }

        /// <summary>
        /// Identifies the name of the show.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="ep">The episode.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked to identify a show after the local database failed.</param>
        /// <returns></returns>
        private static Tuple<string, string, DateTime> IdentifyShow(string name, ShowEpisode ep, bool askRemote = false)
        {
            var title = string.Empty;
            var date  = DateTime.MinValue;
            var match = false;

            // try to find show in local database

            if (LocalTVShows == null || LoadDate < Database.DataChange)
            {
                LoadDate     = DateTime.Now;
                LocalTVShows = Database.Query("select showid, name, release from tvshows");
            }

            var fileParts = ShowNames.Parser.GetRoot(name);

            foreach (var show in LocalTVShows)
            {
                var titleParts = string.IsNullOrWhiteSpace(show["release"])
                               ? ShowNames.Parser.GetRoot(show["name"])
                               : show["release"].Split(' ');

                if (titleParts.SequenceEqual(fileParts))
                {
                    var episode = Database.Query("select name, airdate from episodes where episodeid = ?", ep.Episode + (ep.Season * 1000) + (show["showid"].ToInteger() * 100 * 1000));
                    if (episode.Count != 0)
                    {
                        match = true;
                        name  = show["name"];
                        title = episode[0]["name"];
                        date  = episode[0]["airdate"].ToDouble().GetUnixTimestamp();

                        break;
                    }
                }
            }

            // try to find show in cache

            if (!match && askRemote && ShowIDCache.ContainsKey(name))
            {
                match = true;
                name  = ShowIDCache[name].Title;

                var eps = TVShowCache[name].Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                if (eps.Count() != 0)
                {
                    title = eps[0].Title;
                    date  = eps[0].Airdate;
                }
            }

            // try to identify show using lab.rolisoft.net's API

            if (!match && askRemote)
            {
                var req = Remote.API.GetShowInfo(name, new[] { "Title", "Source", "SourceID" });

                if (req.Success)
                {
                    var guide = Updater.CreateGuide(req.Source);
                    var data  = guide.GetData(req.SourceID);

                    ShowIDCache[name] = new ShowID { Title = data.Title };

                    match = true;
                    name  = data.Title;

                    TVShowCache[name] = data;

                    var eps = data.Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                    if (eps.Count() != 0)
                    {
                        title = eps[0].Title;
                        date  = eps[0].Airdate;
                    }
                }
            }

            // return

            return match
                   ? new Tuple<string, string, DateTime>(name, title, date)
                   : null;
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
            var variables = new Dictionary<string, string>
                {
                    {
	                    "$show",
	                    file.Show
                    },
                    {
	                    "$seasonz",
	                    file.Episode.Season.ToString("0")
                    },
                    {
	                    "$season",
	                    file.Episode.Season.ToString("00")
                    },
                    {
	                    "$episodez",
	                    file.Episode.SecondEpisode.HasValue ? file.Episode.Episode.ToString("0") + "-" + file.Episode.SecondEpisode.Value.ToString("0") : file.Episode.Episode.ToString("0")
                    },
                    {
	                    "$episode",
	                    file.Episode.SecondEpisode.HasValue ? file.Episode.Episode.ToString("00") + "-" + file.Episode.SecondEpisode.Value.ToString("00") : file.Episode.Episode.ToString("00")
                    },
                    {
	                    "$title",
	                    file.Episode.SecondEpisode.HasValue ? Regexes.PartText.Replace(file.Title, string.Empty) : file.Title
                    },
                    {
	                    "$quality",
	                    file.Quality
                    },
                    {
	                    "$group",
	                    file.Group
                    },
                    {
	                    "$ext",
	                    file.Extension
                    },
                    {
	                    "$year",
	                    file.Airdate.Year.ToString()
                    },
                    {
	                    "$monthz",
	                    file.Airdate.Month.ToString()
                    },
                    {
	                    "$month",
	                    file.Airdate.Month.ToString("00")
                    },
                    {
	                    "$dayz",
	                    file.Airdate.Day.ToString()
                    },
                    {
	                    "$day",
	                    file.Airdate.Day.ToString("00")
                    }
                };

            foreach (var variable in variables)
            {
                format = format.Replace(variable.Key, variable.Value);
            }

            return format;
        }
    }
}
