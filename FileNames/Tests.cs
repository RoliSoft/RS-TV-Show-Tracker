namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides unit tests for the file name matching methods.
    /// </summary>
    [TestFixture]
    public class ParsingTests
    {
        public static Dictionary<string, Qualities> TvStoreList = new Dictionary<string, Qualities>
            {
                { "SVCD-Rip",      Qualities.VHSRipXviD  },
                { "VHSRip",        Qualities.VHSRipXviD  },
                { "HDTV-Rip",      Qualities.HDTVXviD    },
                { "DVBRip",        Qualities.HDTVXviD    },
                { "PDTV",          Qualities.HDTVXviD    },
                { "DSRIP",         Qualities.HDTVXviD    },
                { "TvRip",         Qualities.SDTVRip     },
                { "Web-Dl",        Qualities.WebRipXviD  }, // no resolution specified
                { "DVDSCR",        Qualities.Screener    },
                { "Web-DL-Rip",    Qualities.WebRipXviD  },
                { "DVDRip",        Qualities.DVDRipXviD  },
                { "DVD-5",         Qualities.DVD         },
                { "DVD-9",         Qualities.DVD         },
                { "BRRip",         Qualities.BDRipXviD   },
                { "BDRip",         Qualities.BDRipXviD   },
                { "HR-HDTV",       Qualities.HRx264      },
                { "HDTV-720p",     Qualities.HDTV720p    },
                { "Web-Dl-720p",   Qualities.WebDL720p   },
                { "HDTV-1080i",    Qualities.HDTV1080i   },
                { "HDTV-1080p",    Qualities.HDTV1080i   },
                { "Blu-ray 720p",  Qualities.BluRay720p  },
                { "Blu-ray 1080p", Qualities.BluRay1080p },
                { "Egyéb",         Qualities.Unknown     }
            };

        /// <summary>
        /// Tests if the quality descriptions match themselves.
        /// </summary>
        [Test]
        public void QualitiesSelfTest()
        {
            foreach (var quality in Enum.GetValues(typeof(Qualities)).Cast<Qualities>().Reverse())
            {
                var descr = quality.GetAttribute<System.ComponentModel.DescriptionAttribute>().Description;
                var parse = Parser.ParseQuality(descr);

                Console.WriteLine(descr.PadRight(13) + " [" + parse.ToEdition() + "]");
                Assert.AreEqual(quality, parse);
            }
        }

        /// <summary>
        /// Tests if the engine correctly maps all of TvStore's pre-defined categories.
        /// </summary>
        [Test]
        public void TvStoreQualities()
        {
            foreach (var tvsq in TvStoreList)
            {
                var parse = Parser.ParseQuality(tvsq.Key);

                Console.WriteLine(tvsq.Key.PadRight(13) + " -> " + parse);
                Assert.AreEqual(tvsq.Value, parse);
            }
        }
    }
}
