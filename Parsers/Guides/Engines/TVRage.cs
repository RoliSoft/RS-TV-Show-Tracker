namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for the TVRage XML API.
    /// </summary>
    [TestFixture]
    public class TVRage : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TVRage";
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
                return "http://tvrage.com/";
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
                return "/RSTVShowTracker;component/Images/tvrage.png";
            }
        }

        private const string Key = "d3fGaRW6adgVgvVNMLa4";

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var list = XDocument.Load("http://services.tvrage.com/myfeeds/search.php?key={0}&show={1}".FormatWith(Key, Uri.EscapeUriString(name)));

            foreach (var show in list.Descendants("show"))
            {
                var id = new ShowID();

                id.ID       = show.GetValue("showid");
                id.Title    = show.GetValue("name");
                id.Cover    = "http://images.tvrage.com/shows/4/{0}.jpg".FormatWith(id.ID);
                id.Language = "en";
                id.URL      = show.GetValue("link");

                yield return id;
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id, string language = "en")
        {
            var info = XDocument.Load("http://services.tvrage.com/myfeeds/showinfo.php?key={0}&sid={1}".FormatWith(Key, id));
            var list = XDocument.Load("http://services.tvrage.com/myfeeds/episode_list.php?key={0}&sid={1}".FormatWith(Key, id));
            var show = new TVShow();

            show.Title       = info.GetValue("showname");
            show.Genre       = info.Descendants("genre").Aggregate(string.Empty, (current, g) => current + (g.Value + ", ")).TrimEnd(", ".ToCharArray());
            show.Description = info.GetValue("summary");
            show.Cover       = info.GetValue("image");
            show.Airing      = !Regex.IsMatch(info.GetValue("status"), "(Canceled|Ended)");
            show.AirTime     = info.GetValue("airtime");
            show.AirDay      = info.GetValue("airday");
            show.Network     = info.GetValue("network");
            show.URL         = info.GetValue("showlink");
            show.Episodes    = new List<TVShow.Episode>();

            show.Runtime = info.GetValue("runtime").ToInteger();
            show.Runtime = show.Runtime == 30
                           ? 20
                           : show.Runtime == 50 || show.Runtime == 60
                             ? 40
                             : show.Runtime;

            foreach (var node in list.Descendants("episode"))
            {
                int sn;
                try { sn = node.Parent.Attribute("no").Value.ToInteger(); } catch { continue; }

                var ep = new TVShow.Episode();

                ep.Season  = sn;
                ep.Number  = node.GetValue("seasonnum").ToInteger();
                ep.Title   = node.GetValue("title");
                ep.Summary = node.GetValue("summary");
                ep.Picture = node.GetValue("screencap");
                ep.URL     = node.GetValue("link");

                DateTime dt;
                ep.Airdate = DateTime.TryParse(node.GetValue("airdate"), out dt)
                             ? dt
                             : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            return show;
        }
    }
}
