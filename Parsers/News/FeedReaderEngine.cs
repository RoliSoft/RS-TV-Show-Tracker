namespace RoliSoft.TVShowTracker.Parsers.News
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
                        var result = Search(query).ToList();

                        ArticleSearchDone.Fire(this, result);

                        SetCache(query, result);
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
            var list = Search("Bones").ToList();

            Assert.Greater(list.Count, 0, "Failed to grab any articles for Bones on {0}.".FormatWith(Name));

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

                try
                {
                    article.Title   = HtmlEntity.DeEntitize(item.Title.Text);
                    article.Summary = Regex.Replace(HtmlEntity.DeEntitize(item.Summary.Text), @"\s*<[^>]+>\s*", string.Empty).Trim();
                    article.Date    = item.PublishDate.DateTime;
                    article.Link    = HtmlEntity.DeEntitize(item.Links[0].Uri.ToString());
                }
                catch
                {
                    continue;
                }

                yield return article;
            }
        }

        /// <summary>
        /// Gets the date when the cache was last written to.
        /// </summary>
        /// <param name="query">The name of the TV show to get the cache's date for.</param>
        /// <returns>Date of last file write or <c>DateTime.MinValue</c>.</returns>
        public DateTime GetCacheDate(string query)
        {
            var path = Path.Combine(Signature.FullPath, @"feeds\" + Utils.CreateSlug(Name, false) + @"\" + Utils.CreateSlug(query, false));

            return !File.Exists(path)
                  ? DateTime.MinValue
                  : File.GetLastWriteTime(path);
        }

        /// <summary>
        /// Gets the cached articles from the last search on this service.
        /// </summary>
        /// <param name="query">The name of the TV show to get the cached articles for.</param>
        /// <returns>List of cached articles from the last search or <c>null</c>.</returns>
        public List<Article> GetCache(string query)
        {
            var list = new List<Article>();
            var path = Path.Combine(Signature.FullPath, @"feeds\" + Utils.CreateSlug(Name, false) + @"\" + Utils.CreateSlug(query, false));

            if (!File.Exists(path))
            {
                return list;
            }

            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                var ver = br.ReadByte();
                var upd = br.ReadUInt32();
                var cnt = br.ReadUInt32();

                for (var i = 0; i < cnt; i++)
                {
                    var article = new Article(this);

                    article.Title   = br.ReadString();
                    article.Date    = ((double)br.ReadInt32()).GetUnixTimestamp();
                    article.Summary = br.ReadString();
                    article.Link    = br.ReadString();

                    list.Add(article);
                }
            }

            return list;
        }

        /// <summary>
        /// Sets the cached articles on this service for this show.
        /// </summary>
        /// <param name="query">The name of the TV show to set the cached aricles for.</param>
        /// <param name="articles">The articles to cache.</param>
        public void SetCache(string query, List<Article> articles)
        {
            var path = Path.Combine(Signature.FullPath, @"feeds\" + Utils.CreateSlug(Name, false) + @"\" + Utils.CreateSlug(query, false));

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)articles.Count);

                foreach (var article in articles)
                {
                    bw.Write(article.Title ?? string.Empty);
                    bw.Write((int)article.Date.ToUnixTimestamp());
                    bw.Write(article.Summary ?? string.Empty);
                    bw.Write(article.Link ?? string.Empty);
                }
            }
        }
    }
}