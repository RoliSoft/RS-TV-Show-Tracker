namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Addic7ed.
    /// </summary>
    [TestFixture]
    public class Addic7ed : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Addic7ed";
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
                return "http://www.addic7ed.com/favicon.ico";
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var show = ShowNames.Split(query);
            var dbid = Database.GetShowID(show[0]);
            var adid = string.Empty;

            if (!string.IsNullOrWhiteSpace(dbid))
            {
                adid = Database.ShowData(dbid, "Addic7ed.ID");
            }

            if (string.IsNullOrWhiteSpace(adid))
            {
                adid = Regex.Match(Utils.Bing(show[0] + " site:addic7ed.com/serie/"), @"/serie/([^/$]+)").Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(dbid))
                {
                    Database.ShowData(dbid, "Addic7ed.ID", adid);
                }
            }

            var html = Utils.GetHTML("http://www.addic7ed.com/serie/" + adid + "/" + ShowNames.ExtractEpisode(query, 1).Replace('x', '/') + "/episode");
            var subs = html.DocumentNode.SelectNodes("//a[starts-with(@href,'/original/')] | //a[starts-with(@href,'/updated/')]");

            if (subs == null)
            {
                yield break;
            }

            var head = Regex.Split(html.DocumentNode.SelectSingleNode("//div[@id='container']//tr[1]/td[1]/div/span").InnerText.Trim(), @" \- ");

            foreach (var node in subs)
            {
                yield return new Subtitle
                    {
                        Site     = Name,
                        Release  = head[0] + " " + head[1] + " - "
                                   + node.SelectSingleNode("../../../tr/td[contains(text(),'Version')]").InnerText.Trim().Replace("Version ", string.Empty).Split(", ".ToCharArray())[0]
                                   + (node.SelectSingleNode("../../../tr/td/img[contains(@src,'hdicon')]") != null ? "/HD" : string.Empty)
                                   + (node.SelectSingleNode("../../../tr/td/img[contains(@src,'bullet_go')]") != null ? " - corrected" : string.Empty)
                                   + (node.SelectSingleNode("../../../tr/td/img[contains(@src,'hi.jpg')]") != null ? " - HI" : string.Empty)
                                   + (node.InnerText != "Download" ? " - " + node.InnerText : string.Empty),
                        Language = ParseLanguage(node.SelectSingleNode("../../td[3]").InnerText.Replace("&nbsp;", string.Empty).Trim()),
                        URL      = "http://www.addic7ed.com" + node.GetAttributeValue("href", string.Empty)
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
            language = Regex.Replace(language, @"\s?\(.+\)", string.Empty);
            var detected = Languages.Unknown;
            Enum.TryParse(language, out detected);
            return detected;
        }
    }
}
