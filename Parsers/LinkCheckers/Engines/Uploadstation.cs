namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Uploadstation links.
    /// </summary>
    [TestFixture]
    public class Uploadstation : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Uploadstation";
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
                return "http://www.uploadstation.com/";
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
                return Utils.DateTimeToVersion("2011-09-22 5:14 PM");
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
            var html = Utils.GetHTML(Site + "check-links.php", "urls=" + Uri.EscapeUriString(url));
            var node = html.DocumentNode.SelectSingleNode("//div[@class='col col4' and text() = 'Available']");

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
            return new Uri(url).Host.EndsWith("uploadstation.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("mjMvXkWR00fcc7sOKuLMWpQAsVqPII8j8HXbAnBd7wTDVnvk8/pKU39AJ1TTJTDBfNIfFaxSUPWBNw0615FTeI50YFUUfQKqs8lxU9wn1sz9p3Sdu5IncQ4BkoZn+0L1gsbwqf/L2lZcgMWy5Usgcw==", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("mjMvXkWR00fcc7sOKuLMWpQAsVqPII8j8HXbAnBd7wRfV+gSzXOEcnejK9iIkSbYSaETFzqo6aaewE7CJxNH0FckfmdpFHPQxHV/ZjfO/MwCsQ8ZC+7KKk/JbwXv6WiSXXWqC0pga9EsFSOJqSGWnw==", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
