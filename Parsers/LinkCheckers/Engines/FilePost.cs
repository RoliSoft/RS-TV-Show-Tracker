namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;
    using System.Text.RegularExpressions;
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
                return Utils.DateTimeToVersion("2012-04-17 6:08 PM");
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
            var host = new Uri(url).Host;
            return host.EndsWith("fp.io") || host.EndsWith("filepost.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check("http://filepost.com/files/4f278173/rs_tv_show_tracker_unit_test_file.txt/");
            Assert.IsTrue(s1);

            var s2 = Check("http://filepost.com/files/xx278173/rs_tv_show_tracker_unit_test_file.txt/");
            Assert.IsFalse(s2);
        }
    }
}
