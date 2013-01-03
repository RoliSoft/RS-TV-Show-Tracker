namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping EPisodeWorld pages.
    /// </summary>
    [TestFixture]
    public class EPisodeWorld : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "EPisodeWorld";
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
                return "http://www.episodeworld.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/episodeworld.png";
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
                return Utils.DateTimeToVersion("2011-07-19 8:12 PM");
            }
        }

        /// <summary>
        /// Gets the list of supported languages.
        /// </summary>
        /// <value>The list of supported languages.</value>
        public override string[] SupportedLanguages
        {
            get
            {
                return new[]
                    {
                        "en",
                        "hu",
                        "de",
                        "hr",
                        "cs",
                        "da",
                        "nl",
                        "fi",
                        "fr",
                        "el",
                        "he",
                        "it",
                        "ja",
                        "ko",
                        "lt",
                        "no",
                        "pl",
                        "pt",
                        "ru",
                        "sk",
                        "es",
                        "sv",
                        "tr",
                        "cy"
                    };
            }
        }

        /// <summary>
        /// Contains a list of supported languages and their numeric IDs.
        /// </summary>
        public static Dictionary<string, int> LanguageIDs = new Dictionary<string, int>
            {
                { "hr", 17 },
                { "cs", 15 },
                { "da", 14 },
                { "nl", 10 },
                { "en",  1 },
                { "fi",  9 },
                { "fr",  3 },
                { "de",  2 },
                { "el", 16 },
                { "he", 22 },
                { "hu", 19 },
                { "it",  5 },
                { "ja",  7 },
                { "ko", 21 },
                { "lt", 24 },
                { "no", 13 },
                { "pl",  6 },
                { "pt",  8 },
                { "ru", 12 },
                { "sk", 20 },
                { "es",  4 },
                { "sv", 11 },
                { "tr", 18 },
                { "cy", 23 }
            };

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var html  = Utils.GetHTML("http://www.episodeworld.com/search/?searchlang=" + LanguageIDs[language] + "&searchitem=" + Utils.EncodeURL(name));
            var shows = html.DocumentNode.SelectNodes("//table[@id='list']/tr/td[3]/a/b");

            if (shows == null)
            {
                var title = html.DocumentNode.GetTextValue("//div[@class='orangecorner_content']//h1");
                if (!string.IsNullOrWhiteSpace(title))
                {
                    var id  = new ShowID();
                
                    id.URL      = Site.TrimEnd('/') + HtmlEntity.DeEntitize(html.DocumentNode.GetNodeAttributeValue("//td[@class='boxes']/a[contains(text(), 'All Seasons')]", "href"));
                    id.ID       = Regex.Match(id.URL, @"show/([^/$]+)").Groups[1].Value;
                    id.Title    = HtmlEntity.DeEntitize(title);
                    id.Language = language;

                    yield return id;
                }

                yield break;
            }

            foreach (var show in shows)
            {
                var id  = new ShowID();
                
                id.URL      = Site.TrimEnd('/') + HtmlEntity.DeEntitize(show.GetNodeAttributeValue("..", "href"));
                id.ID       = Regex.Match(id.URL, @"show/([^/$]+)").Groups[1].Value;
                id.Title    = HtmlEntity.DeEntitize(show.InnerText);
                id.Language = language;

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
            var listing = Utils.GetHTML("http://www.episodeworld.com/show/" + id + "/season=all/" + Languages.List[language].ToLower() + "/episodeguide");
            var show    = new TVShow();

            show.Title       = HtmlEntity.DeEntitize(listing.DocumentNode.GetTextValue("//div[@class='orangecorner_content']//h1"));
            show.Description = HtmlEntity.DeEntitize(listing.DocumentNode.GetTextValue("//table/tr/td[1]/table[1]/tr[@class='centerbox_orange']/td[@class='centerbox_orange']") ?? string.Empty).Trim();
            show.Cover       = listing.DocumentNode.GetNodeAttributeValue("//table/tr/td[1]/table[1]/tr[@class='centerbox_orange']/td[@class='centerbox_orange_l']//img", "src");
            show.Airing      = !Regex.IsMatch(listing.DocumentNode.GetTextValue("//div[@class='orangecorner_content']//th[4]") ?? string.Empty, "(Canceled|Ended)");
            show.AirTime     = "20:00";
            show.Language    = language;
            show.URL         = "http://www.episodeworld.com/show/" + id + "/season=all/" + Languages.List[language].ToLower() + "/episodeguide";
            show.Episodes    = new List<Episode>();

            var runtxt   = Regex.Match(listing.DocumentNode.GetTextValue("//td[@class='centerbox_orange']/b[text() = 'Runtime:']/following-sibling::text()[1]") ?? string.Empty, "([0-9]+)");
            show.Runtime = runtxt.Success
                         ? int.Parse(runtxt.Groups[1].Value)
                         : 30;
            
            var genre = listing.DocumentNode.SelectNodes("//td[@class='centerbox_orange']/a[starts-with(@href, '/browse/')]");
            if (genre != null)
            {
                foreach (var gen in genre)
                {
                    show.Genre += gen.InnerText + ", ";
                }

                show.Genre = show.Genre.TrimEnd(", ".ToCharArray());
            }

            var network = listing.DocumentNode.GetTextValue("//td[@class='centerbox_orange']/b[text() = 'Premiered on Network:']/following-sibling::text()[1]");
            if (!string.IsNullOrWhiteSpace(network))
            {
                show.Network = Regex.Replace(network, @" in .+$", string.Empty);
            }

            var nodes = listing.DocumentNode.SelectNodes("//table[@id='list']/tr");
            if (nodes == null)
            {
                return show;
            }

            foreach (var node in nodes)
            {
                var episode = Regex.Match(node.GetTextValue("td[2]") ?? string.Empty, "([0-9]{1,2})x([0-9]{1,3})");
                if (!episode.Success)
                {
                    continue;
                }

                var ep = new Episode();

                ep.Season  = episode.Groups[1].Value.ToInteger();
                ep.Number  = episode.Groups[2].Value.ToInteger();
                ep.Title   = HtmlEntity.DeEntitize(node.GetTextValue("td[3]").Trim());
                ep.URL     = node.GetNodeAttributeValue("td[3]/a", "href");

                if (ep.URL != null)
                {
                    ep.URL = Site.TrimEnd('/') + ep.URL;
                }

                DateTime dt;
                ep.Airdate = DateTime.TryParse(HtmlEntity.DeEntitize(node.GetTextValue("td[7]") ?? string.Empty).Trim(), out dt)
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
