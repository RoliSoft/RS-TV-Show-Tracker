namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Provides support for scraping bitHUmen.
    /// </summary>
    [Parser("RoliSoft", "2009-12-09 5:03 PM")]
    public class BitHUmen : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "bitHUmen";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://bithumen.be/favicon.ico";
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
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML("http://bithumen.be/browse.php?c7=1&c26=1&genre=0&search=" + Uri.EscapeUriString(query), cookies: Cookies, encoding: Encoding.GetEncoding("iso-8859-2"));
            var links = html.DocumentNode.SelectNodes("//table[@id='torrenttable']/tr/td[2]/a[1]/b");

            if (links == null)
            {
                return null;
            }

            return links.Select(node => new Link
                   {
                       Site    = Name,
                       Release = node.ParentNode.GetAttributeValue("title", string.Empty) != string.Empty ? node.ParentNode.GetAttributeValue("title", string.Empty) : node.InnerText,
                       URL     = "http://bithumen.be/" + node.SelectSingleNode("../../a[starts-with(@title, 'Let')]").GetAttributeValue("href", string.Empty),
                       Size    = node.SelectSingleNode("../../../td[6]/u").InnerHtml.Replace("<br>", " "),
                       Quality = ThePirateBay.ParseQuality(node.ParentNode.GetAttributeValue("title", string.Empty) != string.Empty ? node.ParentNode.GetAttributeValue("title", string.Empty) : node.InnerText),
                       Type    = Types.Torrent
                   });
        }
    }
}
