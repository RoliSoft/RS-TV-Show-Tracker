namespace RoliSoft.TVShowTracker.Parsers.WebSearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Tests the web search methods.
    /// </summary>
    [TestFixture]
    public class SearchingTests
    {
        /// <summary>
        /// Tests the Google API.
        /// </summary>
        [Test]
        public void GoogleTest()
        {
            TestEngine(Engines.Google);
        }

        /// <summary>
        /// Tests the Bing API.
        /// </summary>
        [Test]
        public void BingTest()
        {
            TestEngine(Engines.Bing);
        }

        /// <summary>
        /// Tests the DuckDuckGo API.
        /// </summary>
        [Test]
        public void DuckDuckGoTest()
        {
            TestEngine(Engines.DuckDuckGo);
        }

        /// <summary>
        /// Tests the specified web search engine.
        /// </summary>
        /// <param name="method">The method.</param>
        public void TestEngine(Func<string, IEnumerable<SearchResult>> method)
        {
            var list = method("RS TV Show Tracker").ToList();

            Assert.Greater(list.Count, 0, "The search didn't return any results.");

            Console.WriteLine("┌────────────────────────────────┬────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Title                          │ URL                                                │");
            Console.WriteLine("├────────────────────────────────┼────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-30} │ {1,-50} │".FormatWith(item.Title.Transliterate().CutIfLonger(30), item.URL.Transliterate().CutIfLonger(50))));
            Console.WriteLine("└────────────────────────────────┴────────────────────────────────────────────────────┘");
        }
    }
}