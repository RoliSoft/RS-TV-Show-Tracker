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
                return Utils.DateTimeToVersion("2011-09-18 2:57 AM");
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
            var html  = Utils.GetHTML("http://www.tv.com/search?type=11&stype=all&tag=search%3Bfrontdoor&q=" + Uri.EscapeUriString(name));
            var shows = html.DocumentNode.SelectNodes("//div/h4/a");

            if (shows == null)
            {
                yield break;
            }

            foreach (var show in shows)
            {
                var id  = new ShowID();
                
                id.URL      = Site.TrimEnd('/') + HtmlEntity.DeEntitize(show.GetAttributeValue("href"));
                id.ID       = Regex.Match(id.URL, @"/shows/([^/]+)/").Groups[1].Value;
                id.Title    = HtmlEntity.DeEntitize(show.InnerText);
                id.Cover    = show.GetNodeAttributeValue("../../../a/img", "src");
                id.Language = "en";

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
            var show    = new TVShow();

            show.Title       = HtmlEntity.DeEntitize(summary.DocumentNode.GetNodeAttributeValue("//meta[@property='og:title']", "content"));
            show.Genre       = Regex.Replace(summary.DocumentNode.GetTextValue("//h4[text()='Genre']/following-sibling::p[1]") ?? string.Empty, @"\s+", string.Empty).Replace(",", ", ");
            show.Description = HtmlEntity.DeEntitize((summary.DocumentNode.GetTextValue("//div[starts-with(@class, 'show_description')]") ?? string.Empty).Replace("&hellip; More", string.Empty));
            show.Cover       = summary.DocumentNode.GetNodeAttributeValue("//meta[@property='og:image']", "content");
            show.Airing      = !Regex.IsMatch(summary.DocumentNode.GetTextValue("//h4[text()='Status']/following-sibling::p[1]") ?? string.Empty, "(Canceled|Ended)");
            show.Runtime     = 30;
            show.Language    = "en";
            show.URL         = "http://www.tv.com/shows/{0}/".FormatWith(id);
            show.Episodes    = new List<TVShow.Episode>();

            var airinfo = summary.DocumentNode.GetTextValue("//span[@class='tagline']");
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

                var network = Regex.Match(airinfo, @"on ([^\s$\(]+)");
                if (network.Success)
                {
                    show.Network = network.Groups[1].Value;
                }
            }

            var listing = Utils.GetHTML("http://www.tv.com/shows/{0}/season/?season=all".FormatWith(id));
            var nodes   = listing.DocumentNode.SelectNodes("//li[starts-with(@class, 'episode')]");

            if (nodes == null)
            {
                return show;
            }

            foreach (var node in nodes.Reverse())
            {
                var meta   = node.GetTextValue(".//div[@class='meta']");
                var season = Regex.Match(meta, "Season ([0-9]+)");
                var epnr   = Regex.Match(meta, "Episode ([0-9]+)");
                var aired  = Regex.Match(meta, @"Aired: (\d{1,2}/\d{1,2}/\d{4})");

                if (!season.Success || !epnr.Success) { continue; }

                var ep = new TVShow.Episode();

                ep.Season  = season.Groups[1].Value.ToInteger();
                ep.Number  = epnr.Groups[1].Value.ToInteger();
                ep.Title   = HtmlEntity.DeEntitize(node.GetTextValue(".//h3").Trim());
                ep.Summary = HtmlEntity.DeEntitize(node.GetTextValue(".//p[@class='synopsis']").Trim());
                ep.Picture = node.GetNodeAttributeValue(".//div[@class='THUMBNAIL']/a/img", "src");
                ep.URL     = node.GetNodeAttributeValue("div/h3/a", "href");

                if (ep.Summary == "No synopsis available. Write a synopsis.")
                {
                    ep.Summary = null;
                }

                DateTime dt;
                ep.Airdate = DateTime.TryParseExact(aired.Groups[1].Value, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                             ? dt
                             : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            return show;
        }
    }
}
