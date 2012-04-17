namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping ScienceHD.
    /// </summary>
    [TestFixture]
    public class ScienceHD : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "ScienceHD";
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
                return "https://sciencehd.me/";
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
                return Utils.DateTimeToVersion("2011-08-16 16:15 PM");
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
            var split = ShowNames.Parser.Split(query);
            var html  = Utils.GetHTML(Site + "torrents.php?action=advanced&seriesname=" + Utils.EncodeURL(split[0]) + "&torrentname=" + (split.Length != 1 ? Utils.EncodeURL(split[1]) : string.Empty), cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[@class='torrent_table']/tr");

            if (links == null)
            {
                yield break;
            }

            var group = string.Empty;

            foreach (var node in links)
            {
                var type = node.GetAttributeValue("class");

                if (string.IsNullOrEmpty(type))
                {
                    group = HtmlEntity.DeEntitize(node.GetTextValue("td[2]/a[@class='tseries']") + " " + node.GetTextValue("td[2]/a[@class='tdetails']"));
                }
                else if (type.StartsWith("group_torrent") && group.Length != 0)
                {
                    var link = new Link(this);
                    var item = node.GetTextValue("td[1]/span[2]/a") ?? string.Empty;

                    link.Release = group;
                    link.InfoURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("td[1]/span[2]/a", "href"));
                    link.FileURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("td[1]/span[1]/a[1]", "href"));
                    link.Size    = node.GetTextValue("td[4]").Trim();
                    link.Quality = ParseQuality(item);
                    link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("td[7]").Trim(), node.GetTextValue("td[8]").Trim())
                                 + (item.Contains("Scene") ? ", Scene" : string.Empty)
                                 + (item.Contains("Freeleech") ? ", Free" : string.Empty);

                    if (link.Quality == Qualities.Unknown)
                    {
                        link.Release += " " + item;
                    }

                    yield return link;
                }
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

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            if (IsMatch("1080(i|p)", release))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch("720p", release))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch("MKV", release))
            {
                return Qualities.HRx264;
            }
            if (IsMatch("AVI", release))
            {
                return Qualities.HDTVXviD;
            }

            return Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified pattern is a match.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input.</param>
        /// <returns>
        /// 	<c>true</c> if the specified pattern is a match; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string pattern, string input)
        {
            return Regex.IsMatch(input, pattern.Replace("-", @"(\-|\s)?"), RegexOptions.IgnoreCase);
        }
    }
}
