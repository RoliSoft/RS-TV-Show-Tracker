namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for using the official TMDb API.
    /// </summary>
    [TestFixture]
    public class TMDb : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TMDb";
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
                return "http://www.themoviedb.org/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/tmdb.png";
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
                return Utils.DateTimeToVersion("2013-12-27 8:42 PM");
            }
        }

        private const string Key = "815b5890a9fb4b6a08d7d18d35fde57b";

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var json = Utils.GetJSON("http://api.themoviedb.org/3/search/tv?api_key=" + Key + "&query=" + Utils.EncodeURL(name));

            if (json["total_results"] == 0)
            {
                yield break;
            }

            foreach (var show in json["results"])
            {
                var id = new ShowID(this);

                id.ID       = ((int)show["id"]).ToString();
                id.URL      = Site + "tv/" + id.ID;
                id.Title    = (string)show["name"] + (show["first_air_date"] != null ? " (" + ((string)show["first_air_date"]).Substring(0, 4) + ")" : string.Empty);
                id.Language = "en";

                if (show["poster_path"] != null)
                {
                    id.Cover = "http://image.tmdb.org/t/p/original" + (string)show["poster_path"];
                }
                else if (show["backdrop_path"] != null)
                {
                    show.Cover = "http://image.tmdb.org/t/p/original" + (string)show["backdrop_path"];
                }

                yield return id;
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
            var main = Utils.GetJSON("http://api.themoviedb.org/3/tv/" + id + "?api_key=" + Key);
            var show = new TVShow();

            show.Title       = (string)main["name"];
            show.Source      = GetType().Name;
            show.SourceID    = id;
            show.Description = (string)main["overview"];
            show.Airing      = (bool)main["in_production"];
            show.Runtime     = (main["episode_run_time"] == null || main["episode_run_time"].Count == 0) ? 30 : (int)main["episode_run_time"][0];
            show.Network     = (main["networks"] == null || main["networks"].Count == 0) ? null : (string)main["networks"][0]["name"];
            show.Language    = "en";
            show.URL         = Site + "tv/" + id;
            show.Episodes    = new List<Episode>();

            if (main["poster_path"] != null)
            {
                show.Cover = "http://image.tmdb.org/t/p/original" + (string)main["poster_path"];
            }
            else if (main["backdrop_path"] != null)
            {
                show.Cover = "http://image.tmdb.org/t/p/original" + (string)main["backdrop_path"];
            }

            foreach (var genre in main["genres"])
            {
                show.Genre += (string)genre["name"] + ", ";
            }

            show.Genre = show.Genre.TrimEnd(", ".ToCharArray());

            var atr = new List<string>();

            foreach (var sn in main["seasons"])
            {
                if (sn["season_number"] == 0)
                {
                    continue;
                }

                atr.Add("season/" + (int)sn["season_number"]);
            }

            var epdata = Utils.GetJSON("http://api.themoviedb.org/3/tv/" + id + "?api_key=" + Key + "&append_to_response=" + string.Join(",", atr));

            foreach (var sn in atr)
            {
                if (epdata[sn]["season_number"] == null) continue;

                var snr = (int)epdata[sn]["season_number"];

                foreach (var episode in epdata[sn]["episodes"])
                {
                    if (episode["episode_number"] == null) continue;

                    var ep = new Episode();

                    ep.Season  = snr;
                    ep.Number  = (int)episode["episode_number"];
                    ep.Title   = (string)episode["name"];
                    ep.Summary = (string)episode["overview"];
                    ep.URL     = show.URL + "/season/" + ep.Season + "/episode/" + ep.Number;

                    if (episode["still_path"] != null)
                    {
                        ep.Picture = "http://image.tmdb.org/t/p/original" + (string)episode["still_path"];
                    }

                    DateTime dt;
                    ep.Airdate = DateTime.TryParseExact((string)episode["air_date"] ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
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
