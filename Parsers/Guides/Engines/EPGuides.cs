namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using WebSearch.Engines;

    /// <summary>
    /// Provides support for scraping EPGuides pages.
    /// </summary>
    [TestFixture]
    public class EPGuides : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "EPGuides";
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
                return "http://epguides.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/epg.png";
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
                return Utils.DateTimeToVersion("2012-04-15 4:03 AM");
            }
        }

        private readonly Regex _infoRegex = new Regex(@"
(?:<h1>(?:<a.*?>)?(?<title>[^<]+)| # title
(?<airing>to:\s<em>_*\s_*)|        # airing
<td>.*?(?<runtime>[0-9]+)\s*min)   # runtime
", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _guideRegex = new Regex(@"
\s*(?:[0-9]*\.?)\s+                                   # episode id
(?<season>[0-9]+)\-                                   # season
(?:\s|0)?(?<episode>[0-9]+)                           # episode
\s{1,4}(?<airdate>.{9})\s+                            # airdate
(?:<a.*?>(?:<img.*?></a>\s*<a.*?>)?)?(?<title>[^<$]+) # title
", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private readonly string _defaultDb;

        /// <summary>
        /// Initializes a new instance of the <see cref="EPGuides"/> class.
        /// </summary>
        public EPGuides()
        {
            _defaultDb = "tv.com";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EPGuides"/> class.
        /// </summary>
        /// <param name="site">The site which to use through EPGuides.</param>
        public EPGuides(string site)
        {
            _defaultDb = site;
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var list = new DuckDuckGo().Search("{0} site:epguides.com".FormatWith(name)).ToList();

            foreach (var result in list)
            {
                if (!Regex.IsMatch(result.URL, @"^http://(?:www\.)?epguides\.com/(?!menu|current|grid|spring|dvds|faq|search)([a-z0-9_]+)/$", RegexOptions.IgnoreCase)) continue;

                yield return new ShowID
                    {
                        ID       = result.URL,
                        Title    = Regex.Replace(result.Title, @"\s+\(a Titles (?:&|and) (?:Air Dates|Seasons) Guide\).*$", string.Empty, RegexOptions.IgnoreCase),
                        Language = "en",
                        URL      = result.URL
                    };
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>TV show data.</returns>
        /// <exception cref="Exception">Failed to extract the listing. Maybe the EPGuides.com regex is out of date.</exception>
        public override TVShow GetData(string id, string language = "en")
        {
            var db = _defaultDb;

            if (id.Contains('\0'))
            {
                var tmp = id.Split('\0');
                     id = tmp[0];
                     db = tmp[1];
            }

            var listing = Utils.GetURL(id, "list=" + db, autoDetectEncoding: true);
            var show = new TVShow
                {
                    AirTime  = "20:00",
                    Language = "en",
                    URL      = id,
                    Episodes = new List<TVShow.Episode>()
                };

            var mc = _infoRegex.Matches(listing);

            foreach (Match m in mc)
            {
                if (m.Groups["title"].Success)
                {
                    show.Title = m.Groups["title"].Value;
                }

                if (m.Groups["airing"].Success)
                {
                    show.Airing = true;
                }

                if (m.Groups["runtime"].Success)
                {
                    show.Runtime = m.Groups["runtime"].Value.Trim().ToInteger();
                    show.Runtime = show.Runtime == 30
                                   ? 20
                                   : show.Runtime == 50 || show.Runtime == 60
                                     ? 40
                                     : show.Runtime;
                }
            }

            var prod = Regex.Match(listing, @"(?<start>_+\s{1,5}_+\s{1,5})(?<length>_+\s{1,5})_+\s{1,5}_+");
            listing = Regex.Replace(listing, @"(.{" + prod.Groups["start"].Value.Length + "})(.{" + prod.Groups["length"].Value.Length + "})(.+)", "$1$3");

            mc = _guideRegex.Matches(listing);

            if (mc.Count == 0)
            {
                throw new Exception("Failed to extract the listing. Maybe the EPGuides.com regex is out of date.");
            }

            foreach (Match m in mc)
            {
                var ep = new TVShow.Episode();

                ep.Season = m.Groups["season"].Value.Trim().ToInteger();
                ep.Number = m.Groups["episode"].Value.Trim().ToInteger();
                ep.Title = HtmlEntity.DeEntitize(m.Groups["title"].Value);

                var dt = DateTime.MinValue;

                switch (db)
                {
                    case "tvrage.com":
                        DateTime.TryParseExact(m.Groups["airdate"].Value.Trim(), "dd/MMM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        break;

                    case "tv.com":
                        DateTime.TryParseExact(m.Groups["airdate"].Value.Trim(), "d MMM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        break;
                }

                if (dt == DateTime.MinValue && !DateTime.TryParse(m.Groups["airdate"].Value.Trim(), out dt))
                {
                    dt = Utils.UnixEpoch;
                }

                ep.Airdate = dt;

                show.Episodes.Add(ep);
            }

            if (show.Episodes.Count != 0)
            {
                show.AirDay = show.Episodes.Last().Airdate.DayOfWeek.ToString();
            }

            return show;
        }
    }
}
