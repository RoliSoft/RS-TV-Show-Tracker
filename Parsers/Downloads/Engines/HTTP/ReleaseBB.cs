namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping ReleaseBB.
    /// </summary>
    [Parser("2011-10-01 6:33 AM"), TestFixture]
    public class ReleaseBB : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Release BB";
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
                return "http://rlsbb.com/";
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
            var html  = Utils.GetHTML(Site + "category/tv-shows/?s=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//div[@class='postContent']");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var infourl  = node.GetNodeAttributeValue("..//a[1]", "href");
                var releases = node.SelectNodes("p[contains(@style, 'center') or @align='center']/strong/..");

                if (releases == null)
                {
                    continue;
                }

                foreach (var relnode in releases)
                {
                    var release = relnode.GetTextValue("strong").Trim();
                    var quality = FileNames.Parser.ParseQuality(release);
                    var sizergx = Regex.Match(relnode.InnerText, @"(\d+(?:\.\d+)?)\s*([KMG]B)", RegexOptions.IgnoreCase);
                    var size    = string.Empty;

                    if (sizergx.Success)
                    {
                        size = sizergx.Groups[1].Value + " " + sizergx.Groups[2].Value.ToUpper();
                    }

                    var sites = relnode.SelectNodes("a");

                    foreach (var site in sites)
                    {
                        if (Regex.IsMatch(site.InnerText, @"(NFO|Torrent Search)"))
                        {
                            continue;
                        }

                        var link = new Link(this);

                        link.Release = release;
                        link.InfoURL = infourl;
                        link.FileURL = site.GetAttributeValue("href");
                        link.Size    = size;
                        link.Infos   = site.InnerText.ToLower().ToUppercaseFirst();
                        link.Quality = quality;

                        yield return link;
                    }
                }
            }
        }
    }
}
