namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking WUpload links.
    /// </summary>
    [TestFixture]
    public class WUpload : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "WUpload";
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
                return "http://www.wupload.com/";
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
            var node = html.DocumentNode.SelectSingleNode("//tr[@class='success']");

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
            return new Uri(url).Host.EndsWith("wupload.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("m4xyT1tfBvzrzpgk1uWHRGFpBMYBeGx0uYLNy1GxntNQ6weL1VRqp9pKV4COGlgq/K0vCdrFo6eWx2xiUwIV2+Xmb16VLXeqinb2ZXug/VKOOaW58/XU/sdPZTyk+HG6srlMHvt6h31p5TAKyfGSOg==", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("m4xyT1tfBvzrzpgk1uWHRGbM189cgtrodqJMVrnmEfzEit7nDvIYgNtyUimqExfSPtlbQf9dZzDAYOF6RozgINMpH1ZDM+8FWI7iOH5eu13yfiI6nH2jT9lb0gh5a7fH1NvDfegjtdxiWKSawUJ5+A==", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
