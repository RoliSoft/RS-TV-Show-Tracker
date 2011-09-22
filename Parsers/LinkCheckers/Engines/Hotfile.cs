namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Hotfile links.
    /// </summary>
    [TestFixture]
    public class Hotfile : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Hotfile";
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
                return "http://www.hotfile.com/";
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
            var html = Utils.GetHTML(Site + "checkfiles.html", "but=+Check+Urls+&files=" + Uri.EscapeUriString(url));
            var node = html.DocumentNode.SelectSingleNode("//table[@class='tbl']//span[text() = 'Existent']");

            return node != null;
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("nJhwFy6p4OWQmo15RU3tLBanVAmHlIc04D6/h0zbIxdVLB8chcyEpDdYMEjv0n3/RV/roQPXVTgL+wDeNZGYCMJLd7WRpuF/p3tcpcMe/h0=", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("nJhwFy6p4OWQmo15RU3tLAzqef+94Qy7jZVgaKbJZ/CykyTHe9fi1769ImLKC4jr2ZBAez5LrOJOzeiKWwxF1b32Jb0LtJAmcMwy7puRSIw=", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
