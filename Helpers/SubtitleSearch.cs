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
        public SubtitleSearch(IEnumerable<SubtitleSearchEngine> engines = null)
        {
            SearchEngines = (engines ?? SubtitlesPage.SearchEngines).ToList();

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
            try { _remaining.Remove((sender as SubtitleSearchEngine).Name); } catch { }

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
            try { _remaining.Remove((sender as SubtitleSearchEngine).Name); } catch { }

            SubtitleSearchError.Fire(this, e.First, e.Second);

            if (_remaining.Count == 0)
            {
                SubtitleSearchDone.Fire(this);
            }
        }
    }
}
