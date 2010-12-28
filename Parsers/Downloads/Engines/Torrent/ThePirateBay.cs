namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for scraping The Pirate Bay.
    /// </summary>
    [Parser("RoliSoft", "2010-12-09 2:34 AM")]
    public class ThePirateBay : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "The Pirate Bay";
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
                return "http://thepiratebay.org/favicon.ico";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires cookies to authenticate.
        /// </summary>
        /// <value><c>true</c> if requires cookies; otherwise, <c>false</c>.</value>
        public override bool RequiresCookies
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML("http://thepiratebay.org/search/" + Uri.EscapeUriString(query) + "/0/7/0");
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/div/a");

            if (links == null)
            {
                return null;
            }

            return links.Select(node => new Link
                   {
                       Site    = Name,
                       Release = node.InnerText,
                       URL     = node.SelectSingleNode("../../a[1]").GetAttributeValue("href", string.Empty),
                       Size    = Regex.Match(node.SelectSingleNode("../../font").InnerText, "Size (.*?),").Groups[1].Value.Replace("&nbsp;", " ").Replace("i", string.Empty),
                       Quality = ParseQuality(node.InnerText.Replace(' ', '.')),
                       Type    = Types.Torrent
                   });
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            if (IsMatch(release, @"\.1080(i|p)\.", @"\.WEB[_\-]?DL\."))
            {
                return Qualities.WebDL1080p;
            }
            if (IsMatch(release, @"\.1080(i|p)\.", @"\.BluRay\."))
            {
                return Qualities.BluRay1080p;
            }
            if (IsMatch(release, @"\.1080(i|p)\.", @"\.HDTV\."))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch(release, @"\.720p\.", @"\.WEB[_\-]?DL\."))
            {
                return Qualities.WebDL720p;
            }
            if (IsMatch(release, @"\.720p\.", @"\.BluRay\."))
            {
                return Qualities.BluRay720p;
            }
            if (IsMatch(release, @"\.720p\.", @"\.HDTV\."))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch(release, @"\.((HR|HiRes|High[_\-]?Resolution)\.|x264\-|H264)"))
            {
                return Qualities.HRx264;
            }
            if (IsMatch(release, @"\.(HDTV|PDTV|DVBRip|DVDRip)\."))
            {
                return Qualities.HDTVXviD;
            }
            if (IsMatch(release, @"\.TVRip\."))
            {
                return Qualities.TVRip;
            }
            return Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified input is matches all the specified regexes.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="regexes">The regexes.</param>
        /// <returns>
        /// 	<c>true</c> if the specified input matches all the specified regexes; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string input, params string[] regexes)
        {
            return regexes.All(regex => Regex.IsMatch(input, regex, RegexOptions.IgnoreCase));
        }
    }
}
