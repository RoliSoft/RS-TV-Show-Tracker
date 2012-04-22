namespace RoliSoft.TVShowTracker.Parsers.News.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping TV.com's news section.
    /// </summary>
    [TestFixture]
    public class TVcom : FeedReaderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TV.com";
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
                return "http://www.tv.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/tvcom.png";
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
                return Utils.DateTimeToVersion("2012-04-22 9:45 PM");
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
            var html  = Utils.GetHTML(Site + "shows/" + Utils.CreateSlug(query, false) + "/news/");
            var nodes = html.DocumentNode.SelectNodes("//div[@class='info']");

            if (nodes == null)
            {
                yield break;
            }

            foreach (var node in nodes)
            {
                var article = new Article(this);

                article.Title   = HtmlEntity.DeEntitize(node.GetTextValue("h3")).Trim();
                article.Summary = Regex.Replace(HtmlEntity.DeEntitize(node.GetTextValue("p")).Trim(), @"\s+read more\s*$", string.Empty);
                article.Link    = Site.TrimEnd('/') + node.GetNodeAttributeValue("h3/a", "href");

                var subline = node.GetTextValue("h4");

                if (Regex.IsMatch(subline, @", \d{1,2} (hours?|minutes?) ago"))
                {
                    article.Date = DateTime.Today;
                }
                else if (subline.Contains(", Yesterday"))
                {
                    article.Date = DateTime.Today.AddDays(-1);
                }
                else
                {
                    var date = Regex.Match(subline, @"[A-Za-z]{3} \d{1,2}, \d{4}");
                    if (date.Success)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(date.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            article.Date = dt;
                        }
                    }
                }

                yield return article;
            }
        }
    }
}
