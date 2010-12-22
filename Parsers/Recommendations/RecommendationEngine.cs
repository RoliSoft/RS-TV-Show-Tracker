namespace RoliSoft.TVShowTracker.Parsers.Recommendations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                        RecommendationError.Fire(this, "There was an error while getting the recommendations. Try again later.", ex.Message);
                    }
                }).Start();
        }
    }
}
