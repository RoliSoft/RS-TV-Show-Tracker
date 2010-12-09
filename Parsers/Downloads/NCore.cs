namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides support for scraping nCore.
    /// </summary>
    public class NCore : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "nCore";
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
                return true;
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
        public override List<Link> Search(string query)
        {
            var html  = Utils.GetHTML("http://ncore.cc/torrents.php", "nyit_sorozat_resz=true&kivalasztott_tipus[]=xvidser_hun&kivalasztott_tipus[]=xvidser&kivalasztott_tipus[]=dvdser_hun&kivalasztott_tipus[]=dvdser&kivalasztott_tipus[]=hdser_hun&kivalasztott_tipus[]=hdser&mire=" + Uri.EscapeUriString(query) + "&miben=name&tipus=kivalasztottak_kozott&aktiv_inaktiv_ingyenes=mindehol", Cookies);
            var links = html.DocumentNode.SelectNodes("//a[starts-with(@onclick, 'torrent(')]");

            if (links == null)
            {
                return null;
            }

            return links.Select(node => new Link
                   {
                       Site    = Name,
                       Release = node.GetAttributeValue("title", string.Empty),
                       URL     = "http://ncore.cc/" + node.SelectSingleNode("../../../../../..//div[@class='letoltve_txt']/a").GetAttributeValue("href", string.Empty),
                       Size    = node.SelectSingleNode("../../../../div[@class='box_meret2']/text()").InnerText.Trim(),
                       Quality = ThePirateBay.ParseQuality(node.GetAttributeValue("title", string.Empty)),
                       Type    = Types.Torrent
                   }).ToList();
        }
    }
}
