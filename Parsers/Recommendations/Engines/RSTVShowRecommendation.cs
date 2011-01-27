namespace RoliSoft.TVShowTracker.Parsers.Recommendations.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Provides support for the service located at http://lab.rolisoft.net/tv/
    /// </summary>
    public class RSTVShowRecommendation : RecommendationEngine
    {
        private readonly int _type;
        private readonly string _key  = "S2qNfbCFCWoQ8RoL1S0FTbjbW",
                                _uuid = Utils.GetUUID();

        /// <summary>
        /// Initializes a new instance of the <see cref="RSTVShowRecommendation"/> class.
        /// </summary>
        /// <param name="type">The type of the algorithm to use.</param>
        public RSTVShowRecommendation(int type)
        {
            _type = type;
        }

        /// <summary>
        /// Gets the list of recommended TV show from the engine.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        /// <returns>Recommended shows list.</returns>
        public override IEnumerable<RecommendedShow> GetList(IEnumerable<string> shows)
        {
            var lab = XDocument.Load("http://lab.rolisoft.net/tv/api.php?key=" + _key + "&uid=" + _uuid + (_type == 1 ? "&genre=true" : String.Empty) + "&output=xml" + shows.Aggregate(String.Empty, (current, r) => current + ("&show[]=" + Uri.EscapeUriString(r))));

            return lab.Descendants("show")
                   .Select(item => new RecommendedShow
                   {
                       Name      = item.Value,
                       Score     = item.Attribute("score").Value,
                       Wikipedia = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString(item.Value + " TV Series site:en.wikipedia.org"),
                       Epguides  = item.Attribute("epguides").Value,
                       Imdb      = item.Attribute("imdb").Value
                   });
        }
    }
}
