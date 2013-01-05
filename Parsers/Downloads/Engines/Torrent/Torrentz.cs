namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Torrentz.
    /// </summary>
    [TestFixture]
    public class Torrentz : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Torrentz";
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
                return "http://torrentz.eu/";
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
                return Utils.DateTimeToVersion("2013-01-05 6:46 PM");
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
            var xml = Utils.GetXML(Site + "feed/?q=" + Utils.EncodeURL(query));

            foreach (var node in xml.Descendants("item"))
            {
                var link = new Link(this);

                var info = node.GetValue("description");

                link.Release = node.GetValue("title");
                link.FileURL = "http://torcache.net/torrent/" + Regex.Match(info, @"Hash:\s*([0-9a-f]+)").Groups[1].Value + ".torrent";
                link.InfoURL = node.GetValue("link");
                link.Size    = Regex.Match(info, @"Size:\s(\d+(?:\.\d+)?\s*[KMG]B)").Groups[1].Value;
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(Regex.Match(info, @"Seeds:\s(\d+(?:,\d+)?)").Groups[1].Value, Regex.Match(info, @"Peers:\s(\d+(?:,\d+)?)").Groups[1].Value);

                yield return link;
            }
        }
    }
}
