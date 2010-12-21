namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Occurs when a subtitle search is done on all engines.
    /// </summary>
    public delegate void SubtitleSearchDone();

    /// <summary>
    /// Occurs when a subtitle search progress has changed.
    /// </summary>
    public delegate void SubtitleSearchProgressChanged(List<SubtitleSearchEngine.Subtitle> subtitles, double percentage, List<string> remaining);

    /// <summary>
    /// Occurs when a subtitle search has encountered an error.
    /// </summary>
    public delegate void SubtitleSearchError(string message, string detailed = null);

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
        public event EventHandler<EventArgs<List<SubtitleSearchEngine.Subtitle>, double, List<string>>> SubtitleSearchProgressChanged;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, string>> SubtitleSearchError;

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
        /// Searches for subtitles on multiple services asynchronously.
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
        /// Called when a subtitle search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.Collections.Generic.List&lt;RoliSoft.TVShowTracker.Parsers.Subtitles.SubtitleSearchEngine.Subtitle&gt;&gt;"/> instance containing the event data.</param>
        private void SingleSubtitleSearchDone(object sender, EventArgs<List<SubtitleSearchEngine.Subtitle>> e)
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
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.String,System.String&gt;"/> instance containing the event data.</param>
        private void SingleSubtitleSearchError(object sender, EventArgs<string, string> e)
        {
            _remaining.Remove((sender as SubtitleSearchEngine).Name);

            SubtitleSearchError.Fire(this, e.First, e.Second);
        }
    }
}
