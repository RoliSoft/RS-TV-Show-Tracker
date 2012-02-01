namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for using the official IMDb mobile API.
    /// </summary>
    [TestFixture]
    public class IMDb : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "IMDb";
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
                return "http://www.imdb.com/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "pack://application:,,,/RSTVShowTracker;component/Images/imdb.png";
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
                return Utils.DateTimeToVersion("2011-09-01 2:07 PM");
            }
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var json = Utils.GetJSON("http://app.imdb.com/find?api=v1&locale=en_US&q=" + Uri.EscapeUriString(name));

            foreach (var result in json["data"]["results"])
            {
                foreach (var show in result["list"])
                {
                    if (show["type"] != "tv_series") continue;

                    var id = new ShowID();

                    id.URL      = "http://www.imdb.com/title/" + (string)show["tconst"] + "/";
                    id.ID       = ((string)show["tconst"]).Substring(2);
                    id.Title    = (string)show["title"] + " (" + (string)show["year"] + ")";
                    id.Cover    = (string)show["image"]["url"];
                    id.Language = "en";

                    yield return id;
                }
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id, string language = "en")
        {
            var main = Utils.GetJSON("http://app.imdb.com/title/maindetails?api=v1&locale=en_US&tconst=tt" + id);
            var show = new TVShow();

            show.Title       = (string)main["data"]["title"];
            show.Description = (string)main["data"]["plot"]["outline"];
            show.Cover       = (string)main["data"]["image"]["url"];
            show.Airing      = (string)main["data"]["year_end"] == "????";
            show.Runtime     = (int)main["data"]["runtime"]["time"] / 60;
            show.Language    = "en";
            show.URL         = "http://www.imdb.com/title/tt" + id + "/";
            show.Episodes    = new List<TVShow.Episode>();

            foreach (var genre in main["data"]["genres"])
            {
                show.Genre += (string)genre + ", ";
            }

            show.Genre = show.Genre.TrimEnd(", ".ToCharArray());

            var epdata = Utils.GetJSON("http://app.imdb.com/title/episodes?api=v1&locale=en_US&tconst=tt" + id);

            foreach (var season in epdata["data"]["seasons"])
            {
                var snr = int.Parse((string)season["token"]);
                var enr = 0;

                foreach (var episode in season["list"])
                {
                    if (episode["type"] != "tv_episode") continue;
                    
                    var ep = new TVShow.Episode();

                    ep.Season = snr;
                    ep.Number = ++enr;
                    ep.Title  = (string)episode["title"];
                    ep.URL    = "http://www.imdb.com/title/" + (string)episode["tconst"] + "/";
                    
                    DateTime dt;
                    ep.Airdate = DateTime.TryParseExact((string)episode["release_date"]["normal"] ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                               ? dt
                               : Utils.UnixEpoch;

                    show.Episodes.Add(ep);
                }
            }

            if (show.Episodes.Count != 0)
            {
                show.AirDay = show.Episodes.Last().Airdate.DayOfWeek.ToString();
            }

            return show;
        }
    }
}
