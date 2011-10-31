namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;

    /// <summary>
    /// Provides support for searching videos on SideReel.
    /// </summary>
    public class SideReel : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "SideReel";
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
                return "http://www.sidereel.com/";
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
                return Site + "images/favicon.ico";
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
            var g = new Google().Search("intitle:Watch {0} online site:sidereel.com".FormatWith(name)).ToList();

            if (g.Count == 0)
            {
                throw new OnlineVideoNotFoundException("No videos could be found on SideReel using Google." + Environment.NewLine + "You can try to use SideReel's internal search engine.", "Open SideReel search page", "http://www.sidereel.com/_television/search?q=" + Uri.EscapeUriString(ShowNames.Parser.Normalize(name)));
            }

            var id = Regex.Match(g[0].URL, @"sidereel\.com/([^/$]+)", RegexOptions.IgnoreCase);

            return "http://www.sidereel.com/{0}/{1}/search".FormatWith(id.Groups[1].Value, ShowNames.Parser.ExtractEpisode(episode, "season-{0}/episode-{1}"));
        }
    }
}
