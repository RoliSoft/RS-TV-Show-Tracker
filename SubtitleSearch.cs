namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        public event SubtitleSearchDone SubtitleSearchDone;

        /// <summary>
        /// Occurs when a subtitle search progress has changed.
        /// </summary>
        public event SubtitleSearchProgressChanged SubtitleSearchProgressChanged;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event SubtitleSearchError SubtitleSearchError;

        /// <summary>
        /// Gets or sets the search engines.
        /// </summary>
        /// <value>The search engines.</value>
        public List<SubtitleSearchEngine> SearchEngines { get; set; }

        private volatile List<string> _remaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleSearch"/> class.
        /// </summary>
        public SubtitleSearch()
        {
            SearchEngines = new List<SubtitleSearchEngine>(
                typeof(SubtitleSearchEngine).GetDerivedTypes()
                                            .Select(type => Activator.CreateInstance(type) as SubtitleSearchEngine));

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
        /// Called when a subtitle search is done.
        /// </summary>
        /// <param name="name">The name of the engine.</param>
        /// <param name="subtitles">The found subtitles.</param>
        private void SingleSubtitleSearchDone(string name, List<SubtitleSearchEngine.Subtitle> subtitles)
        {
            _remaining.Remove(name);

            var percentage = (double)(SearchEngines.Count - _remaining.Count) / SearchEngines.Count * 100;

            if (SubtitleSearchProgressChanged != null)
            {
                SubtitleSearchProgressChanged(subtitles, percentage, _remaining);
            }

            if (_remaining.Count == 0 && SubtitleSearchDone != null)
            {
                SubtitleSearchDone();
            }
        }

        /// <summary>
        /// Called when a subtitle search has encountered an error.
        /// </summary>
        /// <param name="name">The name of the engine.</param>
        /// <param name="message">The error message.</param>
        /// <param name="detailed">The detailed error message.</param>
        private void SingleSubtitleSearchError(string name, string message, string detailed = null)
        {
            _remaining.Remove(name);

            if (SubtitleSearchError != null)
            {
                SubtitleSearchError(message, detailed);
            }
        }
    }
}
