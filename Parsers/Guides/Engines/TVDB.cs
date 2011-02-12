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
            var info = XDocument.Load("http://www.thetvdb.com/api/{0}/series/{1}/all/en.xml".FormatWith(Key, id));
            var show = new TVShow();

            show.Title       = info.GetValue("SeriesName");
            show.Genre       = info.GetValue("Genre").Trim('|').Replace("|", ", ");
            show.Description = info.GetValue("Overview");
            show.Airing      = !Regex.IsMatch(info.GetValue("Status"), "(Canceled|Ended)");
            show.AirTime     = info.GetValue("Airs_Time");
            show.AirDay      = info.GetValue("Airs_DayOfWeek");
            show.Network     = info.GetValue("Network");
            show.Episodes    = new List<TVShow.Episode>();

            show.Cover = info.GetValue("poster");
            if (!string.IsNullOrWhiteSpace(show.Cover))
            {
                show.Cover = "http://thetvdb.com/banners/_cache/" + show.Cover;
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
                if ((sn = node.GetValue("SeasonNumber").ToInteger()) == 0) { continue; }

                var ep = new TVShow.Episode();

                ep.Season  = sn;
                ep.Number  = node.GetValue("EpisodeNumber").ToInteger();
                ep.Title   = node.GetValue("EpisodeName");
                ep.Summary = node.GetValue("Overview");

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

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override string GetID(string name)
        {
            var txt = XDocument.Load("http://www.thetvdb.com/api/GetSeries.php?seriesname=" + Uri.EscapeUriString(ShowNames.Tools.Normalize(name)));
            var id  = txt.Descendants("Series").First().Element("seriesid").Value;
            return id;
        }
    }
}
