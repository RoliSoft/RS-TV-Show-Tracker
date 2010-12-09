namespace RoliSoft.TVShowTracker.Parsers.Recommendations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Provides support for TasteKid's TV show recommendation service.
    /// </summary>
    public class TasteKid : RecommendationEngine
    {
        /// <summary>
        /// Gets the list of recommended TV show from the engine.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        /// <returns>Recommended shows list.</returns>
        public override List<RecommendedShow> GetList(List<string> shows)
        {
            var kid = XDocument.Load("http://www.tastekid.com/ask/ws?verbose=1&q=" + shows.Aggregate(String.Empty, (current, r) => current + (Uri.EscapeUriString(r.Replace(",", String.Empty)) + ",")).TrimEnd(','));

            return kid.Descendants("results").Descendants("resource").Select(item => new RecommendedShow
            {
                Name      = item.Descendants("name").First().Value,
                Tagline   = item.Descendants("wTeaser").First().Value,
                Runtime   = "N/A",
                Episodes  = "N/A",
                Genre     = "N/A",
                Score     = "N/A",
                Wikipedia = item.Descendants("wUrl").First().Value,
                // since TasteKid doesn't give us EPGuides and IMDb, we need to improvise :)
                Epguides  = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString(item.Descendants("name").First().Value + " intitle:\"Titles & Air Dates Guide\" site:epguides.com"),
                Imdb      = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString(item.Descendants("name").First().Value + " intitle:\"TV Series\" site:imdb.com"),
            }).ToList();
        }
    }
}
