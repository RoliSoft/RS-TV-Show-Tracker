namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using ProtoBuf;

    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Provides support for searching and downloading links automatically.
    /// </summary>
    public static class AutoDownloader
    {
        /// <summary>
        /// Gets or sets the search engines loaded in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public static IEnumerable<DownloadSearchEngine> SearchEngines { get; set; }

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
        /// Gets or sets the missing episodes.
        /// </summary>
        /// <value>
        /// The missing episodes.
        /// </value>
        public static List<MissingEpisode> MissingEpisodes { get; set; }

        /// <summary>
        /// Initializes the <see cref="AutoDownloader"/> class.
        /// </summary>
        static AutoDownloader()
        {
            LoadParsers();
            LoadMissingEpisodes();
        }

        /// <summary>
        /// Loads the parsers.
        /// </summary>
        public static void LoadParsers()
        {
            SearchEngines = typeof(DownloadSearchEngine)
                            .GetDerivedTypes()
                            .Select(type => Activator.CreateInstance(type) as DownloadSearchEngine);

            Actives = Settings.Get<List<string>>("Active Trackers");
            Parsers = Settings.Get<List<string>>("Tracker Order");
            Parsers.AddRange(SearchEngines
                             .Where(engine => Parsers.IndexOf(engine.Name) == -1)
                             .Select(engine => engine.Name));

            WaitForPreferred = TimeSpan.FromSeconds(Settings.Get("Wait for Preferred Quality", TimeSpan.FromDays(2).TotalSeconds));
            PreferredQuality = (Qualities)Enum.Parse(typeof(Qualities), Settings.Get("Preferred Download Quality", Qualities.HDTV720p.ToString()));
            SecondPreferredQuality = (Qualities)Enum.Parse(typeof(Qualities), Settings.Get("Second Preferred Download Quality", Qualities.HDTVXviD.ToString()));
        }

        /// <summary>
        /// Searches for the missing episodes.
        /// </summary>
        public static void SearchForMissingEpisodes()
        {
            RefreshMissingEpisodes();

            foreach (var ep in MissingEpisodes)
            {
                if (ep.Downloaded)
                {
                    continue;
                }

                ep.LastSearch = DateTime.Now;

                var links = SearchForEpisode(ep.Episode);

                if (links.Count == 0)
                {
                    ep.LastBestMatch = null;
                    continue;
                }

                var link = SelectBestLink(links);

                ep.LastBestMatch = new[] { link.Release, link.Quality.ToString(), link.Size, link.Source.Name };

                if (link.Quality == PreferredQuality
                 || (DateTime.Now - ep.Episode.Airdate) > WaitForPreferred)
                {
                    ep.Downloaded = true;

                    DownloadFile(link);
                }
            }

            SaveMissingEpisodes();
        }

        /// <summary>
        /// Refreshes the list of missing episodes.
        /// </summary>
        public static void RefreshMissingEpisodes()
        {
            MissingEpisodes.RemoveAll(ep => ep.Episode == null || (DateTime.Now - ep.Episode.Airdate).TotalDays > 21);

            foreach (var ep in Database.Episodes.Where(x => !x.Watched && x.Airdate < DateTime.Now && (DateTime.Now - x.Airdate).TotalDays < 21 && MissingEpisodes.Count(y => y.EpisodeID == x.EpisodeID) == 0))
            {
                MissingEpisodes.Add(new MissingEpisode(ep));
            }
        }

        /// <summary>
        /// Searches for the specified episode and returns the download links.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// List of download links.
        /// </returns>
        public static List<Link> SearchForEpisode(Episode ep)
        {
            var links = new List<Link>();
            var dlsrc = new DownloadSearch(ActiveSearchEngines, true);
            var start = DateTime.Now;
            var busy  = true;

            dlsrc.DownloadSearchProgressChanged += (s, e) => links.AddRange(e.First);
            dlsrc.DownloadSearchDone            += (s, e) => { busy = false; };

            dlsrc.SearchAsync(ep.Show.Name + " " + (ep.Show.Data.Get("notation") == "airdate" ? ep.Airdate.ToOriginalTimeZone(ep.Show.Data.Get("timezone")).ToString("yyyy.MM.dd") : string.Format("S{0:00}E{1:00}", ep.Season, ep.Number)));

            while (busy && (DateTime.Now - start).TotalMinutes < 1)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

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
                return null;
            }

            if (links.Count == 1)
            {
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
                return best;
            }

            // get the second preferred quality from the highest priority site

            var sbest = ordered.FirstOrDefault(link => link.Quality == SecondPreferredQuality);

            if (sbest != null)
            {
                return sbest;
            }

            // return the highest found quality from the highest priority site

            return ordered.First();
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

        /// <summary>
        /// Loads the list of missing episodes and their current states.
        /// </summary>
        public static void LoadMissingEpisodes()
        {
            var path = Path.Combine(Signature.FullPath, "AutoDownloader.bin");

            if (File.Exists(path))
            {
                using (var file = File.OpenRead(path))
                {
                    MissingEpisodes = Serializer.Deserialize<List<MissingEpisode>>(file);
                }
            }
            else
            {
                MissingEpisodes = new List<MissingEpisode>();
            }
        }

        /// <summary>
        /// Saves the list of missing episodes and their current states.
        /// </summary>
        public static void SaveMissingEpisodes()
        {
            using (var file = File.Create(Path.Combine(Signature.FullPath, "AutoDownloader.bin")))
            {
                Serializer.Serialize(file, MissingEpisodes);
            }
        }

        /// <summary>
        /// Represents a missing episode.
        /// </summary>
        [ProtoContract]
        public class MissingEpisode
        {
            /// <summary>
            /// Gets or sets the episode ID.
            /// </summary>
            /// <value>
            /// The episode ID.
            /// </value>
            [ProtoMember(1)]
            public int EpisodeID { get; set; }

            private Episode _episode;

            /// <summary>
            /// Gets the episode.
            /// </summary>
            public Episode Episode
            {
                get
                {
                    return _episode ?? (_episode = Database.Episodes.FirstOrDefault(ep => ep.EpisodeID == EpisodeID));
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="MissingEpisode"/> has been downloaded.
            /// </summary>
            /// <value>
            ///   <c>true</c> if it's been downloaded; otherwise, <c>false</c>.
            /// </value>
            [ProtoMember(2)]
            public bool Downloaded { get; set; }

            /// <summary>
            /// Gets or sets the date of the last search.
            /// </summary>
            /// <value>
            /// The date of the last search.
            /// </value>
            [ProtoMember(3)]
            public DateTime LastSearch { get; set; }

            /// <summary>
            /// Gets or sets the last best match.
            /// </summary>
            /// <value>
            /// The last best match.
            /// </value>
            [ProtoMember(4)]
            public string[] LastBestMatch { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="MissingEpisode"/> class.
            /// </summary>
            public MissingEpisode()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MissingEpisode"/> class.
            /// </summary>
            /// <param name="ep">The episode.</param>
            public MissingEpisode(Episode ep)
            {
                EpisodeID = ep.EpisodeID;
            }
        }
    }
}
