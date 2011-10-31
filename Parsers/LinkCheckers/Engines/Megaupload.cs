namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Megaupload links.
    /// </summary>
    [TestFixture]
    public class Megaupload : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Megaupload";
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
                return "http://www.megaupload.com/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>
        /// The icon location.
        /// </value>
        public override string Icon
        {
            get
            {
                return "http://wwwstatic.megaupload.com/images/icon.ico";
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
            var id  = Regex.Match(url, @"\?d=([^&$]+)").Groups[1].Value;
            var req = Utils.GetURL(Site + "mgr_linkcheck.php", "id0=" + id);
            
            return Regex.IsMatch(req, "&n=.+");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("fQvwj9uty5cwz66A5BiBp1b/pbu8Jsdd1662wCY6ZmdN8N7Bi/MUkYrdQSbTRxUX", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("fQvwj9uty5cwz66A5BiBp2ZYBN+KWGNxHdglcmNclXBWtxOkciTvaavfSaJ1sIvH", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
