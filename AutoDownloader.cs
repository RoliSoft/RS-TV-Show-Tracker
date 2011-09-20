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
            var dlsrc = new DownloadSearch();
            var start = DateTime.Now;
            var busy  = true;

            dlsrc.DownloadSearchProgressChanged += (s, e) => links.AddRange(e.First);
            dlsrc.DownloadSearchDone            += (s, e) => { busy = false; };

            dlsrc.SearchAsync(string.Format("{0} S{1:00}E{2:00}", ep.Show.Name, ep.Season, ep.Number));

            while (busy && (DateTime.Now - start).TotalMinutes < 1)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            return links;
        }
    }
}
