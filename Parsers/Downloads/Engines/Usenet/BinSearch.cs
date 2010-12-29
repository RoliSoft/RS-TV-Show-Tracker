namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent;

    /// <summary>
    /// Provides support for scraping BinSearch.
    /// </summary>
    [Parser("RoliSoft", "2010-12-09 4:21 AM")]
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
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML("http://www.binsearch.info/index.php?q=" + Uri.EscapeUriString(query) + "&m=&max=25&adv_g=&adv_age=999&adv_sort=date&adv_col=on&minsize=200&maxsize=&font=&postdate=");
            var links = html.DocumentNode.SelectNodes("//td/span[@class='s']");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                yield return new Link
                    {
                        Site         = Name,
                        Release      = HtmlEntity.DeEntitize(node.InnerText),
                        URL          = "http://www.binsearch.info" + HtmlEntity.DeEntitize(node.SelectSingleNode("../span[@class='d']/a").GetAttributeValue("href", string.Empty)),
                        Size         = Regex.Match(HtmlEntity.DeEntitize(node.SelectSingleNode("../span[@class='d']").InnerText), @"size: ([^,<]+)").Groups[1].Value,
                        Quality      = ThePirateBay.ParseQuality(HtmlEntity.DeEntitize(node.InnerText).Replace(' ', '.')),
                        Type         = Types.Usenet,
                        IsLinkDirect = false
                    };
            }
        }
    }
}
