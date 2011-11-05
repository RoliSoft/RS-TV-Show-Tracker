namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;

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
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://www.addic7ed.com/";
            }
        }
        
        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var show = ShowNames.Parser.Split(query);
            var dbid = Database.GetShowID(show[0]);
            var adid = string.Empty;

            if (dbid != int.MinValue)
            {
                adid = Database.ShowData(dbid, "Addic7ed.ID");
            }

            if (string.IsNullOrWhiteSpace(adid))
            {
                var url = new Google().Search(show[0] + " site:addic7ed.com/serie/").ToList();
                if (url.Count == 0)
                {
                    yield break;
                }

                adid = Regex.Match(url[0].URL, @"/serie/([^/$]+)").Groups[1].Value;

                if (dbid != int.MinValue)
                {
                    Database.ShowData(dbid, "Addic7ed.ID", adid);
                }
            }

            var html = Utils.GetHTML(Site + "serie/" + adid + "/" + ShowNames.Parser.ExtractEpisode(query, "{0:0}/{1:00}") + "/episode");
            var subs = html.DocumentNode.SelectNodes("//a[starts-with(@href,'/original/')] | //a[starts-with(@href,'/updated/')]");

            if (subs == null)
            {
                yield break;
            }

            var head = Regex.Split(html.DocumentNode.SelectSingleNode("//div[@id='container']//tr[1]/td[1]/div/span").InnerText.Trim(), @" \- ");

            foreach (var node in subs)
            {
                if (Regex.IsMatch(node.GetTextValue("../.."), @"\d{1,3}(?:\.\d{1,2})?% Completed"))
                {
                    continue;
                }

                var sub = new Subtitle(this);

                sub.Corrected   = node.SelectSingleNode("../../../tr/td/img[contains(@src,'bullet_go')]") != null;
                sub.HINotations = node.SelectSingleNode("../../../tr/td/img[contains(@src,'hi.jpg')]") != null;
                sub.Release     = head[0] + " " + head[1] + " - "
                                + node.GetTextValue("../../../tr/td[contains(text(),'Version')]").Trim().Replace("Version ", string.Empty).Split(", ".ToCharArray())[0]
                                + (node.SelectSingleNode("../../../tr/td/img[contains(@src,'hdicon')]") != null ? "/HD" : string.Empty)
                                + (node.InnerText != "Download" ? " - " + node.InnerText : string.Empty);
                sub.Language    = Languages.Parse(node.GetTextValue("../../td[3]").Replace("&nbsp;", string.Empty).Trim());
                sub.URL         = Site.TrimEnd('/') + node.GetAttributeValue("href");

                yield return sub;
            }
        }

        /// <summary>
        /// Tests the parser by searching for "House" on the site.
        /// </summary>
        public override void TestSearchShow()
        {
            Assert.Inconclusive("Addic7ed only supports searching for specific episodes.");
        }
    }
}
