namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Netload links.
    /// </summary>
    [TestFixture]
    public class Netload : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Netload";
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
                return "http://www.netload.in/";
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
            var id  = Regex.Match(url, @"/datei([^$\./]+)").Groups[1].Value;
            var req = Utils.GetURL("http://api.netload.in/info.php", "auth=BVm96BWDSoB4WkfbEhn42HgnjIe1ilMt&file_id=" + id);

            return req.TrimEnd().EndsWith(";online");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("xV6Cg3GjsX5raTHgRcW8Anmy861U9tTvHUq/95siqoruD/n57xq4wMQfBN6SqJ/Y", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("xV6Cg3GjsX5raTHgRcW8Aq2eK6YkG2eeRnMlfeV2VJFde3wibbgYJeirTp6kssCb", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
