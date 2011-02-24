namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods to search for episodes on online services.
    /// </summary>
    public abstract class OnlineVideoSearchEngine
    {
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
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">The extra information required for the selected parser.</param>
        /// <returns>URL of the video.</returns>
        public abstract string Search(string name, string episode, object extra = null);

        /// <summary>
        /// Gets the search thread.
        /// </summary>
        /// <value>The search thread.</value>
        public Thread SearchThread { get; internal set; }

        /// <summary>
        /// Searches for videos on the service asynchronously.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">The extra information required for the selected parser.</param>
        public void SearchAsync(string name, string episode, object extra = null)
        {
            SearchThread = new Thread(() =>
                {
                    try
                    {
                        var url = Search(name, episode, extra);
                        OnlineSearchDone.Fire(this, name + " " + episode, url);
                    }
                    catch (OnlineVideoNotFoundException ex)
                    {
                        OnlineSearchError.Fire(this, name + " " + episode, ex.Message, new Tuple<string, string, string>(ex.LinkTitle, ex.LinkURL, null));
                    }
                    catch (Exception ex)
                    {
                        OnlineSearchError.Fire(this, name + " " + episode, "There was an error while searching for the video on this service.", new Tuple<string, string, string>(null, null, ex.Message));
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