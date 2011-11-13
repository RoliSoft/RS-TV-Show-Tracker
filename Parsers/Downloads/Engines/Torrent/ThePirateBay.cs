namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping The Pirate Bay.
    /// </summary>
    [TestFixture]
    public class ThePirateBay : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "The Pirate Bay";
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
                return "http://thepiratebay.org/";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-02-13 4:46 PM");
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
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "search/" + Uri.EscapeUriString(query) + "/0/7/0");
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/div/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.InnerText;
                link.FileURL = node.GetNodeAttributeValue("../../a[1]", "href");
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.Size    = Regex.Match(node.GetTextValue("../../font"), "Size (.*?),").Groups[1].Value.Replace("&nbsp;", " ").Replace("i", string.Empty);
                link.Quality = FileNames.Parser.ParseQuality(node.InnerText);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../td[3]").Trim(), node.GetTextValue("../../../td[4]").Trim())
                             + (node.GetHtmlValue("../..//img[@title='VIP']") != null ? ", VIP Uploader" : string.Empty)
                             + (node.GetHtmlValue("../..//img[@title='Trusted']") != null ? ", Trusted Uploader" : string.Empty);

                yield return link;
            }
        }
    }
}
