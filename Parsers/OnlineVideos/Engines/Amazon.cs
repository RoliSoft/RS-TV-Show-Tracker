namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;

    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Provides support for searching videos on Amazon Instant Video.
    /// </summary>
    public class Amazon : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Amazon Instant Video";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://www.amazon.com/Instant-Video/b/?ie=UTF8&node=2858778011";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>
        /// The icon location.
        /// </value>
        public override string Icon
        {
            get
            {
                return "pack://application:,,,/RSTVShowTracker;component/Images/amazon.png";
            }
        }

        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public override int Index
        {
            get
            {
                return 4;
            }
        }

        /// <summary>
        /// Searches for videos on BBC iPlayer.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// URL of the video.
        /// </returns>
        public override string Search(Episode ep)
        {
            return "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + ep.Show.Name + " intitle:\"Season " + ep.Season + ", Episode " + ep.Number + "\" site:amazon.com");
        }
    }
}
