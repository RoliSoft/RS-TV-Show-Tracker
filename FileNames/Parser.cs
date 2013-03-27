namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;
    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Provides support for parsing scene release file names.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Contains a list of previously seen names associated to their <c>ShowID</c> information.
        /// </summary>
        public static readonly Dictionary<string, ShowID> ShowIDCache = new Dictionary<string, ShowID>();

        /// <summary>
        /// Contains a list of previously seen names associated to their <c>TVShow</c> information.
        /// </summary>
        public static readonly Dictionary<string, TVShow> TVShowCache = new Dictionary<string, TVShow>();

        /// <summary>
        /// Contains a list of all the known TV show names on lab.rolisoft.net.
        /// </summary>
        public static List<KnownTVShow> AllKnownTVShows = new List<KnownTVShow>();

        /// <summary>
        /// Contains a small list of popular TV shows with airdate notation in their file name.
        /// </summary>
        public static List<string> AirdateNotationShows = new List<string>
            {
                "dailyshow", "colbertreport", "tonightshowwithjayleno", "jayleno", "conan", "latelateshowwithcraigferguson",
                "craigferguson", "jimmykimmellive", "jimmykimmel", "realtimewithbillmaher", "latenightwithjimmyfallon",
                "jimmyfallon", "lateshowwithdavidletterman", "davidletterman", "sundayfootyshow", "sundayroast", "attackshow"
            };

        /// <summary>
        /// Gets the number of supported qualities.
        /// </summary>
        /// <value>
        /// The number of supported qualities.
        /// </value>
        public static int QualityCount { get; internal set; }

        /// <summary>
        /// Initializes the <see cref="Parser"/> class.
        /// </summary>
        static Parser()
        {
            QualityCount = Enum.GetValues(typeof(Qualities)).Length - 1;
        }

        /// <summary>
        /// Parses the name of the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="parents">The name of the parent directories.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked to identify a show after the local database failed.</param>
        /// <param name="extractEpisode">if set to <c>true</c> the file will be mapped to an episode in the database.</param>
        /// <returns>
        /// Parsed file information.
        /// </returns>
        public static ShowFile ParseFile(string file, string[] parents = null, bool askRemote = true, bool extractEpisode = true)
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

            var name = fi[0].ToUpper();
                name = Regexes.Contractions.Replace(name, string.Empty);
                name = Regexes.StartGroup.Replace(name, string.Empty);
                name = Regexes.SpecialChars.Replace(name, " ").Trim();

            var title = string.Empty;
            var date  = DateTime.MinValue;
            var match = false;
            var ltvsh = default(TVShow);
            var lepis = default(Episode);

            // try to identify show by file name

            if (!string.IsNullOrWhiteSpace(name))
            {
                var info = IdentifyShow(name, extractEpisode ? ep : null, askRemote);

                if (info != null)
                {
                    match = true;
                    name  = info.Item1;
                    title = info.Item2;
                    date  = info.Item3;
                    ltvsh = info.Item4;
                    lepis = info.Item5;
                }
            }

            // try to identify show by the name of the parent directories

            if (!match && parents != null)
            {
                for (var i = 1; i < 6; i++) // limit traversal up to 5 directory names, because identification can be expensive
                {
                    if ((parents.Length - i) <= 0) break;

                    var dir = Regexes.VolNumbering.Replace(Regexes.SpecialChars.Replace(parents[parents.Length - i].ToUpper(), " ").Trim(), string.Empty);
                    var dirinfo = IdentifyShow(dir, extractEpisode ? ep : null);

                    if (dirinfo != null)
                    {
                        match = true;
                        name  = dirinfo.Item1;
                        title = dirinfo.Item2;
                        date  = dirinfo.Item3;
                        ltvsh = dirinfo.Item4;
                        lepis = dirinfo.Item5;

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
                return new ShowFile(file, name.ToLower().ToUppercaseWords(), ep, title, quality, group, date, ltvsh, lepis, false)
                    {
                        ParseError = ShowFile.FailureReasons.ShowNotIdentified
                    };
            }

            return new ShowFile(file, name, ep, title, quality, group, date, ltvsh, lepis);
        }

        /// <summary>
        /// Identifies the name of the show.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="ep">The episode.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked to identify a show after the local database failed.</param>
        /// <returns>
        /// A tuple containing the show's and episode's title and airdate.
        /// </returns>
        private static Tuple<string, string, DateTime, TVShow, Episode> IdentifyShow(string name, ShowEpisode ep, bool askRemote = false)
        {
            var title = string.Empty;
            var date  = DateTime.MinValue;
            var match = false;
            var ltvsh = default(TVShow);
            var lepis = default(Episode);

            // try to find show in local database
            
            foreach (var show in Database.TVShows.Values.ToList())
            {
                var titleMatch   = ShowNames.Parser.GenerateTitleRegex(show.Name).Match(name);
                var releaseMatch = !string.IsNullOrWhiteSpace(show.Release) ? Regex.Match(name, show.Release) : null;

                if ((titleMatch.Success && titleMatch.Value == name) || (releaseMatch != null && releaseMatch.Success && releaseMatch.Value == name))
                {
                    if (ep == null)
                    {
                        match = true;
                        ltvsh = show;
                        name  = show.Name;

                        break;
                    }
                    else if (ep.AirDate != null)
                    {
                        var episode = show.Episodes.Where(x => x.Airdate.ToOriginalTimeZone(x.Show.TimeZone).Date == ep.AirDate.Value.Date).ToList();
                        if (episode.Count != 0)
                        {
                            match = true;
                            ltvsh = show;
                            name  = show.Name;
                            lepis = episode[0];
                            title = episode[0].Name;
                            date  = episode[0].Airdate;

                            ep.Season  = episode[0].Season;
                            ep.Episode = episode[0].Number;

                            break;
                        }
                    }
                    else
                    {
                        Episode episode;
                        if (show.EpisodeByID.TryGetValue(ep.Season * 1000 + ep.Episode, out episode))
                        {
                            match = true;
                            ltvsh = show;
                            name  = show.Name;
                            lepis = episode;
                            title = episode.Name;
                            date  = episode.Airdate;

                            break;
                        }
                    }
                }
            }
            
            // try to find show in the local cache of the list over at lab.rolisoft.net

            if (!match)
            {
                if (AllKnownTVShows.Count == 0)
                {
                    var path = Path.Combine(Signature.InstallPath, @"misc\tvshows");

                    if (File.Exists(path) && new FileInfo(path).Length != 0)
                    {
                        using (var fs = File.OpenRead(path))
                        using (var br = new BinaryReader(fs))
                        {
                            var ver = br.ReadByte();
                            var upd = br.ReadUInt32();
                            var cnt = br.ReadUInt32();

                            AllKnownTVShows = new List<KnownTVShow>();

                            for (var i = 0; i < cnt; i++)
                            {
                                var show = new KnownTVShow();

                                show.Title      = br.ReadString();
                                show.Slug       = br.ReadString();
                                show.Database   = br.ReadString();
                                show.DatabaseID = br.ReadString();

                                AllKnownTVShows.Add(show);
                            }
                        }
                    }
                    else
                    {
                        try { GetAllKnownTVShows(); } catch { }
                    }
                }

                var slug    = Utils.CreateSlug(name);
                var matches = new List<KnownTVShow>();

                foreach (var show in AllKnownTVShows)
                {
                    if (show.Slug == slug)
                    {
                        matches.Add(show);
                    }
                }

                if (matches.Count != 0 && ep == null)
                {
                    match = true;
                    name  = matches[0].Title;
                }
                else if (matches.Count != 0 && ep != null)
                {
                    TVShow local = null;

                    foreach (var mtch in matches)
                    {
                        foreach (var show in Database.TVShows.Values)
                        {
                            if (show.Source == mtch.Database && show.SourceID == mtch.DatabaseID)
                            {
                                local = show;
                                break;
                            }
                        }
                    }

                    if (local != null)
                    {
                        match = true;
                        name  = local.Name;

                        if (ep.AirDate != null)
                        {
                            var eps = local.Episodes.Where(ch => ch.Airdate.Date == ep.AirDate.Value.Date).ToList();
                            if (eps.Count() != 0)
                            {
                                ltvsh = eps[0].Show;
                                title = eps[0].Name;
                                lepis = eps[0];
                                date  = eps[0].Airdate;

                                ep.Season  = eps[0].Season;
                                ep.Episode = eps[0].Number;
                            }
                        }
                        else
                        {
                            var eps = local.Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                            if (eps.Count() != 0)
                            {
                                ltvsh = eps[0].Show;
                                title = eps[0].Name;
                                lepis = eps[0];
                                date  = eps[0].Airdate;
                            }
                        }
                    }
                    else if (ShowIDCache.ContainsKey(name) && TVShowCache.ContainsKey(name))
                    {
                        match = true;
                        name  = ShowIDCache[name].Title;

                        if (ep.AirDate != null)
                        {
                            var eps = TVShowCache[name].Episodes.Where(ch => ch.Airdate.Date == ep.AirDate.Value.Date).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;

                                ep.Season  = eps[0].Season;
                                ep.Episode = eps[0].Number;
                            }
                        }
                        else
                        {
                            var eps = TVShowCache[name].Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;
                            }
                        }
                    }
                    else if (askRemote)
                    {
                        var guide = Updater.CreateGuide(matches[0].Database);
                        var data  = guide.GetData(matches[0].DatabaseID);

                        ShowIDCache[name] = new ShowID { Title = data.Title };

                        match = true;
                        name  = data.Title;

                        TVShowCache[name] = data;
                        
                        if (ep.AirDate != null)
                        {
                            var eps = data.Episodes.Where(ch => ch.Airdate.Date == ep.AirDate.Value.Date).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;

                                ep.Season  = eps[0].Season;
                                ep.Episode = eps[0].Number;
                            }
                        }
                        else
                        {
                            var eps = data.Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;
                            }
                        }
                    }
                }
            }

            // try to find show in cache

            if (!match && ShowIDCache.ContainsKey(name))
            {
                match = true;
                name  = ShowIDCache[name].Title;

                if (ep != null)
                {
                    if (TVShowCache.ContainsKey(name))
                    {
                        if (ep.AirDate != null)
                        {
                            var eps = TVShowCache[name].Episodes.Where(ch => ch.Airdate.Date == ep.AirDate.Value.Date).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;

                                ep.Season  = eps[0].Season;
                                ep.Episode = eps[0].Number;
                            }
                        }
                        else
                        {
                            var eps = TVShowCache[name].Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;
                            }
                        }
                    }
                    else
                    {
                        match = false;
                    }
                }
            }

            // try to identify show using lab.rolisoft.net's API

            if (!match && askRemote)
            {
                var req = Remote.API.GetShowInfo(name, new[] { "Title", "Source", "SourceID" });

                if (req.Success)
                {
                    if (ep == null)
                    {
                        ShowIDCache[name] = new ShowID { Title = req.Title };

                        match = true;
                        name  = req.Title;
                    }
                    else
                    {
                        var guide = Updater.CreateGuide(req.Source);
                        var data  = guide.GetData(req.SourceID);

                        ShowIDCache[name] = new ShowID { Title = data.Title };

                        match = true;
                        name  = data.Title;

                        TVShowCache[name] = data;
                    
                        if (ep.AirDate != null)
                        {
                            var eps = data.Episodes.Where(ch => ch.Airdate.Date == ep.AirDate.Value.Date).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;

                                ep.Season  = eps[0].Season;
                                ep.Episode = eps[0].Number;
                            }
                        }
                        else
                        {
                            var eps = data.Episodes.Where(ch => ch.Season == ep.Season && ch.Number == ep.Episode).ToList();
                            if (eps.Count() != 0)
                            {
                                title = eps[0].Title;
                                date  = eps[0].Airdate;
                            }
                        }
                    }
                }
            }

            // return

            return match
                   ? new Tuple<string, string, DateTime, TVShow, Episode>(name, title, date, ltvsh, lepis)
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

            if (AreMatching(release, @"1080[ip]", @"(WEB[_\-\.\s]?DL|iTunesHD)"))
            {
                return Qualities.WebDL1080p;
            }
            if (AreMatching(release, @"1080[ip]", @"(Blu[_\-\.\s]?Ray|HD[_\-\.\s]?DVD|AVC)"))
            {
                return Qualities.BluRay1080p;
            }
            if (AreMatching(release, @"1080[ip]"))
            {
                return Qualities.HDTV1080i;
            }
            if (AreMatching(release, @"720p", @"(WEB[_\-\.\s]?DL|iTunesHD)"))
            {
                return Qualities.WebDL720p;
            }
            if (AreMatching(release, @"720p", @"(Blu[_\-\.\s]?Ray|HD[_\-\.\s]?DVD|AVC)"))
            {
                return Qualities.BluRay720p;
            }
            if (AreMatching(release, @"720p"))
            {
                return Qualities.HDTV720p;
            }
            if (AreMatching(release, @"576p", @"(WEB[_\-\.\s]?DL|iTunesHD)"))
            {
                return Qualities.WebDL576p;
            }
            if (AreMatching(release, @"576p", @"(Blu[_\-\.\s]?Ray|HD[_\-\.\s]?DVD|AVC)"))
            {
                return Qualities.BluRay576p;
            }
            if (AreMatching(release, @"576[ip]"))
            {
                return Qualities.HDTV576p;
            }
            if (AreMatching(release, @"480p", @"(WEB[_\-\.\s]?DL|iTunesHD)"))
            {
                return Qualities.WebDL480p;
            }
            if (AreMatching(release, @"480p", @"(Blu[_\-\.\s]?Ray|HD[_\-\.\s]?DVD|AVC)"))
            {
                return Qualities.BluRay480p;
            }
            if (AreMatching(release, @"480[ip]"))
            {
                return Qualities.HDTV480p;
            }
            if (AreMatching(release, @"DVD(?![_\-\.\s]?Rip)([_\-\.\s]?[R59])?"))
            {
                return Qualities.DVD;
            }
            if (AreMatching(release, @"DVD[_\-\.\s]?Rip", @"[Hx]264"))
            {
                return Qualities.DVDRipx264;
            }
            if (AreMatching(release, @"(HDTV|PDTV|DSR(ip)?|DTH(Rip)?|DVB[_\-\.\s]?Rip|PPV(Rip)?|VOD(R(ip)?)?)", @"[Hx]264"))
            {
                return Qualities.HDTVx264;
            }
            if (AreMatching(release, @"(B[DR][_\-\.\s]?Rip|BDR)") || AreMatching(release, @"(Blu[_\-\.\s]?Ray|HD[_\-\.\s]?DVD)", @"XviD"))
            {
                return Qualities.BDRipXviD;
            }
            if (AreMatching(release, @"DVD[_\-\.\s]?Rip"))
            {
                return Qualities.DVDRipXviD;
            }
            if (AreMatching(release, @"(WEB[_\-\.\s]?DL|iTunesHD)", @"[Hx]264"))
            {
                return Qualities.WebDLx264;
            }
            if (AreMatching(release, @"XviD", @"(WEB[_\-\.\s]?DL|iTunesHD)"))
            {
                return Qualities.WebDLXviD;
            }
            if (AreMatching(release, @"WEB(([_\-\.\s]?DL)?[_\-\.\s]?Rip)?", @"[Hx]264"))
            {
                return Qualities.WebRipx264;
            }
            if (AreMatching(release, @"WEB(([_\-\.\s]?DL)?[_\-\.\s]?Rip)?"))
            {
                return Qualities.WebRipXviD;
            }
            if (AreMatching(release, @"(S?VCD|VHS)([_\-\.\s]?Rip)?", @"[Hx]264"))
            {
                return Qualities.VHSRipx264;
            }
            if (AreMatching(release, @"(S?VCD|VHS)([_\-\.\s]?Rip)?"))
            {
                return Qualities.VHSRipXviD;
            }
            if (AreMatching(release, @"(SDTV|([SE]D)?TV[_\-\.\s]?Rip)"))
            {
                return Qualities.SDTVRip;
            }
            if (AreMatching(release, @"((HR|HQ|HiRes|High[_\-\.\s]?Res(olution)?)\b|[Hx]264)"))
            {
                return Qualities.HRx264;
            }
            if (AreMatching(release, @"(HDTV([_\-\.\s]?Rip)?|PDTV([_\-\.\s]?Rip)?|DSR(ip)?|DTH(Rip)?|DVB[_\-\.\s]?Rip|PPV(Rip)?|VOD(R(ip)?)?)"))
            {
                return Qualities.HDTVXviD;
            }
            if (AreMatching(release, @"((DVD)?Screener|(DVD|BR|BD)SCR|DDC|PreAir)"))
            {
                return Qualities.Screener;
            }

            // if quality can't be determined based on the release name,
            // try to make wild guesses based on the extension

            if (Regex.IsMatch(release, @"\.ts$", RegexOptions.IgnoreCase))
            {
                return Qualities.HDTV1080i;
            }
            if (Regex.IsMatch(release, @"\.mkv$", RegexOptions.IgnoreCase))
            {
                return Qualities.HDTV720p;
            }
            if (Regex.IsMatch(release, @"\.avi$", RegexOptions.IgnoreCase))
            {
                return Qualities.HDTVXviD;
            }
            if (Regex.IsMatch(release, @"\.m(ov|pg)$", RegexOptions.IgnoreCase))
            {
                return Qualities.SDTVRip;
            }

            return Qualities.Unknown;
        }

        /// <summary>
        /// Parses the edition of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted edition or Unknown.</returns>
        public static Editions ParseEdition(string release)
        {
            release = release.Replace((char)160, '.').Replace((char)32, '.');

            if (AreMatching(release, @"(TV[_\-\.\s]?Rip|HDTV|PDTV|DSR(ip)?|DTH(Rip)?|DVB[_\-\.\s]?Rip|PPV(Rip)?|VOD(R(ip)?)?)"))
            {
                return Editions.TV;
            }
            if (AreMatching(release, @"WEB[_\-\.]?(DL|Rip)"))
            {
                return Editions.WebDL;
            }
            if (AreMatching(release, @"(DVD|HD\-?DVD|Blu[_\-]?Ray|BD[59]|B[DR][_\-]?Rip|VHS)"))
            {
                return Editions.Retail;
            }
            if (AreMatching(release, @"((DVD)?Screener|(DVD|BR|BD)SCR|DDC|PreAir)"))
            {
                return Editions.Screener;
            }

            return Editions.Unknown;
        }

        /// <summary>
        /// Extension method to <c>Qualities</c> to convert them to their corresponding <c>Editions</c>.
        /// </summary>
        /// <param name="quality">The quality.</param>
        /// <returns>The corresponding edition.</returns>
        public static Editions ToEdition(this Qualities quality)
        {
            if (quality == Qualities.Screener)
            {
                return Editions.Screener;
            }

            if (Regex.IsMatch(quality.ToString(), @"^(SDTV|HDTV|HR)"))
            {
                return Editions.TV;
            }

            if (quality.ToString().StartsWith("Web"))
            {
                return Editions.WebDL;
            }

            if (Regex.IsMatch(quality.ToString(), @"^(VHSRip|DVD|BDRip|BluRay)"))
            {
                return Editions.Retail;
            }

            return Editions.Unknown;
        }

        /// <summary>
        /// Determines whether the specified input matches all the specified regexes.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="regexes">The regexes.</param>
        /// <returns>
        /// 	<c>true</c> if the specified input matches all the specified regexes; otherwise, <c>false</c>.
        /// </returns>
        private static bool AreMatching(string input, params string[] regexes)
        {
            return regexes.All(regex => Regex.IsMatch(input, @"(\b|_)" + regex + @"(\b|_)", RegexOptions.IgnoreCase));
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

        /// <summary>
        /// Gets the title of all known TV shows from lab.rolisoft.net.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the operation was successful; otherwise, <c>false</c>.
        /// </returns>
        public static bool GetAllKnownTVShows()
        {
            var req = Remote.API.GetListOfShows();

            if (!req.Success || req.Result.Count == 0)
            {
                return false;
            }

            AllKnownTVShows = new List<KnownTVShow>();

            foreach (var item in req.Result)
            {
                AllKnownTVShows.Add(new KnownTVShow
                    {
                        Title      = item[0],
                        Slug       = item[1],
                        Database   = item[2],
                        DatabaseID = item[3]
                    });
            }

            var path = Path.Combine(Signature.InstallPath, @"misc\tvshows");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)AllKnownTVShows.Count);

                foreach (var show in AllKnownTVShows)
                {
                    bw.Write(show.Title ?? string.Empty);
                    bw.Write(show.Slug ?? string.Empty);
                    bw.Write(show.Database ?? string.Empty);
                    bw.Write(show.DatabaseID ?? string.Empty);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the type of the episode notation for the specified show.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>
        /// Episode notation type.
        /// </returns>
        public static string GetEpisodeNotationType(int id)
        {
            if (AirdateNotationShows.Contains(Utils.CreateSlug(Database.TVShows[id].Name)))
            {
                return "airdate";
            }

            return Database.TVShows[id].Data.Get("notation", "standard");
        }

        /// <summary>
        /// Gets the type of the episode notation for the specified show.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <returns>
        /// Episode notation type.
        /// </returns>
        public static string GetEpisodeNotationType(string show)
        {
            if (AirdateNotationShows.Contains(Utils.CreateSlug(show)))
            {
                return "airdate";
            }

            var ids = Database.TVShows.Values.Where(x => x.Name == show).ToList();
            if (ids.Count != 0)
            {
                return ids[0].Data.Get("notation", "standard");
            }

            return "standard";
        }

        /// <summary>
        /// Represents a known TV show.
        /// </summary>
        public class KnownTVShow
        {
            /// <summary>
            /// Gets or sets the title of the TV show.
            /// </summary>
            /// <value>
            /// The title of the TV show.
            /// </value>
            public string Title { get; set; }

            /// <summary>
            /// Gets or sets the slug name of the TV show.
            /// </summary>
            /// <value>
            /// The slug name of the TV show.
            /// </value>
            public string Slug { get; set; }

            /// <summary>
            /// Gets or sets the database.
            /// </summary>
            /// <value>
            /// The database.
            /// </value>
            public string Database { get; set; }

            /// <summary>
            /// Gets or sets the ID on the database.
            /// </summary>
            /// <value>
            /// The ID on the database.
            /// </value>
            public string DatabaseID { get; set; }
        }
    }
}
