namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking links via a link checker definition file.
    /// </summary>
    [TestFixture]
    public class UniversalEngine : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Universal Link Checker";
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
                return "http://lab.rolisoft.net/";
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
                return Utils.DateTimeToVersion("2013-01-12 3:41 PM");
            }
        }

        /// <summary>
        /// Gets or sets the link checker definitions.
        /// </summary>
        /// <value>
        /// The link checker definitions.
        /// </value>
        public static List<Definition> Definitions { get; set; }

        /// <summary>
        /// Initializes the <see cref="UniversalEngine" /> class.
        /// </summary>
        static UniversalEngine()
        {
            var path = Path.Combine(Signature.InstallPath, @"misc\linkchecker");

            if (File.Exists(path) && new FileInfo(path).Length != 0)
            {
                using (var fs = File.OpenRead(path))
                using (var br = new BinaryReader(fs))
                {
                    var ver = br.ReadByte();
                    var upd = br.ReadUInt32();
                    var cnt = br.ReadUInt32();

                    Definitions = new List<Definition>();

                    for (var i = 0; i < cnt; i++)
                    {
                        var def = new Definition();

                        def.Name       = br.ReadString();
                        def.SiteRegex  = br.ReadString();
                        def.GoodRegex  = br.ReadString();
                        def.BadRegex   = br.ReadString();
                        def.MaybeRegex = br.ReadString();

                        Definitions.Add(def);
                    }
                }
            }
            else
            {
                try { GetLinkCheckerDefinitions(); } catch { }
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
                var def = Definitions.FirstOrDefault(d => Regex.IsMatch(url, d.SiteRegex, RegexOptions.IgnoreCase));

                if (def == null)
                {
                    return false;
                }

                var html = Utils.GetURL(url);

                if (!string.IsNullOrWhiteSpace(def.GoodRegex) && !string.IsNullOrWhiteSpace(def.BadRegex))
                {
                    return Regex.IsMatch(html, def.GoodRegex, RegexOptions.IgnoreCase) && !Regex.IsMatch(html, def.BadRegex, RegexOptions.IgnoreCase);
                }
                else if (!string.IsNullOrWhiteSpace(def.GoodRegex))
                {
                    return Regex.IsMatch(html, def.GoodRegex, RegexOptions.IgnoreCase);
                }
                else if (!string.IsNullOrWhiteSpace(def.BadRegex))
                {
                    return !Regex.IsMatch(html, def.BadRegex, RegexOptions.IgnoreCase);
                }
                else if (!string.IsNullOrWhiteSpace(def.MaybeRegex))
                {
                    return Regex.IsMatch(html, def.MaybeRegex, RegexOptions.IgnoreCase);
                }

                return false;
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
            return Definitions.Any(d => Regex.IsMatch(url, d.SiteRegex, RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("PYmrXLxP1q0955G4qq7xh/xxRU538f5WjPGJ82uQ3ZeF42jpOtFjL+LvtZ0UiOej", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("PYmrXLxP1q0955G4qq7xh/dMEd0TOBA3SDmvE8JzaYr6Zjijx0uAcW9igTqWRGkM", Signature.Software));
            Assert.IsFalse(s2);
        }

        /// <summary>
        /// Gets the latest link checker definitions from lab.rolisoft.net.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the operation was successful; otherwise, <c>false</c>.
        /// </returns>
        public static bool GetLinkCheckerDefinitions()
        {
            var req = Remote.API.GetLinkCheckerDefinitions();

            if (!req.Success || req.Result.Count == 0)
            {
                return false;
            }

            Definitions = new List<Definition>();

            foreach (var item in req.Result)
            {
                Definitions.Add(new Definition
                    {
                        Name       = item[0],
                        SiteRegex  = item[1],
                        GoodRegex  = item[2],
                        BadRegex   = item[3],
                        MaybeRegex = item[4]
                    });
            }

            var path = Path.Combine(Signature.InstallPath, @"misc\linkchecker");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)Definitions.Count);

                foreach (var def in Definitions)
                {
                    bw.Write(def.Name ?? string.Empty);
                    bw.Write(def.SiteRegex ?? string.Empty);
                    bw.Write(def.GoodRegex ?? string.Empty);
                    bw.Write(def.BadRegex ?? string.Empty);
                    bw.Write(def.MaybeRegex ?? string.Empty);
                }
            }

            return true;
        }

        /// <summary>
        /// Represents a link checker definition.
        /// </summary>
        public class Definition
        {
            /// <summary>
            /// Gets or sets the name of the site or definition.
            /// </summary>
            /// <value>
            /// The name of the site or definition.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets a regular expression which matches the URL this definition is meant for.
            /// </summary>
            /// <value>
            /// A regular expression which matches the URL this definition is meant for.
            /// </value>
            public string SiteRegex { get; set; }

            /// <summary>
            /// Gets or sets a regular expression which, if matches, the link is online.
            /// </summary>
            /// <value>
            /// A regular expression which, if matches, the link is online.
            /// </value>
            public string GoodRegex { get; set; }

            /// <summary>
            /// Gets or sets a regular expression which, if matches, the link is offline.
            /// </summary>
            /// <value>
            /// A regular expression which, if matches, the link is offline.
            /// </value>
            public string BadRegex { get; set; }

            /// <summary>
            /// Gets or sets a regular expression which indicates the link *may* be online.
            /// </summary>
            /// <value>
            /// A regular expression which indicates the link *may* be online.
            /// </value>
            public string MaybeRegex { get; set; }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return Name + " /" + SiteRegex + "/i";
            }
        }
    }
}
