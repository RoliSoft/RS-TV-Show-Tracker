namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Provides support for searching videos on Serienjunkies.
    /// </summary>
    public class Serienjunkies : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Serienjunkies";
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
                return "http://serienjunkies.org/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/serienjunkies.png";
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
                return Utils.DateTimeToVersion("2012-04-20 1:32 AM");
            }
        }

        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public override float Index
        {
            get
            {
                return 8;
            }
        }

        /// <summary>
        /// Searches for videos on Serienjunkies.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// URL of the video.
        /// </returns>
        public override string Search(Episode ep)
        {
            var name = ep.Show.Title.ToLower();
                name = Regex.Replace(name, @"[^a-z0-9\s]", string.Empty);
                name = Regex.Replace(name, @"\s+", "-");

            if (Downloads.Engines.HTTP.Serienjunkies.AlternativeNames.ContainsKey(name))
            {
                name = Downloads.Engines.HTTP.Serienjunkies.AlternativeNames[name];
            }

            return "{0}serie/{1}/#{2},{3}".FormatWith(Site, name, ep.Season, ep.Number);
        }
    }
}
