namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Represents a subtitle search engine.
    /// </summary>
    public abstract class SubtitleSearchEngine
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
        /// Occurs when a subtitle search is done.
        /// </summary>
        public event EventHandler<EventArgs<IEnumerable<Subtitle>>> SubtitleSearchDone;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, string>> SubtitleSearchError;

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public abstract IEnumerable<Subtitle> Search(string query);

        private Thread _job;

        /// <summary>
        /// Searches for subtitles on the service asynchronously.
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
                        SubtitleSearchDone.Fire(this, list);
                    }
                    catch (ThreadAbortException ex)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        SubtitleSearchError.Fire(this, "There was an error while searching for subtitles.", ex.Message);
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
