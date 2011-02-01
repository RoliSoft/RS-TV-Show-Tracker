namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for the TVDB XML API.
    /// </summary>
    [TestFixture]
    public class TVDB : Guide
    {
        private const string Key = "AB576C5FF150A8EE";

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id)
        {
            var info = XDocument.Load("http://www.thetvdb.com/api/" + Key + "/series/" + id + "/all/en.xml");
            var show = new TVShow
                {
                    Title       = info.GetValue("SeriesName"),
                    Genre       = info.GetValue("Genre").Trim('|').Replace("|", ", "),
                    Description = info.GetValue("Overview"),
                    Cover       = info.GetValue("poster"),
                    Airing      = !Regex.IsMatch(info.GetValue("Status"), "(Canceled|Ended)"),
                    AirTime     = info.GetValue("Airs_Time"),
                    AirDay      = info.GetValue("Airs_DayOfWeek"),
                    Network     = info.GetValue("Network"),
                    Runtime     = info.GetValue("Runtime").ToInteger(),
                    Episodes    = new List<TVShow.Episode>()
                };

            if (!string.IsNullOrWhiteSpace(show.Cover))
            {
                show.Cover = "http://thetvdb.com/banners/_cache/" + show.Cover;
            }

            show.Runtime = show.Runtime == 30
                           ? 20
                           : show.Runtime == 50 || show.Runtime == 60
                             ? 40
                             : show.Runtime;

            foreach (var ep in info.Descendants("Episode"))
            {
                DateTime dt;
                int sn;
                string pic;

                if ((sn = ep.GetValue("SeasonNumber").ToInteger()) == 0)
                {
                    continue;
                }

                show.Episodes.Add(new TVShow.Episode
                    {
                        Season  = sn,
                        Number  = ep.GetValue("EpisodeNumber").ToInteger(),
                        Airdate = DateTime.TryParse(ep.GetValue("FirstAired"), out dt)
                                  ? dt
                                  : Utils.UnixEpoch,
                        Title   = ep.GetValue("EpisodeName"),
                        Summary = ep.GetValue("Overview"),
                        Picture = !string.IsNullOrWhiteSpace(pic = ep.GetValue("filename"))
                                  ? "http://thetvdb.com/banners/_cache/" + pic
                                  : null
                    });
            }

            return show;
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override string GetID(string name)
        {
            var txt = XDocument.Load("http://www.thetvdb.com/api/GetSeries.php?seriesname=" + Uri.EscapeUriString(ShowNames.Normalize(name)));
            var id  = txt.Descendants("Series").First().Element("seriesid").Value;
            return id;
        }
    }
}
