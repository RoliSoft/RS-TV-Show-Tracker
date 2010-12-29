namespace RoliSoft.TVShowTracker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

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
        /// Occurs when a download link search progress has changed.
        /// </summary>
        public event EventHandler<EventArgs<List<Link>, double, List<string>>> DownloadSearchProgressChanged;

        /// <summary>
        /// Occurs when a download link search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, string>> DownloadSearchError;

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

                if (engine.Private)
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
        /// Searches for download links on multiple services. The search is made in parallel, but this method is blocking.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public IEnumerable<Link> Search(string query)
        {
            _remaining = SearchEngines.Select(engine => engine.Name).ToList();
            query = ShowNames.Normalize(query);

            // start in parallel
            var tasks = SearchEngines.Select(engine => Task<IEnumerable<Link>>.Factory.StartNew(() =>
                {
                    try { return engine.Search(query).ToList(); }
                    catch (Exception ex)
                    {
                        DownloadSearchError.Fire(this, "There was an error while searching for download links.", ex.Message);
                        return null;
                    }
                })).ToArray();

            // wait all
            Task.WaitAll(tasks);

            // collect and return
            return tasks
                   .Where(task => task.IsCompleted && task.Result != null)
                   .SelectMany(task => task.Result);
        }

        /// <summary>
        /// Searches for download links on multiple services asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            _remaining = SearchEngines.Select(engine => engine.Name).ToList();
            query      = ShowNames.Normalize(query);

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
        /// Called when a download link search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.Collections.Generic.List&lt;RoliSoft.TVShowTracker.Parsers.Downloads.DownloadSearchEngine.Link&gt;&gt;"/> instance containing the event data.</param>
        private void SingleDownloadSearchDone(object sender, EventArgs<List<Link>> e)
        {
            _remaining.Remove((sender as DownloadSearchEngine).Name);

            var percentage = (double)(SearchEngines.Count - _remaining.Count) / SearchEngines.Count * 100;

            DownloadSearchProgressChanged.Fire(this, e.Data, percentage, _remaining);

            if (_remaining.Count == 0)
            {
                DownloadSearchDone.Fire(this);
            }
        }

        /// <summary>
        /// Called when a download link search has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.String,System.String&gt;"/> instance containing the event data.</param>
        private void SingleDownloadSearchError(object sender, EventArgs<string, string> e)
        {
            _remaining.Remove((sender as DownloadSearchEngine).Name);

            DownloadSearchError.Fire(this, e.First, e.Second);
        }
    }
}