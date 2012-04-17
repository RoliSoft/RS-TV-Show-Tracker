namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Legendas.TV.
    /// </summary>
    [TestFixture]
    public class LegendasTV : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Legendas.TV";
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
                return "http://legendas.tv/";
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
                return Utils.DateTimeToVersion("2012-04-17 5:59 AM");
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML(Site + "index.php?opcao=buscarlegenda", "selTipo=1&int_idioma=99&btn_buscar.x=25&btn_buscar.y=6&txtLegenda=" + Utils.EncodeURL(ShowNames.Parser.CleanTitleWithEp(query)));
            var subs = html.DocumentNode.SelectNodes("//span[@class='brls']");

            if (subs == null)
            {
                yield break;
            }

            foreach (var node in subs)
            {
                var sub = new Subtitle(this);
                var id  = Regex.Match(node.GetNodeAttributeValue("../../..", "onclick") ?? string.Empty, @"[a-f0-9]{32}");
                var lng = Regex.Match(node.GetNodeAttributeValue("../../..//img[contains(@src, 'flag')]", "src") ?? string.Empty, @"flag_([a-z]{2})\.");

                if (!id.Success) continue;

                sub.Release  = node.InnerText.Trim();
                sub.Language = lng.Success ? lng.Groups[1].Value == "us" ? "en" : lng.Groups[1].Value : string.Empty;
                sub.InfoURL  = Site + "info.php?d=" + id.Value;
                sub.FileURL  = Site + "info.php?c=1&d=" + id.Value;

                yield return sub;
            }
        }
    }
}
