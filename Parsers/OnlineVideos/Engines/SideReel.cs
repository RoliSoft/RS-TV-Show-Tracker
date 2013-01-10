namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;

    using RoliSoft.TVShowTracker.Parsers.Guides;

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
                return Utils.DateTimeToVersion("2012-03-11 2:43 AM");
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
            var html = Utils.GetHTML("http://www.sidereel.com/_television/search?q=" + Utils.EncodeURL(ep.Show.Name));
            var link = html.DocumentNode.GetNodeAttributeValue("//div[@class='title']/h2/a[1]", "href");
            
            if (string.IsNullOrWhiteSpace(link))
            {
                throw new OnlineVideoNotFoundException("No matching videos were found.", "Open SideReel search page", "http://www.sidereel.com/_television/search?q=" + Utils.EncodeURL(ep.Show.Name));
            }

            return "http://www.sidereel.com/{0}/season-{1}/episode-{2}/search".FormatWith(link.Trim(" /".ToCharArray()), ep.Season, ep.Number);
        }
    }
}
