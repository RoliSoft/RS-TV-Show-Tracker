namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for scraping nCore.
    /// </summary>
    [Parser("RoliSoft", "2010-12-09 4:41 PM")]
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
            var html  = Utils.GetHTML("http://ncore.cc/torrents.php", "nyit_sorozat_resz=true&kivalasztott_tipus[]=xvidser_hun&kivalasztott_tipus[]=xvidser&kivalasztott_tipus[]=dvdser_hun&kivalasztott_tipus[]=dvdser&kivalasztott_tipus[]=hdser_hun&kivalasztott_tipus[]=hdser&mire=" + Uri.EscapeUriString(query) + "&miben=name&tipus=kivalasztottak_kozott&aktiv_inaktiv_ingyenes=mindehol", Cookies);
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
                        Release = node.GetAttributeValue("title", string.Empty),
                        URL     = "http://ncore.cc/torrents.php?action=download&id=" + Regex.Match(node.GetAttributeValue("href", string.Empty), @"id=(\d+)").Groups[1].Value,
                        Size    = node.SelectSingleNode("../../../../div[@class='box_meret2']/text()").InnerText.Trim(),
                        Quality = ThePirateBay.ParseQuality(node.GetAttributeValue("title", string.Empty)),
                        Type    = Types.Torrent
                    };
            }
        }
    }
}
