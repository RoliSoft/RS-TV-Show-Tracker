namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;

    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Provides support for searching videos on iTunes.
    /// </summary>
    public class iTunes : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "iTunes";
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
                return "http://www.apple.com/itunes/charts/tv-shows/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/apple.png";
            }
        }

        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public override int Index
        {
            get
            {
                return 3;
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
            return "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Uri.EscapeUriString("intitle:" + ep.Show.Name + " intitle:\"season " + ep.Season + "\" site:itunes.apple.com inurl:/tv-season/");
        }
    }
}
