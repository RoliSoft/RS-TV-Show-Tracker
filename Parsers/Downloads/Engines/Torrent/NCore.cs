namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping nCore.
    /// </summary>
    [Parser("RoliSoft", "2011-01-31 4:45 PM"), TestFixture]
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
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://ncore.cc/";
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
                return "http://static.ncore.cc/styles/ncore.ico";
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
                return new[] { "nick", "pass", "nyelv", "stilus" };
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
            var html  = Utils.GetHTML(Site + "torrents.php", "nyit_sorozat_resz=true&kivalasztott_tipus[]=xvidser_hun&kivalasztott_tipus[]=xvidser&kivalasztott_tipus[]=dvdser_hun&kivalasztott_tipus[]=dvdser&kivalasztott_tipus[]=hdser_hun&kivalasztott_tipus[]=hdser&mire=" + Uri.EscapeUriString(query) + "&miben=name&tipus=kivalasztottak_kozott&aktiv_inaktiv_ingyenes=mindehol", Cookies, Encoding.GetEncoding("iso-8859-2"));
            var links = html.DocumentNode.SelectNodes("//a[starts-with(@onclick, 'torrent(')]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                yield return new Link
                    {
                        Site    = Name,
                        Release = node.GetAttributeValue("title"),
                        URL     = Site + "torrents.php?action=download&id=" + Regex.Match(node.GetAttributeValue("href"), @"id=(\d+)").Groups[1].Value,
                        Size    = node.GetTextValue("../../../../div[@class='box_meret2']/text()").Trim(),
                        Quality = ThePirateBay.ParseQuality(node.GetAttributeValue("title")),
                        Type    = Types.Torrent
                    };
            }
        }
    }
}
