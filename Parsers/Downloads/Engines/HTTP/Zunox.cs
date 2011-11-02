namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping Zunox.
    /// </summary>
    [Parser("RoliSoft", "2011-03-26 3:51 PM"), TestFixture]
    public class Zunox : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Zunox";
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
                return "http://zunox.co/";
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
                return "http://media.zunox.co/favicon.png";
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
            var html  = Utils.GetHTML(Site, "q=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//div[@class='dllist']/dl");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                var site = node.GetTextValue("dd[@class='pro']/a");
                var star = node.GetTextValue("dd[@class='pro']/img/preceding-sibling::text()");
                var list = node.SelectNodes("dd[@class='fh']/abbr");

                link.Release = HtmlEntity.DeEntitize(node.GetTextValue("dt/a")).Trim()
                             + (!string.IsNullOrWhiteSpace(site) ? " @ " + site : string.Empty)
                             + (!string.IsNullOrWhiteSpace(star) && Regex.IsMatch(star, @"\s*\d") ? " " + star.Trim() + "✩" : string.Empty);
                link.InfoURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("dt/a", "href");
                link.Quality = Regex.IsMatch(link.Release, @"(720p|x264)", RegexOptions.IgnoreCase)
                             ? Qualities.HDTV720p
                             : Qualities.HDTVXviD;

                if (list != null)
                {
                    foreach (var fs in list)
                    {
                        link.Infos += fs.GetAttributeValue("title") + ", ";
                    }

                    link.Infos = link.Infos.TrimEnd(", ".ToCharArray());
                }

                yield return link;
            }
        }
    }
}
