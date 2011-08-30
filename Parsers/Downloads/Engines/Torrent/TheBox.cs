namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping TheBox.
    /// </summary>
    [Parser("2011-07-08 12:16 AM"), TestFixture]
    public class TheBox : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TheBox";
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
                return "http://thebox.bz/";
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
                return new[] { "uid", "pass", "session" };
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
            var html = Utils.GetHTML(Site + "browse.php?incldead=0&nonboolean=1&search=" + Uri.EscapeUriString(query), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//tr[@class='ttable']/td[2]/a[1]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.GetAttributeValue("title");
                link.InfoURL = Site + node.GetAttributeValue("href");
                link.FileURL = Site + node.GetNodeAttributeValue("../../td[3]/a[2]", "href");
                link.Size    = node.GetHtmlValue("../../td[7]").Trim().Replace("<br>", " ");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[9]").Trim(), node.GetTextValue("../../td[10]").Trim())
                             + (node.GetHtmlValue("../a/b/font[@color='blue']") != null ? ", Free" : string.Empty)
                             + (node.GetHtmlValue("../a/b/font[@color='green']") != null ? ", Neutral" : string.Empty);

                link.Release = Regex.Replace(link.Release, @"\s\(\d{1,2}(?:st|nd|rd|th)? [A-Z][a-z]+ \d{4}\)", string.Empty);
                
                yield return link;
            }
        }
    }
}
