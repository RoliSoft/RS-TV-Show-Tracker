namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos
{
    using System;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// Provides support for searching videos on Hulu.
    /// </summary>
    public class Hulu : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Searches for videos on Hulu.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">The title of the episode.</param>
        /// <exception cref="OnlineVideoSearchEngine.OnlineVideoNotFoundException">No video was found.</exception>
        public override string Search(string name, string episode, object extra = null)
        {
            var g = Utils.Google(String.Format("\"{0}\" \"{1}\" \"{2}\" site:hulu.com/watch/", ShowNames.Normalize(name), extra as string, Regex.Replace(episode, "S0?([0-9]{1,2})E0?([0-9]{1,2})", "Season $1 Ep. $2", RegexOptions.IgnoreCase)));

            if (g != string.Empty)
            {
                return g;
            }

            var xml  = Utils.GetURL("http://www.hulu.com/feed/search?fs=0&query=" + Uri.EscapeUriString("show:" + ShowNames.Normalize(name) + Regex.Replace(episode, "S0?([0-9]{1,2})E0?([0-9]{1,2})", " season:$1 episode:$2", RegexOptions.IgnoreCase) + " type:episode") + "&sort_by=relevance&st=1");
            var xdoc = XDocument.Parse(xml);
            var link = xdoc.XPathSelectElement("//item/link[1]");

            if (link != null)
            {
                return link.Value;
            }
            else
            {
                throw new OnlineVideoNotFoundException("No videos could be found on Hulu using Google or Hulu's internal search engine." + Environment.NewLine + "You can try to use Hulu's internal search engine anyways.", "Open Hulu search page", "http://www.hulu.com/search?query=" + Uri.EscapeUriString("show:" + ShowNames.Normalize(name) + Regex.Replace(episode, "S0?([0-9]{1,2})E0?([0-9]{1,2})", " season:$1 episode:$2", RegexOptions.IgnoreCase) + " type:episode") + "&st=1");
            }
        }
    }
}
