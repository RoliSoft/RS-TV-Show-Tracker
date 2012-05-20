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
    /// Provides support for scraping TV-Release.
    /// </summary>
    [TestFixture]
    public class TVRelease : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TV-Release";
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
                return "http://tv-release.net/";
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
                return Utils.DateTimeToVersion("2012-05-20 4:16 PM");
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
                return Settings.Get("TV-Release Extract Limit", 10);
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
                return Settings.Get("TV-Release Extract Sleep", 250);
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "?cat=163&s=" + Utils.EncodeURL(query));
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/a/b/font");

            if (links == null)
            {
                yield break;
            }

            var i = 0;
            foreach (var node in links)
            {
                var release = HtmlEntity.DeEntitize(node.InnerText);
                var quality = FileNames.Parser.ParseQuality(release);
                var infourl = node.GetNodeAttributeValue("../..", "href");
                var size    = node.GetTextValue("../../../../td[5]").Trim();

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
                var sites = html2.DocumentNode.SelectNodes("//td[@class='td_cols']");

                if (sites == null)
                {
                    continue;
                }

                foreach (var site in sites)
                {
                    var link = new Link(this);

                    link.Release = release;
                    link.InfoURL = infourl;
                    link.Size    = size;
                    link.Quality = quality;

                    var links2 = site.SelectNodes("a");
                    foreach (var link2 in links2)
                    {
                        link.FileURL += "\0" + link2.GetAttributeValue("href").Trim();
                    }

                    link.FileURL = link.FileURL.Trim('\0');
                    link.Infos   = Regex.Match(link.FileURL, @"https?://(?:www\.)?([^\.]+)").Groups[1].Value.ToUppercaseFirst();

                    yield return link;
                }
            }
        }
    }
}
