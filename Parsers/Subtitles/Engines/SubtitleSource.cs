namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping SubtitleSource.
    /// </summary>
    [TestFixture]
    public class SubtitleSource : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "SubtitleSource";
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
                return "http://www.subtitlesource.com/";
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
                return "http://kat.ph/favicon.ico";
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
                return Utils.DateTimeToVersion("2013-01-05 7:00 PM");
            }
        }

        /// <summary>
        /// Gets or sets the show IDs on the site.
        /// </summary>
        /// <value>The show IDs.</value>
        public static Dictionary<string, string> ShowIDs { get; set; }

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

            if (id == null || ep == null)
            {
                yield break;
            }

            var html = Utils.GetHTML(Site + id + "-season-" + ep.Season + "-episode-" + ep.Episode + "-subtitles.html");
            var subs = html.DocumentNode.SelectNodes("//table[@class='mainTable']/tbody/tr/td[@class='noColor']/a");

            if (subs == null)
            {
                yield break;
            }

            var head1 = Regex.Match(html.DocumentNode.SelectSingleNode("//div[@id='contentBox']/h1").InnerText.Trim(), @"\s*(.*?)\sSeason\s\d{1,3}[^\-]+\-\s(.{6})");
            var head = head1.Groups[1].Value + " " + head1.Groups[2].Value;

            foreach (var node in subs)
            {
                var sub = new Subtitle(this);

                var fid = Regex.Match(node.GetAttributeValue("href"), @"\-(\d+)\.html").Groups[1].Value;
                var rip = node.GetTextValue("../../td[4]");
                var rel = node.GetTextValue("../../td[5]");
                var aut = node.GetTextValue("../../td[6]");

                sub.Release     = head
                                + (!string.IsNullOrWhiteSpace(rel) ? " - " + rel.Trim() : !string.IsNullOrWhiteSpace(rip) ? " - " + rip.Trim() : string.Empty)
                                + (!string.IsNullOrWhiteSpace(aut) ? " - " + aut.Trim() : string.Empty);
                sub.Language    = Languages.Parse(node.InnerText.Trim());
                sub.InfoURL     = Site.TrimEnd('/') + node.GetAttributeValue("href");
                sub.FileURL     = Site + "files/subtitles/" + fid + "/subtitle.zip";

                yield return sub;
            }
        }

        /// <summary>
        /// Tests the parser by searching for "House" on the site.
        /// </summary>
        public override void TestSearchShow()
        {
            Assert.Inconclusive("SubtitleSource only supports searching for specific episodes.");
        }

        /// <summary>
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var page = Utils.GetHTML(Site + "tv-shows.html");
            var opts = page.DocumentNode.SelectNodes("//table[@class='mainTable']/tbody/tr/td[@class='noColor']/a");

            ShowIDs = new Dictionary<string, string>();

            if (opts == null)
            {
                return;
            }

            foreach (var opt in opts)
            {
                var idx = Regex.Match(opt.GetAttributeValue("href") ?? string.Empty, @"/(.*?)\-subtitles\.html");

                if (idx.Success && !string.IsNullOrWhiteSpace(idx.Groups[1].Value))
                {
                    ShowIDs.Add(idx.Groups[1].Value, HtmlEntity.DeEntitize(opt.InnerText.Trim()));
                }
            }

            Database.SaveDict(@"misc\subtitlesource", ShowIDs);
        }

        /// <summary>
        /// Gets the ID for a show name.
        /// </summary>
        /// <param name="name">The show name.</param>
        /// <returns>Corresponding ID.</returns>
        public string GetIDForShow(string name)
        {
            var fn = Path.Combine(Database.DataPath, @"misc\subtitlesource");

            if (ShowIDs == null)
            {
                if (File.Exists(fn))
                {
                    ShowIDs = Database.LoadDictStrStr(fn);
                }
                else
                {
                    GetIDs();
                }
            }

            if (ShowIDs != null)
            {
                var id = SearchForID(name);
                if (id != null)
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
                    if (id != null)
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
        private string SearchForID(string name)
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
