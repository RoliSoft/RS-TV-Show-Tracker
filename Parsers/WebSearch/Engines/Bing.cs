namespace RoliSoft.TVShowTracker.Parsers.WebSearch.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Net;

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
                return Utils.DateTimeToVersion("2012-12-30 11:56 AM");
            }
        }

        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public override IEnumerable<SearchResult> Search(string query)
        {
            var search = Utils.GetURL("https://api.datamarket.azure.com/Bing/Search/Web?Query=%27{0}%27&$format=json".FormatWith(Utils.EncodeURL(query)), request: request => request.Credentials = new NetworkCredential("4NrIZv4C92lrK8G0M7VNi/lzavUnJqs5xqmYTgeY1pc=", "4NrIZv4C92lrK8G0M7VNi/lzavUnJqs5xqmYTgeY1pc="));
            var json   = JObject.Parse(search);

            if (!json["d"]["results"].HasValues)
            {
                yield break;
            }

            foreach (var result in json["d"]["results"])
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