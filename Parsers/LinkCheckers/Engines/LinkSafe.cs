namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking LinkSafe links.
    /// </summary>
    [TestFixture]
    public class LinkSafe : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "LinkSafe";
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
                return "http://linksafe.me/";
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
                return Site + "images/logo.png";
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
                return Utils.DateTimeToVersion("2013-02-02 2:33 AM");
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
            var loc = string.Empty;

            Utils.GetURL(url,
                request: r =>
                    {
                        r.Method = "HEAD";
                        r.AllowAutoRedirect = false;
                    },
                response: r =>
                    {
                        loc = r.Headers[HttpResponseHeader.Location];
                    });

            if (string.IsNullOrWhiteSpace(loc) || !CanCheck(loc))
            {
                return false;
            }

            var checker = Extensibility.GetNewInstances<LinkCheckerEngine>().FirstOrDefault(x => x.CanCheck(loc));

            if (checker == null)
            {
                throw new NotSupportedException();
            }

            return checker.Check(loc);
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
            return Regex.IsMatch(url, @"[/\.]linksafe.me/", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("QheKhgEzKIM7WIdDenPsI2ftZZIuRoyHjh8+F4C1Pb4=", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("QheKhgEzKIM7WIdDenPsIyZn31/6BacJy+Cf/hgfK08=", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
