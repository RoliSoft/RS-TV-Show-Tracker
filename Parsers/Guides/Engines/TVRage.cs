namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// Provides support for the TVRage XML API.
    /// </summary>
    public class TVRage : Guide
    {
        private const string Key = "d3fGaRW6adgVgvVNMLa4";

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id)
        {
            var info = XDocument.Load("http://services.tvrage.com/myfeeds/showinfo.php?key=" + Key + "&sid=" + id);
            var list = XDocument.Load("http://services.tvrage.com/myfeeds/episode_list.php?key=" + Key + "&sid=" + id);
            var show = new TVShow
                {
                    Title       = info.GetValue("showname"),
                    Genre       = info.Descendants("genre").Aggregate(string.Empty, (current, g) => current + (g.Value + ", ")).TrimEnd(", ".ToCharArray()),
                    Description = info.GetValue("summary"),
                    Cover       = info.GetValue("image"),
                    Airing      = !Regex.IsMatch(info.GetValue("status"), "(Canceled|Ended)"),
                    AirTime     = info.GetValue("airtime"),
                    AirDay      = info.GetValue("airday"),
                    Network     = info.GetValue("network"),
                    Runtime     = info.GetValue("runtime").ToInteger(),
                    Episodes    = new List<TVShow.Episode>()
                };

            show.Runtime = show.Runtime == 30
                           ? 20
                           : show.Runtime == 50 || show.Runtime == 60
                             ? 40
                             : show.Runtime;

            DateTime dt;
            int sn;
            foreach (var ep in list.Descendants("episode"))
            {
                try { sn = ep.Parent.Attribute("no").Value.ToInteger(); } catch { continue; }

                show.Episodes.Add(new TVShow.Episode
                    {
                        Season  = sn,
                        Number  = ep.GetValue("seasonnum").ToInteger(),
                        Airdate = DateTime.TryParse(ep.GetValue("airdate"), out dt)
                                  ? dt
                                  : Utils.UnixEpoch,
                        Title   = ep.GetValue("title"),
                        Summary = ep.GetValue("summary"),
                        Picture = ep.GetValue("screencap")
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
            var txt = Utils.GetURL("http://services.tvrage.com/tools/quickinfo.php?show=" + Uri.EscapeUriString(name));
            var id  = Regex.Match(txt, @"Show ID@([0-9]+)").Groups[1].Value;
            return id;
        }
    }
}
