namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping NZBs'R'US.
    /// </summary>
    [Parser("RoliSoft", "2011-09-20 7:47 PM"), TestFixture]
    public class NZBsRus : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "NZBs'R'US";
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
                return "http://www.nzbsrus.com/";
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
                return new[] { "uid", "pass", "class" };
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
                return "https://www.nzbsrus.com/takelogin.php";
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
                        { "returnto", LoginFieldTypes.ReturnTo },
                        {
                            "token",
                            (Func<string>)(() =>
                                {
                                    var login = Utils.GetHTML("https://www.nzbsrus.com/login.php");
                                    var token = login.DocumentNode.GetNodeAttributeValue("//input[@type='hidden' and @name='token']", "value");

                                    return token;
                                })
                        },
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
                return Types.Usenet;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html = Utils.GetHTML(Site + "nzbbrowse.php?searchwhere=title&cat=20s&listname=date&searchtext=" + Uri.EscapeUriString(query), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//div[@id='nzbtable']//div[@class='pstnam']/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);
                var nzbl = node.GetNodeAttributeValue("../../../../div/div[@class='dlnzb']/a/@href", "href");

                if (string.IsNullOrEmpty(nzbl))
                {
                    continue;
                }

                link.Release = HtmlEntity.DeEntitize(node.InnerText);
                link.InfoURL = Site + HtmlEntity.DeEntitize(node.GetAttributeValue("href"));
                link.FileURL = Site + nzbl;
                link.Size    = node.GetHtmlValue("../../../..//abbr[starts-with(@title, 'Total size')]").Trim().Replace("i", string.Empty);
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Utils.ParseAge(node.GetTextValue("../../..//div[@class='pstdat']"));

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
