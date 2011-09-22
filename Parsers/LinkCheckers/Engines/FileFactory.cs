namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking FileFactory links.
    /// </summary>
    [TestFixture]
    public class FileFactory : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FileFactory";
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
                return "http://www.filefactory.com/";
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
            var html = Utils.GetHTML(Site + "tool/links.php", "func=links&links=" + Uri.EscapeUriString(url));
            var node = html.DocumentNode.SelectSingleNode("//table[@class='items']/tr/td[2]");

            return node != null;
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("d+d2U2zWw8eCeTHtTECIzDRQTNaMaqRsq2wbtv/gZq41CaEwChlu3/mEuI86J0lGDz49JCsug+wBatD84QqEG4LqNFEyECFun6PH0H6btb5fotd9wLsBUtKbDpbN/fZ/evgPKNr9pMlayQX2jZmOoA==", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("d+d2U2zWw8eCeTHtTECIzDRQTNaMaqRsq2wbtv/gZq7JAywoZfy7T3JtSupQZVZn/ZOLmvohIYs2o+otUFyAVmLfnMdJjUBXJ6cpopxeJg/GnIaAa9NdiQuW6VBCGbc0KIvbwSN6qNwUDPtAYGSH0w==", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
