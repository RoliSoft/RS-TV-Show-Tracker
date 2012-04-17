namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Xml.XPath;

    using Tables;

    /// <summary>
    /// Provides support for searching videos on Hulu.
    /// </summary>
    public class Hulu : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Hulu";
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
                return "http://www.hulu.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/hulu.png";
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
                return Utils.DateTimeToVersion("2012-03-11 2:11 AM");
            }
        }

        /// <summary>
        /// Gets a number representing where should the engine be placed in the list.
        /// </summary>
        public override float Index
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Searches for videos on Hulu.
        /// </summary>
        /// <param name="ep">The episode.</param>
        /// <returns>
        /// URL of the video.
        /// </returns>
        public override string Search(Episode ep)
        {
            var xdoc = Utils.GetXML("http://www.hulu.com/feed/search?fs=0&query=" + Utils.EncodeURL("show:" + ep.Show.Name + " season:" + ep.Season + " episode:" + ep.Number + " type:episode") + "&sort_by=relevance&st=1");
            var link = xdoc.XPathSelectElement("//item/link[1]");

            if (link != null)
            {
                return link.Value;
            }

            throw new OnlineVideoNotFoundException("No matching videos were found.", "Open Hulu search page", "http://www.hulu.com/search?query=" + Utils.EncodeURL("show:" + ShowNames.Parser.CleanTitleWithEp(ep.Show.Name) + " season:" + ep.Season + " episode:" + ep.Number + " type:episode") + "&st=1");
        }
    }
}
