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
    /// Provides support for scraping Twilight.
    /// </summary>
    [TestFixture]
    public class Twilight : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Twilight";
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
                return "http://twilight.ws/";
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
                return Utils.DateTimeToVersion("2013-09-15 7:58 PM");
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
                return Types.HTTP;
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
            var html = Utils.GetHTML(Site + "downloads.php?type=tv&q=" + Utils.EncodeURL(query));
            var links = html.DocumentNode.SelectNodes("//div[@class='dllist']/dl");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                if (node.GetHtmlValue("dt[contains(@class, 'tv')]") == null) continue; // header and ads

                var site = node.GetTextValue("dd/a/span[@class='rating']/../a");
                var star = node.GetTextValue("dd/a/span[@class='rating']");
                var list = node.SelectNodes("dd[@class='fh']/span");

                link.Release = HtmlEntity.DeEntitize(node.GetTextValue("dt/a")).Trim()
                             + (!string.IsNullOrWhiteSpace(site) ? " @ " + site.Trim() : string.Empty)
                             + (!string.IsNullOrWhiteSpace(star) && Regex.IsMatch(star, @"\s*\d") ? " " + star.Trim() + "✩" : string.Empty);
                link.InfoURL = Site + node.GetNodeAttributeValue("dt/a", "href");
                link.Quality = Regex.IsMatch(link.Release, @"(720p|\bHD\b)", RegexOptions.IgnoreCase)
                             ? Qualities.HDTV720p
                             : Qualities.HDTVXviD;

                if (list != null)
                {
                    foreach (var fs in list)
                    {
                        link.Infos += fs.GetAttributeValue("title") + ", ";
                    }

                    link.Infos = link.Infos.TrimEnd(", ".ToCharArray());
                }

                yield return link;
            }
        }
    }
}
