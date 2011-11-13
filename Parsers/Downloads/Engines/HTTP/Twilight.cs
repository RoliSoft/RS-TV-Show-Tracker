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
                return Utils.DateTimeToVersion("2011-03-26 4:06 PM");
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
            var html  = Utils.GetHTML(Site, "type=TV&searchoption=broad&q=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//table[1]/tr");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                if (node.SelectNodes("td/div[contains(@class, 'verified')]") != null || node.SelectNodes("td[contains(@class, 'dltitle')]") == null)
                {
                   continue;
                }

                var link = new Link(this);

                var site = node.GetTextValue("td[contains(@class, 'dlsite')]/a/span/following-sibling::text()");
                var star = node.GetTextValue("td[contains(@class, 'dlsite')]/a/span/preceding-sibling::text()");
                var list = node.SelectNodes("td[contains(@class, 'dlhost')]/span");

                link.Release = HtmlEntity.DeEntitize(node.GetTextValue("td[contains(@class, 'dltitle')]/a[@title != '']")).Trim()
                             + (!string.IsNullOrWhiteSpace(site) ? " @ " + site : string.Empty)
                             + (!string.IsNullOrWhiteSpace(star) && Regex.IsMatch(star, @"\s*\d") ? " " + star.Trim() + "✩" : string.Empty);
                link.InfoURL = Site + node.GetNodeAttributeValue("td[contains(@class, 'dltitle')]/a[@title != '']", "href");
                link.Quality = Qualities.HDTVXviD;

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
