namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Linq;

    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;
    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Provides support for searching videos on BBC iPlayer.
    /// </summary>
    public class BBCiPlayer : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "BBC iPlayer";
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
                return "http://www.bbc.co.uk/iplayer/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/bbc.png";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-11-12 6:01 PM");
            }
        }

        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public override float Index
        {
            get
            {
                return 1;
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
            var g = new Scroogle().Search("intitle:{0} intitle:\"Series {1} Episode {2}\" site:bbc.co.uk/iplayer/episode/".FormatWith(ep.Show.Name, ep.Season, ep.Number)).ToList();

            if (g.Count != 0)
            {
                return g[0].URL;
            }
            else
            {
                throw new OnlineVideoNotFoundException("No videos could be found on iPlayer using Google." + Environment.NewLine + "You can try to use iPlayer's internal search engine.", "Open iPlayer search page", "http://www.bbc.co.uk/iplayer/search?q=" + Uri.EscapeUriString(ep.Show.Name + " Series " + ep.Season + " Episode " + ep.Number));
            }
        }
    }
}
