namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping FreshWap.
    /// </summary>
    [TestFixture]
    public class FreshWap : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FreshWap";
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
                return "http://freshwap.com/";
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
                return Utils.DateTimeToVersion("2011-11-03 7:31 AM");
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
        /// Gets a number representing the number of links to visit for link and info extraction.
        /// </summary>
        /// <remarks>
        /// The default number is 10. It's configurable from Settings.json, but you shouldn't increase it, as it might get you an IP ban.
        /// </remarks>
        public static int ExtractLimit
        {
            get
            {
                return Settings.Get("FreshWap Extract Limit", 10);
            }
        }

        /// <summary>
        /// Gets a number representing the number of milliseconds to wait before requesting the next link.
        /// </summary>
        /// <remarks>
        /// The default number is 250. It's configurable from Settings.json, but you shouldn't decrease it, as it might get you an IP ban.
        /// </remarks>
        public static int ExtractSleep
        {
            get
            {
                return Settings.Get("FreshWap Extract Sleep", 250);
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "index.php?do=search&subaction=search&search_start=1&full_search=1&titleonly=3&searchuser=&replyless=0&replylimit=0&searchdate=0&beforeafter=after&sortby=date&resorder=desc&result_num=30&result_from=1&showposts=0&catlist%5B%5D=7&story=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//div[@class='title']/a");

            if (links == null)
            {
                yield break;
            }

            var i = 0;
            foreach (var node in links)
            {
                var release = HtmlEntity.DeEntitize(node.InnerText);
                var quality = FileNames.Parser.ParseQuality(release);
                var infourl = node.GetAttributeValue("href");
                var sizergx = Regex.Match(node.GetTextValue("../../../div[starts-with(@id, 'news-id-')]"), @"(\d+(?:\.\d+)?)\s*([KMG]B)(?![/p])", RegexOptions.IgnoreCase);
                var size    = string.Empty;

                if (sizergx.Success)
                {
                    size = sizergx.Groups[1].Value + " " + sizergx.Groups[2].Value.ToUpper();
                }

                if (i == ExtractLimit)
                {
                    var link = new Link(this);

                    link.Release = release;
                    link.InfoURL = infourl;
                    link.Size    = size;
                    link.Quality = quality;

                    yield return link;
                    continue;
                }

                i++;
                Thread.Sleep(ExtractSleep);

                var html2 = Utils.GetHTML(infourl);
                var sites = Regex.Matches(html2.DocumentNode.GetHtmlValue("//div[@class='quote']") ?? string.Empty, @"(http://[^<$\s]+)");

                if (sites.Count == 0)
                {
                    continue;
                }

                for (var x = 0; x < sites.Count; x++)
                {
                    var link = new Link(this);

                    link.Release = release;
                    link.InfoURL = infourl;
                    link.FileURL = sites[x].Groups[1].Value;
                    link.Size    = size;
                    link.Quality = quality;
                    link.Infos   = Regex.Match(link.FileURL, @"http://(?:www\.)?([^\.]+)").Groups[1].Value.ToUppercaseFirst();

                    var first = new Uri(sites[x].Groups[1].Value);

                tryNext:
                    if (x + 1 < sites.Count && first.Host == new Uri(sites[x + 1].Groups[1].Value).Host)
                    {
                        x++;
                        link.FileURL += "\0" + sites[x].Groups[1].Value;
                        goto tryNext;
                    }

                    yield return link;
                }
            }
        }
    }
}
