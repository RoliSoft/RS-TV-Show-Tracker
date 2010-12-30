namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for searching videos on BBC iPlayer.
    /// </summary>
    public class BBCiPlayer : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Searches for videos on BBC iPlayer.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">This field is not used here.</param>
        /// <exception cref="OnlineVideoNotFoundException">No video was found.</exception>
        public override string Search(string name, string episode, object extra = null)
        {
            var g = Utils.Google(String.Format("intitle:{0} intitle:\"{1}\" site:bbc.co.uk/iplayer/episode/", name, Regex.Replace(episode, "S0?([0-9]{1,2})E0?([0-9]{1,2})", "Series $1 Episode $2", RegexOptions.IgnoreCase)));

            if (!string.IsNullOrWhiteSpace(g))
            {
                return g;
            }
            else
            {
                throw new OnlineVideoNotFoundException("No videos could be found on iPlayer using Google." + Environment.NewLine + "You can try to use iPlayer's internal search engine.", "Open iPlayer search page", "http://www.bbc.co.uk/iplayer/search?q=" + Uri.EscapeUriString(name + " " + Regex.Replace(episode, "S0?([0-9]{1,2})E0?([0-9]{1,2})", "Series $1 Episode $2", RegexOptions.IgnoreCase)));
            }
        }
    }
}
