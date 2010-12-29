namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides support for scraping FileList.ro.
    /// </summary>
    [Parser("RoliSoft", "2010-12-09 6:33 PM")]
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
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://filelist.ro/favicon.ico";
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
            var html  = Utils.GetHTML("http://filelist.ro/browse.php?cat=14&searchin=0&sort=0&search=" + Uri.EscapeUriString(query), cookies: Cookies, userAgent: Settings.Get("FileList User Agent"));
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/a/b");
            
            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                yield return new Link
                    {
                        Site    = Name,
                        Release = node.ParentNode.GetAttributeValue("title", string.Empty) != string.Empty ? node.ParentNode.GetAttributeValue("title", string.Empty) : node.InnerText,
                        URL     = "http://filelist.ro/" + node.SelectSingleNode("../../../td[3]/a").GetAttributeValue("href", string.Empty),
                        Size    = node.SelectSingleNode("../../../td[7]").InnerHtml.Replace("<br>", " "),
                        Quality = ThePirateBay.ParseQuality(node.ParentNode.GetAttributeValue("title", string.Empty) != string.Empty ? node.ParentNode.GetAttributeValue("title", string.Empty) : node.InnerText),
                        Type    = Types.Torrent
                    };
            }
        }
    }
}
