/*namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Authentication;
    using System.Text;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping IPTorrents.
    /// </summary>
    [Parser("2011-10-30 18:15 PM"), TestFixture]
    public class IPTorrents : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "IPTorrents";
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
                return "http://on.iptorrents.com/";
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
                        { "username",   LoginFieldTypes.UserName },
                        { "password",   LoginFieldTypes.Password },
                        { "vImageCodP", LoginFieldTypes.Captcha  },
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
            var html = Utils.GetHTML(Site + "torrents/?title_only=1&q=" + Uri.EscapeUriString(query), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/b/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText).Trim();
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.FileURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("../../../td[4]/a", "href");
                link.Size    = node.GetTextValue("../../../td[6]").Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../td[8]").Trim(), node.GetTextValue("../../../td[9]").Trim())
                             + (node.GetHtmlValue("..//font[@color='#FF0000' and contains(text(), 'FreeLeech')]") != null ? ", Free" : string.Empty);

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
            // get captcha image

            var reqcook = new StringBuilder();
            var sectext = string.Empty;
            var captcha = Utils.GetURL(Site + "img.php?size=6", encoding: new Utils.Base64Encoding(),
                request:  req  => req.Referer = Site,
                response: resp =>
                    {
                        if (resp.Cookies == null || resp.Cookies.Count == 0)
                        {
                            return;
                        }

                        foreach (Cookie cookie in resp.Cookies)
                        {
                            if (reqcook.Length != 0)
                            {
                                reqcook.Append("; ");
                            }

                            reqcook.Append(cookie.Name + "=" + cookie.Value);
                        }
                    });

            // show captcha to user

            MainWindow.Active.Run(() =>
                {
                    var cw  = new CaptchaWindow(Name, Convert.FromBase64String(captcha), 160, 20);
                    var res = cw.ShowDialog();

                    if (res.HasValue && res.Value)
                    {
                        sectext = cw.Solution;
                    }
                });

            if (string.IsNullOrWhiteSpace(sectext))
            {
                return string.Empty;
            }

            // send login request

            var cookies = new StringBuilder();
            var post    = "username=" + Uri.EscapeDataString(username) + "&password=" + Uri.EscapeDataString(password) + "&vImageCodP=" + Uri.EscapeDataString(sectext);

            Utils.GetURL(LoginURL, post, reqcook.ToString(),
                request: req =>
                    {
                        req.Referer = Site;
                        req.AllowAutoRedirect = false;
                    },
                response: resp =>
                    {
                        if (resp.Cookies == null || resp.Cookies.Count == 0)
                        {
                            return;
                        }

                        foreach (Cookie cookie in resp.Cookies)
                        {
                            if (cookie.Name == "PHPSESSID" || cookie.Name == "JSESSIONID" || cookie.Value == "deleted")
                            {
                                continue;
                            }

                            if (cookies.Length != 0)
                            {
                                cookies.Append("; ");
                            }

                            cookies.Append(cookie.Name + "=" + cookie.Value);
                        }
                    });

            return cookies.ToString();
        }
    }
}
*/