namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping nCore.
    /// </summary>
    [TestFixture]
    public class nCore : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "nCore";
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
                return "http://ncore.cc/";
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
                return "http://static.ncore.cc/styles/ncore.ico";
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
                return Utils.DateTimeToVersion("2011-08-16 15:59 PM");
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
                return new[] { "nick", "pass", "nyelv", "stilus" };
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
                        { "set_lang",        "hu"                     },
                        { "submitted",       "1"                      },
                        { "submit",          "Belépés!"               },
                        { "nev",             LoginFieldTypes.UserName },
                        { "pass",            LoginFieldTypes.Password },
                        { "ne_leptessen_ki", "1"                      },
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
            var html = Utils.GetHTML(Site + "torrents.php", "nyit_sorozat_resz=true&kivalasztott_tipus[]=xvidser_hun&kivalasztott_tipus[]=xvidser&kivalasztott_tipus[]=dvdser_hun&kivalasztott_tipus[]=dvdser&kivalasztott_tipus[]=hdser_hun&kivalasztott_tipus[]=hdser&mire=" + Uri.EscapeUriString(query) + "&miben=name&tipus=kivalasztottak_kozott&aktiv_inaktiv_ingyenes=mindehol", Cookies, Encoding.GetEncoding("iso-8859-2"));

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//a[starts-with(@onclick, 'torrent(')]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.GetAttributeValue("title");
                link.InfoURL = Site + "torrents.php?action=details&id=" + Regex.Match(node.GetAttributeValue("href"), @"id=(\d+)").Groups[1].Value;
                link.FileURL = Site + "torrents.php?action=download&id=" + Regex.Match(node.GetAttributeValue("href"), @"id=(\d+)").Groups[1].Value;
                link.Size    = node.GetTextValue("../../../../div[@class='box_meret2']/text()").Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../../div[@class='box_s2']").Trim(), node.GetTextValue("../../../../div[@class='box_l2']").Trim())
                             + (node.GetHtmlValue("../..//div[contains(@title, 'Ingyenes torrent!')]") != null ? ", Free" : string.Empty)
                             + (node.GetHtmlValue("../../../..//span[@class='bonus_down']") != null ? ", " + node.GetTextValue("../../../..//span[@class='bonus_down']").Trim().Substring(2) + " Download" : string.Empty)
                             + (node.GetHtmlValue("../../../..//span[@class='bonus_up']") != null ? ", " + node.GetTextValue("../../../..//span[@class='bonus_up']").Trim().Substring(2) + " Upload" : string.Empty);

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
