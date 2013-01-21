namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Usenet
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides support for scraping nzbX.
    /// </summary>
    [TestFixture]
    public class nzbX : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "nzbX";
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
                return "https://nzbx.co/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return Site + "favicon.png";
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
                return Utils.DateTimeToVersion("2012-12-30 10:59 PM");
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.Usenet;
            }
        }
        
        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var json = Utils.GetJSON(Site + "api/search?q=" + Utils.EncodeURL(query));

            if (!(json is JArray) || json.Count == 0)
            {
                yield break;
            }

            foreach (JContainer item in json)
            {
                yield return new Link(this)
                    {
                        Release = (string)item["name"],
                        InfoURL = Site + "d?" + (string)item["guid"],
                        FileURL = (string)item["nzb"],
                        Size    = Utils.GetFileSize((long)item["size"]),
                        Quality = FileNames.Parser.ParseQuality((string)item["name"]),
                        Infos   = Utils.DetermineAge(((int)item["postdate"]*1.0).GetUnixTimestamp(), true)
                    };
            }
        }
    }
}
