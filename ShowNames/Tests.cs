namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;

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
        public static readonly KeyValueList<string, string> ShowNames = new KeyValueList<string, string>
            {
                // test to see whether irrevelant single characters are removed
                {
                    "House, M.D.",
                    "HOUSE"
                },
                {
                    "Two and a half men",
                    "TWO AND A HALF MEN"
                },
                {
                    "How I met your mother",
                    "HOW I MET YOUR MOTHER"
                },

                // test to see if "&"/"and" can be swapped without consequences
                {
                    "Penn & Teller: Bullshit!",
                    "PENN TELLER BULLSHIT"
                },
                {
                    "Penn & Teller: Bullshit!",
                    "PENN AND TELLER BULLSHIT"
                },
                {
                    "Penn and Teller: Bullshit!",
                    "PENN TELLER BULLSHIT"
                },
                {
                    "Penn and Teller: Bullshit!",
                    "PENN AND TELLER BULLSHIT"
                },

                // test to see if PHP's fsm-damn magic quotes are recognized
                {
                    "It's Always Sunny in Philadelphia",
                    "ITS ALWAYS SUNNY IN PHILADELPHIA"
                },
                {
                    "It's Always Sunny in Philadelphia",
                    "IT\\'S ALWAYS SUNNY IN PHILADELPHIA"
                },
                
                // test to see how years are handled
                // if the show is newer than 2000, the year is removed
                {
                    "V (2009)",
                    "V"
                },
                {
                    "The V (2009)",
                    "V"
                },
                {
                    "V (1965)",
                    "V 1965"
                },
                {
                    "The V (1965)",
                    "V 1965"
                },
                
                // test to see whether the country is removed
                {
                    "Top Gear (US)",
                    "TOP GEAR US"
                },
                {
                    "Top Gear (UK)",
                    "TOP GEAR (UK)"
                },

                // test the use of dictionary lookup-based cleaning
                {
                    "Sci-Fi Science: Physics of the Impossible",
                    "SCI-FI SCIENCE"
                },
                
                // test wierd names
                {
                    "Tosh.0",
                    "TOSH.0"
                },
                {
                    "Numb3rs",
                    "NUMB3RS"
                },
                {
                    "$#*!  My Dad Says",
                    "SHIT MY DAD SAYS"
                },
                {
                    "Don't Trust the B---- in Apartment 23",
                    "Apartment 23"
                },
            };

        /// <summary>
        /// Contains a list of standard, non-standard and downright pervert episode notations.
        /// </summary>
        public static readonly KeyValueList<string, ShowEpisode> EpisodeNotations = new KeyValueList<string, ShowEpisode>
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
                    "top.gear.us.s02e01.720p.hdtv.x264-momentum.mkv",
                    new ShowEpisode(2, 1)
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
                    // seriously, why the fuck did immerse and dimension use this instead of
                    // the standard PartXX for miniseries where XX are arabic numerals?
                    // see http://scenerules.irc.gs/n.html?id=2011_TV_X264.nfo line 210
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

                // test anime
                {
                    "[HorribleSubs] Hunter X Hunter - 32 [1080p].mkv",
                    new ShowEpisode(1, 32)
                },
            };

        /// <summary>
        /// Contains a list of release names with season pack notations and how they're supposed to look after removal.
        /// </summary>
        public static readonly KeyValueList<string, string> PackNotations = new KeyValueList<string, string>
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
        /// Tests the <see cref="RoliSoft.TVShowTracker.ShowNames.Parser.GenerateTitleRegex"/> method's ability to generate regular expressions.
        /// </summary>
        [Test]
        public void TitleRegexGeneration()
        {
            foreach (var show in ShowNames)
            {
                var rgx = TVShowTracker.ShowNames.Parser.GenerateTitleRegex(show.Key);

                Console.WriteLine(show.Key + "\r\n -> " + rgx + "\r\n -> " + show.Value);
                Assert.IsTrue(rgx.IsMatch(show.Key.ToUpper()), "The generated regular expression didn't match the original string.");
                Assert.IsTrue(rgx.IsMatch(show.Value), "The generated regular expression didn't match the sample string.");
            }
        }

        /// <summary>
        /// Tests the <see cref="RoliSoft.TVShowTracker.ShowNames.Regexes.AdvNumbering"/> dynamically generated regular
        /// expression through <see cref="RoliSoft.TVShowTracker.ShowNames.Parser.ExtractEpisode"/> against a few weird cases.
        /// </summary>
        [Test]
        public void EpisodeNumberingExtraction()
        {
            foreach (var show in EpisodeNotations)
            {
                var test = FileNames.Parser.ParseFile(show.Key, null, false, true);
                Console.WriteLine(show.Key.PadRight(78) + " -> " + test);
                Assert.AreEqual(show.Value, test.Episode);
            }
        }

        /// <summary>
        /// Tests the <see cref="RoliSoft.TVShowTracker.ShowNames.Regexes.VolNumbering"/> static regular expression.
        /// </summary>
        [Test]
        public void SeasonPackRemoval()
        {
            foreach (var show in PackNotations)
            {
                Console.WriteLine(show.Key.PadRight(37) + " -> " + show.Value);
                Assert.AreEqual(show.Value, Regexes.VolNumbering.Replace(show.Key, string.Empty));
            }
        }
    }
}
