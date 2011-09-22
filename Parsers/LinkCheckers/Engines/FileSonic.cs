namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking FileSonic links.
    /// </summary>
    [TestFixture]
    public class FileSonic : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FileSonic";
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
                return "http://www.filesonic.ro/";
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
            var html = Utils.GetHTML(Site + "link-checker", "redirect=&controls%5Bsubmit%5D=&links=" + Uri.EscapeUriString(url));
            var node = html.DocumentNode.SelectSingleNode("//strong[text() = 'Available']");

            return node != null;
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("45v1x5chT+D9YfTjnFthCRaRT95Bzi5sPi4H4nK3r4A7iZN2/rrAs1ZoMu/KZWgCDRptdqc6Lw4O/vdOUfEUwHggm0WQS3CDJQWUPHSRyqdcN7OsMB99e1vqbyeyorNP99cB6IC7nTmBEwHm8d+wNw==", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("45v1x5chT+D9YfTjnFthCf0u1CCoJHp23Yy7je0z8Z5s/cUrTBkXm25S3fujAGUmvayPd7SNP2LaYdaPjuUe7VhvTVuFB8YTwzEJZpD3GlXF6dJ+IjmWqx3ze/i+jNCjcypxiawP82YG1w97wZw4sw==", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
