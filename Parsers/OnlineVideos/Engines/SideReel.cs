namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;
    using RoliSoft.TVShowTracker.Tables;

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
                return "pack://application:,,,/RSTVShowTracker;component/Images/sidereel.png";
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
                return 6;
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
            var g = new Scroogle().Search("intitle:Watch {0} online site:sidereel.com".FormatWith(ep.Show.Name)).ToList();

            if (g.Count == 0)
            {
                throw new OnlineVideoNotFoundException("No videos could be found on SideReel using Google." + Environment.NewLine + "You can try to use SideReel's internal search engine.", "Open SideReel search page", "http://www.sidereel.com/_television/search?q=" + Uri.EscapeUriString(ShowNames.Parser.CleanTitleWithEp(ep.Show.Name)));
            }

            var id = Regex.Match(g[0].URL, @"sidereel\.com/([^/$]+)", RegexOptions.IgnoreCase);

            return "http://www.sidereel.com/{0}/season-{1}/episode-{2}/search".FormatWith(id.Groups[1].Value, ep.Season, ep.Number);
        }
    }
}
