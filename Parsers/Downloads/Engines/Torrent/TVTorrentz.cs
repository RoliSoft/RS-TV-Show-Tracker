namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;

    /// <summary>
    /// Provides support for scraping TVTorrentz.
    /// </summary>
    [Parser("2011-08-30 11:55 AM"), TestFixture]
    public class TVTorrentz : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TVTorrentz";
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
                return "http://tvtorrentz.org/";
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
                        { "username", LoginFieldTypes.UserName },
                        { "password", LoginFieldTypes.Password },
                        { "returnto", LoginFieldTypes.ReturnTo },
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
            var id = GetIDForShow(ShowNames.Parser.Split(query)[0]);

            if (!id.HasValue)
            {
                yield break;
            }

            var html = Utils.GetHTML(Site + "browse.php?cat=" + id.Value, cookies: Cookies);

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[@class='torrent_table']/tr/td[3]/a");

            if (links == null)
            {
                yield break;
            }

            var episode = ShowNames.Parser.ExtractEpisode(query, "{0:0}x{1:00}");

            foreach (var node in links)
            {
                if (!string.IsNullOrWhiteSpace(episode) && !node.InnerText.Contains(episode))
                {
                    continue;
                }

                var link = new Link(this);

                var dl = node.GetNodeAttributeValue("../../td[4]/a[1]", "href");
                if (!string.IsNullOrEmpty(dl))
                {
                    link.FileURL = Site + dl;
                }
                else
                {
                    link.Infos = "N/A for your class, ";
                }

                link.Release = node.GetTextValue("../../td[2]/a") + " " + Regex.Replace(node.InnerText, @"(?:\b|_)([0-9]{1,2})x([0-9]{1,2})(?:\b|_)(\s-)?", me => "S" + me.Groups[1].Value.ToInteger().ToString("00") + "E" + me.Groups[2].Value.ToInteger().ToString("00"), RegexOptions.IgnoreCase);
                link.InfoURL = Site + node.GetAttributeValue("href");
                link.Size    = node.GetTextValue("../../td[7]").Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos  += Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[8]").Trim(), node.GetTextValue("../../td[9]").Trim()) + " / " + node.GetTextValue("../../td[10]").Trim() + " adoption"
                             + (node.GetHtmlValue("../../td[11]/img[starts-with(@title, 'Free')]") != null ? ", Free" : string.Empty);

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
            var cache   = Utils.GetURL(Site + "searchCache.js", cookies: Cookies);
            var matches = Regex.Matches(cache, @"\{text:""(?<name>[^""]+)"",url:""(?<id>\d+)""\}");

            ShowIDs = matches.Cast<Match>()
                     .ToDictionary(match => match.Groups["id"].Value.ToInteger(),
                                   match => HtmlEntity.DeEntitize(match.Groups["name"].Value));

            using (var file = File.Create(Path.Combine(Path.GetTempPath(), "TVTorrentz-IDs")))
            using (var bson = new BsonWriter(file))
            {
                new JsonSerializer().Serialize(bson, ShowIDs);
            }
        }

        /// <summary>
        /// Gets the ID for a show name.
        /// </summary>
        /// <param name="name">The show name.</param>
        /// <returns>Corresponding ID.</returns>
        public int? GetIDForShow(string name)
        {
            var fn = Path.Combine(Path.GetTempPath(), "TVTorrentz-IDs");

            if (ShowIDs == null)
            {
                if (File.Exists(fn))
                {
                    using (var file = File.OpenRead(fn))
                    using (var bson = new BsonReader(file))
                    {
                        var js = new JsonSerializer();
                        ShowIDs = js.Deserialize<Dictionary<int, string>>(bson);
                        file.Close();
                    }
                }
                else
                {
                    GetIDs();
                }
            }

            if (ShowIDs != null)
            {
                var id = SearchForID(name);
                if (id.HasValue)
                {
                    return id;
                }
            }

            // try to refresh if the cache is older than an hour
            if ((DateTime.Now - File.GetLastWriteTime(fn)).TotalHours > 1)
            {
                GetIDs();

                if (ShowIDs != null)
                {
                    var id = SearchForID(name);
                    if (id.HasValue)
                    {
                        return id;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for the specified show in the local cache.
        /// </summary>
        /// <param name="name">The show name.</param>
        /// <returns>Corresponding ID.</returns>
        private int? SearchForID(string name)
        {
            var parts = Database.GetReleaseName(name);

            foreach (var show in ShowIDs)
            {
                if (ShowNames.Parser.IsMatch(show.Value, parts, null, false) &&
                    ShowNames.Parser.IsMatch(name, Database.GetReleaseName(show.Value), null, false))
                {
                    return show.Key;
                }
            }

            return null;
        }
    }
}
