namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping bitHUmen.
    /// </summary>
    [Parser("2011-08-16 16:02 PM"), TestFixture]
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
        /// Gets the URL to the login page.
        /// </summary>
        /// <value>The URL to the login page.</value>
        public override string LoginURL
        {
            get
            {
                return Site + "takelogin.php";
            }
        }

        /// <summary>
        /// Gets the input fields of the login form.
        /// </summary>
        /// <value>The input fields of the login form.</value>
        public override Dictionary<string, object> LoginFields
        {
            get
            {
                return new Dictionary<string, object>
                    {
                        { "salted_passhash", string.Empty             },
                        { "username",        LoginFieldTypes.UserName },
                        { "password",        LoginFieldTypes.Password },
                        { "megjegyez",       "on"                     },
                    };
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
                var link = new Link(this);

                link.Release = node.GetNodeAttributeValue("../", "title") ?? node.InnerText;
                link.InfoURL = Site + node.GetNodeAttributeValue("../../a", "href");
                link.FileURL = Site + node.GetNodeAttributeValue("../../a[starts-with(@title, 'Let')]", "href");
                link.Size    = node.GetHtmlValue("../../../td[6]/u").Replace("<br>", " ");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../td[8]").Trim(), node.GetTextValue("../../../td[9]").Trim().Split('/')[1].Trim());

                yield return link;
            }
        }
    }
}
