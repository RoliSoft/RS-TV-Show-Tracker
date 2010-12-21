namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos
{
    using System;
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
        /// Searches for videos on the service asynchronously.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">The extra information required for the selected parser.</param>
        public void SearchAsync(string name, string episode, object extra = null)
        {
            new Task(() =>
                {
                    try
                    {
                        var url = Search(name, episode, extra);
                        OnlineSearchDone.Fire(this, name + " " + episode, url);
                    }
                    catch (OnlineVideoNotFoundException ex)
                    {
                        OnlineSearchError.Fire(this, name + " " + episode, ex.Message, new Tuple<string, string, string>(ex.LinkTitle, ex.LinkUrl, null));
                    }
                    catch (Exception ex)
                    {
                        OnlineSearchError.Fire(this, name + " " + episode, "There was an error while searching for the video on this service.", new Tuple<string, string, string>(null, null, ex.Message));
                    }
                }).Start();
        }

        /// <summary>
        /// Represents an error which occurs when the search was successful, but no video was found.
        /// </summary>
        public class OnlineVideoNotFoundException : Exception
        {
            /// <summary>
            /// Gets the link title.
            /// </summary>
            /// <value>The link title.</value>
            public string LinkTitle { get; private set; }

            /// <summary>
            /// Gets the link URL.
            /// </summary>
            /// <value>The link URL.</value>
            public string LinkUrl { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="OnlineVideoNotFoundException"/> class.
            /// </summary>
            /// <param name="message">The message.</param>
            /// <param name="linkTitle">The link title.</param>
            /// <param name="linkUrl">The link URL.</param>
            public OnlineVideoNotFoundException(string message, string linkTitle = null, string linkUrl = null) : base(message)
            {
                LinkTitle = linkTitle;
                LinkUrl   = linkUrl;
            }
        }
    }
}