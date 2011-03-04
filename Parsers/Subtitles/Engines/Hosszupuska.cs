namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Hosszupuska Sub.
    /// </summary>
    /// <remarks>
    /// WARNING!
    /// Hosszupuskasub.com currently is not accessable from outside Hungary.
    /// It either resolves to 127.0.0.1 or shows a "Hamarosan..." page with HTTP 403 status code.
    /// If I can't access the page anymore, I can't maintain the plugin either, which means,
    /// if they continue to fuck off everyone outside Hungary, the plugin will be permanently removed.
    /// </remarks>
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
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML(Site + "sorozatok.php", "cim=" + Uri.EscapeUriString(ShowNames.Parser.Normalize(query)), encoding: Encoding.GetEncoding("iso-8859-2"));
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

        /// <summary>
        /// Tests the parser by searching for "House S07E01" on the site.
        /// </summary>
        [Test]
        public override void TestSearch()
        {
            if (Dns.GetHostAddresses("hosszupuskasub.com")[0].Equals(IPAddress.Parse("127.0.0.1")))
            {
                Assert.Inconclusive("Your DNS resolver resolves hosszupuskasub.com to 127.0.0.1.");
            }

            try
            {
                Utils.GetHTML(Site);
            }
            catch (WebException ex)
            {
                Assert.Inconclusive("Hosszupuskasub.com seems to work only from within Hungary. Again.");
            }

            base.TestSearch();
        }
    }
}
