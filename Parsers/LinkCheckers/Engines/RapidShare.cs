namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking RapidShare links.
    /// </summary>
    [TestFixture]
    public class RapidShare : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "RapidShare";
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
                return "http://www.rapidshare.com/";
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
                return "http://images3.rapidshare.com/img/favicon.ico";
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
                return Utils.DateTimeToVersion("2011-11-21 10:40 PM");
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
            var qs = Regex.Match(url, @"(?:files/(?<id>\d+)/(?<file>[^/$]+)|#!download\|[^\|]+\|(?<id>\d+)\|(?<file>[^\|$]+))");

            if (!qs.Success)
            {
                return false;
            }

            var req = Utils.GetURL("http://api.rapidshare.com/cgi-bin/rsapi.cgi?sub=checkfiles&files=" + qs.Groups["id"].Value + "&filenames=" + qs.Groups["file"].Value);

            if (req.TrimStart().StartsWith("ERROR:"))
            {
                return false;
            }

            var csv = req.Trim().Split(',');

            return csv.Length >= 4 && csv[4] == "1";
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
            return new Uri(url).Host.EndsWith("rapidshare.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("662cXKdkkqt37Pe+Ky0VXR7ENTTRoUUAfx6r4BoEBny4U3a8LKA15FzN32mblcIySiGZgURlgvnNTBJKCSQLuvxFXiOUiOQG5jselGvXRWFXkgJgSisCJaRn6G4fAoop", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("662cXKdkkqt37Pe+Ky0VXShH6I3UvzZZc9V8exgCWitdWZqvFxMdeo4xzN/9PMsfepVGMPLRWbYqDzeoIcNcLOoohYa6cojxAVqnUR4ZW1yVJJMJduToJZlY63oh1lrL", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}
