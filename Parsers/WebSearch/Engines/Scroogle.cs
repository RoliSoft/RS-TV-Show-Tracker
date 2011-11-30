namespace RoliSoft.TVShowTracker.Parsers.WebSearch.Engines
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for searching on Scroogle.
    /// </summary>
    [TestFixture]
    public class Scroogle : WebSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Scroogle";
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
                return "http://www.scroogle.org/";
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
                return Utils.DateTimeToVersion("2011-12-01 1:27 AM");
            }
        }

        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public override IEnumerable<SearchResult> Search(string query)
        {
            var search = Utils.GetHTML(Site + "cgi-bin/nbbw.cgi", "Gw=" + Uri.EscapeUriString(query) + "&n=2", request: r => r.Referer = Site + "/cgi-bin/scraper.htm");
            var links  = search.DocumentNode.SelectNodes("//font/blockquote/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var result in links)
            {
                yield return new SearchResult
                    {
                        Title = HtmlEntity.DeEntitize(result.InnerText),
                        URL   = result.GetAttributeValue("href")
                    };
            }
        }
    }
}