namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using ProtoBuf;

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
                return Utils.DateTimeToVersion("2011-12-11 2:08 AM");
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

            var iurl = Site + "re_episode.php?ep=" + id + "-" + ep.Season + "x" + ep.Episode;
            var html = Utils.GetHTML(iurl, response: r => iurl = r.ResponseUri.ToString());
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
                sub.InfoURL     = iurl;
                sub.FileURL     = Site.TrimEnd('/') + node.GetAttributeValue("href");

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

        /// <summary>
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var page = Utils.GetHTML(Site);
            var opts = page.DocumentNode.SelectNodes("//select[@name='qsShow']/option[position()!=1]");

            ShowIDs = new Dictionary<int, string>();

            if (opts == null)
            {
                return;
            }

            foreach (var opt in opts)
            {
                int id;

                if (int.TryParse(opt.GetAttributeValue("value"), out id))
                {
                    ShowIDs.Add(id, HtmlEntity.DeEntitize(opt.NextSibling.InnerText));
                }
            }

            using (var file = File.Create(Path.Combine(Path.GetTempPath(), "Addic7ed-IDs.bin")))
            {
                Serializer.Serialize(file, ShowIDs);
            }
        }

        /// <summary>
        /// Gets the ID for a show name.
        /// </summary>
        /// <param name="name">The show name.</param>
        /// <returns>Corresponding ID.</returns>
        public int? GetIDForShow(string name)
        {
            var fn = Path.Combine(Path.GetTempPath(), "Addic7ed-IDs.bin");

            if (ShowIDs == null)
            {
                if (File.Exists(fn))
                {
                    using (var file = File.OpenRead(fn))
                    {
                        ShowIDs = Serializer.Deserialize<Dictionary<int, string>>(file);
                    }
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
                var m = regex.Match(show.Value);

                if (m.Success && Math.Abs(show.Value.Length - m.Length) <= 3)
                {
                    return show.Key;
                }
            }

            return null;
        }
    }
}
