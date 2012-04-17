namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    using System;
    using System.Linq;
    using System.Text;

    using HtmlAgilityPack;

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
                return Utils.DateTimeToVersion("2012-01-06 2:52 AM");
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
            name = ShowNames.Regexes.Year.Replace(name, string.Empty);
            name = ShowNames.Regexes.Countries.Replace(name, string.Empty);
            name = name.Trim();

            var html  = Utils.GetHTML("http://port." + _tld + "/pls/ci/films.film_list?i_area_id=17&i_text=" + Utils.EncodeURL(name), encoding: Encoding.GetEncoding("iso-8859-2"));
            var head  = html.DocumentNode.SelectSingleNode("//h1[@class='blackbigtitle']");
            var shows = html.DocumentNode.SelectNodes("//a[contains(@href, 'films.film_page')]");

            if (head == null && shows == null && html.DocumentNode.SelectSingleNode("//input[@name='i_text']") != null)
            {
                html  = Utils.GetHTML("http://port." + _tld + "/pls/ci/cinema.film_creator?i_film_creator=1&i_text=" + Utils.EncodeURL(name), encoding: Encoding.GetEncoding("iso-8859-2"));
                head  = html.DocumentNode.SelectSingleNode("//h1[@class='blackbigtitle']");
                shows = html.DocumentNode.SelectNodes("//a[contains(@href, 'films.film_page')]");
            }

            if (head != null)
            {
                var title = HtmlEntity.DeEntitize(head.InnerText).Trim();

                if (title.First() != '(' && title.Last() != ')')
                {
                    return title;
                }

                return null;
            }

            if (shows != null)
            {
                var title = HtmlEntity.DeEntitize(shows[0].InnerText).Trim();

                if (title.First() != '(' && title.Last() != ')')
                {
                    return title;
                }

                return null;
            }

            return null;
        }
    }
}
