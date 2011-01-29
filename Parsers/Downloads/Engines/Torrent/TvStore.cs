namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;

    /// <summary>
    /// Provides support for scraping tvstore.me.
    /// </summary>
    [Parser("RoliSoft", "2011-01-29 9:32 PM")]
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
        public Dictionary<int, string> ShowIDs { get; set; }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var gyors = Utils.GetURL(Site + "torrent/br_process.php?gyors=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ShowNames.ReplaceEpisode(query, 1))).Replace('=', '_') + "&now=" + DateTime.Now.ToUnixTimestamp(), cookies: Cookies);
            var arr   = gyors.Split('\\');

            if (arr[0] == "0")
            {
                yield break;
            }

            var idx  = 3;

            for (;idx <= (arr.Length - 10);)
            {
                var link = new Link { Site = Name };
                var name = GetShowForID(arr[idx].Trim().ToInteger());

                idx++;

                link.URL = Site + "torrent/download.php?id=" + arr[idx].Trim();

                idx++;

                link.Release = name + " " + arr[idx].Trim();

                idx++;

                var quality   = arr[idx].Trim();
                link.Quality  = ParseQuality(quality);
                link.Release += " " + quality.Replace("[", string.Empty).Replace("]", string.Empty).Replace(" - ", " ");

                idx += 7;

                link.Size = Utils.GetFileSize(long.Parse(arr[idx].Trim()));

                idx += 18;

                yield return link;
            }
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            var q = Regex.Match(release, @"\[(?:(?:PROPER|REPACK)(?:\s\-)?)?\s*(.*?)\s\-").Groups[1].Value;

            if (IsMatch("Blu-ray-1080p", q))
            {
                return Qualities.BluRay1080p;
            }
            if (IsMatch("HDTV-1080(p|i)", q))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch("Web-Dl-720p", q))
            {
                return Qualities.WebDL720p;
            }
            if (IsMatch("Blu-ray-720p", q))
            {
                return Qualities.BluRay720p;
            }
            if (IsMatch("HDTV-720p", q))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch("HR-HDTV", q))
            {
                return Qualities.HRx264;
            }
            if (IsMatch("TvRip", q))
            {
                return Qualities.TVRip;
            }
            if (IsMatch("(PDTV|DVDSRC|Rip$)", q))
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

        /// <summary>
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var browse  = Utils.GetURL(Site + "torrent/browse.php", cookies: Cookies);
            var matches = Regex.Matches(browse, @"catse\[(?<id>\d+)\]\s*=\s*'(?<name>[^']+)';");

            ShowIDs = matches.Cast<Match>()
                     .ToDictionary(match => match.Groups["id"].Value.ToInteger(),
                                   match => match.Groups["name"].Value);

            using (var file = File.Create(Path.Combine(Path.GetTempPath(), "TvStore-IDs")))
            using (var bson = new BsonWriter(file))
            {
                var js = new JsonSerializer();
                js.Serialize(bson, ShowIDs);
                file.Close();
            }
        }

        /// <summary>
        /// Gets the show name for an ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>Corresponding show name.</returns>
        public string GetShowForID(int id)
        {
            if (ShowIDs == null)
            {
                var fn = Path.Combine(Path.GetTempPath(), "TvStore-IDs");

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

            if (ShowIDs.ContainsKey(id))
            {
                return ShowIDs[id];
            }
            else
            {
                // try to refresh
                GetIDs();

                if (ShowIDs.ContainsKey(id))
                {
                    return ShowIDs[id];
                }
                else
                {
                    return "ID-" + id;
                }
            }
        }
    }
}
