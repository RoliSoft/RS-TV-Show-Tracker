namespace RoliSoft.TVShowTracker.Parsers.News.Engines
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support reading Seriale Tari's RSS feed.
    /// </summary>
    [TestFixture]
    public class SerialeTari : FeedReaderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Seriale Tari";
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
                return "http://serialetari.com/";
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
                return Utils.DateTimeToVersion("2012-04-22 11:22 PM");
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
                return "ro";
            }
        }

        /// <summary>
        /// Searches for articles on the service.
        /// </summary>
        /// <param name="query">The name of the TV show to search for.</param>
        /// <returns>List of found articles.</returns>
        public override IEnumerable<Article> Search(string query)
        {
            return ParseFeed(Site + "category/seriale/" + Utils.CreateSlug(query, false) + "/feed");
        }
    }
}
