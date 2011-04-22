namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides unit tests for the file name matching methods.
    /// </summary>
    [TestFixture]
    public class ParsingTests
    {
        /// <summary>
        /// Tests if the quality descriptions match themselves.
        /// </summary>
        [Test]
        public void Qualities()
        {
            foreach (var quality in Enum.GetValues(typeof(Qualities)).Cast<Qualities>().Reverse())
            {
                var descr = quality.GetAttribute<System.ComponentModel.DescriptionAttribute>().Description;
                var parse = Parser.ParseQuality(descr);

                Console.WriteLine(descr);
                Assert.AreEqual(quality, parse);
            }
        }
    }
}
