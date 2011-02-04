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
                /*
                 * The official site would be feliratok.hu, however that no longer works.
                 * There are multiple mirrors which continue the work of the main site,
                 * and they look like they share their data with each other.
                 * 
                 * These mirrors are:
                 * - http://feliratok.ro.lt 
                 * - http://feliratok.hs.vc
                 * - http://feliratok.na.tl 
                 * 
                 * Source: http://freeforum.n4.hu/feliratok/index.php?topic=40.0 (2011-02-04)
                 */
                return "http://feliratok.ro.lt/";
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML(Site + "index.php?search=" + Uri.EscapeUriString(ShowNames.Tools.ReplaceEpisode(query, "- {0:0}x{1:00}", true, false)));
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
                        URL      = Site + node.GetNodeAttributeValue("td[6]/a", "href")
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
