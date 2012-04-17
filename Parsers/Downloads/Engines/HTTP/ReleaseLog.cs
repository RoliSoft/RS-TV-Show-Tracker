namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping ReleaseLog.
    /// </summary>
    [TestFixture]
    public class ReleaseLog : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "ReleaseLog";
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
                return "http://rlslog.net/";
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
                return "http://rlslog.net/wp-content/favicon.ico";
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
                return Utils.DateTimeToVersion("2011-09-24 2:17 AM");
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
            var req  = Utils.GetURL(Site + "category/tv-shows/feed/?s=" + Utils.EncodeURL(query))
                            .Replace("content:encoded", "content") // HtmlAgilityPack doesn't like tags with colons in their names
                            .Replace("<![CDATA[", string.Empty)
                            .Replace("]]>", string.Empty)
                            .Replace("×", "x");

            var html = new HtmlDocument();
            html.LoadHtml(req);

            var links = html.DocumentNode.SelectNodes("//item");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                if (node.SelectSingleNode("category[contains(text(), 'TV Packs')]") != null)
                {
                    continue;
                }

                var infourl  = (node.GetTextValue("comments") ?? string.Empty).Replace("#comments", string.Empty); // can't get <link>
                var releases = node.SelectNodes("content/p[contains(@style, 'center') or @align='center']/strong/..");

                if (releases == null)
                {
                    continue;
                }

                foreach (var relnode in releases)
                {
                    var release = relnode.GetTextValue("strong").Trim();
                    var quality = FileNames.Parser.ParseQuality(release);
                    var sizergx = Regex.Match(relnode.InnerText, @"(\d+(?:\.\d+)?)\s*([KMG]B)", RegexOptions.IgnoreCase);
                    var size    = string.Empty;

                    if (sizergx.Success)
                    {
                        size = sizergx.Groups[1].Value + " " + sizergx.Groups[2].Value.ToUpper();
                    }

                    var sites = relnode.SelectNodes("a");

                    foreach (var site in sites)
                    {
                        if (Regex.IsMatch(site.InnerText, @"(NTi|NFO)"))
                        {
                            continue;
                        }

                        var link = new Link(this);

                        link.Release = release;
                        link.InfoURL = infourl;
                        link.FileURL = site.GetAttributeValue("href");
                        link.Size    = size;
                        link.Infos   = site.InnerText.ToLower().ToUppercaseFirst();
                        link.Quality = quality;

                        yield return link;
                    }
                }
            }
        }
    }
}
