namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using HtmlAgilityPack;

    /// <summary>
    /// Provides support for scraping SceneReleases.
    /// </summary>
    public class SceneReleases : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "SceneReleases";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires cookies to authenticate.
        /// </summary>
        /// <value><c>true</c> if requires cookies; otherwise, <c>false</c>.</value>
        public override bool RequiresCookies
        {
            get
            {
                return false;
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
                return Types.Http;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override List<Link> Search(string query)
        {
            var html  = Utils.GetHTML("http://scenereleases.info/?s=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//h3/a");

            if (links == null)
            {
                return null;
            }

            return links.Select(node => new Link
                   {
                       Site         = Name,
                       Release      = HtmlEntity.DeEntitize(node.InnerText).Trim().Replace(' ', '.').Replace(".&.", " & "),
                       URL          = node.GetAttributeValue("href", string.Empty),
                       Size         = "N/A",
                       Quality      = ThePirateBay.ParseQuality(HtmlEntity.DeEntitize(node.InnerText).Trim().Replace(' ', '.')),
                       Type         = Types.Http,
                       IsLinkDirect = false
                   }).ToList();
        }
    }
}
