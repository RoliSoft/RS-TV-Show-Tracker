namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using Newtonsoft.Json;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping DirectDownload.tv.
    /// </summary>
    [TestFixture]
    public class DirectDownload : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "DirectDownload.tv";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://directdownload.tv/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://directdownload.tv/favicon.png";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-11-08 6:05 PM");
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.DirectHTTP;
            }
        }

        /// <summary>
        /// Returns an <c>IDownloader</c> object which can be used to download the URLs provided by this parser.
        /// </summary>
        /// <value>The downloader.</value>
        public override IDownloader Downloader
        {
            get
            {
                return new ExternalDownloader();
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var links = Utils.GetJSON<List<ReleaseInfo>>(Site + "api.php?keyword=" + Uri.EscapeUriString(query));

            if (links.Count == 0)
            {
                yield break;
            }

            foreach (var item in links)
            {
                foreach (var site in item.Links)
                {
                    var link = new Link(this);

                    link.Release = item.Release;
                    link.InfoURL = Site + "s/" + item.Release;
                    link.FileURL = string.Join("\0", site.Values.First());
                    link.Quality = FileNames.Parser.ParseQuality(item.Release);
                    link.Size    = Utils.GetFileSize((long)(item.Size * 1048576));
                    link.Infos   = site.Keys.First().ToLower().ToUppercaseFirst();

                    yield return link;
                }
            }
        }

        /// <summary>
        /// Represents the release information (name, size, download links, etc) of an episode.
        /// </summary>
        public class ReleaseInfo
        {
            /// <summary>
            /// Gets or sets the release name.
            /// </summary>
            /// <value>
            /// The release name.
            /// </value>
            [JsonProperty("release")]
            public string Release { get; set; }

            /// <summary>
            /// Gets or sets the upload date of the release.
            /// </summary>
            /// <value>
            /// The upload date of the release.
            /// </value>
            [JsonProperty("dateGMT")]
            public DateTime Date { get; set; }

            /// <summary>
            /// Gets or sets the size of the release in MB.
            /// </summary>
            /// <value>
            /// The size of the release in MB.
            /// </value>
            [JsonProperty("size")]
            public double Size { get; set; }

            /// <summary>
            /// Gets or sets the quality of the release.
            /// </summary>
            /// <value>
            /// The quality of the release.
            /// </value>
            [JsonProperty("quality")]
            public string Quality { get; set; }

            /// <summary>
            /// Gets or sets the name of the show.
            /// </summary>
            /// <value>
            /// The name of the show.
            /// </value>
            [JsonProperty("showName")]
            public string ShowName { get; set; }

            /// <summary>
            /// Gets or sets the URLs to the files hosted on various sites.
            /// </summary>
            /// <value>
            /// The URLs to the files hosted on various sites.
            /// </value>
            [JsonProperty("links")]
            public List<Dictionary<string, List<string>>> Links { get; set; }
        }
    }
}
