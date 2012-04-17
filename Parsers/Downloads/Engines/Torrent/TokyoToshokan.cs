namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Tokyo Toshokan.
    /// </summary>
    [TestFixture]
    public class TokyoToshokan : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Tokyo Toshokan";
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
                return "http://tokyotosho.info/";
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
                return Utils.DateTimeToVersion("2012-04-17 7:31 PM");
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "search.php?type=1&terms=" + Uri.EscapeUriString(ShowNames.Parser.ReplaceEpisode(query, "{1}", false, false)));
            var links = html.DocumentNode.SelectNodes("//table/tr/td[@class='desc-top']/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText);

                if (string.IsNullOrWhiteSpace(link.Release)) continue;

                link.InfoURL = Site + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("../../td[@class='web']/a[last()]", "href"));
                link.FileURL = HtmlEntity.DeEntitize(node.GetAttributeValue("href"));
                link.Quality = FileNames.Parser.ParseQuality(link.Release);

                var info = HtmlEntity.DeEntitize(node.GetTextValue("../../following-sibling::tr[1]"));
                var size = Regex.Match(info, @"Size: (\d+(?:\.\d+)?)([KMG]B)");
                
                if (size.Success)
                {
                    link.Size = size.Groups[1].Value + " " + size.Groups[2].Value;
                }

                link.Infos = Link.SeedLeechFormat.FormatWith(Regex.Match(info, @"S: (\d+) ").Groups[1].Value, Regex.Match(info, @"L: (\d+) ").Groups[1].Value);

                yield return link;
            }
        }
    }
}
