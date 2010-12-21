namespace RoliSoft.TVShowTracker.Parsers.Recommendations
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
        public EventHandler<EventArgs<string, string>> RecommendationError;

        /// <summary>
        /// Represents a recommended TV show.
        /// </summary>
        public class RecommendedShow
        {
            public string Name { get; set; }
            public string Tagline { get; set; }
            public string Score { get; set; }
            public string Runtime { get; set; }
            public string Episodes { get; set; }
            public string Genre { get; set; }
            public string Wikipedia { get; set; }
            public string Epguides { get; set; }
            public string Imdb { get; set; }
        }

        /// <summary>
        /// Gets the list of recommended TV show from the engine.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        /// <returns>Recommended shows list.</returns>
        public abstract List<RecommendedShow> GetList(List<string> shows);

        /// <summary>
        /// Gets the list of recommended TV show from the engine asynchronously.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        public void GetListAsync(List<string> shows)
        {
            new Task(() =>
                {
                    try
                    {
                        var list = GetList(shows);
                        RecommendationDone.Fire(this, list);
                    }
                    catch (Exception ex)
                    {
                        RecommendationError.Fire(this, "There was an error while getting the recommendations. Try again later.", ex.Message);
                    }
                }).Start();
        }
    }
}
