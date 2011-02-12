namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for the AniDB HTTP API.
    /// </summary>
    [TestFixture]
    public class AniDB : Guide
    {
        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id)
        {
            // XDocument will fail, because the response is gzipped; Utils.GetURL supports gzipped content
            var req  = Utils.GetURL("http://api.anidb.net:9001/httpapi?request=anime&client=rstvshowtracker&clientver=2&protover=1&aid={0}".FormatWith(id));
            var info = XDocument.Load(new StringReader(req));
            var show = new TVShow();

            show.Title       = info.GetValue("title");
            show.Genre       = info.Descendants("category").Aggregate(string.Empty, (current, g) => current + (g.GetValue("name") + ", ")).TrimEnd(", ".ToCharArray());
            show.Description = info.GetValue("description");
            show.Airing      = string.IsNullOrWhiteSpace(info.GetValue("enddate"));
            show.Runtime     = info.GetValue("length").ToInteger();
            show.Episodes    = new List<TVShow.Episode>();

            var picture = info.GetValue("picture");
            if (!string.IsNullOrWhiteSpace(picture))
            {
                show.Cover = "http://img7.anidb.net/pics/anime/" + picture;
            }

            foreach (var node in info.Descendants("episode"))
            {
                try { node.GetValue("epno").ToInteger(); } catch { continue; }

                var ep = new TVShow.Episode();

                ep.Season = 1;
                ep.Number = node.GetValue("epno").ToInteger();

                try   { ep.Title = node.Descendants("title").Where(t => t.Attributes().First().Value == "en").First().Value; }
                catch { ep.Title = node.GetValue("title"); }

                DateTime dt;
                ep.Airdate = DateTime.TryParse(node.GetValue("airdate"), out dt)
                             ? dt
                             : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            show.Episodes = show.Episodes.OrderBy(e => e.Number).ToList();

            return show;
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override string GetID(string name)
        {
            var info = XDocument.Load("http://anisearch.outrance.pl/?task=search&query=" + Uri.EscapeUriString(name));
            return info.GetAttributeValue("anime", "aid");
        }
    }
}
