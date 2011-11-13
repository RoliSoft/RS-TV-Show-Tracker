namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Linq;

    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;
    using RoliSoft.TVShowTracker.Tables;

    /// <summary>
    /// Provides support for searching videos on Tube+.
    /// </summary>
    public class TubePlus : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Tube+";
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
                return "http://tubeplus.me/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/tubeplus.png";
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
                return 7;
            }
        }

        /// <summary>
        /// Searches for videos on SideReel.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// URL of the video.
        /// </returns>
        public override string Search(Episode ep)
        {
            var g = new Google().Search("intitle:{0} intitle:\"S{1:00}E{2:00}\" site:tubeplus.me/player/".FormatWith(ep.Show.Name, ep.Season, ep.Number)).ToList();

            if (g.Count != 0)
            {
                return g[0].URL;
            }
            else
            {
                throw new OnlineVideoNotFoundException("No videos could be found on Tube+ using Google." + Environment.NewLine + "You can try to use Tube+'s internal search engine.", "Open Tube+ search page", "http://tubeplus.me/search/tv-shows/{0}/0/".FormatWith(Uri.EscapeUriString(ShowNames.Parser.Normalize(ep.Show.Name))));
            }
        }
    }
}
