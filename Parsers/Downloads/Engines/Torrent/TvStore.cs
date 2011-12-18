namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using ProtoBuf;

    /// <summary>
    /// Provides support for scraping tvstore.me.
    /// </summary>
    [TestFixture]
    public class TvStore : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TvStore";
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
                return "http://tvstore.me/";
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
                return "http://tvstore.me/pic/favicon.ico";
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
                return Utils.DateTimeToVersion("2011-12-18 17:36 PM");
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
                return new[] { "id", "pass" };
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
                        { "back",     LoginFieldTypes.ReturnTo },
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
        /// Gets or sets the show IDs on the site.
        /// </summary>
        /// <value>The show IDs.</value>
        public static Dictionary<int, string> ShowIDs { get; set; }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var gyors = Utils.GetURL(Site + "torrent/br_process.php?gyors=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ShowNames.Parser.ReplaceEpisode(query, "{0:0}x{1:00}"))).Replace('=', '_') + "&now=" + DateTime.Now.ToUnixTimestamp(), cookies: Cookies);

            if (string.IsNullOrWhiteSpace(gyors))
            {
                throw new InvalidCredentialException();
            }
            
            var arr = gyors.Split('\\');

            if (arr[0] == "0")
            {
                yield break;
            }

            var idx = 4;

            for (;idx <= (arr.Length - 10);)
            {
                var link = new Link(this);
                var name = GetShowForID(arr[idx].Trim().ToInteger());

                idx++;

                link.InfoURL = Site + "torrent/browse.php?id=" + arr[idx].Trim();
                link.FileURL = Site + "torrent/download.php?id=" + arr[idx].Trim();

                idx++;

                link.Release = HtmlEntity.DeEntitize(name + " " + Regex.Replace(arr[idx].Trim(), @"(?:\b|_)([0-9]{1,2})x([0-9]{1,2})(?:\b|_)", me => "S" + me.Groups[1].Value.ToInteger().ToString("00") + "E" + me.Groups[2].Value.ToInteger().ToString("00"), RegexOptions.IgnoreCase));

                idx++;

                var quality   = arr[idx].Trim();
                link.Quality  = FileNames.Parser.ParseQuality(Regex.Match(quality, @"\[(?:(?:PROPER|REPACK)(?:\s\-)?)?\s*(.*?)\s\-").Groups[1].Value);
                link.Release += " " + quality.Replace("[", string.Empty).Replace("]", string.Empty).Replace(" - ", " ");

                idx += 7;

                link.Size = Utils.GetFileSize(long.Parse(arr[idx].Trim()));

                idx += 10;

                link.Infos = Link.SeedLeechFormat.FormatWith(arr[idx + 1].Trim(), arr[idx].Trim());

                idx += 7;

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
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var browse  = Utils.GetURL(Site + "torrent/browse.php", cookies: Cookies);
            var matches = Regex.Matches(browse, @"catse\[(?<id>\d+)\]\s*=\s*'(?<name>[^']+)';");

            ShowIDs = matches.Cast<Match>()
                     .ToDictionary(match => match.Groups["id"].Value.ToInteger(),
                                   match => HtmlEntity.DeEntitize(match.Groups["name"].Value));

            using (var file = File.Create(Path.Combine(Path.GetTempPath(), "TvStore-IDs.bin")))
            {
                Serializer.Serialize(file, ShowIDs);
            }
        }

        /// <summary>
        /// Gets the show name for an ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>Corresponding show name.</returns>
        public string GetShowForID(int id)
        {
            var fn = Path.Combine(Path.GetTempPath(), "TvStore-IDs.bin");

            if (ShowIDs == null)
            {
                if (File.Exists(fn))
                {
                    using (var file = File.OpenRead(fn))
                    {
                        ShowIDs = Serializer.Deserialize<Dictionary<int, string>>(file);
                    }
                }
                else
                {
                    GetIDs();
                }
            }

            string show;
            if (ShowIDs != null && ShowIDs.TryGetValue(id, out show))
            {
                return show;
            }

            // try to refresh if the cache is older than an hour
            if ((DateTime.Now - File.GetLastWriteTime(fn)).TotalHours > 1)
            {
                GetIDs();

                if (ShowIDs != null && ShowIDs.TryGetValue(id, out show))
                {
                    return show;
                }
            }

            return "ID-" + id;
        }
    }
}
