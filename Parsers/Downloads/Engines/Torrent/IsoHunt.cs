namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping isoHunt.
    /// </summary>
    [Parser("RoliSoft", "2011-04-17 6:42 PM"), TestFixture]
    public class IsoHunt : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "isoHunt";
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
                return "http://isohunt.com/";
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
            var html  = Utils.GetHTML(Site + "torrents/" + Uri.EscapeUriString(query) + "?iht=3");
            var links = html.DocumentNode.SelectNodes("//td[starts-with(@id, 'name')]/a[2]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(Regex.Replace(node.InnerHtml, @"(^.+<br>|<[^>]+>)", string.Empty));
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.FileURL = "http://ca.isohunt.com/download/{0}.torrent".FormatWith(Regex.Match(link.InfoURL, @"/(\d+/[^\?\.$]+)").Groups[1].Value);
                link.Size    = node.GetTextValue("../../td[4]");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[5]").Trim(), node.GetTextValue("../../td[6]").Trim())
                             + (node.GetHtmlValue("../a[starts-with(@href, '/release/')]") != null ? ", isoHunt Release" : string.Empty)
                             + (node.GetTextValue("../a[1]/img[1]/preceding-sibling::text()") != "0" ? ", " + node.GetTextValue("../a[1]/img[1]/preceding-sibling::text()") + " rating" : string.Empty);

                yield return link;
            }
        }
    }
}
