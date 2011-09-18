namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Newzbin.
    /// </summary>
    [Parser("2011-09-19 12:08 AM"), TestFixture]
    public class Newzbin : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Newzbin";
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
                return "http://newzbin.com/";
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
                return new[] { "NzbSessionID", "NzbSmoke" };
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
                return Site + "account/login/";
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
                        { "ret_url",  LoginFieldTypes.ReturnTo },
                        { "username", LoginFieldTypes.UserName },
                        { "password", LoginFieldTypes.Password },
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
            var html = Utils.GetHTML(Site + "search/query/?area=c.8&fpn=p&searchaction=Go&btnG_x=10&btnG_y=6&category=8&areadone=c.8&q=" + Uri.EscapeUriString(ShowNames.Parser.ReplaceEpisode(query, "{0}x{1:00}", false, false)), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table/tbody/tr/td[@class='title']/strong/a");

            if (links == null)
            {
                yield break;
            }

            var premium = html.DocumentNode.SelectSingleNode("//li[@class='message-warning']/a[contains(@href, '/account/topup/')]") == null;

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText) + (node.GetNodeAttributeValue("../../../following-sibling::tr[1]/td/a/img[@alt='NFO']", "title") ?? string.Empty).Replace("View Report NFO", string.Empty);
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = node.GetTextValue("../../../td[5]/span").Trim() + " old";

                if (premium)
                {
                    link.FileURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("../../../following-sibling::tr[1]/td/a[starts-with(@title, 'Download')]", "href");
                }

                var size = Regex.Match(node.GetTextValue("../../../following-sibling::tr[1]/td[3]") ?? string.Empty, @"(\d+(?:\.\d+)?)([KMG]B)");

                if (size.Success)
                {
                    link.Size = size.Groups[1].Value + " " + size.Groups[2].Value;
                }

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
