namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using NUnit.Framework;

    /// <summary>
    /// Represents a download link search engine.
    /// </summary>
    public abstract class DownloadSearchEngine
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
        public abstract string Icon { get; }

        /// <summary>
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public abstract bool Private { get; }

        /// <summary>
        /// Gets or sets the cookies used to access the site.
        /// </summary>
        /// <value>The cookies in the same format in which <c>alert(document.cookie)</c> returns in a browser.</value>
        public virtual string Cookies { get; set; }

        /// <summary>
        /// Gets the names of the required cookies for the authentication.
        /// </summary>
        /// <value>The required cookies for authentication.</value>
        public virtual string[] RequiredCookies { get; internal set; }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public abstract Types Type { get; }

        /// <summary>
        /// Occurs when a download link search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<Link>>> DownloadSearchDone;

        /// <summary>
        /// Occurs when a download link search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> DownloadSearchError;

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public abstract IEnumerable<Link> Search(string query);

        private Thread _job;

        /// <summary>
        /// Searches for download links on the service asynchronously.
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
                        DownloadSearchDone.Fire(this, list.ToList());
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        DownloadSearchError.Fire(this, "There was an error while searching for download links.", ex);
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
        /// Tests the parser by searching for "House" on the tracker.
        /// </summary>
        [Test]
        public void TestSearch()
        {
            if (Private)
            {
                Cookies = Settings.Get(Name + " Cookies");

                if (string.IsNullOrWhiteSpace(Cookies))
                {
                    Assert.Inconclusive("Cookies are required to test a private site.");
                }
            }

            var list = Search("House").ToList();

            Assert.Greater(list.Count, 0);

#if DEBUG
            Debug.WriteLine("┌────────────────────────────────────────────────────┬────────────┬────────────┬──────────────────────────────────────────────────────────────┐");
            Debug.WriteLine("│ Release name                                       │ Size       │ Quality    │ URL                                                          │");
            Debug.WriteLine("├────────────────────────────────────────────────────┼────────────┼────────────┼──────────────────────────────────────────────────────────────┤");
            list.ForEach(item => Debug.WriteLine("│ {0,-50} │ {1,-10} │ {2,-10} │ {3,-60} │".FormatWith(item.Release.Transliterate().CutIfLonger(50), item.Size.CutIfLonger(10), item.Quality, item.URL.CutIfLonger(60))));
            Debug.WriteLine("└────────────────────────────────────────────────────┴────────────┴────────────┴──────────────────────────────────────────────────────────────┘");
#endif
        }
    }
}
