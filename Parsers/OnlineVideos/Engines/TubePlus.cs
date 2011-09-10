namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Linq;

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
        /// Searches for videos on SideReel.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">This field is not used here.</param>
        /// <exception cref="OnlineVideoNotFoundException">No video was found.</exception>
        public override string Search(string name, string episode, object extra = null)
        {
            var g = WebSearch.Engines.Google("intitle:{0} intitle:\"{1}\" site:tubeplus.me/player/".FormatWith(name, episode)).ToList();

            if (g.Count != 0)
            {
                return g[0].URL;
            }
            else
            {
                throw new OnlineVideoNotFoundException("No videos could be found on Tube+ using Google." + Environment.NewLine + "You can try to use Tube+'s internal search engine.", "Open Tube+ search page", "http://tubeplus.me/search/tv-shows/{0}/0/".FormatWith(Uri.EscapeUriString(ShowNames.Parser.Normalize(name))));
            }
        }
    }
}
