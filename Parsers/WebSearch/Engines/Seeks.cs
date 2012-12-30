namespace RoliSoft.TVShowTracker.Parsers.WebSearch.Engines
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for searching on Seeks Project.
    /// </summary>
    [TestFixture]
    public class Seeks : WebSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Seeks Project";
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
                return "http://www.seeks-project.info/";
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
                return "http://www.seeks-project.info/site/wp-content/themes/sight/images/favico.ico";
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
                return Utils.DateTimeToVersion("2012-04-17 5:28 PM");
            }
        }

        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public override IEnumerable<SearchResult> Search(string query)
        {
            var search = Utils.GetHTML(Site + "search.php/search?q=" + Utils.EncodeURL(query));
            var links  = search.DocumentNode.SelectNodes("//h3/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var result in links)
            {
                yield return new SearchResult
                    {
                        Title = HtmlEntity.DeEntitize(result.InnerText),
                        URL   = HtmlEntity.DeEntitize(result.GetNodeAttributeValue("../..//a[@class='search_cite']/cite/..", "href"))
                    };
            }
        }
    }
}