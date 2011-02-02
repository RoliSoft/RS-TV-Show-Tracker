namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;
    using System.Collections.Generic;

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
                Console.WriteLine(show.Key + ": [" + String.Join(", ", Tools.GetRoot(show.Key)) + "]");
                Console.WriteLine(show.Value + ": [" + String.Join(", ", Tools.GetRoot(show.Value)) + "]");

                Assert.IsTrue(cmp.Equals(show.Key, show.Value), "'{0}' doesn't equal '{1}'".FormatWith(show.Key, show.Value));
            }

            foreach (var show in shouldntMatch)
            {
                Console.WriteLine(show.Key + ": [" + String.Join(", ", Tools.GetRoot(show.Key)) + "]");
                Console.WriteLine(show.Value + ": [" + String.Join(", ", Tools.GetRoot(show.Value)) + "]");

                Assert.IsFalse(cmp.Equals(show.Key, show.Value), "'{0}' shouldn't equal '{1}'".FormatWith(show.Key, show.Value));
            }
        }
    }
}
