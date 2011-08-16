namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping BroadcasTheNet.
    /// </summary>
    [Parser("2011-08-16 16:09 PM"), TestFixture]
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
            var html = Utils.GetHTML(Site + "torrents.php?searchstr=" + Uri.EscapeUriString(query), cookies: Cookies);

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

                var release = Regex.Match(node.InnerHtml, @"<b>Release Name</b>: ([^<$]+)");
                var quality = node.GetTextValue("a[2]/following-sibling::text()").Trim();

                if (release.Success && !release.Groups[1].Value.Trim().Contains("Not Available"))
                {
                    link.Release = HtmlEntity.DeEntitize(release.Groups[1].Value.Trim());
                    link.Quality = FileNames.Parser.ParseQuality(link.Release);
                }
                else
                {
                    link.Release = HtmlEntity.DeEntitize(node.GetTextValue("a[1]") + " " + node.GetTextValue("a[2]") + " " + quality.Replace("[", string.Empty).Replace("]", string.Empty).Replace(" / ", " "));
                }

                if (link.Quality == Qualities.Unknown)
                {
                    link.Quality = ParseQuality(quality);
                }

                link.InfoURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("a[2]", "href"));
                link.FileURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("span/a[contains(@href, 'action=download')]", "href"));
                link.Size    = node.GetTextValue("../td[5]").Trim();
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../td[6]").Trim(), node.GetTextValue("../td[7]").Trim())
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

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            if (IsMatch(release, @"1080(i|p)", @"WEB"))
            {
                return Qualities.WebDL1080p;
            }
            if (IsMatch(release, @"1080(i|p)", @"(Bluray|BD|HDDVD)"))
            {
                return Qualities.BluRay1080p;
            }
            if (IsMatch(release, @"1080(i|p)", @"HDTV"))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch(release, @"720p", @"WEB"))
            {
                return Qualities.WebDL720p;
            }
            if (IsMatch(release, @"720p", @"(Bluray|BD|HDDVD)"))
            {
                return Qualities.BluRay720p;
            }
            if (IsMatch(release, @"720p", @"HDTV"))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch(release, @"(x264|h.264|MKV)"))
            {
                return Qualities.HRx264;
            }
            if (IsMatch(release, @"(HDTV|DSR|DVDRip)"))
            {
                return Qualities.HDTVXviD;
            }
            if (IsMatch(release, @"TVRip"))
            {
                return Qualities.TVRip;
            }

            return Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified input is matches all the specified regexes.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="regexes">The regexes.</param>
        /// <returns>
        /// 	<c>true</c> if the specified input matches all the specified regexes; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string input, params string[] regexes)
        {
            return regexes.All(regex => Regex.IsMatch(input, regex, RegexOptions.IgnoreCase));
        }
    }
}
