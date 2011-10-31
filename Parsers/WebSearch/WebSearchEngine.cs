namespace RoliSoft.TVShowTracker.Parsers.WebSearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Represents a web search engine.
    /// </summary>
    public abstract class WebSearchEngine : ParserEngine
    {
        /// <summary>
        /// Searches for the specified query with this service.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public abstract IEnumerable<SearchResult> Search(string query);

        /// <summary>
        /// Tests the parser by searching for "RS TV Show Tracker" with this service.
        /// </summary>
        [Test]
        public virtual void TestSearch()
        {
            var list = Search("RS TV Show Tracker").ToList();

            Assert.Greater(list.Count, 0, "The search didn't return any results.");

            Console.WriteLine("┌────────────────────────────────┬────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Title                          │ URL                                                │");
            Console.WriteLine("├────────────────────────────────┼────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-30} │ {1,-50} │".FormatWith(item.Title.Transliterate().CutIfLonger(30), item.URL.Transliterate().CutIfLonger(50))));
            Console.WriteLine("└────────────────────────────────┴────────────────────────────────────────────────────┘");
        }
    }
}
