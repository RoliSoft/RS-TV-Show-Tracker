namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;

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
                return Utils.DateTimeToVersion("2011-12-10 4:48 PM");
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var sfmt = Utils.EncodeURL(ShowNames.Parser.ReplaceEpisode(query, "- {0:0}x{1:00}", true, false));
            var html = Utils.GetHTML(Site + "index.php?search=" + sfmt);
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
                sub.InfoURL  = Site + "index.php?search=" + sfmt;
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
    }
}
