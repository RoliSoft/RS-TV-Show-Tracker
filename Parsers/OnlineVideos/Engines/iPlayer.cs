namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;

    using RoliSoft.TVShowTracker.Parsers.Guides;

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
                return Utils.DateTimeToVersion("2012-03-11 2:28 AM");
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
            var html  = Utils.GetHTML("http://www.bbc.co.uk/iplayer/search?q=" + Utils.EncodeURL(ep.Show.Title + " Series " + ep.Season + " Episode " + ep.Number));
            var links = html.DocumentNode.SelectNodes("//h3/a/span[@class='title']");
            
            if (links != null)
            {
                foreach (var link in links)
                {
                    if (link.InnerText.Contains(" - Series " + ep.Season + "Episode " + ep.Number))
                    {
                        return "http://www.bbc.co.uk" + link.GetNodeAttributeValue("..", "href");
                    }
                }
            }

            throw new OnlineVideoNotFoundException("No matching videos were found.", "Open iPlayer search page", "http://www.bbc.co.uk/iplayer/search?q=" + Utils.EncodeURL(ep.Show.Title + " Series " + ep.Season + " Episode " + ep.Number));
        }
    }
}
