namespace RoliSoft.TVShowTracker.Parsers.News.Engines
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support reading TVLine's RSS feed.
    /// </summary>
    [TestFixture]
    public class TVLine : FeedReaderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TVLine";
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
                return "http://tvline.com/";
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
                return "http://0.gravatar.com/blavatar/287ed40095fad9b4cfed057b0dcaf6b1?s=16";
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
                return Utils.DateTimeToVersion("2012-04-22 9:37 PM");
            }
        }

        /// <summary>
        /// Gets the language of the articles.
        /// </summary>
        /// <value>The articles' language.</value>
        public override string Language
        {
            get
            {
                return "en";
            }
        }

        /// <summary>
        /// Searches for articles on the service.
        /// </summary>
        /// <param name="query">The name of the TV show to search for.</param>
        /// <returns>List of found articles.</returns>
        public override IEnumerable<Article> Search(string query)
        {
            return ParseFeed(Site + "tag/" + Utils.CreateSlug(query, false) + "/feed/");
        }
    }
}
