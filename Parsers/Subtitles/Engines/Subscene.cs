namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping Subscene.
    /// </summary>
    [TestFixture]
    public class Subscene : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Subscene";
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
                return "http://subscene.com/";
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
                return "http://subscene.com/favicon.png";
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
                return Utils.DateTimeToVersion("2011-12-02 2:50 AM");
            }
        }

        /// <summary>
        /// Returns an <c>IDownloader</c> object which can be used to download the URLs provided by this parser.
        /// </summary>
        /// <value>The downloader.</value>
        public override IDownloader Downloader
        {
            get
            {
                return new SubsceneDownloader();
            }
        }

        /// <summary>
        /// A regular expression to match the HI notation tag in the release names.
        /// </summary>
        public static Regex HINotationRegex = new Regex(@"[\.\-_]HI\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// A regular expression to match the corrected tag in the release names.
        /// </summary>
        public static Regex CorrectedRegex = new Regex(@"\s+\((?:sync,?)?\s*correc(?:ted\s*(?:by [^\)]+)?|\.{3})\)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML(Site + "s.aspx?q=" + Uri.EscapeUriString(ShowNames.Parser.CleanTitleWithEp(query)));
            var subs = html.DocumentNode.SelectNodes("//a[@class='a1']");
            
            if (subs == null)
            {
                yield break;
            }

            foreach (var node in subs)
            {
                if (!ShowNames.Parser.IsMatch(query, node.GetTextValue("span[2]").Trim()))
                {
                    continue;
                }

                var sub = new Subtitle(this);

                sub.Release     = node.GetTextValue("span[2]").Trim();
                sub.HINotations = HINotationRegex.IsMatch(sub.Release);
                sub.Corrected   = CorrectedRegex.IsMatch(sub.Release);
                sub.Language    = Languages.Parse(node.GetTextValue("span[1]").Trim());
                sub.InfoURL     = Site.TrimEnd('/') + node.GetAttributeValue("href");

                if (sub.HINotations)
                {
                    sub.Release = HINotationRegex.Replace(sub.Release, string.Empty);
                }

                if (sub.Corrected)
                {
                    sub.Release = CorrectedRegex.Replace(sub.Release, string.Empty);
                }

                yield return sub;
            }
        }
    }
}
