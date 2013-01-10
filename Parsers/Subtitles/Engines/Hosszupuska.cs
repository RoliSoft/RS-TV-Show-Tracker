namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

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
                return Utils.DateTimeToVersion("2011-12-10 4:42 PM");
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
            var id = GetIDForShow(pr[0]);

            if (!id.HasValue)
            {
                yield break;
            }

            var se = string.Empty;
            var ep = string.Empty;

            if (pr.Length > 1)
            {
                var sr = Regex.Match(pr[1], @"(?:[Ss](?<n>\d{1,2})|(?<n>\d{1,2})x\d{1,3})");
                var er = Regex.Match(pr[1], @"(?:[Ee](?<n>\d{1,2})|\d{1,2}x(?<n>\d{1,3}))");

                if (sr.Success)
                {
                    se = "s" + sr.Groups["n"].Value.PadLeft(2, '0');
                }

                if (er.Success)
                {
                    ep = "e" + er.Groups["n"].Value.PadLeft(2, '0');
                }
            }

            var html = Utils.GetHTML(Site + "kereso.php", "sorozatid=" + id.Value + "&nyelvtipus=%25&evad=" + se + "&resz=" + ep, encoding: Encoding.GetEncoding("iso-8859-2"));
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
                sub.InfoURL  = Site + "kereso.php?sorozatid=" + id.Value + "&nyelvtipus=%25&evad=" + se + "&resz=" + ep;
                sub.FileURL  = Site + node.SelectSingleNode("../../td[7]/a").GetAttributeValue("href", string.Empty);

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
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var page = Utils.GetHTML(Site);
            var opts = page.DocumentNode.SelectNodes("//select[@name='sorozatid']/option[position()!=1]");

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
            
            Database.SaveDict(@"misc\hosszupuska", ShowIDs);
        }

        /// <summary>
        /// Gets the ID for a show name.
        /// </summary>
        /// <param name="name">The show name.</param>
        /// <returns>Corresponding ID.</returns>
        public int? GetIDForShow(string name)
        {
            var fn = Path.Combine(Database.DataPath, @"misc\hosszupuska");

            if (ShowIDs == null)
            {
                if (File.Exists(fn))
                {
                    ShowIDs = Database.LoadDictIntStr(fn);
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
