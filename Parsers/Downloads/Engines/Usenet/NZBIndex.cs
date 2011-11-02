namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping NZBIndex.
    /// </summary>
    [Parser("RoliSoft", "2011-09-20 8:39 PM"), TestFixture]
    public class NZBIndex : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "NZBIndex";
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
                return "http://nzbindex.nl/";
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
                return Types.Usenet;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "search/?minsize=100&q=" + Uri.EscapeUriString(query), cookies: "agreed=true; lang=2");
            var links = html.DocumentNode.SelectNodes("//label[starts-with(@for, 'box')]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText);
                link.InfoURL = node.GetNodeAttributeValue("..//a[contains(text(), 'View collection')]", "href");
                link.FileURL = node.GetNodeAttributeValue("..//a[contains(text(), 'Download')]", "href");
                link.Size    = node.GetTextValue("../../td[3]").Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Utils.ParseAge(node.GetTextValue("../../td[5]").Trim());

                yield return link;
            }
        }
    }
}
