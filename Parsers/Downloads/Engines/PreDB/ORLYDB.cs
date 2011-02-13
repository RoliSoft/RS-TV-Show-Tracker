namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.PreDB
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent;

    /// <summary>
    /// Provides support for scraping ReleaseLog.
    /// </summary>
    [Parser("RoliSoft", "2011-02-12 1:46 AM"), TestFixture]
    public class ORLYDB : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "ORLYDB";
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
                return "http://orlydb.com/";
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
                return "http://orlydb.com/favicon.ico";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public override bool Private
        {
            get
            {
                return false;
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
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "?q=" + Uri.EscapeUriString(query));
            var links = html.DocumentNode.SelectNodes("//span[@class='release']");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.InnerText;
                link.Quality = ThePirateBay.ParseQuality(link.Release);

                var info  = node.GetTextValue("..//span[@class='info']");
                link.Size = info != null
                            ? info.Replace("MB", " MB").Split('|')[0].Trim()
                            : null;

                yield return link;
            }
        }
    }
}
