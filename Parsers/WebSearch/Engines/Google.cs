namespace RoliSoft.TVShowTracker.Parsers.WebSearch
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides access to web search services.
    /// </summary>
    public static partial class Engines
    {
        /// <summary>
        /// Googles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static IEnumerable<SearchResult> Google(string query)
        {
            var search = Utils.GetURL("http://www.google.com/uds/GwebSearch?callback=google.search.WebSearch.RawCompletion&context=0&lstkp=0&rsz=small&hl=en&source=gsc&gss=.com&sig=22c4e39868158a22aac047a2c138a780&q={0}&gl=www.google.com&qid=12a9cb9d0a6870d28&key=AIzaSyA5m1Nc8ws2BbmPRwKu5gFradvD_hgq6G0&v=1.0".FormatWith(Uri.EscapeUriString(query)));
            var json   = JObject.Parse(search.Remove(0, "google.search.WebSearch.RawCompletion('0',".Length));

            if (!json["results"].HasValues)
            {
                yield break;
            }

            foreach (var result in json["results"])
            {
                yield return new SearchResult
                    {
                        Title = Regex.Replace(result["title"].Value<string>(), @"<.*?>", string.Empty),
                        URL   = result["unescapedUrl"].Value<string>()
                    };
            }
        }
    }
}