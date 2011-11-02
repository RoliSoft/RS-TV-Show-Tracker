namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.PreDB
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping ReleaseLog.
    /// </summary>
    [Parser("RoliSoft", "2011-09-24 1:39 PM"), TestFixture]
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
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = node.GetTextValue("../span[@class='timestamp']").Trim();

                var info = node.GetTextValue("..//span[@class='info']");

                if (info != null)
                {
                    link.Size = info.Replace("MB", " MB").Split('|')[0].Trim();
                }

                var nuke = node.GetTextValue("..//span[@class='nuke']");

                if (nuke != null)
                {
                    link.Infos += ", Nuked: " + nuke;
                }

                yield return link;
            }
        }
    }
}
