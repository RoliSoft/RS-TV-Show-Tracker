namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping NZBClub.
    /// </summary>
    [TestFixture]
    public class NZBClub : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "NZBClub";
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
                return "http://nzbclub.com/";
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
                return "http://nzbclub.com/images/favicon.ico";
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
                return Utils.DateTimeToVersion("2011-09-19 1:21 AM");
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
                return Types.Usenet;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "search.aspx?q=" + Utils.EncodeURL(query));
            var links = html.DocumentNode.SelectNodes("//span[contains(@id, 'SubjectLabel')]/a[1]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = HtmlEntity.DeEntitize(node.InnerText);
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.FileURL = Site.TrimEnd('/') + node.GetNodeAttributeValue("../../..//span[contains(@id, 'sizelabel')]/a", "href");
                link.Size    = Regex.Match(node.GetHtmlValue("../../..//span[contains(@id, 'sizecolumnlabel')]"), @"^(?:<b>)?([^<]+)").Groups[1].Value;
                link.Quality = FileNames.Parser.ParseQuality(link.Release.Replace(' ', '.'));
                link.Infos   = Utils.ParseAge(node.GetTextValue("../../..//span[contains(@id, 'agecolumnlabel')]").Trim());

                yield return link;
            }
        }
    }
}
