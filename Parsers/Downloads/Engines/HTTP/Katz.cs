namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent;

    /// <summary>
    /// Provides support for scraping Katz Downloads.
    /// </summary>
    [Parser("RoliSoft", "2011-02-17 1:06 PM"), TestFixture]
    public class Katz : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Katz";
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
                return "http://katz.cd/";
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
                return "http://katz.cd/favicon.ico";
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
                return Types.HTTP;
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
            var html  = Utils.GetHTML(Site + "search?type=tv&q=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//div[@id='list']//dl");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                var site = node.GetTextValue("dd[@class='si']/a");
                var star = node.GetTextValue("dd[@class='si']/span");
                var list = node.SelectNodes("dd[@class='fh']/abbr");

                link.Release = HtmlEntity.DeEntitize(node.GetTextValue("dt/a/text()")).Trim()
                             + (!string.IsNullOrWhiteSpace(site) ? " @ " + site : string.Empty)
                             + (!string.IsNullOrWhiteSpace(star) ? " " + star + "✩" : string.Empty);
                link.InfoURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("dt/a", "href");
                link.Quality = (node.GetTextValue("dt/a/span") ?? string.Empty).Contains("MKV")
                               ? Qualities.HDTV720p
                               : Qualities.HDTVXviD;

                if (list != null)
                {
                    foreach (var fs in list)
                    {
                        link.Infos += fs.GetTextValue("span") + " " + fs.GetAttributeValue("title") + ", ";
                    }

                    link.Infos = link.Infos.TrimEnd(", ".ToCharArray());
                }

                yield return link;
            }
        }
    }
}
