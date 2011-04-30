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
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "AniDB";
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
                return "http://anidb.net/";
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
                return "/RSTVShowTracker;component/Images/anidb.png";
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
                return Languages.List.Keys.ToArray();
            }
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var list = XDocument.Load("http://anisearch.outrance.pl/?task=search&query=" + Uri.EscapeUriString(name));

            foreach (var show in list.Descendants("anime"))
            {
                var id = new ShowID();
                
                try
                {
                    id.Title = show.Descendants("title").Where(t => t.Attribute("lang").Value == "en").First().Value;
                    id.Language = "en";
                }
                catch
                {
                    try
                    {
                        id.Title = show.Descendants("title").Where(t => t.Attribute("lang").Value == language).First().Value;
                        id.Language = language;
                    }
                    catch
                    {
                        id.Title = show.GetValue("title");
                        id.Language = "en";
                    }
                }

                id.ID  = show.Attribute("aid").Value;
                id.URL = "http://anidb.net/perl-bin/animedb.pl?show=anime&aid=" + id.ID;

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
            // XDocument will fail, because the response is gzipped; Utils.GetURL supports gzipped content
            var req  = Utils.GetURL("http://api.anidb.net:9001/httpapi?request=anime&client=rstvshowtracker&clientver=2&protover=1&aid=" + id);
            var info = XDocument.Load(new StringReader(req));
            var show = new TVShow();

            try { show.Title = info.Descendants("title").Where(t => t.Attributes().First().Value == language).First().Value; }
            catch
            {
                try   { show.Title = info.Descendants("title").Where(t => t.Attributes().First().Value == "en").First().Value; }
                catch { show.Title = info.GetValue("title"); }
            }

            show.Genre       = info.Descendants("category").Aggregate(string.Empty, (current, g) => current + (g.GetValue("name") + ", ")).TrimEnd(", ".ToCharArray());
            show.Description = info.GetValue("description");
            show.Airing      = string.IsNullOrWhiteSpace(info.GetValue("enddate"));
            show.Runtime     = info.GetValue("length").ToInteger();
            show.TimeZone    = "Tokyo Standard Time";
            show.Language    = language;
            show.URL         = "http://anidb.net/perl-bin/animedb.pl?show=anime&aid=" + id;
            show.Episodes    = new List<TVShow.Episode>();

            show.Cover = info.GetValue("picture");
            if (!string.IsNullOrWhiteSpace(show.Cover))
            {
                show.Cover = "http://img7.anidb.net/pics/anime/" + show.Cover;
            }

            foreach (var node in info.Descendants("episode"))
            {
                try { node.GetValue("epno").ToInteger(); } catch { continue; }

                var ep = new TVShow.Episode();

                ep.Season = 1;
                ep.Number = node.GetValue("epno").ToInteger();
                ep.URL    = "http://anidb.net/perl-bin/animedb.pl?show=ep&eid=" + node.Attribute("id").Value;

                try { ep.Title = node.Descendants("title").Where(t => t.Attributes().First().Value == language).First().Value; }
                catch
                {
                    try   { ep.Title = node.Descendants("title").Where(t => t.Attributes().First().Value == "en").First().Value; }
                    catch { ep.Title = node.GetValue("title"); }
                }

                DateTime dt;
                ep.Airdate = DateTime.TryParse(node.GetValue("airdate"), out dt)
                             ? dt
                             : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            show.Episodes = show.Episodes.OrderBy(e => e.Number).ToList();

            return show;
        }
    }
}
