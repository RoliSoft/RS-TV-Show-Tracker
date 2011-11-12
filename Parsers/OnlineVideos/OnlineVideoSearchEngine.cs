namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos
{
    using System;
    using System.Threading;

    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Provides methods to search for episodes on online services.
    /// </summary>
    public abstract class OnlineVideoSearchEngine : ParserEngine
    {
        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public virtual float Index
        {
            get
            {
                return short.MaxValue;
            }
        }

        /// <summary>
        /// Occurs when an online search is done.
        /// </summary>
        public event EventHandler<EventArgs<string, string>> OnlineSearchDone;

        /// <summary>
        /// Occurs when an online search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, string, Tuple<string, string, string>>> OnlineSearchError;

        /// <summary>
        /// Searches for videos on the service.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// URL of the video.
        /// </returns>
        public abstract string Search(Episode ep);

        /// <summary>
        /// Gets the search thread.
        /// </summary>
        /// <value>The search thread.</value>
        public Thread SearchThread { get; internal set; }

        /// <summary>
        /// Searches for videos on the service asynchronously.
        /// </summary>
        /// <param name="ep">The episode.</param>
        public void SearchAsync(Episode ep)
        {
            SearchThread = new Thread(() =>
                {
                    try
                    {
                        var url = Search(ep);
                        OnlineSearchDone.Fire(this, "{0} S{1:00}E{2:00}".FormatWith(ep.Show.Name, ep.Season, ep.Number), url);
                    }
                    catch (OnlineVideoNotFoundException ex)
                    {
                        OnlineSearchError.Fire(this, "{0} S{1:00}E{2:00}".FormatWith(ep.Show.Name, ep.Season, ep.Number), ex.Message, new Tuple<string, string, string>(ex.LinkTitle, ex.LinkURL, null));
                    }
                    catch (Exception ex)
                    {
                        OnlineSearchError.Fire(this, "{0} S{1:00}E{2:00}".FormatWith(ep.Show.Name, ep.Season, ep.Number), "There was an error while searching for the video on this service.", new Tuple<string, string, string>(null, null, ex.Message));
                    }
                });
            SearchThread.Start();
        }

        /// <summary>
        /// Cancels the asynchronous search.
        /// </summary>
        public void CancelSearch()
        {
            try { SearchThread.Abort(); } catch { }
        }
    }
}