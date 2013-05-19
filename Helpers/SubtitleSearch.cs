namespace RoliSoft.TVShowTracker.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

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
        public event EventHandler<EventArgs<List<SubtitleSearchEngine>>> SubtitleSearchEngineDone;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> SubtitleSearchEngineError;

        /// <summary>
        /// Occurs when a subtitle search found a new link.
        /// </summary>
        public event EventHandler<EventArgs<Subtitle>> SubtitleSearchEngineNewLink;

        /// <summary>
        /// Gets or sets the search engines.
        /// </summary>
        /// <value>The search engines.</value>
        public List<SubtitleSearchEngine> SearchEngines { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to filter search results.
        /// </summary>
        /// <value><c>true</c> if filtering is enabled; otherwise, <c>false</c>.</value>
        public bool Filter { get; set; }

        /// <summary>
        /// Gets or sets the list of languages to search for.
        /// </summary>
        /// <value>The languages to search for.</value>
        public static List<string> Langs { get; set; }

        private ConcurrentBag<SubtitleSearchEngine> _done;
        private Regex _titleRegex, _episodeRegex;
        private DateTime _start;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleSearch"/> class.
        /// </summary>
        /// <param name="engines">The engines to use for searching.</param>
        /// <param name="filter">if set to <c>true</c> the search results will be filtered.</param>
        public SubtitleSearch(IEnumerable<SubtitleSearchEngine> engines = null, IEnumerable<string> languages = null, bool filter = false)
        {
            Log.Debug("Initializing search engines...");

            SearchEngines = (engines ?? SubtitlesPage.SearchEngines).ToList();
            Langs         = (languages ?? new List<string>()).ToList();
            Filter        = filter;

            var remove = new List<SubtitleSearchEngine>();

            foreach (var engine in SearchEngines)
            {
                engine.SubtitleSearchNewLink += SingleSubtitleSearchNewLink;
                engine.SubtitleSearchDone    += SingleSubtitleSearchDone;
                engine.SubtitleSearchError   += SingleSubtitleSearchError;

                if (engine.Private)
                {
                    engine.Cookies = Utils.Decrypt(engine, Settings.Get(engine.Name + " Cookies"))[0];

                    // if requires authentication and no cookies or login information were provided, ignore the engine
                    if (string.IsNullOrWhiteSpace(engine.Cookies) && string.IsNullOrWhiteSpace(Settings.Get(engine.Name + " Login")))
                    {
                        remove.Add(engine);
                        Log.Warn(engine.Name + " is private and no login info specified.");
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
        /// Searches for subtitles on multiple services asynchronously.
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

            _done = new ConcurrentBag<SubtitleSearchEngine>();

            Log.Debug("Starting async search for " + query + "...");
            _start = DateTime.Now;


            foreach (var engine in SearchEngines.OrderBy(e => SubtitlesPage.Actives.IndexOf(e.Name)))
            {
                engine.SearchAsync(query);
            }
        }

        /// <summary>
        /// Cancels the active asynchronous searches on all services.
        /// </summary>
        public void CancelAsync()
        {
            SearchEngines.ForEach(engine =>
                {
                    engine.SubtitleSearchNewLink -= SingleSubtitleSearchNewLink;
                    engine.SubtitleSearchDone    -= SingleSubtitleSearchDone;
                    engine.SubtitleSearchError   -= SingleSubtitleSearchError;

                    engine.CancelAsync();
                });
        }

        /// <summary>
        /// Occurs when a subtitle search found a new link.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleSubtitleSearchNewLink(object sender, EventArgs<Subtitle> e)
        {
            if ((Langs != null && !Langs.Contains(e.Data.Language))
             || (Filter && !ShowNames.Parser.IsMatch(e.Data.Release, _titleRegex, _episodeRegex, false)))
            {
                Log.Trace("Dropping result " + e.Data.Release + " due to language or name filtering.");
                return;
            }

            SubtitleSearchEngineNewLink.Fire(this, e.Data);
        }


        /// <summary>
        /// Called when a subtitle search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SingleSubtitleSearchDone(object sender, EventArgs e)
        {
            _done.Add((SubtitleSearchEngine)sender);

            (sender as SubtitleSearchEngine).SubtitleSearchNewLink += SingleSubtitleSearchNewLink;
            (sender as SubtitleSearchEngine).SubtitleSearchDone    += SingleSubtitleSearchDone;
            (sender as SubtitleSearchEngine).SubtitleSearchError   += SingleSubtitleSearchError;

            SubtitleSearchEngineDone.Fire(this, SearchEngines.Except(_done).ToList());

            if (_done.Count == SearchEngines.Count)
            {
                Log.Debug("Search finished in " + (DateTime.Now - _start).TotalSeconds + "s.");
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
            _done.Add((SubtitleSearchEngine)sender);

            (sender as SubtitleSearchEngine).SubtitleSearchNewLink += SingleSubtitleSearchNewLink;
            (sender as SubtitleSearchEngine).SubtitleSearchDone    += SingleSubtitleSearchDone;
            (sender as SubtitleSearchEngine).SubtitleSearchError   += SingleSubtitleSearchError;

            Log.Warn("Error while searching on " + ((SubtitleSearchEngine)sender).Name + ".", e.Second);

            SubtitleSearchEngineError.Fire(this, e.First, e.Second);
            SubtitleSearchEngineDone.Fire(this, SearchEngines.Except(_done).ToList());

            if (_done.Count == SearchEngines.Count)
            {
                Log.Debug("Search finished in " + (DateTime.Now - _start).TotalSeconds + "s.");
                SubtitleSearchDone.Fire(this);
            }
        }
    }
}
