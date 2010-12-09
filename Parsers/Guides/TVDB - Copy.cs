namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// Provides support for the TVDB XML API.
    /// </summary>
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
                    Title       = info.Descendants("Series").First().Element("SeriesName").Value,
                    Genre       = info.Descendants("Series").First().Element("Genre").Value.Trim('|').Replace("|", ", "),
                    Actors      = info.Descendants("Series").First().Element("Actors").Value.Trim('|').Split("|".ToCharArray()).ToList(),
                    Description = info.Descendants("Series").First().Element("Overview").Value,
                    Cover       = "http://thetvdb.com/banners/_cache/" + info.Descendants("Series").First().Element("poster").Value,
                    Airing      = !Regex.IsMatch(info.Descendants("Series").First().Element("Status").Value, "(Canceled|Ended)"),
                    AirTime     = info.Descendants("Series").First().Element("Airs_Time").Value,
                    AirDay      = info.Descendants("Series").First().Element("Airs_DayOfWeek").Value,
                    Network     = info.Descendants("Series").First().Element("Network").Value,
                    Runtime     = int.Parse(info.Descendants("Series").First().Element("Runtime").Value),
                    Episodes    = new List<TVShow.Episode>()
                };

            show.Runtime = show.Runtime == 30
                           ? 20
                           : show.Runtime == 50 || show.Runtime == 60
                             ? 40
                             : show.Runtime;

            DateTime dt;
            int sn;
            string pic;
            foreach (var ep in info.Descendants("Episode"))
            {
                if ((sn = int.Parse(ep.GetValue("SeasonNumber"))) == 0)
                {
                    continue;
                }

                show.Episodes.Add(new TVShow.Episode
                    {
                        Season  = sn,
                        Number  = int.Parse(ep.GetValue("EpisodeNumber")),
                        AirDate = DateTime.TryParse(ep.GetValue("FirstAired"), out dt)
                                  ? dt
                                  : new DateTime(1970, 1, 1, 0, 0, 0, 0),
                        Title   = ep.GetValue("EpisodeName"),
                        Summary = ep.GetValue("Overview"),
                        Picture = (pic = ep.GetValue("filename")) != null
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
            var txt = XDocument.Load("http://www.thetvdb.com/api/GetSeries.php?seriesname=" + Uri.EscapeUriString(Helpers.Normalizer.Normalize(name)));
            var id  = txt.Descendants("Series").First().Element("seriesid").Value;
            return id;
        }
    }
}
