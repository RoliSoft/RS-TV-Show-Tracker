namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    /// <summary>
    /// Provides support for extracting titles off any imdb.xx site.
    /// </summary>
    public abstract class IMDbInternational : ForeignTitleEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name
        {
            get
            {
                return "IMDb." + _tld;
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>
        /// The site location.
        /// </value>
        public override string Site
        {
            get
            {
                return "http://www.imdb." + _tld + "/";
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
                return Utils.DateTimeToVersion("2012-01-06 3:08 AM");
            }
        }

        private readonly string _tld;

        /// <summary>
        /// Initializes a new instance of the <see cref="IMDbInternational"/> class.
        /// </summary>
        /// <param name="country">The TLD to query.</param>
        protected IMDbInternational(string country)
        {
            _tld = country;
        }

        /// <summary>
        /// Searches the foreign title of the specified show.
        /// </summary>
        /// <param name="name">The name of the show to search for.</param>
        /// <returns>The foreign title or <c>null</c>.</returns>
        public override string Search(string name)
        {
            var html  = Utils.GetHTML("http://www.imdb." + _tld + "/search/title?title_type=tv_series&title=" + Utils.EncodeURL(name), headers: new Dictionary<string, string> { { "Accept-Language", Language } });
            var shows = html.DocumentNode.SelectNodes("//td[@class='title']");

            if (shows != null)
            {
                var attr = shows[0].GetNodeAttributeValue("../td[@class='image']//img", "title");

                if (attr != null)
                {
                    return Regex.Replace(HtmlEntity.DeEntitize(attr), @"\s+\((?:[A-Z]{2,3}\s|\d{4}\s)?TV Series\)", string.Empty);
                }
            }

            return null;
        }
    }
}
