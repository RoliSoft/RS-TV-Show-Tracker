namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping TV.com pages.
    /// </summary>
    [TestFixture]
    public class TVcom : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TV.com";
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
                return "http://www.tv.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/tvcom.png";
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
                return Utils.DateTimeToVersion("2013-12-27 7:29 PM");
            }
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var html  = Utils.GetHTML("http://www.tv.com/search?q=" + Utils.EncodeURL(name));
            var shows = html.DocumentNode.SelectNodes("//div/h4/a");

            if (shows == null)
            {
                yield break;
            }

            foreach (var show in shows)
            {
                var id  = new ShowID(this);
                
                id.URL      = Site.TrimEnd('/') + HtmlEntity.DeEntitize(show.GetAttributeValue("href"));
                id.ID       = Regex.Match(id.URL, @"/shows/([^/]+)/").Groups[1].Value;
                id.Title    = HtmlEntity.DeEntitize(show.InnerText);
                id.Cover    = show.GetNodeAttributeValue("../../../a/img", "src");
                id.Language = "en";

                if (string.IsNullOrWhiteSpace(id.ID))
                {
                    continue;
                }

                yield return id;
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id, string language = "en")
        {
            var summary = Utils.GetHTML("http://www.tv.com/shows/{0}/".FormatWith(id));
            var listing = Utils.GetHTML("http://www.tv.com/shows/{0}/episodes/?printable=1".FormatWith(id));
            var show    = new TVShow();

            show.Title       = HtmlEntity.DeEntitize(summary.DocumentNode.GetNodeAttributeValue("//meta[@property='og:title']", "content"));
            show.Source      = GetType().Name;
            show.SourceID    = id;
            show.Description = HtmlEntity.DeEntitize((summary.DocumentNode.GetTextValue("//div[@class='description']/span") ?? string.Empty).Replace("&nbsp;", " ").Replace("moreless", string.Empty).Trim());
            show.Genre       = Regex.Replace(summary.DocumentNode.GetTextValue("//div[contains(@class, 'categories')]") ?? string.Empty, @"\s+", string.Empty).Replace("Categories", string.Empty).Replace(",", ", ");
            show.Cover       = summary.DocumentNode.GetNodeAttributeValue("//meta[@property='og:image']", "content");
            show.Airing      = !Regex.IsMatch(summary.DocumentNode.GetTextValue("//div[@class='tagline']") ?? string.Empty, "ended");
            show.Runtime     = 30;
            show.Language    = "en";
            show.URL         = "http://www.tv.com/shows/{0}/".FormatWith(id);
            show.Episodes    = new List<Episode>();

            var airinfo = summary.DocumentNode.GetTextValue("//div[@class='tagline']");
            if (airinfo != null)
            {
                airinfo = Regex.Replace(airinfo, @"\s+", " ").Trim();

                var airday = Regex.Match(airinfo, @"(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)");
                if (airday.Success)
                {
                    show.AirDay = airday.Groups[1].Value;
                }

                var airtime = Regex.Match(airinfo, @"(\d{1,2}:\d{2}(?: (?:AM|PM))?)", RegexOptions.IgnoreCase);
                if (airtime.Success)
                {
                    show.AirTime = airtime.Groups[1].Value;
                }

                var network = Regex.Match(airinfo, @"(?:on (?<n>[^\s$\(]+)|(?<n>.*)\s\(ended)");
                if (network.Success)
                {
                    show.Network = network.Groups["n"].Value;
                }
            }

            var nodes = listing.DocumentNode.SelectNodes("//li[@class='episode']");
            if (nodes == null)
            {
                return show;
            }

            foreach (var node in nodes)
            {
                var season = Regex.Match(node.GetTextValue("dl[1]/dd[2]") ?? string.Empty, "([0-9]+)");
                var epnr   = Regex.Match(node.GetTextValue("dl[1]/dd[3]") ?? string.Empty, "([0-9]+)");

                if (!season.Success || !epnr.Success) { continue; }

                var ep = new Episode();

                ep.Season  = season.Groups[1].Value.ToInteger();
                ep.Number  = epnr.Groups[1].Value.ToInteger();
                ep.Title   = HtmlEntity.DeEntitize(node.GetTextValue("a[@class='title']"));
                ep.Summary = HtmlEntity.DeEntitize(node.GetTextValue("div[contains(@class,'description')]").Replace("&nbsp;", " ").Trim());
                ep.URL     = node.GetNodeAttributeValue("a[@class='title']", "href");

                if (!string.IsNullOrWhiteSpace(ep.URL))
                {
                    ep.URL = "http://www.tv.com" + ep.URL;

                    var epid = Regex.Match(ep.URL, @"\-(\d+)\/?$");
                    if (epid.Success)
                    {
                        ep.Picture = "http://img2.tvtome.com/i/tve/em/" + epid.Groups[1].Value + ".jpg";
                    }
                }

                DateTime dt;
                ep.Airdate = DateTime.TryParseExact(node.GetTextValue("dl[1]/dd[1]") ?? string.Empty, "M/d/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                             ? dt
                             : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            show.Episodes.Reverse();
            return show;
        }
    }
}
