namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent;

    /// <summary>
    /// Provides support for scraping BinSearch.
    /// </summary>
    [Parser("RoliSoft", "2011-01-29 9:48 PM"), TestFixture]
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
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://www.binsearch.info/favicon.ico";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public override bool Private
        {
            get
            {
                return false;
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
                return new ExternalDownloader();
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "index.php?q=" + Uri.EscapeUriString(query) + "&m=&max=25&adv_g=&adv_age=999&adv_sort=date&adv_col=on&minsize=200&maxsize=&font=&postdate=");
            var links = html.DocumentNode.SelectNodes("//td/span[@class='s']");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText);
                link.InfoURL = Site.TrimEnd('/') + HtmlEntity.DeEntitize(node.GetNodeAttributeValue("../span[@class='d']/a", "href"));
                link.Size    = Regex.Match(HtmlEntity.DeEntitize(node.GetTextValue("../span[@class='d']")), @"size: ([^,<]+)").Groups[1].Value;
                link.Quality = ThePirateBay.ParseQuality(link.Release.Replace(' ', '.'));

                yield return link;
            }
        }
    }
}
