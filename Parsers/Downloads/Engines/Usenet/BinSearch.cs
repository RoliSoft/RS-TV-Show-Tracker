namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping BinSearch.
    /// </summary>
    [TestFixture]
    public class BinSearch : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "BinSearch";
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
                return "http://binsearch.info/";
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
                return Utils.DateTimeToVersion("2011-01-29 9:48 PM");
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
                return Types.Usenet;
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
                return new BinSearchDownloader();
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "index.php?q=" + Uri.EscapeUriString(query) + "&max=50&adv_age=999&adv_sort=date&adv_col=on&minsize=100");
            var links = html.DocumentNode.SelectNodes("//td/span[@class='s']");
            var ages  = Regex.Matches(html.DocumentNode.InnerHtml, @"<td>(\d+(?:\.\d+)?)\s*([mhdwy])<"); // HtmlAgilityPack won't parse it due to broken markup

            if (links == null)
            {
                yield break;
            }

            var i = 0;
            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText);
                link.InfoURL = Site.TrimEnd('/') + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("../span[@class='d']/a", "href"));
                link.FileURL = "http://www.binsearch.info/fcgi/nzb.fcgi?q=" + Uri.EscapeUriString(query) + "&m=&max=50&adv_g=&adv_age=999&adv_sort=date&adv_col=on&minsize=100;" + node.GetNodeAttributeValue("../..//input", "name") + "=on&action=nzb;" + Utils.SanitizeFileName(link.Release.CutIfLonger(200)).Replace('/', '-') + ".nzb";
                link.Size    = Regex.Match(HtmlEntity.DeEntitize(node.GetTextValue("../span[@class='d']")), @"size: ([^,<]+)").Groups[1].Value;
                link.Quality = FileNames.Parser.ParseQuality(link.Release.Replace(' ', '.'));
                link.Infos   = Utils.ParseAge(ages[i++].Value);
                
                yield return link;
            }
        }
    }
}
