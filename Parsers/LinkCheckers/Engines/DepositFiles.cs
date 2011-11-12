namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Deposit Files links.
    /// </summary>
    [TestFixture]
    public class DepositFiles : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Deposit Files";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://www.depositfiles.com/";
            }
        }

        /// <summary>
        /// Checks the availability of the link on the service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if the link is available; otherwise, <c>false</c>.
        /// </returns>
        public override bool Check(string url)
        {
            var html = Utils.GetHTML(url);
            var node = html.DocumentNode.SelectSingleNode("//div[@class='downloadblock']/div[@class='info']/span[@class='nowrap']/b");

            return node != null;
        }

        /// <summary>
        /// Determines whether this instance can check the availability of the link on the specified service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if this instance can check the specified service; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanCheck(string url)
        {
            return new Uri(url).Host.EndsWith("depositfiles.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("i0mJNmLH7QXH7gH4KD+GlBGR91dw4mhc0COMgwr7M6yJa3r+5us2V6PitNUWINYgG7SKbhKo+lqyRVOXdbFWJdjTA0xzUVhj+gzIUzsuKPXlSwGHva7F6De/E6vFGgrL", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("i0mJNmLH7QXH7gH4KD+GlFDm9SbedYZ1xkApfxQ2xIibnO0IOfC4sBtuU/L/cBLtQP8yStOTYAdmohhc2Yq8THiap+nNaQrDq3IQCAWWpyQi2e9mioQ71Z2Bs8FifTyL", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
