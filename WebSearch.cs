namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides access to web search services.
    /// </summary>
    public static class WebSearch
    {
        /// <summary>
        /// Googles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static IEnumerable<string> Google(string query)
        {
            var search = Utils.GetURL("http://www.google.com/uds/GwebSearch?callback=google.search.WebSearch.RawCompletion&context=0&lstkp=0&rsz=small&hl=en&source=gsc&gss=.com&sig=22c4e39868158a22aac047a2c138a780&q={0}&gl=www.google.com&qid=12a9cb9d0a6870d28&key=AIzaSyA5m1Nc8ws2BbmPRwKu5gFradvD_hgq6G0&v=1.0".FormatWith(Uri.EscapeUriString(query)));
            var json   = JObject.Parse(search.Remove(0, "google.search.WebSearch.RawCompletion('0',".Length));

            if (!json["results"].HasValues)
            {
                yield break;
            }

            foreach (var result in json["results"])
            {
                yield return result["unescapedUrl"].Value<string>();
            }
        }

        /// <summary>
        /// Bings (...it just doesn't sound right...) the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static IEnumerable<string> Bing(string query)
        {
            var search = Utils.GetURL("http://api.bing.net/json.aspx?AppId=072CCFDBC52FB4552FF96CE87A95F8E9DE30C37B&Query={0}&Sources=Web&Version=2.0&Market=en-us&Adult=Off&Web.Count=1&Web.Offset=0&Web.Options=DisableHostCollapsing+DisableQueryAlterations".FormatWith(Uri.EscapeUriString(query)));
            var json   = JObject.Parse(search);

            if (json["SearchResponse"]["Web"]["Total"].Value<int>() == 0)
            {
                yield break;
            }

            foreach (var result in json["SearchResponse"]["Web"]["Results"])
            {
                yield return result["Url"].Value<string>();
            }
        }

        /// <summary>
        /// Searches DuckDuckGo with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static IEnumerable<string> DuckDuckGo(string query)
        {
            var search = Utils.GetURL("http://duckduckgo.com/d.js?q={0}&l=us-en&s=0".FormatWith(Uri.EscapeUriString(query)));
            var json   = JArray.Parse(search.Substring(search.IndexOf('[')));

            if (json[0]["t"].Value<string>() == "EOF")
            {
                yield break;
            }

            foreach (var result in json)
            {
                var url = result["u"];

                if (url != null)
                {
                    yield return url.Value<string>();
                }
            }
        }

        /// <summary>
        /// Tests the web search methods.
        /// </summary>
        [TestFixture]
        public class Tests
        {
            /// <summary>
            /// Tests the Google API.
            /// </summary>
            [Test]
            public void GoogleTest()
            {
                TestEngine(Google);
            }

            /// <summary>
            /// Tests the Bing API.
            /// </summary>
            [Test]
            public void BingTest()
            {
                TestEngine(Bing);
            }

            /// <summary>
            /// Tests the DuckDuckGo API.
            /// </summary>
            [Test]
            public void DuckDuckGoTest()
            {
                TestEngine(DuckDuckGo);
            }

            /// <summary>
            /// Tests the specified web search engine.
            /// </summary>
            /// <param name="method">The method.</param>
            public void TestEngine(Func<string, IEnumerable<string>> method)
            {
                var list = method("RS TV Show Tracker").ToList();

                Assert.Greater(list.Count, 0, "The search didn't return any results.");

                Console.WriteLine("┌────────────────────────────────────────────────────┐");
                Console.WriteLine("│ Search result                                      │");
                Console.WriteLine("├────────────────────────────────────────────────────┤");
                list.ForEach(item => Console.WriteLine("│ {0,-50} │".FormatWith(item.Transliterate().CutIfLonger(50))));
                Console.WriteLine("└────────────────────────────────────────────────────┘");
            }
        }
    }
}