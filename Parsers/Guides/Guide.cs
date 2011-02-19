namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Represents a TV show database.
    /// </summary>
    public abstract class Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public abstract string Site { get; }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public virtual string Icon
        {
            get
            {
                return Site + "favicon.ico";
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public abstract TVShow GetData(string id);

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public abstract string GetID(string name);

        /// <summary>
        /// Tests the parser by searching and downloading the data for "House" on the site.
        /// </summary>
        [Test]
        public void TestGrab()
        {
            var id = GetID(this is Engines.AniDB ? "hack" : "House");

            Assert.IsNotNullOrEmpty(id, "Failed to find the ID of the show.");

            var data = GetData(id);

            Assert.IsNotNull(data, "Failed to grab data for the show.");
            Assert.IsNotNullOrEmpty(data.Title, "Failed to get title for the show. If you're seeing this message it means that the grabber silently failed and returned a 'new TVShow()' without informations.");
            Assert.Greater(data.Episodes.Count, 0, "The object contains basic show informations, but failed to grab episode listing -- which is kind of the whole point...");

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
