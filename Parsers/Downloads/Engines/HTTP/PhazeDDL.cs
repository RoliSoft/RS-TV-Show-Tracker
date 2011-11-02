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
    /// Provides support for scraping PhazeDDL.
    /// </summary>
    [Parser("RoliSoft", "2011-03-26 4:32 PM"), TestFixture]
    public class PhazeDDL : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "PhazeDDL";
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
                return "http://www.phazeddl.com/";
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
            var html  = Utils.GetHTML(Site + "searchcat.php?cat=5&q=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//td[@class='vertTh']/..");

            if (links == null || Regex.IsMatch(html.DocumentNode.InnerHtml, @"Sorry no results found"))
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                var site = node.GetTextValue("td[4]/a");
                var star = node.GetTextValue("td[4]/a/following-sibling::text()");

                link.Release = HtmlEntity.DeEntitize(node.GetTextValue("td[2]/a")).Trim()
                             + (!string.IsNullOrWhiteSpace(site) ? " @ " + site : string.Empty)
                             + (!string.IsNullOrWhiteSpace(star) && Regex.IsMatch(star, @"\s*\*\d") ? " " + star.Trim(new[] { ' ', '*' }) + "✩" : string.Empty);
                link.InfoURL = node.GetNodeAttributeValue("td[2]/a", "href");
                link.Quality = Qualities.HDTVXviD;

                yield return link;
            }
        }
    }
}
