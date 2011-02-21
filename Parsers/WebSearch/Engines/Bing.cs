﻿namespace RoliSoft.TVShowTracker.Parsers.WebSearch
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides access to web search services.
    /// </summary>
    public static partial class Engines
    {
        /// <summary>
        /// Bings (...it just doesn't sound right...) the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static IEnumerable<SearchResult> Bing(string query)
        {
            var search = Utils.GetURL("http://api.bing.net/json.aspx?AppId=072CCFDBC52FB4552FF96CE87A95F8E9DE30C37B&Query={0}&Sources=Web&Version=2.0&Market=en-us&Adult=Off&Web.Count=1&Web.Offset=0&Web.Options=DisableHostCollapsing+DisableQueryAlterations".FormatWith(Uri.EscapeUriString(query)));
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