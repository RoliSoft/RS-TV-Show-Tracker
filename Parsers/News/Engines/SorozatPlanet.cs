namespace RoliSoft.TVShowTracker.Parsers.News.Engines
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping SorozatPlanet's news section.
    /// </summary>
    [TestFixture]
    public class SorozatPlanet : FeedReaderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "SorozatPlanet";
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
                return "http://sorozatplanet.web4.hu/";
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
                return "http://admin.web4.hu/blogimages/s/sorozatplanet/gallery_2/149_original.jpg";
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
                return Utils.DateTimeToVersion("2012-07-28 7:40 PM");
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
                return "hu";
            }
        }

        /// <summary>
        /// Searches for articles on the service.
        /// </summary>
        /// <param name="query">The name of the TV show to search for.</param>
        /// <returns>List of found articles.</returns>
        public override IEnumerable<Article> Search(string query)
        {
            var html  = Utils.GetHTML(Site + Utils.CreateSlug(query, false));
            var nodes = html.DocumentNode.SelectNodes("//div[@class='fe_article']");

            if (nodes == null)
            {
                yield break;
            }

            foreach (var node in nodes)
            {
                var article = new Article(this);

                article.Title   = HtmlEntity.DeEntitize(node.GetTextValue("h1")).Trim();
                article.Summary = HtmlEntity.DeEntitize(node.GetTextValue("p[@class='lead']")).Trim();
                article.Link    = Site.TrimEnd('/') + node.GetNodeAttributeValue("h1/a", "href");
                article.Date    = DateTime.Parse(node.GetTextValue("p[@class='date']").Split(new[] {" - "}, StringSplitOptions.RemoveEmptyEntries)[0]);

                yield return article;
            }
        }
    }
}
