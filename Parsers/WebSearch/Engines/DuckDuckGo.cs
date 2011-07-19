namespace RoliSoft.TVShowTracker.Parsers.WebSearch
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides access to web search services.
    /// </summary>
    public static partial class Engines
    {
        /// <summary>
        /// Searches DuckDuckGo with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static IEnumerable<SearchResult> DuckDuckGo(string query)
        {
            var search = Utils.GetURL("http://duckduckgo.com/d.js?q={0}&l=us-en&s=0".FormatWith(Uri.EscapeUriString(query)));
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

                if (result["u"] != null)
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