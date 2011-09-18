namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping NyaaTorrents.
    /// </summary>
    [Parser("2011-09-18 11:22 AM"), TestFixture]
    public class NyaaTorrents : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "NyaaTorrents";
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
                return "http://www.nyaa.eu/";
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
                return "http://files.nyaa.eu/nt";
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
            var html  = Utils.GetHTML(Site + "?page=search&cats=1_0&filter=0&term=" + Uri.EscapeUriString(ShowNames.Parser.ReplaceEpisode(query, "{1}", false, false)));
            var links = html.DocumentNode.SelectNodes("//table/tr/td[@class='tlistname']/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText);
                link.InfoURL = HtmlEntity.DeEntitize(node.GetAttributeValue("href"));
                link.FileURL = HtmlEntity.DeEntitize(node.GetNodeAttributeValue("../../td[@class='tlistdownload']/a", "href"));
                link.Size    = node.GetTextValue("../../td[@class='tlistsize']").Trim().Replace("i", string.Empty);
                link.Quality = FileNames.Parser.ParseQuality(link.Release);

                var tlistf = node.GetTextValue("../../td[@class='tlistfailed']");

                if (tlistf == null)
                {
                    link.Infos = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../td[@class='tlistsn']").Trim(), node.GetTextValue("../../td[@class='tlistln']").Trim())
                                 + (node.GetNodeAttributeValue("../..", "class").Contains("aplus") ? ", A+" : string.Empty)
                                 + (node.GetNodeAttributeValue("../..", "class").Contains("trusted") ? ", Trusted" : string.Empty)
                                 + (node.GetNodeAttributeValue("../..", "class").Contains("remake") ? ", Remake" : string.Empty);
                }
                else
                {
                    link.Infos = tlistf;
                }

                yield return link;
            }
        }
    }
}
