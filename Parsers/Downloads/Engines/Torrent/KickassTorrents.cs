namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping KickassTorrents.
    /// </summary>
    [TestFixture]
    public class KickassTorrents : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "KickassTorrents";
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
                return "http://kat.ph/";
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
                return Utils.DateTimeToVersion("2013-01-13 8:53 PM");
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
            var xml = Utils.GetXML(Site + "usearch/" + Utils.EncodeURL(query) + "/?rss=1");

            foreach (var node in xml.Descendants("item"))
            {
                var link = new Link(this);

                int size;
                int.TryParse(node.GetValue("size") ?? string.Empty, out size);
                
                link.Release = node.GetValue("title");
                link.FileURL = node.GetNodeAttributeValue("enclosure", "url");
                link.InfoURL = node.GetValue("link");
                link.Size    = Utils.GetFileSize(size);
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetValue("seeds"), node.GetValue("leechs"))
                             + (node.GetValue("verified") != "0" ? ", Verified" : string.Empty);

                yield return link;
            }
        }
    }
}
