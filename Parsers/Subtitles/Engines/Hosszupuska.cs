namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Hosszupuska Sub.
    /// </summary>
    [TestFixture]
    public class Hosszupuska : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Hosszupuska";
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
                return "http://hosszupuskasub.com/";
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
                return Utils.DateTimeToVersion("2010-12-22 6:39 PM");
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML(Site + "kereso.php", "cim=" + Uri.EscapeUriString(ShowNames.Parser.Normalize(query)), encoding: Encoding.GetEncoding("iso-8859-2"));
            var subs = html.DocumentNode.SelectNodes("//td/a[starts-with(@href,'download.php?file=')]");

            if (subs == null)
            {
                yield break;
            }

            foreach (var node in subs)
            {
                var sub = new Subtitle(this);

                sub.Release  = Regex.Replace(node.SelectSingleNode("../../td[2]").InnerHtml, @".*?<br>", string.Empty);
                sub.Language = ParseLanguage(node.SelectSingleNode("../../td[3]/img").GetAttributeValue("src", string.Empty));
                sub.URL      = Site + node.SelectSingleNode("../../td[7]/a").GetAttributeValue("href", string.Empty);

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
                case "flags/1.gif":
                    return "hu";

                case "flags/2.gif":
                    return "en";

                default:
                    return string.Empty;
            }
        }
    }
}
