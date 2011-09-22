namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking FileServe links.
    /// </summary>
    [TestFixture]
    public class FileServe : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FileServe";
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
                return "http://www.fileserve.com/";
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
            var html = Utils.GetHTML(Site + "link-checker.php", "ppp=201&submit=Check+Urls&urls=" + Uri.EscapeUriString(url));
            var node = html.DocumentNode.SelectSingleNode("//div[@class='link_checker']//td/img[contains(@src, 'green_alert')]");

            return node != null;
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("45v1x5chT+D9YfTjnFthCYkAo3/ZXJZ1xvnw3hQK8UEQOl9s8W6Uog2DrSqRXCsQtof2dInrVsEQdndA7aalzXWGYNG8FF8Zjsm1f55lGcMVLBLpYJLdfqzmj93P7w9RcRIpjJeXBckUep/fN1Os4w==", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("45v1x5chT+D9YfTjnFthCQM+Lpam+t5FKSCO3yM2ClsSkCtnNMPVBb8n3NF4p4dgCUyfG+eHk5N3jG7RTRanBHiv3yg1AasPcj+RsKNXTqwUmfGcspeGNsuYfqeqDMgU8bkxPsDH6ggWcFxJD7wQcw==", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
