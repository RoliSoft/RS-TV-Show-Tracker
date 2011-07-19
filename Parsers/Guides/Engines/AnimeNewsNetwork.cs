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
    /// Provides support for scraping Anime News Network pages.
    /// </summary>
    [TestFixture]
    public class AnimeNewsNetwork : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Anime News Network";
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
                return "http://www.animenewsnetwork.com/";
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
                return "/RSTVShowTracker;component/Images/animenewsnetwork.png";
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
            var results = WebSearch.Engines.DuckDuckGo(name + " intitle:\"episode titles\" site:animenewsnetwork.com/encyclopedia/").ToList();

            if (results.Count == 0)
            {
                yield break;
            }

            var ids = new List<string>();
            
            foreach (var result in results)
            {
                var id  = new ShowID();
                
                id.URL      = result.URL;
                id.ID       = Regex.Match(id.URL, @"\?id=(\d+)").Groups[1].Value;
                id.Title    = Regex.Split(result.Title, @"(?:\s\((?:TV|OAV)\)?|\s\[(?:Episode titles|Trivia|Links|In the news)\]|\s-\s(?:Anime\s?(?:\.\.\.|News\s?(?:\.\.\.|Network))))")[0];
                id.Language = "en";

                if (!ids.Contains(id.ID))
                {
                    ids.Add(id.ID);
                    yield return id;
                }
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
            var summary = Utils.GetHTML("http://www.animenewsnetwork.com/encyclopedia/anime.php?id=" + id);
            var show    = new TVShow();

            show.Title       = Regex.Replace(HtmlEntity.DeEntitize(summary.DocumentNode.GetTextValue("//h1[@id='page_header']")), @"\([^\)]+\)$", string.Empty);
            show.Description = HtmlEntity.DeEntitize(summary.DocumentNode.GetTextValue("//strong[text() = 'Plot Summary:']/following-sibling::span[1]") ?? string.Empty).Trim();
            show.Cover       = summary.DocumentNode.GetNodeAttributeValue("//img[@id='vid-art']", "src");
            show.Airing      = !Regex.IsMatch(summary.DocumentNode.GetTextValue("//strong[text() = 'Vintage:']/following-sibling::span[1]") ?? string.Empty, " to ");
            show.AirTime     = "20:00";
            show.Language    = "en";
            show.URL         = "http://www.animenewsnetwork.com/encyclopedia/anime.php?id=" + id;
            show.Episodes    = new List<TVShow.Episode>();

            var runtxt   = Regex.Match(summary.DocumentNode.GetTextValue("//strong[text() = 'Running time:']/following-sibling::span[1]") ?? string.Empty, "([0-9]+)");
            show.Runtime = runtxt.Success
                         ? int.Parse(runtxt.Groups[1].Value)
                         : 30;

            var genre = summary.DocumentNode.SelectNodes("//a[contains(@href, '/genreresults?') and @class = 'discreet']");
            if (genre != null)
            {
                foreach (var gen in genre)
                {
                    show.Genre += gen.InnerText + ", ";
                }

                show.Genre = show.Genre.TrimEnd(", ".ToCharArray());
            }

            var listing = Utils.GetHTML("http://www.animenewsnetwork.com/encyclopedia/anime.php?id=" + id + "&page=25");
            var nodes   = listing.DocumentNode.SelectNodes("//table[@class='episode-list']/tr");

            if (nodes == null)
            {
                return show;
            }

            foreach (var node in nodes)
            {
                var epnr = Regex.Match(node.GetTextValue("td[@class='n'][1]") ?? string.Empty, "([0-9]+)");
                if (!epnr.Success)
                {
                    continue;
                }

                var ep = new TVShow.Episode();

                ep.Season = 1;
                ep.Number = epnr.Groups[1].Value.ToInteger();
                ep.Title  = HtmlEntity.DeEntitize(node.GetTextValue("td[@valign='top'][1]/div[1]").Trim());
                ep.URL    = show.URL + "&page=25";

                DateTime dt;
                ep.Airdate = DateTime.TryParse((node.GetTextValue("td[@class='d'][1]/div") ?? string.Empty), out dt)
                           ? dt
                           : Utils.UnixEpoch;

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
