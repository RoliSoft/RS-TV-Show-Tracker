﻿namespace RoliSoft.TVShowTracker.Parsers.Recommendations
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
        private readonly string _key, _uuid;
        private readonly int _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="RSTVShowRecommendation"/> class.
        /// </summary>
        /// <param name="key">The API key.</param>
        /// <param name="uuid">The unique ID of the user.</param>
        /// <param name="type">The type of the algorithm to use.</param>
        public RSTVShowRecommendation(string key, string uuid, int type)
        {
            _key  = key;
            _uuid = uuid;
            _type = type;
        }

        /// <summary>
        /// Gets the list of recommended TV show from the engine.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        /// <returns>Recommended shows list.</returns>
        public override List<RecommendedShow> GetList(List<string> shows)
        {
            var lab = XDocument.Load("http://lab.rolisoft.net/tv/api.php?key=" + _key + "&uid=" + _uuid + (_type == 1 ? "&genre=true" : String.Empty) + "&output=xml" + shows.Aggregate(String.Empty, (current, r) => current + ("&show[]=" + Uri.EscapeUriString(r))));

            return lab.Descendants("show").Select(item => new RecommendedShow
            {
                Name      = item.Value,
                Tagline   = item.Attribute("tagline") != null ? item.Attribute("tagline").Value : item.Attribute("plot") != null ? item.Attribute("plot").Value : String.Empty,
                Runtime   = item.Attribute("runtime").Value + " minutes",
                Episodes  = "~" + item.Attribute("episodes").Value,
                Genre     = item.Attribute("genre").Value,
                Score     = item.Attribute("score").Value,
                Wikipedia = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString(item.Value + " TV Series site:en.wikipedia.org"),
                Epguides  = item.Attribute("epguides").Value,
                Imdb      = item.Attribute("imdb").Value
            }).ToList();
        }
    }
}
