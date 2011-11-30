namespace RoliSoft.TVShowTracker.Parsers.WebSearch.Engines
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for searching on ixquick.
    /// </summary>
    [TestFixture]
    public class Ixquick : WebSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "ixquick";
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
                return "http://ixquick.com/";
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
                return Utils.DateTimeToVersion("2011-12-01 1:04 AM");
            }
        }

        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public override IEnumerable<SearchResult> Search(string query)
        {
            var search = Utils.GetHTML("http://eu.ixquick.com/do/metasearch.pl?cmd=process_search&pid=e729da4630e3ad8fee0b29f42b8b9edb&nossl=1", "cat=web&cmd=process_search&language=english&engine0=v1all&query=" + Uri.EscapeUriString(query) + "&prf=3670146b5ded1d599d7f30331221aa62&suggestOn=0");
            var links  = search.DocumentNode.SelectNodes("//div[@id='results']/div/h3/a");

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