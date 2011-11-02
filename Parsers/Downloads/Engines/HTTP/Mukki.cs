namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping Mukki.org.
    /// </summary>
    [Parser("RoliSoft", "2011-10-01 6:51 AM"), TestFixture]
    public class Mukki : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Mukki.org";
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
                return "http://mukki.org/";
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
        /// Gets a number representing the number of links to visit for link and info extraction.
        /// </summary>
        /// <remarks>
        /// The default number is 5. It's configurable from Settings.json, but you shouldn't increase it, as it might get you an IP ban.
        /// </remarks>
        public static int ExtractLimit
        {
            get
            {
                return Settings.Get("Mukki.org Extract Limit", 5);
            }
        }

        /// <summary>
        /// Gets a number representing the number of milliseconds to wait before requesting the next link.
        /// </summary>
        /// <remarks>
        /// The default number is 500. It's configurable from Settings.json, but you shouldn't decrease it, as it might get you an IP ban.
        /// </remarks>
        public static int ExtractSleep
        {
            get
            {
                return Settings.Get("Mukki.org Extract Sleep", 500);
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "?s=" + Uri.EscapeUriString(query + " @category tv"));
            var links = html.DocumentNode.SelectNodes("//h2/a");

            if (links == null)
            {
                yield break;
            }

            var i = 0;
            foreach (var node in links)
            {
                var release = HtmlEntity.DeEntitize(node.InnerText);
                var quality = FileNames.Parser.ParseQuality(release);
                var infourl = node.GetAttributeValue("href");

                if (i == ExtractLimit)
                {
                    var link = new Link(this);

                    link.Release = release;
                    link.InfoURL = infourl;
                    link.Quality = quality;

                    yield return link;
                    continue;
                }

                i++;
                Thread.Sleep(ExtractSleep);

                var html2   = Utils.GetHTML(infourl);
                var sizergx = Regex.Match((html2.DocumentNode.GetTextValue("//table[2]//tr/td[position() = 1 and contains(text(), 'Size')]/following-sibling::td") ?? string.Empty), @"(\d+(?:\.\d+)?)\s*([KMG]B)", RegexOptions.IgnoreCase);
                var size    = string.Empty;

                if (sizergx.Success)
                {
                    size = sizergx.Groups[1].Value + " " + sizergx.Groups[2].Value.ToUpper();
                }

                var sites = html2.DocumentNode.SelectNodes("//span[@class='links']");

                if (sites == null)
                {
                    continue;
                }

                foreach (var site in sites)
                {
                    var parts = site.SelectNodes("a");

                    if (parts == null)
                    {
                        continue;
                    }
                    
                    var link = new Link(this);

                    link.Release = release;
                    link.InfoURL = infourl;
                    link.Size    = size;
                    link.Infos   = Regex.Match(parts[0].GetAttributeValue("href"), @"https?://(?:www\.)?([^\./]+)").Groups[1].Value.ToUppercaseFirst();
                    link.Quality = quality;

                    foreach (var part in parts)
                    {
                        if (link.FileURL != null)
                        {
                            link.FileURL += "\0";
                        }

                        link.FileURL += part.GetAttributeValue("href");
                    }

                    yield return link;
                }
            }
        }
    }
}
