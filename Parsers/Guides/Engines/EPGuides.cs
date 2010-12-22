namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    /// <summary>
    /// Provides support for scraping EPGuides pages.
    /// </summary>
    public class EPGuides : Guide
    {
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
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        /// <exception cref="Exception">Failed to extract the listing. Maybe the EPGuides.com regex is out of date.</exception>
        public override TVShow GetData(string id)
        {
            var db = _defaultDb;

            if (id.Contains('\0'))
            {
                var tmp = id.Split('\0');
                     id = tmp[0];
                     db = tmp[1];
            }

            var listing = Utils.GetURL(id, "list=" + db, autoDetectEncoding: true);
            var show    = new TVShow
                {
                    AirTime  = "20:00",
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
                    show.Runtime = int.Parse(m.Groups["runtime"].Value.Trim());
                    show.Runtime = show.Runtime == 30
                                   ? 20
                                   : show.Runtime == 50 || show.Runtime == 60
                                     ? 40
                                     : show.Runtime;
                }
            }

            var prod = Regex.Match(listing, @"(?<start>_+\s{1,5}_+\s{1,5})(?<length>_+\s{1,5})_+\s{1,5}_+");
            listing  = Regex.Replace(listing, @"(.{" + prod.Groups["start"].Value.Length + "})(.{" + prod.Groups["length"].Value.Length + "})(.+)", "$1$3");

            mc = _guideRegex.Matches(listing);

            if (mc.Count == 0)
            {
                throw new Exception("Failed to extract the listing. Maybe the EPGuides.com regex is out of date.");
            }

            DateTime dt;
            foreach(Match m in mc)
            {
                show.Episodes.Add(new TVShow.Episode
                    {
                        Season  = int.Parse(m.Groups["season"].Value.Trim()),
                        Number  = int.Parse(m.Groups["episode"].Value.Trim()),
                        Airdate = DateTime.TryParse(m.Groups["airdate"].Value, out dt)
                                ? dt
                                : Utils.UnixEpoch,
                        Title   = HtmlEntity.DeEntitize(m.Groups["title"].Value)
                    });
            }

            return show;
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override string GetID(string name)
        {
            // EPGuides doesn't have an internal search engine.
            return Utils.Google("allintitle:" + name + " site:epguides.com");
        }
    }
}
