namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Anime News Network pages.
    /// </summary>
    [TestFixture]
    public class AnimeNewsNetwork : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Anime News Network";
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
                return "http://www.animenewsnetwork.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/animenewsnetwork.png";
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
                return Utils.DateTimeToVersion("2012-07-31 6:34 PM");
            }
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var list = Utils.GetXML("http://cdn.animenewsnetwork.com/encyclopedia/api.xml?anime=~" + Utils.EncodeURL(name));

            foreach (var show in list.Descendants("anime"))
            {
                int eps;
                if (!int.TryParse(GetDescendantItemValue(show, "info", "type", "Number of episodes"), out eps) || eps == 0)
                {
                    continue;
                }

                var id = new ShowID();

                id.Title    = GetDescendantItemValue(show, "info", "type", "Main title");
                id.Language = "en";
                id.ID       = show.Attribute("id").Value;
                id.URL      = Site + "encyclopedia/anime.php?id=" + id.ID;
                id.Cover    = GetDescendantItemAttribute(show, "info", "type", "Picture", "src");

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
            var summary = Utils.GetXML("http://cdn.animenewsnetwork.com/encyclopedia/api.xml?anime=" + id);
            var show    = new TVShow();
            
            show.Title       = GetDescendantItemValue(summary, "info", "type", "Main title");
            show.Description = GetDescendantItemValue(summary, "info", "type", "Plot Summary");
            show.Cover       = GetDescendantItemAttribute(summary, "info", "type", "Picture", "src");
            show.Airing      = !Regex.IsMatch(GetDescendantItemValue(summary, "info", "type", "Vintage"), " to ");
            show.AirTime     = "20:00";
            show.Language    = "en";
            show.URL         = Site + "encyclopedia/anime.php?id=" + id;
            show.Episodes    = new List<TVShow.Episode>();

            var runtxt = Regex.Match(GetDescendantItemValue(summary, "info", "type", "Running time"), "([0-9]+)");
            show.Runtime = runtxt.Success
                         ? int.Parse(runtxt.Groups[1].Value)
                         : 30;

            var genre = GetDescendantItemValues(summary, "info", "type", "Genres");
            if (genre.Count() != 0)
            {
                foreach (var gen in genre)
                {
                    show.Genre += gen.ToUppercaseFirst() + ", ";
                }

                show.Genre = show.Genre.TrimEnd(", ".ToCharArray());
            }

            var listing = Utils.GetHTML(Site + "encyclopedia/anime.php?id=" + id + "&page=25");
            var nodes   = listing.DocumentNode.SelectNodes("//table[@class='episode-list']/tr");

            if (nodes == null)
            {
                return show;
            }

            foreach (var node in nodes)
            {
                var epnr = Regex.Match(node.GetTextValue("td[@class='n'][1]") ?? string.Empty, "([0-9]+)");
                if (!epnr.Success)
                {
                    continue;
                }

                var ep = new TVShow.Episode();

                ep.Season = 1;
                ep.Number = epnr.Groups[1].Value.ToInteger();
                ep.Title  = HtmlEntity.DeEntitize(node.GetTextValue("td[@valign='top'][1]/div[1]").Trim());
                ep.URL    = show.URL + "&page=25";

                DateTime dt;
                ep.Airdate = DateTime.TryParse(node.GetTextValue("td[@class='d'][1]/div") ?? string.Empty, out dt)
                           ? dt
                           : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            if (show.Episodes.Count != 0)
            {
                show.AirDay = show.Episodes.Last().Airdate.DayOfWeek.ToString();
            }

            return show;
        }

        /// <summary>
        /// Gets the value of the descendant node which has a specified attribute.
        /// </summary>
        /// <param name="xdoc">The XML document.</param>
        /// <param name="descendant">The targeted descendant nodes.</param>
        /// <param name="attribute">The attribute name.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns>
        /// Value of the searched node.
        /// </returns>
        private static string GetDescendantItemValue(XContainer xdoc, string descendant, string attribute, string attributeValue)
        {
            try
            {
                return xdoc.Descendants(descendant).First(i => i.Attribute(attribute).Value == attributeValue).Value;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the value of the second specified attribute of the descendant node which has a specified attribute. If you didn't understand what I just said, look at the fucking source.
        /// </summary>
        /// <param name="xdoc">The XML document.</param>
        /// <param name="descendant">The targeted descendant nodes.</param>
        /// <param name="attribute">The attribute name.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="secondAttribute">The second attribute whose value to return.</param>
        /// <returns>
        /// Value of the searched node's second attribute.
        /// </returns>
        private static string GetDescendantItemAttribute(XContainer xdoc, string descendant, string attribute, string attributeValue, string secondAttribute)
        {
            try
            {
                return xdoc.Descendants(descendant).First(i => i.Attribute(attribute).Value == attributeValue).Attribute(secondAttribute).Value;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets a list of values of the descendant node which has a specified attribute.
        /// </summary>
        /// <param name="xdoc">The XML document.</param>
        /// <param name="descendant">The targeted descendant nodes.</param>
        /// <param name="attribute">The attribute name.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns>
        /// Values of the searched node.
        /// </returns>
        private static IEnumerable<string> GetDescendantItemValues(XContainer xdoc, string descendant, string attribute, string attributeValue)
        {
            var list = new List<XElement>();

            try
            {
                list = xdoc.Descendants(descendant).Where(i => i.Attribute(attribute).Value == attributeValue).ToList();
            }
            catch
            {
                yield break;
            }

            foreach (var item in list)
            {
                yield return item.Value;
            }
        }

        /// <summary>
        /// Gets a list of values of the second specified attribute of the descendant node which has a specified attribute. If you didn't understand what I just said, look at the fucking source.
        /// </summary>
        /// <param name="xdoc">The XML document.</param>
        /// <param name="descendant">The targeted descendant nodes.</param>
        /// <param name="attribute">The attribute name.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="secondAttribute">The second attribute whose value to return.</param>
        /// <returns>
        /// Values of the searched node's second attribute.
        /// </returns>
        private static IEnumerable<string> GetDescendantItemAttributes(XContainer xdoc, string descendant, string attribute, string attributeValue, string secondAttribute)
        {
            var list = new List<XElement>();

            try
            {
                list = xdoc.Descendants(descendant).Where(i => i.Attribute(attribute).Value == attributeValue).ToList();
            }
            catch
            {
                yield break;
            }

            foreach (var item in list)
            {
                var str = string.Empty;

                try
                {
                    str = item.Attribute(secondAttribute).Value;
                }
                catch
                {
                    continue;
                }

                yield return str;
            }
        }
    }
}
