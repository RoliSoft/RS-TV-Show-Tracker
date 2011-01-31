namespace RoliSoft.TVShowTracker.Parsers.Recommendations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NUnit.Framework;

    /// <summary>
    /// Represents a recommendation engine.
    /// </summary>
    public abstract class RecommendationEngine
    {
        /// <summary>
        /// Occurs when a recommendation request is processed.
        /// </summary>
        public EventHandler<EventArgs<List<RecommendedShow>>> RecommendationDone;

        /// <summary>
        /// Occurs when a recommendation engine has encountered an error.
        /// </summary>
        public EventHandler<EventArgs<string, Exception>> RecommendationError;

        /// <summary>
        /// Gets the list of recommended TV show from the engine.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        /// <returns>Recommended shows list.</returns>
        public abstract IEnumerable<RecommendedShow> GetList(IEnumerable<string> shows);

        /// <summary>
        /// Gets the list of recommended TV show from the engine asynchronously.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        public void GetListAsync(IEnumerable<string> shows)
        {
            new Task(() =>
                {
                    try
                    {
                        var list = GetList(shows);
                        RecommendationDone.Fire(this, list.ToList());
                    }
                    catch (Exception ex)
                    {
                        RecommendationError.Fire(this, "There was an error while getting the recommendations. Try again later.", ex);
                    }
                }).Start();
        }

        /// <summary>
        /// Tests the recommendation engine by requesting recommendations for "House, M.D.", "Chuck" and "Fringe".
        /// </summary>
        [Test]
        public void TestRecommendation()
        {
            var list = GetList(new[] { "House, M.D.", "Chuck", "Fringe" }).ToList();

            Assert.Greater(list.Count, 0);

            Console.WriteLine("┌────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Recommended show                                   │");
            Console.WriteLine("├────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-50} │".FormatWith(item.Name.Transliterate().CutIfLonger(50))));
            Console.WriteLine("└────────────────────────────────────────────────────┘");
        }
    }
}
