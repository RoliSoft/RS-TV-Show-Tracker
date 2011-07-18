namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping BTDigg.
    /// </summary>
    [Parser("2011-07-19 12:00 AM"), TestFixture]
    public class BTDigg : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "BTDigg";
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
                return "http://btdigg.org/";
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
            var html  = Utils.GetHTML(Site + "search?q=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//td[@class='torrent_name']/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.InnerText;
                link.FileURL = HtmlEntity.DeEntitize(node.GetNodeAttributeValue("../../../..//td[@class='ttth']/a", "href"));
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.Size    = node.GetTextValue("../../../..//td[2]/span[@class='attr_val']").Replace("&nbsp;", " ");
                link.Quality = FileNames.Parser.ParseQuality(node.InnerText);
                link.Infos   = "Reqs: " + node.GetTextValue("../../../..//td[4]/span[@class='attr_val']");

                yield return link;
            }
        }
    }
}
