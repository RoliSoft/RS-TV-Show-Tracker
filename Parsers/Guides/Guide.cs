namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Represents a TV show database.
    /// </summary>
    public abstract class Guide : ParserEngine
    {
        /// <summary>
        /// Gets the list of supported languages.
        /// </summary>
        /// <value>The list of supported languages.</value>
        public virtual string[] SupportedLanguages
        {
            get
            {
                return new[] { "en" };
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>TV show data.</returns>
        public abstract TVShow GetData(string id, string language = "en");

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public abstract IEnumerable<ShowID> GetID(string name, string language = "en");

        /// <summary>
        /// Tests the parser by searching and downloading the data for "House" on the site.
        /// </summary>
        [Test]
        public void TestGrab()
        {
            var id = GetID(this is Engines.AniDB ? "hack" : "House").ToList();

            Assert.Greater(id.Count, 0, "Failed to find any shows on the guide.");

            Console.WriteLine("Shows matching for " + (this is Engines.AniDB ? "hack" : "House") + ":");
            Console.WriteLine();
            Console.WriteLine("┌──────────┬────────────────────────────────────────────────────┬────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ID       │ Name                                               │ Language   │ URL                                                          │");
            Console.WriteLine("├──────────┼────────────────────────────────────────────────────┼────────────┼──────────────────────────────────────────────────────────────┤");
            id.ForEach(item => Console.WriteLine("│ {0,-8} │ {1,-50} │ {2,-10} │ {3,-60} │".FormatWith(item.ID.ToString().CutIfLonger(8), item.Title.Transliterate().CutIfLonger(50), (item.Language ?? string.Empty).ToString().CutIfLonger(10), item.URL.CutIfLonger(60))));
            Console.WriteLine("└──────────┴────────────────────────────────────────────────────┴────────────┴──────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            var data = GetData(id[0].ID);

            Assert.IsNotNull(data, "Failed to grab data for the show.");
            Assert.IsNotNullOrEmpty(data.Title, "Failed to get title for the show. If you're seeing this message it means that the grabber silently failed and returned a 'new TVShow()' without informations.");
            Assert.Greater(data.Episodes.Count, 0, "The object contains basic show informations, but failed to grab episode listing -- which is kind of the whole point...");

            Console.WriteLine("Informations and episode listing for " + id[0].Title + ":");
            Console.WriteLine();
            Console.WriteLine("Title:       " + data.Title.Transliterate());
            Console.WriteLine("Genre:       " + data.Genre);
            Console.WriteLine("Description: " + Regex.Replace((data.Description ?? string.Empty).Transliterate(), @"\s+", " "));
            Console.WriteLine("Cover:       " + data.Cover);
            Console.WriteLine("Airing:      " + data.Airing);
            Console.WriteLine("AirTime:     " + data.AirTime);
            Console.WriteLine("AirDay:      " + data.AirDay);
            Console.WriteLine("Network:     " + data.Network);
            Console.WriteLine("Runtime:     " + data.Runtime);
            Console.WriteLine("URL:         " + data.URL);
            Console.WriteLine();

            Console.WriteLine("┌────────┬────────────────────────────────┬────────────┬──────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Number │ Title                          │ Airdate    │ Summary                                                      │ Screen capture                                               │ Link to episode                                              │");
            Console.WriteLine("├────────┼────────────────────────────────┼────────────┼──────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────┤");
            data.Episodes.ForEach(item => Console.WriteLine("│ S{0:00}E{1:00} │ {2,-30} │ {3:yyyy-MM-dd} │ {4,-60} │ {5,-60} │ {6,-60} │".FormatWith(item.Season, item.Number, item.Title.Transliterate().CutIfLonger(30), item.Airdate, (item.Summary ?? string.Empty).Transliterate().CutIfLonger(60), (item.Picture ?? string.Empty).CutIfLonger(60), (item.URL ?? string.Empty).CutIfLonger(60))));
            Console.WriteLine("└────────┴────────────────────────────────┴────────────┴──────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────┘");
        }
    }
}
