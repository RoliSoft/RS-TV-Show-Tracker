namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping FileList.ro.
    /// </summary>
    [Parser("2011-02-13 6:03 PM"), TestFixture]
    public class FileList : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FileList";
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
                return "http://filelist.ro/";
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
                return new[] { "uid", "pass" };
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
            var html  = Utils.GetHTML(Site + "browse.php?searchin=0&sort=0&search=" + Uri.EscapeUriString(query), cookies: Cookies, userAgent: Settings.Get("FileList User Agent"));
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/a/b");
            
            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.GetNodeAttributeValue("../", "title") ?? node.InnerText;
                link.InfoURL = Site + node.GetNodeAttributeValue("../../a", "href");
                link.FileURL = Site + node.GetNodeAttributeValue("../../../td[3]/a", "href");
                link.Size    = node.GetHtmlValue("../../../td[7]").Replace("<br>", " ");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../td[9]").Trim(), node.GetTextValue("../../../td[10]").Trim())
                             + (node.GetTextValue("../../b/small/font") == "[FreeLeech]" ? ", Free" : string.Empty);
                
                yield return link;
            }
        }
    }
}
