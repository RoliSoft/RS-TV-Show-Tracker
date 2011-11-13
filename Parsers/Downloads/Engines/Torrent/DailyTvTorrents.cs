namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using Newtonsoft.Json;

    /// <summary>
    /// Provides support for Daily TV Torrents' API.
    /// </summary>
    [TestFixture]
    public class DailyTvTorrents : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Daily TV Torrents";
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
                return "http://www.dailytvtorrents.org/";
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
                return Utils.DateTimeToVersion("2011-11-08 5:55 PM");
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
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var parts = ShowNames.Parser.Split(query);
                parts[0] = Regex.Replace(parts[0].ToLower(), @"[^a-z0-9\s]", string.Empty);
                parts[0] = Regex.Replace(parts[0], @"\s+", "-");

            if (parts.Length == 1)
            {
                yield break;
            }

            var links = new List<TorrentInfo>();

            try
            {
                links = Utils.GetJSON<List<TorrentInfo>>("http://api.dailytvtorrents.org/1.0/torrent.getInfosAll?show_name=" + Uri.EscapeUriString(parts[0]) + "&episode_num=" + Uri.EscapeUriString(parts[1]));
            } catch { }

            if (links.Count == 0)
            {
                yield break;
            }

            foreach (var info in links)
            {
                yield return new Link(this)
                    {
                        Release = info.Name,
                        FileURL = info.Link,
                        Size    = Utils.GetFileSize(info.Size),
                        Quality = FileNames.Parser.ParseQuality(info.Name),
                        Infos   = Link.SeedLeechFormat.FormatWith(info.Seed, info.Leech)
                    };
            }
        }

        /// <summary>
        /// Represents the torrent information (download link, seeds, peers, number of files) of an episode.
        /// </summary>
        public class TorrentInfo
        {
            /// <summary>
            /// Gets or sets the name of the torrent.
            /// </summary>
            /// <value>
            /// The name of the torrent.
            /// </value>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the quality of the torrent.
            /// </summary>
            /// <value>
            /// The quality of the torrent.
            /// </value>
            [JsonProperty("quality")]
            public string Quality { get; set; }

            /// <summary>
            /// Gets or sets the age of the torrent.
            /// </summary>
            /// <value>
            /// The age of the torrent.
            /// </value>
            [JsonProperty("age")]
            public int Age { get; set; }

            /// <summary>
            /// Gets or sets the size of the torrent in bytes.
            /// </summary>
            /// <value>
            /// The size of the torrent in bytes.
            /// </value>
            [JsonProperty("data_size")]
            public int Size { get; set; }

            /// <summary>
            /// Gets or sets the seeders count for the torrent.
            /// </summary>
            /// <value>
            /// The seeders count for the torrent.
            /// </value>
            [JsonProperty("seeds")]
            public int Seed { get; set; }

            /// <summary>
            /// Gets or sets the leechers count for the torrent.
            /// </summary>
            /// <value>
            /// The leechers count for the torrent.
            /// </value>
            [JsonProperty("leechers")]
            public int Leech { get; set; }

            /// <summary>
            /// Gets or sets the URL to the torrent file.
            /// </summary>
            /// <value>
            /// The URL to the torrent file.
            /// </value>
            [JsonProperty("link")]
            public string Link { get; set; }
        }
    }
}
