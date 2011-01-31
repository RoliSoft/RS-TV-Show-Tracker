namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping bitHUmen.
    /// </summary>
    [Parser("RoliSoft", "2011-01-29 9:40 PM"), TestFixture]
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
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://bithumen.be/";
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
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public override bool Private
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the names of the required cookies for the authentication.
        /// </summary>
        /// <value>The required cookies for authentication.</value>
        public override string[] RequiredCookies
        {
            get
            {
                return new[] { "uid", "pass", "rid" };
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
            var html  = Utils.GetHTML(Site + "browse.php?c7=1&c26=1&genre=0&search=" + Uri.EscapeUriString(query), cookies: Cookies, encoding: Encoding.GetEncoding("iso-8859-2"));
            var links = html.DocumentNode.SelectNodes("//table[@id='torrenttable']/tr/td[2]/a[1]/b");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                yield return new Link
                    {
                        Site    = Name,
                        Release = node.GetNodeAttributeValue("../", "title") ?? node.InnerText,
                        URL     = Site + node.GetNodeAttributeValue("../../a[starts-with(@title, 'Let')]", "href"),
                        Size    = node.GetHtmlValue("../../../td[6]/u").Replace("<br>", " "),
                        Quality = ThePirateBay.ParseQuality(node.GetNodeAttributeValue("../", "title") ?? node.InnerText),
                        Type    = Types.Torrent
                    };
            }
        }
    }
}
