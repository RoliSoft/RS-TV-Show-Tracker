namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;
    using System.Net;
    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Uploaded.to links.
    /// </summary>
    [TestFixture]
    public class Uploadedto : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Uploaded.tos";
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
                return "http://uploaded.to/";
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
                return Utils.DateTimeToVersion("2011-12-04 3:41 AM");
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
            try
            {
                var html = Utils.GetHTML(url);
                var node = html.DocumentNode.SelectSingleNode("//div[@id='download']/div/h1/a[@id='filename']");

                return node != null;
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
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
            return host.EndsWith("ul.to") || host.EndsWith("uploaded.to");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("PYmrXLxP1q0955G4qq7xh8MriFE+KvBtl5rGHQFWEDTLGXERhY4iauaT7vIkAmw3cIjVQh4/yN+0wegtumlLGya/hXspnE7isxLut9eLBPIVZM0Vvdc7rwvVHFOCtBWl", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("PYmrXLxP1q0955G4qq7xh7Du56+TN0bKfnaVSL2qTPzrzEyWlDpEsB01j68lCAFIaw6OEx614js04WfMk0xGQBmSLgdjUwcMdFFhKQ0Y2mGaGozt/rcgOKC7ACYxjlQb", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
