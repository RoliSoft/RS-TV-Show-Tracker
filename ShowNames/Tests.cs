namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides unit tests for the show name matching methods.
    /// </summary>
    [TestFixture]
    public class ParsingTests
    {
        /// <summary>
        /// Tests several different TV show names spelled differently to see if the engine succeeds at matching them.
        /// </summary>
        [Test]
        public void AlternativeSpellings()
        {
            var cmp = new ShowEqualityComparer();

            var shouldMatch = new Dictionary<string, string>
                {
                    { "House, M.D.", "House" },
                    { "Battlestar Galactica (2003)", "Battlestar Galactica" },
                    { "Supernatural (2005)", "Supernatural" },
                    { "Tosh.0", "Tosh.0" },
                    { "Sci-Fi Science: Physics of the Impossible", "Sci Fi Science" },
                    { "Archer (2009)", "Archer" },
                    { "The Universe", "The Universe" },
                    { "V (2009)", "V" },
                    { "V (1965)", "The V (1965)" }
                };

            var shouldntMatch = new Dictionary<string, string>
                {
                    { "House, M.D.",  "Desperate Housewives" }
                };

            foreach (var show in shouldMatch)
            {
                Console.WriteLine(show.Key + ": [" + String.Join(", ", Parser.GetRoot(show.Key)) + "]");
                Console.WriteLine(show.Value + ": [" + String.Join(", ", Parser.GetRoot(show.Value)) + "]");

                Assert.IsTrue(cmp.Equals(show.Key, show.Value), "'{0}' doesn't equal '{1}'".FormatWith(show.Key, show.Value));
            }

            foreach (var show in shouldntMatch)
            {
                Console.WriteLine(show.Key + ": [" + String.Join(", ", Parser.GetRoot(show.Key)) + "]");
                Console.WriteLine(show.Value + ": [" + String.Join(", ", Parser.GetRoot(show.Value)) + "]");

                Assert.IsFalse(cmp.Equals(show.Key, show.Value), "'{0}' shouldn't equal '{1}'".FormatWith(show.Key, show.Value));
            }
        }

        /// <summary>
        /// Tests whether the show names are correctly cleaned.
        /// </summary>
        [Test]
        public void Cleaning()
        {
            var list = new Dictionary<string, string[]>
                {
                    { "House, M.D.", new[] { "HOUSE" } },
                    { "Two and a half men", new[] { "TWO", "AND", "A", "HALF", "MEN" } },
                    { "How I met your mother", new[] { "HOW", "I", "MET", "YOUR", "MOTHER" } },
                    { "The V", new[] { "V" } }
                };

            foreach (var show in list)
            {
                Console.WriteLine(show.Key + ": [" + String.Join(", ", Parser.GetRoot(show.Key, false)) + "], [" + String.Join(", ", show.Value) + "]");

                Assert.IsTrue(show.Value.SequenceEqual(Parser.GetRoot(show.Key, false)), "'{0}' is not cleaned correctly.".FormatWith(show.Key));
            }
        }
    }
}
