namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos
{
    using System;

    /// <summary>
    /// Represents an error which occurs when the search was successful, but no video was found.
    /// </summary>
    public class OnlineVideoNotFoundException : Exception
    {
        /// <summary>
        /// Gets the link title.
        /// </summary>
        /// <value>The link title.</value>
        public string LinkTitle { get; internal set; }

        /// <summary>
        /// Gets the link URL.
        /// </summary>
        /// <value>The link URL.</value>
        public string LinkURL { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineVideoNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="linkTitle">The link title.</param>
        /// <param name="linkUrl">The link URL.</param>
        public OnlineVideoNotFoundException(string message, string linkTitle = null, string linkUrl = null) : base(message)
        {
            LinkTitle = linkTitle;
            LinkURL   = linkUrl;
        }
    }
}
