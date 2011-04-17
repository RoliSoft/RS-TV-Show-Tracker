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
                    { "V (2009)", new[] { "V" } },
                    { "The V (2009)", new[] { "V" } },
                    { "V (1965)", new[] { "V", "(1965)" } },
                    { "The V (1965)", new[] { "V", "(1965)" } }
                };

            foreach (var show in list)
            {
                Console.WriteLine(show.Key + ": [" + String.Join(", ", Parser.GetRoot(show.Key, false)) + "], [" + String.Join(", ", show.Value) + "]");
                Assert.IsTrue(show.Value.SequenceEqual(Parser.GetRoot(show.Key, false)), "'{0}' is not cleaned correctly.".FormatWith(show.Key));
            }
        }

        /// <summary>
        /// Tests the extraction of episode numberings.
        /// </summary>
        [Test]
        public void EpisodeExtraction()
        {
            var shouldEqual = new Dictionary<string, ShowEpisode>
                {
                    { "lost.s06e03.720p.bluray.x264-macro.mkv", new ShowEpisode(6, 3) },
                    { "lost.s06e17-18.720p.bluray.x264-macro.mkv", new ShowEpisode(6, 17, 18) },
                    { "Archer.1x10.Dial.M.for.Mother.720p.WEB-DL.DD5.1.AVC-DON.mkv", new ShowEpisode(1, 10) },
                    { "Community.S02E01.Anthropology.101.720p.WEB-DL.DD5.1.H.264-HoodBag", new ShowEpisode(2, 1) },
                    { "ARRESTED DEVELOPMENT - S03 EP13 - DEVELOPMENT ARRESTED 720P DD5.1 x264 MMI.mkv", new ShowEpisode(3, 13) },
                    { "top_gear.16x01.real.720p_hdtv_x264-fov.mkv", new ShowEpisode(16, 1) }
                };

            foreach (var show in shouldEqual)
            {
                Console.WriteLine(show.Key + " -> " + show.Value);
                Assert.AreEqual(show.Value, Parser.ExtractEpisode(show.Key));
            }
        }
    }
}
