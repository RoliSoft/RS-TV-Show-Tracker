namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping tvtorrents.com.
    /// </summary>
    [Parser("2011-08-16 16:27 PM"), TestFixture]
    public class TvTorrents : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TvTorrents";
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
                return "http://www.tvtorrents.com/";
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
                return new[] { "cookie_login" };
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
                return Site + "login.do";
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
                        { "posted",   "true"                   },
                        { "username", LoginFieldTypes.UserName },
                        { "password", LoginFieldTypes.Password },
                        { "cookie",   "Y"                      },
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
            var show = ShowNames.Parser.Split(query)[0];
            var html = Utils.GetHTML(Site + "loggedin/search.do?search=" + Uri.EscapeUriString(show), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[2]/tr/td[3]");

            if (links == null)
            {
                yield break;
            }

            var hash    = Regex.Match(html.DocumentNode.InnerHtml, "hash='(.*?)';").Groups[1].Value;
            var digest  = Regex.Match(html.DocumentNode.InnerHtml, "digest='(.*?)';").Groups[1].Value;
            var episode = ShowNames.Parser.ExtractEpisode(query, "{0:0}x{1:00}");
            
            foreach (var node in links)
            {
                if (!string.IsNullOrWhiteSpace(episode) && !node.GetTextValue("a").StartsWith(episode))
                {
                    continue;
                }

                var link = new Link(this);

                link.Release = Regex.Replace(node.InnerText, @"(?:\b|_)([0-9]{1,2})x([0-9]{1,2})(?:\b|_)", me => "S" + me.Groups[1].Value.ToInteger().ToString("00") + "E" + me.Groups[2].Value.ToInteger().ToString("00"), RegexOptions.IgnoreCase);
                link.InfoURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("a", "href");
                link.FileURL = "http://torrent.tvtorrents.com/FetchTorrentServlet?info_hash=" + node.GetNodeAttributeValue("a", "href").Split('=').Last() + "&digest=" + digest + "&hash=" + hash;
                link.Size    = node.GetNodeAttributeValue("../td[5]", "title").Replace("Torrent is ", string.Empty).Replace("b", "B");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../td[4]/br/preceding-sibling::text()").Trim(), node.GetTextValue("../td[4]/br/following-sibling::text()"));

                if (link.Quality == Qualities.Unknown)
                {
                    link.Quality = Qualities.HDTVXviD;
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
