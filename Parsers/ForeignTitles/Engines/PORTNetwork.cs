namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    using System.Linq;

    using WebSearch.Engines;

    /// <summary>
    /// Provides support for extracting titles off any port.xx site.
    /// </summary>
    public abstract class PORTNetwork : ForeignTitleEngine
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
                return "PORT." + _tld;
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
                return "http://port." + _tld + "/";
            }
        }

        private readonly string _tld;

        /// <summary>
        /// Initializes a new instance of the <see cref="PORTNetwork"/> class.
        /// </summary>
        /// <param name="country">The TLD to query.</param>
        protected PORTNetwork(string country)
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
            var q = new Scroogle().Search("\"(\"" + name + "\")\" site:port." + _tld + " inurl:/pls/fi/films.film_page?i_film_id -inurl:i_episode_id").ToList();

            if (q.Count != 0)
            {
                return q[0].Title.Replace(" - PORT." + _tld, string.Empty);
            }

            return null;
        }
    }
}
