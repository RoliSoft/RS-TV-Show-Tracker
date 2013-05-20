namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Provides support for searching and downloading links automatically.
    /// </summary>
    public static class AutoDownloader
    {
        /// <summary>
        /// Gets the search engines loaded in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public static IEnumerable<DownloadSearchEngine> SearchEngines
        {
            get
            {
                return Extensibility.GetNewInstances<DownloadSearchEngine>();
            }
        }

        /// <summary>
        /// Gets the search engines activated in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public static IEnumerable<DownloadSearchEngine> ActiveSearchEngines
        {
            get
            {
                return SearchEngines.Where(engine => Actives.Contains(engine.Name));
            }
        }

        /// <summary>
        /// Gets the search engines activated in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public static IEnumerable<DownloadSearchEngine> AutoSearchEngines
        {
            get
            {
                return SearchEngines.Where(engine => AutoActives.Contains(engine.Name));
            }
        }

        /// <summary>
        /// Gets or sets the list of loaded parsers.
        /// </summary>
        /// <value>
        /// The loaded parsers.
        /// </value>
        public static List<string> Parsers { get; set; }

        /// <summary>
        /// Gets or sets the list of activated parsers.
        /// </summary>
        /// <value>
        /// The activated parsers.
        /// </value>
        public static List<string> Actives { get; set; }

        /// <summary>
        /// Gets or sets the list of activated parsers for auto-downloading.
        /// </summary>
        /// <value>
        /// The activated parsers for auto-downloading.
        /// </value>
        public static List<string> AutoActives { get; set; }

        /// <summary>
        /// Gets or sets the preferred quality.
        /// </summary>
        /// <value>
        /// The preferred quality.
        /// </value>
        public static Qualities PreferredQuality { get; set; }

        /// <summary>
        /// Gets or sets the second preferred quality.
        /// </summary>
        /// <value>
        /// The second preferred quality.
        /// </value>
        public static Qualities SecondPreferredQuality { get; set; }

        /// <summary>
        /// Gets or sets the time to wait for the preferred quality.
        /// </summary>
        /// <value>
        /// The time to wait for the preferred quality.
        /// </value>
        public static TimeSpan WaitForPreferred { get; set; }

        /// <summary>
        /// Initializes the <see cref="AutoDownloader"/> class.
        /// </summary>
        static AutoDownloader()
        {
            LoadParsers();
        }

        /// <summary>
        /// Loads the parsers.
        /// </summary>
        public static void LoadParsers()
        {
            Actives     = Settings.Get<List<string>>("Active Trackers");
            AutoActives = Settings.Get<List<string>>("Auto-Download Trackers");
            Parsers     = Settings.Get<List<string>>("Tracker Order");
            Parsers.AddRange(SearchEngines
                             .Where(engine => Parsers.IndexOf(engine.Name) == -1)
                             .Select(engine => engine.Name));

            WaitForPreferred = TimeSpan.FromSeconds(Settings.Get("Wait for Preferred Quality", TimeSpan.FromDays(2).TotalSeconds));
            PreferredQuality = (Qualities)Enum.Parse(typeof(Qualities), Settings.Get("Preferred Download Quality", Qualities.WebDL1080p.ToString()));
            SecondPreferredQuality = (Qualities)Enum.Parse(typeof(Qualities), Settings.Get("Second Preferred Download Quality", Qualities.HDTV720p.ToString()));
        }

        /// <summary>
        /// Searches for the missing episodes.
        /// </summary>
        public static async void SearchForMissingEpisodes()
        {
            if (!Signature.IsActivated)
            {
                return;
            }

            Log.Debug("Searching links for missing episodes...");

            var eps = GetRecentUnwatchedEps();

            foreach (var ep in eps)
            {
                Log.Debug("Inspecting " + ep + "...");

                // is there any file downloaded which matches the rules set?

                HashSet<string> fns;
                if (Library.Files.TryGetValue(ep.ID, out fns))
                {
                    Log.Trace("Found downloaded files.", fns);

                    var match = fns.FirstOrDefault(file => IsMatchingRule(ep, file));

                    if (match != null)
                    {
                        Log.Debug("File " + Path.GetFileName(match) + " matches your preference, skipping episode.");
                        continue;
                    }
                    else
                    {
                        Log.Debug("None of the downloaded files match your preference, continuing with search.");
                    }
                }
                else
                {
                    Log.Debug("No files found for this episode, continuing with search.");
                }

                // if not, initiate the search for links

                await Task.Delay(TimeSpan.FromSeconds(5));
                var links = await SearchForLinks(ep);

                Log.Debug(Utils.FormatNumber(links.Count, "link") + " found for episode " + ep + ".");
                Log.Trace("Links for " + ep + ":", links.Select(l => l.ToString()));

                if (links.Count == 0)
                {
                    continue;
                }
                
                var link = SelectBestLink(links);

                if (IsMatchingRule(ep, link))
                {
                    Log.Info("Downloading link " + link + " for " + ep + "...");
                    //DownloadFile(link);
                }
                else
                {
                    Log.Debug("The selected link " + link + " for " + ep + " doesn't match your preference.");
                }
            }
        }

        /// <summary>
        /// Compiles a list of recently aired (3 week) and unwatched episodes.
        /// </summary>
        /// <returns>
        /// A list of recently aired (3 week) and unwatched episodes.
        /// </returns>
        public static List<Episode> GetRecentUnwatchedEps()
        {
            return Database.TVShows.Values.SelectMany(s => s.Episodes).Where(x => !x.Watched && x.Airdate < DateTime.Now && (DateTime.Now - x.Airdate).TotalDays < 21).ToList();
        }

        /// <summary>
        /// Searches for the specified episode and returns the download links.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// List of download links.
        /// </returns>
        public static async Task<List<Link>> SearchForLinks(Episode ep)
        {
            var links = new List<Link>();
            var dlsrc = new DownloadSearch(AutoSearchEngines, true);

            dlsrc.DownloadSearchEngineNewLink += (s, e) => links.Add(e.Data);

            var tasks = dlsrc.SearchAsync(ep.Show.Name + " " + (ep.Show.Data.Get("notation") == "airdate" ? ep.Airdate.ToOriginalTimeZone(ep.Show.TimeZone).ToString("yyyy.MM.dd") : string.Format("S{0:00}E{1:00}", ep.Season, ep.Number)));

            await Task.WhenAll(tasks);

            return links;
        }
        
        /// <summary>
        /// Selects the best link based on site priority and content quality.
        /// </summary>
        /// <param name="links">The links to choose from.</param>
        /// <returns>
        /// The best link.
        /// </returns>
        public static Link SelectBestLink(List<Link> links)
        {
            if (links.Count == 0)
            {
                Log.Debug("No links specified.");
                return null;
            }

            if (links.Count == 1)
            {
                Log.Debug("The only link is " + links[0]);
                return links[0];
            }

            // order links to decrease by site priority and quality

            var ordered = links
                          .OrderBy(link => FileNames.Parser.QualityCount - (int)link.Quality)
                          .ThenBy(link => Parsers.IndexOf(link.Source.Name));

            // get the preferred quality from the highest priority site

            var best = ordered.FirstOrDefault(link => link.Quality == PreferredQuality);

            if (best != null)
            {
                Log.Debug("The best link is " + best);
                return best;
            }

            // get the second preferred quality from the highest priority site

            var sbest = ordered.FirstOrDefault(link => link.Quality == SecondPreferredQuality);

            if (sbest != null)
            {
                Log.Debug("The second-best link is " + sbest);
                return sbest;
            }

            // return the highest found quality from the highest priority site

            var last = ordered.First();

            Log.Debug("The closest link is " + last);

            return last;
        }

        /// <summary>
        /// Determines whether the specified file matches the rule set for the specified show
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="file">The file.</param>
        /// <returns>
        ///   <c>true</c> if the specified file matches the rule; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatchingRule(Episode episode, string file)
        {
            var quality = FileNames.Parser.ParseQuality(file);

            return quality == PreferredQuality ||
                   ((DateTime.Now - episode.Airdate) > WaitForPreferred && quality == SecondPreferredQuality);
        }

        /// <summary>
        /// Determines whether the specified link matches the rule set for the specified show
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="link">The link.</param>
        /// <returns>
        ///   <c>true</c> if the specified link matches the rule; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatchingRule(Episode episode, Link link)
        {
            return link.Quality == PreferredQuality ||
                   ((DateTime.Now - episode.Airdate) > WaitForPreferred && link.Quality == SecondPreferredQuality);
        }

        /// <summary>
        /// Downloads the specified file to the default download path.
        /// </summary>
        /// <param name="link">The link.</param>
        public static void DownloadFile(Link link)
        {
            var file = Path.Combine(Settings.Get("Automatic Download Path"), Utils.SanitizeFileName(link.Release.CutIfLonger(200)).Replace('/', '-'));

            switch (link.Source.Type)
            {
                case Types.Torrent:
                case Types.Usenet:
                    link.Source.Downloader.Download(link, file + (link.Source.Type == Types.Torrent ? ".torrent" : link.Source.Type == Types.Usenet ? ".nzb" : string.Empty));
                    break;

                case Types.DirectHTTP:
                    File.WriteAllText(file + ".rsdf", DLCAPI.CreateRSDF(link.FileURL.Split('\0')));
                    break;

                case Types.HTTP:
                    File.WriteAllText(file + ".url", "[InternetShortcut]\r\nURL=" + (link.FileURL ?? link.InfoURL));
                    return;
            }
        }
    }
}
