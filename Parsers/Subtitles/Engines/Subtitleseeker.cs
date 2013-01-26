namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping Subtitleseeker.
    /// </summary>
    [TestFixture]
    public class Subtitleseeker : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Subtitleseeker";
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
                return "http://www.subtitleseeker.com/";
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
                return Utils.DateTimeToVersion("2013-01-26 5:08 AM");
            }
        }

        /// <summary>
        /// Returns an <c>IDownloader</c> object which can be used to download the URLs provided by this parser.
        /// </summary>
        /// <value>The downloader.</value>
        public override Downloaders.IDownloader Downloader
        {
            get
            {
                return new ExternalDownloader();
            }
        }

        private const string Key = "61f19cd9310b2926c4520f6a84411273ee727c2d";

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var json1 = Utils.GetJSON("http://api.subtitleseeker.com/search/?api_key=" + Key + "&q=" + Utils.EncodeURL(query) + "&search_in=tv_episodes&return_type=json");

            if ((int)json1["results"]["got_error"] != 0 || (int)json1["results"]["returned_items"] == 0)
            {
                yield break;
            }

            var json2 = Utils.GetJSON("http://api.subtitleseeker.com/get/title_subtitles/?api_key=" + Key + "&episode_id=" + json1["results"]["items"][0]["episode_id"] + "&return_type=json");

            if ((int)json2["results"]["got_error"] != 0 || (int)json2["results"]["total_matches"] == 0)
            {
                yield break;
            }

            foreach (var node in json2["results"]["items"])
            {
                var sub = new Subtitle(this);

                sub.Release  = (string)node["release"] + " - " + (string)node["site"] + " - " + (string)node["downloads"];
                sub.Language = Languages.Parse((string)node["language"]);
                sub.InfoURL  = (string)node["url"];

                yield return sub;
            }
        }

        /// <summary>
        /// Tests the parser by searching for "House" on the site.
        /// </summary>
        public override void TestSearchShow()
        {
            Assert.Inconclusive("Subtitleseeker only supports searching for specific episodes.");
        }
    }
}
