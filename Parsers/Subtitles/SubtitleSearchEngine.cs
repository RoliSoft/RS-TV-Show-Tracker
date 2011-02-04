namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using NUnit.Framework;

    /// <summary>
    /// Represents a subtitle search engine.
    /// </summary>
    public abstract class SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public abstract string Site { get; }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public virtual string Icon { get; internal set; }

        /// <summary>
        /// Occurs when a subtitle search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<Subtitle>>> SubtitleSearchDone;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> SubtitleSearchError;

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public abstract IEnumerable<Subtitle> Search(string query);

        private Thread _job;

        /// <summary>
        /// Searches for subtitles on the service asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            if (_job != null)
            {
                _job.Abort();
            }

            _job = new Thread(() =>
                {
                    try
                    {
                        var list = Search(query);
                        SubtitleSearchDone.Fire(this, list.ToList());
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        SubtitleSearchError.Fire(this, "There was an error while searching for subtitles.", ex);
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
        /// Tests the parser by searching for "House S07E01" on the site.
        /// </summary>
        [Test]
        public void TestSearch()
        {
            var list = Search("House S07E01").ToList();

            Assert.Greater(list.Count, 0, "Failed to grab any subtitles for a popular show.");

            Console.WriteLine("┌────────────────────────────────────────────────────┬────────────┬──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Release name                                       │ Language   │ URL                                                          │");
            Console.WriteLine("├────────────────────────────────────────────────────┼────────────┼──────────────────────────────────────────────────────────────┤");
            list.ForEach(item => Console.WriteLine("│ {0,-50} │ {1,-10} │ {2,-60} │".FormatWith(item.Release.Transliterate().CutIfLonger(50), item.Language.ToString().CutIfLonger(10), item.URL.CutIfLonger(60))));
            Console.WriteLine("└────────────────────────────────────────────────────┴────────────┴──────────────────────────────────────────────────────────────┘");
        }
    }
}
