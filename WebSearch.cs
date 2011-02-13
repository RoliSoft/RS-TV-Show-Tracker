namespace RoliSoft.TVShowTracker
{
    using System;

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
        public static string Google(string query)
        {
            var search = Utils.GetURL("http://www.google.com/uds/GwebSearch?callback=google.search.WebSearch.RawCompletion&context=0&lstkp=0&rsz=small&hl=en&source=gsc&gss=.com&sig=22c4e39868158a22aac047a2c138a780&q={0}&gl=www.google.com&qid=12a9cb9d0a6870d28&key=AIzaSyA5m1Nc8ws2BbmPRwKu5gFradvD_hgq6G0&v=1.0".FormatWith(Uri.EscapeUriString(query)));
            var json   = JObject.Parse(search.Remove(0, "google.search.WebSearch.RawCompletion('0',".Length));

            return json["results"].HasValues
                 ? json["results"][0]["unescapedUrl"].Value<string>()
                 : string.Empty;
        }

        /// <summary>
        /// Bings (...it just doesn't sound right...) the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static string Bing(string query)
        {
            var search = Utils.GetURL("http://api.bing.net/json.aspx?AppId=072CCFDBC52FB4552FF96CE87A95F8E9DE30C37B&Query={0}&Sources=Web&Version=2.0&Market=en-us&Adult=Off&Web.Count=1&Web.Offset=0&Web.Options=DisableHostCollapsing+DisableQueryAlterations".FormatWith(Uri.EscapeUriString(query)));
            var json   = JObject.Parse(search);

            return json["SearchResponse"]["Web"]["Total"].Value<int>() != 0
                 ? json["SearchResponse"]["Web"]["Results"][0]["Url"].Value<string>()
                 : string.Empty;
        }

        /// <summary>
        /// Searches DuckDuckGo with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static string DuckDuckGo(string query)
        {
            var search = Utils.GetURL("http://duckduckgo.com/d.js?q={0}&l=us-en&s=0".FormatWith(Uri.EscapeUriString(query)));
            var json   = JArray.Parse(search.Substring(search.IndexOf('[')));

            return json[0]["t"].Value<string>() != "EOF"
                 ? json[0]["u"].Value<string>()
                 : string.Empty;
        }

        /// <summary>
        /// Tests the web search methods.
        /// </summary>
        [TestFixture]
        public class Tests
        {
            /// <summary>
            /// The query which is going to be used in the test search.
            /// </summary>
            public static string Query = "RS TV Show Tracker";

            /// <summary>
            /// The URL which is the home page of the software.
            /// Any search engine smart enough should find it.
            /// </summary>
            public static string Site  = "http://lab.rolisoft.net/tvshowtracker.html";

            /// <summary>
            /// Tests the Google API.
            /// </summary>
            [Test]
            public static void GoogleTest()
            {
                var r = Google(Query);
                Assert.IsNotNullOrEmpty(r, "The search resulted in an empty string.");
                Assert.AreEqual(Site, r, "The search resulted in a URL which is not the expected one. The API works, however, the search engine is dumb.");
            }

            /// <summary>
            /// Tests the Bing API.
            /// </summary>
            [Test]
            public static void BingTest()
            {
                var r = Bing(Query);
                Assert.IsNotNullOrEmpty(r, "The search resulted in an empty string.");
                Assert.AreEqual(Site, r, "The search resulted in a URL which is not the expected one. The API works, however, the search engine is dumb.");
            }

            /// <summary>
            /// Tests the DuckDuckGo API.
            /// </summary>
            [Test]
            public static void DuckDuckGoTest()
            {
                var r = DuckDuckGo(Query);
                Assert.IsNotNullOrEmpty(r, "The search resulted in an empty string.");
                Assert.AreEqual(Site, r, "The search resulted in a URL which is not the expected one. The API works, however, the search engine is dumb.");
            }
        }
    }
}