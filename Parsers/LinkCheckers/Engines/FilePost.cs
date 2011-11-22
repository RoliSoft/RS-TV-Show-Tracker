namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking FilePost links.
    /// </summary>
    [TestFixture]
    public class FilePost : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FilePost";
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
                return "http://www.filepost.com/";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-11-22 2:09 AM");
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
            var node = html.DocumentNode.SelectSingleNode("//div[@id='sharing_inputs']");

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
            return new Uri(url).Host.EndsWith("filepost.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("ebHR+Ngk+ZPfOl+sitC/Ys/VWhyhT1kQzddWI8ZeTbV6mycwA0TjXFq3hW3QjD2nOwaYOHGrfh5ius0B27gPiVDYFD2ojweDz8VxCE/57+k=", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("ebHR+Ngk+ZPfOl+sitC/Yu1EWf6ezuv85L02bP+TvvE3HIPegMQ+KggvP/nUsV92qfZ2NCX+uV3QyGNaOVX0SbporEW4Ipwr9kQOCxudji4=", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
