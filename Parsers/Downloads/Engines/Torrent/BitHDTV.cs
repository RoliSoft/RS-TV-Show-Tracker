namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping BiT-HDTV.
    /// </summary>
    [Parser("2011-05-08 1:09 AM"), TestFixture]
    public class BitHDTV : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "BiT-HDTV";
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
                return "http://www.bit-hdtv.com/";
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
                return new[] { "h_sl", "h_sp", "h_su" };
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
            var html  = Utils.GetHTML(Site + "torrents.php?cat=10&search=" + Uri.EscapeUriString(query), cookies: Cookies, encoding: Encoding.GetEncoding("windows-1252"));
            var links = html.DocumentNode.SelectNodes("//a[starts-with(@href,'/download.php/')]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.GetNodeAttributeValue("../a", "title");
                link.InfoURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("../a", "href");
                link.FileURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.Size    = node.GetHtmlValue("../../td[7]").Replace("<br>", " ");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[9]").Trim(), node.GetTextValue("../../td[10]").Trim());

                yield return link;
            }
        }
    }
}
