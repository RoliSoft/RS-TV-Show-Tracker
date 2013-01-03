namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for the TVDB XML API.
    /// </summary>
    [TestFixture]
    public class TVDB : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "The TVDB";
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
                return "http://thetvdb.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/thetvdb.png";
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
                return Utils.DateTimeToVersion("2011-07-19 3:06 AM");
            }
        }

        /// <summary>
        /// Gets the list of supported languages.
        /// </summary>
        /// <value>The list of supported languages.</value>
        public override string[] SupportedLanguages
        {
            get
            {
                return new[]
                    {
                        "en",
                        "hu",
                        "da",
                        "fi",
                        "nl",
                        "de",
                        "it",
                        "es",
                        "fr",
                        "pl",
                        "el",
                        "tr",
                        "ru",
                        "he",
                        "ja",
                        "pt",
                        "zh",
                        "cs",
                        "sl",
                        "hr",
                        "ko",
                        "sv",
                        "no"
                    };
            }
        }

        private const string Key = "AB576C5FF150A8EE";

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var list = Utils.GetXML("http://www.thetvdb.com/api/GetSeries.php?seriesname={0}&language={1}".FormatWith(Utils.EncodeURL(name), language), timeout: 120000);
            var prev = new List<string>();

            foreach (var show in list.Descendants("Series"))
            {
                var id = new ShowID();

                id.ID       = show.GetValue("seriesid");
                id.Title    = show.GetValue("SeriesName");
                id.Language = show.GetValue("language");
                id.URL      = "http://thetvdb.com/?tab=series&id=" + id.ID;
                id.Cover    = show.GetValue("banner");

                if (!string.IsNullOrWhiteSpace(id.Cover))
                {
                    id.Cover = "http://thetvdb.com/banners/_cache/" + id.Cover;
                }

                if (!prev.Contains(id.ID))
                {
                    prev.Add(id.ID);

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
            var info = Utils.GetXML("http://www.thetvdb.com/api/{0}/series/{1}/all/{2}.xml".FormatWith(Key, id, language), timeout: 120000);
            var show = new TVShow();

            show.Title       = info.GetValue("SeriesName");
            show.Genre       = info.GetValue("Genre").Trim('|').Replace("|", ", ");
            show.Description = info.GetValue("Overview");
            show.Airing      = !Regex.IsMatch(info.GetValue("Status"), "(Canceled|Ended)");
            show.AirTime     = info.GetValue("Airs_Time");
            show.AirDay      = info.GetValue("Airs_DayOfWeek");
            show.Network     = info.GetValue("Network");
            show.Language    = language;
            show.URL         = "http://thetvdb.com/?tab=series&id=" + info.GetValue("id");
            show.Episodes    = new List<Episode>();

            if (show.Network.StartsWith("BBC") || show.Network.StartsWith("ITV"))
            {
                show.TimeZone = "GMT+0";
            }

            show.Cover = info.GetValue("poster");
            if (!string.IsNullOrWhiteSpace(show.Cover))
            {
                show.Cover = "http://thetvdb.com/banners/" + show.Cover;
            }

            show.Runtime = info.GetValue("Runtime").ToInteger();
            show.Runtime = show.Runtime == 30
                           ? 20
                           : show.Runtime == 50 || show.Runtime == 60
                             ? 40
                             : show.Runtime;

            foreach (var node in info.Descendants("Episode"))
            {
                int sn;
                if ((sn = node.GetValue("SeasonNumber").ToInteger()) == 0)
                {
                    continue;
                }

                var ep = new Episode();

                ep.Season  = sn;
                ep.Number  = node.GetValue("EpisodeNumber").ToInteger();
                ep.Title   = node.GetValue("EpisodeName");
                ep.Summary = node.GetValue("Overview");
                ep.URL     = "http://thetvdb.com/?tab=episode&seriesid={0}&seasonid={1}&id={2}".FormatWith(node.GetValue("seriesid"), node.GetValue("seasonid"), node.GetValue("id"));

                ep.Picture = node.GetValue("filename");
                if (!string.IsNullOrWhiteSpace(ep.Picture))
                {
                    ep.Picture = "http://thetvdb.com/banners/_cache/" + ep.Picture;
                }

                DateTime dt;
                ep.Airdate = DateTime.TryParse(node.GetValue("FirstAired"), out dt)
                           ? dt
                           : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            return show;
        }
    }
}
