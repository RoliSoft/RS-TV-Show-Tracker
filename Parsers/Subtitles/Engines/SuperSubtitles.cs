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
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://feliratok.ro.lt/favicon.ico";
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML("http://feliratok.ro.lt/index.php?search=" + Uri.EscapeUriString(ShowNames.Tools.ReplaceEpisode(query, "- {0:0}x{1:00}", true, false)));
            var subs = html.DocumentNode.SelectNodes("//tr[@id='vilagit']");

            if (subs == null)
            {
                yield break;
            }

            foreach (var node in subs)
            {
                yield return new Subtitle
                    {
                        Site     = Name,
                        Release  = node.GetTextValue("td[2]/a").Trim(),
                        Language = ParseLanguage(node.GetTextValue("td[4]").Trim()),
                        URL      = "http://feliratok.ro.lt/" + node.GetNodeAttributeValue("td[6]/a", "href")
                    };
            }
        }

        /// <summary>
        /// Parses the language of the subtitle.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>Strongly-typed language of the subtitle.</returns>
        public static Languages ParseLanguage(string language)
        {
            switch(language)
            {
                case "Magyar":
                    return Languages.Hungarian;

                case "Angol":
                    return Languages.English;

                default:
                    return Languages.Unknown;
            }
        }
    }
}
