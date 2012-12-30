namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping UKNova.
    /// </summary>
    [TestFixture]
    public class UKNova : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "UKNova";
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
                return "http://www.uknova.com/";
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
                return Site + "theme/-dark/img/shortcut.ico";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-08-16 16:50 PM");
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
                return new[] { "auth" };
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
                return Site + "wsgi/auth";
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
        /// Gets a value indicating whether this site is deprecated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if deprecated; otherwise, <c>false</c>.
        /// </value>
        public override bool Deprecated
        {
            get
            {
                // My account on this site has been banned due to inactivity.
                // Because of this, I am unable to unit test and maintain this plugin.
                // If you want to take over the maintenance of the plugin:
                // a) fork the git repo and submit pull requests on Github.
                // b) email me the patches to this file and I will commit them on your behalf.
                return true;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var ep    = ShowNames.Parser.ExtractEpisode(query);
            var split = Regex.Replace(ShowNames.Parser.ReplaceEpisode(query, "{0}", true, true), @"[^A-Za-z0-9\s]", string.Empty).Replace(' ', ',').TrimEnd(',');
            var html  = Utils.GetHTML(Site + "wsgi/torrent/find?title=" + Utils.EncodeURL(split), cookies: Cookies);

            if (html.DocumentNode.SelectSingleNode("//form[@id = 'login']") != null)
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[@class='torrents']/tr/td[4]/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = Regex.Replace(node.InnerText, @"(?:\s*\[[SH]D(?: 720)?\]\s*|\s*(?:\- )?(?:HD)?720p?)", string.Empty);

                if (ep != null && !Regex.IsMatch(link.Release, @"[^A-Za-z]e(?:p(?:isode|\.)?)?[\.\s_]?\s?0?" + ep.Episode, RegexOptions.IgnoreCase))
                {
                    continue;
                }

                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.FileURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("../../td[1]/a/img[@alt='[torrent]']/..", "href");
                link.Quality = node.GetHtmlValue("../../td[3]/img[contains(@title, 'High Definition')]") != null
                             ? Qualities.HDTV720p
                             : Qualities.HDTVXviD;
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[5]/img/following-sibling::text()[1]").Trim(), node.GetTextValue("../../td[5]/img/following-sibling::text()[2]").Trim())
                             + (node.GetHtmlValue("../../td[3]/img[contains(@title, 'Internal torrent')]") != null ? ", Internal" : string.Empty);

                var size  = Regex.Match(node.GetTextValue("../div[@class='notes']"), @"(?:(\d+(?:\.\d+)?)([KMG]B))");
                link.Size = size.Groups[1].Value + " " + size.Groups[2].Value;

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
            var cookie = "username=" + Utils.EncodeURL(username) + "&password=" + Utils.EncodeURL(password);
            var login  = Utils.GetURL(LoginURL, cookie);
            var auth   = Regex.Match(login, @"'cookie':\s*'([^']+)");

            if (auth.Success)
            {
                return "auth=" + auth.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}
