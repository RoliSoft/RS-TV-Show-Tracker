namespace RoliSoft.TVShowTracker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Parsers.Downloads;

    /// <summary>
    /// Provides methods for searching download links on multiple provides asynchronously.
    /// </summary>
    public class DownloadSearch
    {
        /// <summary>
        /// Occurs when a download link search is done on all engines.
        /// </summary>
        public event EventHandler<EventArgs> DownloadSearchDone;

        /// <summary>
        /// Occurs when a download link search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<DownloadSearchEngine>>> DownloadSearchEngineDone;

        /// <summary>
        /// Occurs when a download link search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> DownloadSearchEngineError;

        /// <summary>
        /// Occurs when a download link search found a new link.
        /// </summary>
        public event EventHandler<EventArgs<Link>> DownloadSearchEngineNewLink;

        /// <summary>
        /// Gets or sets the search engines.
        /// </summary>
        /// <value>The search engines.</value>
        public List<DownloadSearchEngine> SearchEngines { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to filter search results.
        /// </summary>
        /// <value><c>true</c> if filtering is enabled; otherwise, <c>false</c>.</value>
        public bool Filter { get; set; }

        private List<DownloadSearchEngine> _remaining;
        private Regex _titleRegex, _episodeRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadSearch"/> class.
        /// </summary>
        /// <param name="engines">The engines to use for searching.</param>
        /// <param name="filter">if set to <c>true</c> the search results will be filtered.</param>
        public DownloadSearch(IEnumerable<DownloadSearchEngine> engines = null, bool filter = false)
        {
            SearchEngines = (engines ?? AutoDownloader.ActiveSearchEngines).ToList();
            Filter = filter;

            var remove = new List<DownloadSearchEngine>();

            foreach (var engine in SearchEngines)
            {
                engine.DownloadSearchNewLink += SingleDownloadSearchNewLink;
                engine.DownloadSearchDone    += SingleDownloadSearchDone;
                engine.DownloadSearchError   += SingleDownloadSearchError;

                if (engine.Private)
                {
                    engine.Cookies = Settings.Get(engine.Name + " Cookies");

                    // if requires authentication and no cookies or login information were provided, ignore the engine
                    if (string.IsNullOrWhiteSpace(engine.Cookies) && string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Login")))
                    {
                        remove.Add(engine);
                    }
                }
            }

            // now remove them. if we remove it directly in the previous loop, an exception will be thrown that the enumeration was modified
            foreach (var engine in remove)
            {
                SearchEngines.Remove(engine);
            }
        }

        /// <summary>
        /// Searches for download links on multiple services asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            if (Filter)
            {
                if (ShowNames.Regexes.Numbering.IsMatch(query))
                {
                    var tmp       = ShowNames.Parser.Split(query);
                    _titleRegex   = Database.GetReleaseName(tmp[0]);
                    _episodeRegex = ShowNames.Parser.GenerateEpisodeRegexes(tmp[1]);
                }
                else
                {
                    _titleRegex   = Database.GetReleaseName(query);
                    _episodeRegex = null;
                }
            }

            _remaining = new List<DownloadSearchEngine>(SearchEngines);
            query      = ShowNames.Parser.CleanTitleWithEp(query, false);

            SearchEngines.ForEach(engine => engine.SearchAsync(query));
        }

        /// <summary>
        /// Cancels the active asynchronous searches on all services.
        /// </summary>
        public void CancelAsync()
        {
            new Task(() => SearchEngines.ForEach(engine => engine.CancelAsync())).Start();
        }

        /// <summary>
        /// Occurs when a download link search found a new link.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleDownloadSearchNewLink(object sender, EventArgs<Link> e)
        {
            if (Filter && !ShowNames.Parser.IsMatch(e.Data.Release, _titleRegex, _episodeRegex, false))
            {
                return;
            }

            DownloadSearchEngineNewLink.Fire(this, e.Data);
        }

        /// <summary>
        /// Called when a download link search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleDownloadSearchDone(object sender, EventArgs e)
        {
            try
            {
                lock (_remaining)
                {
                    _remaining.Remove((DownloadSearchEngine)sender);
                }
            }
            catch { }

            DownloadSearchEngineDone.Fire(this, _remaining);

            if (_remaining.Count == 0)
            {
                DownloadSearchDone.Fire(this);
            }
        }

        /// <summary>
        /// Called when a download link search has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleDownloadSearchError(object sender, EventArgs<string, Exception> e)
        {
            try
            {
                lock (_remaining)
                {
                    _remaining.Remove((DownloadSearchEngine)sender);
                }
            }
            catch { }

            DownloadSearchEngineError.Fire(this, e.First, e.Second);

            if (_remaining.Count == 0)
            {
                DownloadSearchDone.Fire(this);
            }
        }
    }
}