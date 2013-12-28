namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Newtonsoft.Json;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for using the official TMDb API.
    /// </summary>
    [TestFixture]
    public class Freebase : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Freebase";
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
                return "http://www.freebase.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/freebase.png";
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
                return Utils.DateTimeToVersion("2013-12-28 5:42 AM");
            }
        }

        private const string Key = "";

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var json = (dynamic)JsonConvert.DeserializeObject(Utils.GetFastURL("https://www.googleapis.com/freebase/v1/search?query=" + Utils.EncodeURL(name) + "&filter=" + Utils.EncodeURL("(all type:/tv/tv_program)") + "&output=" + Utils.EncodeURL("(/common/topic/image /tv/tv_program/air_date_of_first_episode)")));

            if (json["result"] == null || json["result"].Count == 0)
            {
                yield break;
            }

            foreach (var show in json["result"])
            {
                var id = new ShowID(this);

                id.ID       = ((string)show["mid"]).Substring(3);
                id.URL      = Site + "m/" + id.ID;
                id.Title    = (string)show["name"] + (show["output"]["/tv/tv_program/air_date_of_first_episode"]["/tv/tv_program/air_date_of_first_episode"] != null ? " (" + ((string)show["output"]["/tv/tv_program/air_date_of_first_episode"]["/tv/tv_program/air_date_of_first_episode"][0]).Substring(0, 4) + ")" : string.Empty);
                id.Cover    = show["output"]["/common/topic/image"]["/common/topic/image"] != null ? "https://usercontent.googleapis.com/freebase/v1/image" + (string)show["output"]["/common/topic/image"]["/common/topic/image"][0]["mid"] + "?maxwidth=2048" : null;
                id.Language = "en";
                
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
            var query = "[{" +
                            "\"mid\":\"/m/" + id + "\"," +
                            "\"type\":\"/tv/tv_program\"," +
                            "\"name\":null," +
                            "\"air_date_of_first_episode\":{" +
                                "\"value\":null," +
                                "\"optional\":true" +
                            "}," +
                            "\"/common/topic/image\":[{" +
                                "\"mid\":null," +
                                "\"optional\":true" +
                            "}]," +
                            "\"/common/topic/description\":[{" +
                                "\"value\":null," +
                                "\"optional\":true" +
                            "}]," +
                            "\"episode_running_time\":[{" +
                                "\"value\":null," +
                                "\"optional\":true" +
                            "}]," +
                            "\"currently_in_production\":{" +
                                "\"value\":null," +
                                "\"optional\":true" +
                            "}," +
                            "\"original_network\":[{" +
                                "\"network\":null," +
                                "\"optional\":true" +
                            "}]," +
                            "\"genre\":[{" +
                                "\"name\":null," +
                                "\"optional\":true" +
                            "}]," +
                            "\"episodes\":[{" +
                                "\"mid\":null," +
                                "\"name\":null," +
                                "\"season_number\":null," +
                                "\"episode_number\":null," +
                                "\"air_date\":{" +
                                    "\"value\":null," +
                                    "\"optional\":true" +
                                "}," +
                                "\"/common/topic/image\":[{" +
                                    "\"mid\":null," +
                                    "\"optional\":true" +
                                "}]," +
                                "\"/common/topic/description\":[{" +
                                    "\"value\":null," +
                                    "\"optional\":true" +
                                "}]," +
                                "\"limit\":65535" +
                            "}]" +
                        "}]";
            var json = (dynamic)JsonConvert.DeserializeObject(Utils.GetFastURL("https://www.googleapis.com/freebase/v1/mqlread?query=" + Utils.EncodeURL(query)));
            var main = json["result"][0];
            
            var show = new TVShow();

            show.Title       = (string)main["name"];
            show.Source      = GetType().Name;
            show.SourceID    = id;
            show.Description = main["/common/topic/description"].Count > 0 ? (string)main["/common/topic/description"][0]["value"] : null;
            show.Cover       = main["/common/topic/image"].Count > 0 ? "https://usercontent.googleapis.com/freebase/v1/image" + (string)main["/common/topic/image"][0]["mid"] + "?maxwidth=2048" : null;
            show.Airing      = main["currently_in_production"] != null && (bool)main["currently_in_production"]["value"];
            show.Runtime     = main["episode_running_time"].Count > 0 ? (int)main["episode_running_time"][0]["value"] : 30;
            show.Network     = main["original_network"].Count > 0 ? (string)main["original_network"][0]["value"] : null;
            show.Language    = "en";
            show.URL         = Site.TrimEnd("/".ToCharArray()) + (string)main["mid"];
            show.Episodes    = new List<Episode>();

            foreach (var genre in main["genre"])
            {
                show.Genre += (string)genre["name"] + ", ";
            }

            show.Genre = show.Genre.TrimEnd(", ".ToCharArray());

            if (string.IsNullOrWhiteSpace(show.Description))
            {
                var desc = (dynamic)JsonConvert.DeserializeObject(Utils.GetFastURL("https://www.googleapis.com/freebase/v1/topic/m/" + id + "?filter=/common/topic/description&limit=1"));

                if (desc["property"] != null && desc["property"]["/common/topic/description"] != null && desc["property"]["/common/topic/description"]["values"].Count > 0)
                {
                    show.Description = (string)desc["property"]["/common/topic/description"]["values"][0]["value"];
                }
            }

            foreach (var episode in main["episodes"])
            {
                if (episode["season_number"] == null || episode["episode_number"] == null) continue;

                var ep = new Episode();

                ep.Season  = (int)episode["season_number"];
                ep.Number  = (int)episode["episode_number"];
                ep.Title   = (string)episode["name"];
                ep.Summary = episode["/common/topic/description"].Count > 0 ? (string)episode["/common/topic/description"][0]["value"] : null;
                ep.Picture = episode["/common/topic/image"].Count > 0 ? "https://usercontent.googleapis.com/freebase/v1/image" + episode["/common/topic/image"][0]["mid"] + "?maxwidth=2048" : null;
                ep.URL     = Site.TrimEnd("/".ToCharArray()) + (string)episode["mid"];

                DateTime dt;
                ep.Airdate = DateTime.TryParseExact(episode["air_date"] != null ? (string)episode["air_date"]["value"] : string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                            ? dt
                            : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            show.Episodes = show.Episodes.OrderBy(e => e.Number + (e.Season * 1000)).ToList();

            if (show.Episodes.Count != 0)
            {
                show.AirDay = show.Episodes.Last().Airdate.DayOfWeek.ToString();
            }

            return show;
        }
    }
}
