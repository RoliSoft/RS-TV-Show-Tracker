namespace RoliSoft.TVShowTracker.Parsers.WebSearch.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides support for searching on DuckDuckGo.
    /// </summary>
    [TestFixture]
    public class DuckDuckGo : WebSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "DuckDuckGo";
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
                return "http://duckduckgo.com/";
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
                return Utils.DateTimeToVersion("2012-04-15 3:47 AM");
            }
        }

        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public override IEnumerable<SearchResult> Search(string query)
        {
            var search = Utils.GetURL(Site + "d.js?q={0}&l=us-en&s=0".FormatWith(Uri.EscapeUriString(query)));
            var json   = JArray.Parse(search.Substring(search.IndexOf('[')));

            if (json[0]["t"].Value<string>() == "EOF")
            {
                yield break;
            }

            foreach (var result in json)
            {
                try
                {
                    if (result["t"].Value<string>() == "EOF")
                    {
                        yield break;
                    }
                }
                catch
                {
                    yield break;
                }

                if (result["u"] != null && result["u"].Value<string>().StartsWith("http"))
                {
                    yield return new SearchResult
                        {
                            Title = Regex.Replace(HtmlEntity.DeEntitize(result["t"].Value<string>()), "<[^>]+>", string.Empty),
                            URL   = result["u"].Value<string>()
                        };
                }
            }
        }
    }
}