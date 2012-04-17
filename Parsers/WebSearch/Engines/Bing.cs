namespace RoliSoft.TVShowTracker.Parsers.WebSearch.Engines
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides support for searching on Bing.
    /// </summary>
    [TestFixture]
    public class Bing : WebSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Bing";
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
                return "http://www.bing.com/";
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
                return Utils.DateTimeToVersion("2011-10-31 8:14 PM");
            }
        }

        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public override IEnumerable<SearchResult> Search(string query)
        {
            var search = Utils.GetURL("http://api.bing.net/json.aspx?AppId=072CCFDBC52FB4552FF96CE87A95F8E9DE30C37B&Query={0}&Sources=Web&Version=2.0&Market=en-us&Adult=Off&Web.Count=1&Web.Offset=0&Web.Options=DisableHostCollapsing+DisableQueryAlterations".FormatWith(Utils.EncodeURL(query)));
            var json   = JObject.Parse(search);

            if (json["SearchResponse"]["Web"]["Total"].Value<int>() == 0)
            {
                yield break;
            }

            foreach (var result in json["SearchResponse"]["Web"]["Results"])
            {
                yield return new SearchResult
                    {
                        Title = result["Title"].Value<string>(),
                        URL   = result["Url"].Value<string>()
                    };
            }
        }
    }
}