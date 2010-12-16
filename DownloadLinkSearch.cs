namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Occurs when a download link search is done on all engines.
    /// </summary>
    public delegate void DownloadSearchDone();

    /// <summary>
    /// Occurs when a download link search progress has changed.
    /// </summary>
    public delegate void DownloadSearchProgressChanged(List<DownloadSearchEngine.Link> links, double percentage, List<string> remaining);

    /// <summary>
    /// Occurs when a download link search has encountered an error.
    /// </summary>
    public delegate void DownloadSearchError(string message, string detailed = null);

    /// <summary>
    /// Provides methods for searching download links on multiple provides asynchronously.
    /// </summary>
    public class DownloadSearch
    {
        /// <summary>
        /// Occurs when a download link search is done on all engines.
        /// </summary>
        public event DownloadSearchDone DownloadSearchDone;

        /// <summary>
        /// Occurs when a download link search progress has changed.
        /// </summary>
        public event DownloadSearchProgressChanged DownloadSearchProgressChanged;

        /// <summary>
        /// Occurs when a download link search has encountered an error.
        /// </summary>
        public event DownloadSearchError DownloadSearchError;

        /// <summary>
        /// Gets or sets the search engines.
        /// </summary>
        /// <value>The search engines.</value>
        public List<DownloadSearchEngine> SearchEngines { get; set; }

        private volatile List<string> _remaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleSearch"/> class.
        /// </summary>
        /// <param name="engines">The engines to use for searching.</param>
        public DownloadSearch(IEnumerable<Type> engines = null)
        {
            if (engines == null)
            {
                engines = typeof(DownloadSearchEngine).GetDerivedTypes();
            }

            SearchEngines = engines.Select(type => Activator.CreateInstance(type) as DownloadSearchEngine).ToList();

            foreach (var engine in SearchEngines)
            {
                engine.DownloadSearchDone  += SingleDownloadSearchDone;
                engine.DownloadSearchError += SingleDownloadSearchError;

                if (engine.RequiresCookies)
                {
                    engine.Cookies = Settings.Get(engine.Name + " Cookies");

                    // if requires cookies and no cookies were provided, ignore the engine
                    if (string.IsNullOrWhiteSpace(engine.Cookies))
                    {
                        SearchEngines.Remove(engine);
                    }
                }
            }
        }

        /// <summary>
        /// Searches for download links on multiple services asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            _remaining = SearchEngines.Select(engine => engine.Name).ToList();
            query      = ShowNames.Normalize(query);

            foreach (var engine in SearchEngines)
            {
                engine.SearchAsync(query);
            }
        }

        /// <summary>
        /// Cancels the active asynchronous searches on all services.
        /// </summary>
        public void CancelAsync()
        {
            foreach (var engine in SearchEngines)
            {
                engine.CancelAsync();
            }
        }

        /// <summary>
        /// Called when a download link search is done.
        /// </summary>
        /// <param name="name">The name of the engine.</param>
        /// <param name="links">The found links.</param>
        private void SingleDownloadSearchDone(string name, List<DownloadSearchEngine.Link> links)
        {
            _remaining.Remove(name);

            var percentage = (double)(SearchEngines.Count - _remaining.Count) / SearchEngines.Count * 100;

            if (DownloadSearchProgressChanged != null)
            {
                DownloadSearchProgressChanged(links, percentage, _remaining);
            }

            if (_remaining.Count == 0 && DownloadSearchDone != null)
            {
                DownloadSearchDone();
            }
        }

        /// <summary>
        /// Called when a download link search has encountered an error.
        /// </summary>
        /// <param name="name">The name of the engine.</param>
        /// <param name="message">The error message.</param>
        /// <param name="detailed">The detailed error message.</param>
        private void SingleDownloadSearchError(string name, string message, string detailed = null)
        {
            _remaining.Remove(name);

            if (DownloadSearchError != null)
            {
                DownloadSearchError(message, detailed);
            }
        }
    }
}