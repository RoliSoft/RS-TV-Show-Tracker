namespace RoliSoft.TVShowTracker.Parsers.News
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Text.RegularExpressions;
    using System.Threading;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Represents a feed reader engine.
    /// </summary>
    public abstract class FeedReaderEngine : ParserEngine
    {
        /// <summary>
        /// Gets the language of the articles.
        /// </summary>
        /// <value>The articles' language.</value>
        public abstract string Language { get; }

        /// <summary>
        /// Occurs when an article search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<Article>>> ArticleSearchDone;

        /// <summary>
        /// Occurs when an article search is returned from cache.
        /// </summary>
        public event EventHandler<EventArgs<List<Article>>> ArticleSearchCached;

        /// <summary>
        /// Occurs when an article search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> ArticleSearchError;

        /// <summary>
        /// Searches for articles on the service.
        /// </summary>
        /// <param name="query">The name of the TV show to search for.</param>
        /// <returns>List of found articles.</returns>
        public abstract IEnumerable<Article> Search(string query);

        private Thread _job;

        /// <summary>
        /// Searches for articles on the service asynchronously.
        /// </summary>
        /// <param name="query">The name of the TV show to search for.</param>
        public void SearchAsync(string query)
        {
            CancelAsync();

            _job = new Thread(() =>
            {
                try
                {
                    ArticleSearchDone.Fire(this, Search(query).ToList());
                }
                catch (Exception ex)
                {
                    ArticleSearchError.Fire(this, "There was an error while searching for articles.", ex);
                }
            });
            _job.Start();
        }

        /// <summary>
        /// Cancels the active asynchronous search.
        /// </summary>
        public void CancelAsync()
        {
            if (_job != null)
            {
                _job.Abort();
                _job = null;
            }
        }

        /// <summary>
        /// Tests the parser by searching for "House" on the site.
        /// </summary>
        [Test]
        public virtual void Test()
        {
            var list = Search("House").ToList();

            Assert.Greater(list.Count, 0, "Failed to grab any articles for House on {0}.".FormatWith(Name));

            Console.WriteLine("┌──────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┬────────────────────────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Title                                                        │ Summary                                                      │ Date                           │ Link                                                         │");
            Console.WriteLine("├──────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────┼────────────────────────────────┼──────────────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-60} │ {1,-60} │ {2,-30:yyyy-MM-dd HH:mm:ss zzz} │ {3,-60} │".FormatWith(item.Title.Transliterate().CutIfLonger(60), item.Summary.Transliterate().CutIfLonger(60), item.Date, item.Link.CutIfLonger(60))));
            Console.WriteLine("└──────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────┴────────────────────────────────┴──────────────────────────────────────────────────────────────┘");
        }

        /// <summary>
        /// Parses a syndication feed.
        /// </summary>
        /// <param name="url">The URL to the feed.</param>
        /// <returns>
        /// Extracted articles.
        /// </returns>
        protected IEnumerable<Article> ParseFeed(string url)
        {
            var xml = Utils.GetXML(url);
            var rss = SyndicationFeed.Load(xml.CreateReader());

            if (rss == null)
            {
                yield break;
            }

            foreach (var item in rss.Items)
            {
                var article = new Article(this);

                article.Title   = HtmlEntity.DeEntitize(item.Title.Text);
                article.Summary = Regex.Replace(HtmlEntity.DeEntitize(item.Summary.Text), @"\s*<[^>]+>\s*", string.Empty);
                article.Date    = item.PublishDate.DateTime;
                article.Link    = HtmlEntity.DeEntitize(item.Links[0].Uri.ToString());

                yield return article;
            }
        }  
    }
}
