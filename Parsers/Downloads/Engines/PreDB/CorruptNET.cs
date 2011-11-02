namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.PreDB
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping CorruptNET PreDB.
    /// </summary>
    [Parser("RoliSoft", "2011-09-24 12:58 PM"), TestFixture]
    public class CorruptNET : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "CorruptNET";
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
                return "http://pre.corrupt-net.org/";
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
                return Types.PreDB;
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
            var html = Utils.GetHTML(Site + "search.php?search=" + Uri.EscapeUriString(query),
                request: req =>
                    {
                        req.Accept = "*/*";
                        req.Headers[HttpRequestHeader.AcceptLanguage] = "en";
                        req.AutomaticDecompression = DecompressionMethods.None;
                    });

            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText).Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Size    = HtmlEntity.DeEntitize(node.GetTextValue("../td[4]")).Trim().Replace("M", " MB");
                link.Infos   = HtmlEntity.DeEntitize(node.GetTextValue("../td[5]")).Trim();

                var tdt = node.GetAttributeValue("title");

                if (tdt.Contains("Nuked"))
                {
                    var rgx = Regex.Match(HtmlEntity.DeEntitize(tdt), "<font color='red'>([^<]+)");

                    if (rgx.Success)
                    {
                        link.Infos += ", Nuked: " + rgx.Groups[1].Value;
                    }
                }

                yield return link;
            }
        }
    }
}
