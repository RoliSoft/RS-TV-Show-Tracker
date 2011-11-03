namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping 1st.
    /// </summary>
    [Parser("RoliSoft", "2011-11-30 18:40 PM"), TestFixture]
    public class First : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "1st";
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
                return "http://the1st.be/";
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
        /// Gets a value indicating whether this search engine can login using a username and password.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this search engine can login; otherwise, <c>false</c>.
        /// </value>
        public override bool CanLogin
        {
            get
            {
                return true;
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
                        { "username", LoginFieldTypes.UserName },
                        { "password", LoginFieldTypes.Password },
                        { "returnto", LoginFieldTypes.ReturnTo }
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
            var html = Utils.GetHTML(Site + "browse.php?incldead=0&search=" + Uri.EscapeUriString(query), cookies: Cookies, encoding: Encoding.GetEncoding("iso-8859-2"));

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[@id='torrenttable']/tr/td[4]/a[@class='name']");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.GetAttributeValue("title");

                if (string.IsNullOrWhiteSpace(link.Release))
                {
                    link.Release = node.InnerText;
                }

                link.InfoURL = Site + node.GetAttributeValue("href");
                link.FileURL = Site + node.GetNodeAttributeValue("../../td[2]/a[1]", "href");
                link.Size    = node.GetTextValue("../../td[8]/br/preceding-sibling::text()").Replace("&nbsp;", " ").Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[9]").Trim(), node.GetTextValue("../../td[10]").Trim())
                             + ", " + node.GetNodeAttributeValue("../../td[1]", "title")
                             + (node.GetHtmlValue("../../td[8]/span[contains(@class, 'free-rate')]") != null ? ", Free" : string.Empty)
                             + (node.GetHtmlValue("../../td[8]/span[contains(@class, 'down-rate')]") != null ? ", " + node.GetTextValue("../../td[8]/span[contains(@class, 'down-rate')]").Trim().Substring(1).Replace(".0", string.Empty) + "x Download" : string.Empty)
                             + (node.GetHtmlValue("../../td[8]/span[contains(@class, 'up-rate')]") != null ? ", " + node.GetTextValue("../../td[8]/span[contains(@class, 'up-rate')]").Trim().Substring(1).Replace(".0", string.Empty) + "x Upload" : string.Empty);

                yield return link;
            }
        }

        /// <summary>
        /// Authenticates with the site and returns the cookies.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Cookies on success, <c>string.Empty</c> on failure.</returns>
        public override string Login(string username, string password)
        {
            return GazelleTrackerLogin(username, password);
        }
    }
}
