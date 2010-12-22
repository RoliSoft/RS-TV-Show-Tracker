namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides support for scraping Subscene.
    /// </summary>
    public class Subscene : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Subscene";
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
                return "http://subscene.com/favicon.png";
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override List<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML("http://subscene.com/s.aspx?q=" + Uri.EscapeUriString(query));
            var subs = html.DocumentNode.SelectNodes("//a[@class='a1']");

            if (subs == null)
            {
                return null;
            }

            return subs.Where(node => ShowNames.IsMatch(query, node.SelectSingleNode("span[2]").InnerText.Trim()))
                   .Select(node => new Subtitle
                   {
                       Site         = Name,
                       Release      = node.SelectSingleNode("span[2]").InnerText.Trim(),
                       Language     = Addic7ed.ParseLanguage(node.SelectSingleNode("span[1]").InnerText.Trim()),
                       URL          = "http://subscene.com" + node.GetAttributeValue("href", string.Empty),
                       IsLinkDirect = false
                   }).ToList();
        }
    }
}
