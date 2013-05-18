namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping BroadcasTheNet.
    /// </summary>
    [TestFixture]
    public class BroadcasTheNet : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "BroadcasTheNet";
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
                return "https://broadcasthe.net/";
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
                return Utils.DateTimeToVersion("2013-05-19 2:07 AM");
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
                return new[] { "keeplogged" };
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
                        { "keeplogged", "1"                      },
                        { "login",      "Log In!"                },
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
            var html = Utils.GetHTML(Site + "torrents.php?searchstr=" + Utils.EncodeURL(query), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[@id='torrent_table']/tr[not(position() = 1)]/td[3]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                var release = node.GetNodeAttributeValue(".//b[contains('Release Name:', text())]/following-sibling::span", "title");
                var quality = node.GetTextValue("a[2]/following-sibling::text()").Trim();

                if (release != null && !release.Trim().Contains("Not Available"))
                {
                    link.Release = HtmlEntity.DeEntitize(release).Replace("\\", string.Empty);
                    link.Quality = FileNames.Parser.ParseQuality(link.Release);
                }
                else
                {
                    link.Release = HtmlEntity.DeEntitize(node.GetTextValue("a[1]") + " " + node.GetTextValue("a[2]") + " " + quality.Replace("[", string.Empty).Replace("]", string.Empty).Replace(" / ", " "));
                }

                if (link.Quality == Qualities.Unknown)
                {
                    link.Quality = FileNames.Parser.ParseQuality(quality);
                }

                link.InfoURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("a[2]", "href"));
                link.FileURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("span/a[contains(@href, 'action=download')]", "href"));
                link.Size    = node.GetTextValue("../td[5]").Trim();
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../td[7]").Trim(), node.GetTextValue("../td[8]").Trim())
                             + ", Free"
                             + (node.GetHtmlValue("../td[4]/img[@title='FastTorrent']") != null ? ", Fast" : string.Empty)
                             + (node.GetHtmlValue("../td[4]/img[@title='Official BTN AutoUp']") != null ? ", Official Up." : string.Empty);

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
