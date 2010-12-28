namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

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
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public abstract string Icon { get; }

        /// <summary>
        /// Gets a value indicating whether the site requires cookies to authenticate.
        /// </summary>
        /// <value><c>true</c> if requires cookies; otherwise, <c>false</c>.</value>
        public abstract bool RequiresCookies { get; }

        /// <summary>
        /// Gets or sets the cookies used to access the site.
        /// </summary>
        /// <value>The cookies in the same format in which <c>alert(document.cookie)</c> returns in a browser.</value>
        public string Cookies { get; set; }

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
        public event EventHandler<EventArgs<string, string>> DownloadSearchError;

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
                        DownloadSearchError.Fire(this, "There was an error while searching for download links.", ex.Message);
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
    }
}
