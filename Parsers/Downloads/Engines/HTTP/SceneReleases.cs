namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping SceneReleases.
    /// </summary>
    [Parser("2011-09-22 11:51 PM"), TestFixture]
    public class SceneReleases : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "SceneReleases";
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
                return "http://sceper.eu/";
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
                return Types.DirectHTTP;
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
            var html  = Utils.GetHTML(Site + "category/tv-shows?s=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//h2/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var info   = node.GetAttributeValue("href");
                var size   = (node.GetTextValue("../..//span[text() = 'Size:']/following-sibling::text()") ?? string.Empty).Trim();
                var titles = HtmlEntity.DeEntitize(node.InnerText).Split(new[] { " & " }, StringSplitOptions.RemoveEmptyEntries);
                var groups = node.SelectNodes("../..//div[@class='meta' and contains(text(), 'Download Links')]/following-sibling::p");

                if (groups == null)
                {
                    // try the markup for older posts
                    groups = node.SelectNodes("../..//div[@class='ddet_div']/p/span[contains(@style, '#99cc00')]");
                }

                if (groups == null)
                {
                    yield break;
                }

                var i = 0;
                foreach (var group in groups)
                {
                    var title = (i < titles.Length ? titles[i] : titles.Last()).Trim();
                    var files = group.SelectNodes(".//a");

                    foreach (var file in files)
                    {
                        var link = new Link(this);

                        link.Release = title;
                        link.InfoURL = info;
                        link.FileURL = file.GetAttributeValue("href");
                        link.Size    = size;
                        link.Infos   = file.InnerText.ToLower().ToUppercaseFirst();
                        link.Quality = Regex.IsMatch(group.InnerText, "xvid", RegexOptions.IgnoreCase)
                                       ? Qualities.HDTVXviD
                                       : FileNames.Parser.ParseQuality(title);

                        yield return link;
                    }

                    i++;
                }
            }
        }
    }
}
