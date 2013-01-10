namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping feliratok.hu and/or its mirrors.
    /// </summary>
    [TestFixture]
    public class SuperSubtitles : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "SuperSubtitles";
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
                return "http://www.feliratok.info/";
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
                return Utils.DateTimeToVersion("2012-12-31 12:13 PM");
            }
        }

        /// <summary>
        /// Gets or sets the show IDs on the site.
        /// </summary>
        /// <value>The show IDs.</value>
        public static Dictionary<int, string> ShowIDs { get; set; }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var pr = ShowNames.Parser.Split(query);
            var ep = ShowNames.Parser.ExtractEpisode(query);
            var id = GetIDForShow(pr[0]);

            if (!id.HasValue && ep != null)
            {
                yield break;
            }

            var html = Utils.GetHTML(Site + "/index.php?sid=" + id + "&complexsearch=true&evad=" + ep.Season + "&epizod1=" + ep.Episode + "&tab=sorozat");
            var subs = html.DocumentNode.SelectNodes("//tr[@id='vilagit']");

            if (subs == null)
            {
                yield break;
            }

            foreach (var node in subs)
            {
                var sub = new Subtitle(this);

                sub.Release  = node.GetTextValue("td[3]/div[2]").Trim();
                sub.Language = ParseLanguage(node.GetTextValue("td[@class='lang']").Trim());
                sub.InfoURL = Site + "/index.php?sid=" + id + "&complexsearch=true&evad=" + ep.Season + "&epizod1=" + ep.Episode + "&tab=sorozat";
                sub.FileURL  = Site.TrimEnd('/') + node.GetNodeAttributeValue("td[6]/a", "href");

                yield return sub;
            }
        }

        /// <summary>
        /// Extracts the language from the string and returns its ISO 3166-1 alpha-2 code.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>ISO 3166-1 alpha-2 code of the language.</returns>
        public static string ParseLanguage(string language)
        {
            switch (language)
            {
                case "Magyar":
                    return "hu";

                case "Angol":
                    return "en";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var autocmp = Utils.GetJSON(Site + "index.php?term=&nyelv=0&action=autoname");

            ShowIDs = new Dictionary<int, string>();

            foreach (var item in autocmp)
            {
                ShowIDs[(int)item["ID"]] = (string)item["name"];
            }

            Database.SaveDict(@"misc\supersubtitles", ShowIDs);
        }

        /// <summary>
        /// Gets the ID for a show name.
        /// </summary>
        /// <param name="name">The show name.</param>
        /// <returns>Corresponding ID.</returns>
        public int? GetIDForShow(string name)
        {
            var fn = Path.Combine(Database.DataPath, @"misc\supersubtitles");

            if (ShowIDs == null)
            {
                if (File.Exists(fn))
                {
                    ShowIDs = Database.LoadDictIntStr(fn);
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
            var regex = Database.GetReleaseName(name);

            foreach (var show in ShowIDs)
            {
                var m = regex.Match(Regex.Replace(show.Value, @"\s\((1[89]\d{2}|2\d{3})\)$", string.Empty));

                if (m.Success && Math.Abs((show.Value.Length - 4) - m.Length) <= 3)
                {
                    return show.Key;
                }
            }

            return null;
        }
    }
}
