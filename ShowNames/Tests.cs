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
        /// Contains a list of show names and the array they're supposed to look after processing.
        /// </summary>
        public static Dictionary<string, string[]> ShowNames = new Dictionary<string, string[]>
            {
                // test to see whether irrevelant single characters are removed
                {
                    "House, M.D.",
                    new[] { "HOUSE" }
                },
                {
                    "Two and a half men",
                    new[] { "TWO", "AND", "A", "HALF", "MEN" }
                },
                {
                    "How I met your mother",
                    new[] { "HOW", "I", "MET", "YOUR", "MOTHER" }
                },
    
                // test to see how years are handled
                // if the show is newer than 2000, the year is removed
                {
                    "V (2009)",
                    new[] { "V" }
                },
                {
                    "The V (2009)",
                    new[] { "V" }
                },
                {
                    "V (1965)",
                    new[] { "V", "(1965)" }
                },
                {
                    "The V (1965)",
                    new[] { "V", "(1965)" }
                },
    
                // test the use of dictionary lookup-based cleaning
                {
                    "Sci-Fi Science: Physics of the Impossible",
                    new[] { "SCI", "FI", "SCIENCE" }
                },
    
                // test wierd names
                {
                    "Tosh.0",
                    new[] { "TOSH", "0" }
                },
            };

        /// <summary>
        /// Contains a list of standard, non-standard and downright pervert episode notations.
        /// </summary>
        public static Dictionary<string, ShowEpisode> EpisodeNotations = new Dictionary<string, ShowEpisode>
            {
                // standard scene episode numbering tests
                {
                    "lost.s06e03.720p.bluray.x264-macro.mkv",
                    new ShowEpisode(6, 3)
                },
                {
                    // in this test case the "101" was originally recognized as 1[x]01
                    "Community.S02E01.Anthropology.101.720p.WEB-DL.DD5.1.H.264-HoodBag",
                    new ShowEpisode(2, 1)
                },
                {
                    "ARRESTED DEVELOPMENT - S03 EP13 - DEVELOPMENT ARRESTED 720P DD5.1 x264 MMI.mkv",
                    new ShowEpisode(3, 13)
                },
                {
                    "top_gear.16x01.real.720p_hdtv_x264-fov.mkv",
                    new ShowEpisode(16, 1)
                },
                {
                    "Archer.1x10.Dial.M.for.Mother.720p.WEB-DL.DD5.1.AVC-DON.mkv",
                    new ShowEpisode(1, 10)
                },
                {
                    "lost.s06e17-18.720p.bluray.x264-macro.mkv",
                    new ShowEpisode(6, 17, 18)
                },
                {
                    "30.Rock.S05E20E21.720p.HDTV.X264-DIMENSION.mkv",
                    new ShowEpisode(5, 20, 21)
                },
                {
                    "entourage.501.720p.hdtv.x264-sys.mkv",
                    new ShowEpisode(5, 1)
                },

                // non-standard episode numbering tests
                {
                    // get_iplayer downloads the episodes with this notation
                    "Bang_Goes_the_Theory_Series_4_-_Episode_2_b00zvcgk_default.mp4",
                    new ShowEpisode(4, 2)
                },
                {
                    "TopGear Series 10 Ep. 01 2007.10.07.avi",
                    new ShowEpisode(10, 1)
                },

                // extremely non-standard episode numbering tests
                {
                    // seriously, why the fuck did immerse and dimension use this instead of plain E01?
                    "spartacus.gods.of.the.arena.pt.i.720p.hdtv.x264-immerse.mkv",
                    new ShowEpisode(1, 1)
                },
                {
                    "Spartacus.Gods.of.the.Arena.Pt.V.720p.HDTV.X264-DIMENSION.mkv",
                    new ShowEpisode(1, 5)
                },
                {
                    // this release is fictional to test multiple episode matching with roman numbering
                    "Spartacus.Gods.of.the.Arena.Part.II-XV.720p.HDTV.X264-DIMENSION.mkv",
                    new ShowEpisode(1, 2, 15)
                },
            };

        /// <summary>
        /// Contains a list of release names with season pack notations and how they're supposed to look after removal.
        /// </summary>
        public static Dictionary<string, string> PackNotations = new Dictionary<string, string>
            {
                {
                    "Lost.COMPLETE.720p.BluRay.x264-TvT",
                    "Lost"
                },
                {
                    "Seinfeld.Season3.DVDR.NTSC.RO.BlueSky",
                    "Seinfeld"
                },
                {
                    "Grey's Anatomy - Season 1",
                    "Grey's Anatomy"
                },
                {
                    "Top Gear Series 11",
                    "Top Gear"
                },
            };

        /// <summary>
        /// Tests whether the show names are correctly cleaned.
        /// </summary>
        [Test]
        public void NameCleaning()
        {
            foreach (var show in ShowNames)
            {
                Console.WriteLine(show.Key + ": [" + String.Join(", ", show.Value) + "]");
                Assert.IsTrue(show.Value.SequenceEqual(Parser.GetRoot(show.Key, false)), "'{0}' is not cleaned correctly: [{1}]".FormatWith(show.Key, string.Join(", ", Parser.GetRoot(show.Key, false))));
            }
        }

        /// <summary>
        /// Tests the extraction of episode numberings.
        /// </summary>
        [Test]
        public void EpisodeExtraction()
        {
            foreach (var show in EpisodeNotations)
            {
                Console.WriteLine(show.Key + " -> " + show.Value);
                Assert.AreEqual(show.Value, Parser.ExtractEpisode(show.Key));
            }
        }

        /// <summary>
        /// Tests the removal of season pack notations.
        /// </summary>
        [Test]
        public void SeasonPackRemoval()
        {
            foreach (var show in PackNotations)
            {
                Console.WriteLine(show.Key + " -> " + show.Value);
                Assert.AreEqual(show.Value, Regexes.VolNumbering.Replace(show.Key, string.Empty));
            }
        }
    }
}
