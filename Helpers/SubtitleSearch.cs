namespace RoliSoft.TVShowTracker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Provides methods for searching subtitles on multiple provides asynchronously.
    /// </summary>
    public class SubtitleSearch
    {
        /// <summary>
        /// Occurs when a subtitle search is done on all engines.
        /// </summary>
        public event EventHandler<EventArgs> SubtitleSearchDone;

        /// <summary>
        /// Occurs when a subtitle search progress has changed.
        /// </summary>
        public event EventHandler<EventArgs<List<Subtitle>, double, List<string>>> SubtitleSearchProgressChanged;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> SubtitleSearchError;

        /// <summary>
        /// Gets or sets the search engines.
        /// </summary>
        /// <value>The search engines.</value>
        public List<SubtitleSearchEngine> SearchEngines { get; set; }

        private volatile List<string> _remaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleSearch"/> class.
        /// </summary>
        /// <param name="engines">The engines to use for searching.</param>
        public SubtitleSearch(IEnumerable<Type> engines = null)
        {
            if (engines == null)
            {
                engines = typeof(SubtitleSearchEngine).GetDerivedTypes();
            }

            SearchEngines = engines.Select(type => Activator.CreateInstance(type) as SubtitleSearchEngine).ToList();

            foreach (var engine in SearchEngines)
            {
                engine.SubtitleSearchDone  += SingleSubtitleSearchDone;
                engine.SubtitleSearchError += SingleSubtitleSearchError;
            }
        }

        /// <summary>
        /// Searches for subtitles on multiple services. The search is made in parallel, but this method is blocking.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public IEnumerable<Subtitle> Search(string query)
        {
            _remaining = SearchEngines.Select(engine => engine.Name).ToList();
            query = ShowNames.Tools.Normalize(query);

            // start in parallel
            var tasks = SearchEngines.Select(engine => Task<IEnumerable<Subtitle>>.Factory.StartNew(() =>
                {
                    try { return engine.Search(query); }
                    catch (Exception ex)
                    {
                        SubtitleSearchError.Fire(this, "There was an error while searching for download links.", ex);
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
        /// Searches for subtitles on multiple services asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            _remaining = SearchEngines.Select(engine => engine.Name).ToList();
            query = ShowNames.Tools.Normalize(query);

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
        /// Called when a subtitle search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleSubtitleSearchDone(object sender, EventArgs<List<Subtitle>> e)
        {
            _remaining.Remove((sender as SubtitleSearchEngine).Name);

            var percentage = (double)(SearchEngines.Count - _remaining.Count) / SearchEngines.Count * 100;

            SubtitleSearchProgressChanged.Fire(this, e.Data, percentage, _remaining);

            if (_remaining.Count == 0)
            {
                SubtitleSearchDone.Fire(this);
            }
        }

        /// <summary>
        /// Called when a subtitle search has encountered an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleSubtitleSearchError(object sender, EventArgs<string, Exception> e)
        {
            _remaining.Remove((sender as SubtitleSearchEngine).Name);

            SubtitleSearchError.Fire(this, e.First, e.Second);

            if (_remaining.Count == 0)
            {
                SubtitleSearchDone.Fire(this);
            }
        }
    }
}
