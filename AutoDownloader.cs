namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

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
        /// Gets or sets the list of qualities in decreasing order.
        /// </summary>
        /// <value>
        /// The qualities.
        /// </value>
        public static List<string> Qualities { get; set; }

        /// <summary>
        /// Gets or sets the list of activated parsers.
        /// </summary>
        /// <value>
        /// The activated parsers.
        /// </value>
        public static List<string> Actives { get; set; }

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
            SearchEngines = typeof(DownloadSearchEngine)
                            .GetDerivedTypes()
                            .Select(type => Activator.CreateInstance(type) as DownloadSearchEngine);

            Actives = Settings.Get<List<string>>("Active Trackers");
            Parsers = Settings.Get<List<string>>("Tracker Order");
            Parsers.AddRange(SearchEngines
                             .Where(engine => Parsers.IndexOf(engine.Name) == -1)
                             .Select(engine => engine.Name));

            Qualities = Enum.GetNames(typeof(Qualities)).Reverse().ToList();
        }

        /// <summary>
        /// Gets a list of episodes which have aired but aren't yet downloaded.
        /// </summary>
        /// <returns>
        /// List of missing episodes.
        /// </returns>
        public static List<Episode> GetMissingEpisodes()
        {
            return Database.Episodes.Where(x => !x.Watched && x.Airdate < DateTime.Now && (DateTime.Now - x.Airdate).TotalDays < 21).ToList();
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

            return links
                   .OrderBy(link => Qualities.IndexOf(link.Quality.ToString()))
                   .ThenBy(link => Parsers.IndexOf(link.Source.Name))
                   .First();
        }
    }
}
