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
    /// Provides support for scraping DirectDownload.tv.
    /// </summary>
    [Parser("2011-03-26 4:56 PM"), TestFixture]
    public class DirectDownload : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "DirectDownload.tv";
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
                return "http://directdownload.tv/";
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
                return "http://directdownload.tv/favicon.png";
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
            var html  = Utils.GetHTML(Site + "ajaxSearch.php?keyword=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//dl");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var release = HtmlEntity.DeEntitize(node.GetTextValue("dd[@class='title']/strong")).Trim();
                var quality = FileNames.Parser.ParseQuality(release);
                var size    = Regex.Match(node.GetTextValue("dd[@class='title']"), @"(\d+\.\d+ MB)").Groups[1].Value;
                var sites   = node.SelectNodes("dd[@class='links']/a");

                for (var i = 0; i < sites.Count; i++)
                {
                    var link = new Link(this);

                    link.Release = release;
                    link.Quality = quality;
                    link.Size    = size;
                    link.FileURL = sites[i].GetAttributeValue("href");
                    link.Infos   = Regex.Replace(sites[i].GetNodeAttributeValue("img", "title"), @"Download (?:file \d+ )?on ", string.Empty).ToLower().ToUppercaseFirst();

                    var first = new Uri(sites[i].GetAttributeValue("href"));

                tryNext:
                    if (i + 1 < sites.Count && first.Host == new Uri(sites[i + 1].GetAttributeValue("href")).Host)
                    {
                        i++;
                        link.FileURL += "\0" + sites[i].GetAttributeValue("href");
                        goto tryNext;
                    }

                    yield return link;
                }
            }
        }
    }
}
