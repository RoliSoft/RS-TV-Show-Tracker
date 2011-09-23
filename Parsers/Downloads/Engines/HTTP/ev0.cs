namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping ev0.in.
    /// </summary>
    [Parser("2011-09-23 1:57 AM"), TestFixture]
    public class ev0 : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "ev0.in";
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
                return "http://ev0.in/";
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
            var html  = Utils.GetHTML(Site, "name=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//div[@class='rlsrow']/a[@class='rls']");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var release = node.GetAttributeValue("title").Replace("Expand ", string.Empty);
                var infourl = node.GetNodeAttributeValue("..//input[@name='links2']", "value");
                var quality = FileNames.Parser.ParseQuality(release);
                var sites   = node.GetHtmlValue("../following-sibling::div[1]/span[@class='links']").Split(new[] { "<br><br>" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var site in sites)
                {
                    var link = new Link(this);

                    link.Release = release;
                    link.InfoURL = infourl;
                    link.FileURL = site.Replace("<br>", "\0");
                    link.Quality = quality;
                    link.Infos   = Regex.Match(link.FileURL, @"http://(?:www\.)?([^\.]+)").Groups[1].Value.ToUppercaseFirst();

                    yield return link;
                }
            }
        }
    }
}
