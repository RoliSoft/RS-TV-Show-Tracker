namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Demonoid.
    /// </summary>
    [Parser("2011-04-17 7:08 PM"), TestFixture]
    public class Demonoid : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Demonoid";
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
                return "http://www.demonoid.me/";
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
            var html  = Utils.GetHTML(Site + "files/?category=3&query=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//td/a[starts-with(@href, '/files/details/')]");
            var sizes = html.DocumentNode.SelectNodes("//td[starts-with(@class, 'tone_') and @align='right']");

            if (links == null)
            {
                yield break;
            }

            var i = 0;
            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.InnerText;
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.FileURL = link.InfoURL.Replace("/details/", "/download/");
                link.Size    = sizes[i].InnerText.Trim();
                link.Quality = FileNames.Parser.ParseQuality(link.Release.Replace(" ", string.Empty));
                link.Infos   = Link.SeedLeechFormat.FormatWith(sizes[i].GetTextValue("../td[7]").Trim(), sizes[i].GetTextValue("../td[8]").Trim())
                             + (node.GetTextValue("../font") == "(external)" ? ", External" : string.Empty);

                i++;

                yield return link;
            }
        }
    }
}
