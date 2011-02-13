namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping The Pirate Bay.
    /// </summary>
    [Parser("RoliSoft", "2011-02-13 4:46 PM"), TestFixture]
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
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://thepiratebay.org/";
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
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public override bool Private
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
            var html  = Utils.GetHTML(Site + "search/" + Uri.EscapeUriString(query) + "/0/7/0");
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/div/a");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.InnerText;
                link.FileURL = node.GetNodeAttributeValue("../../a[1]", "href");
                link.InfoURL = Site.TrimEnd('/') + node.GetAttributeValue("href");
                link.Size    = Regex.Match(node.GetTextValue("../../font"), "Size (.*?),").Groups[1].Value.Replace("&nbsp;", " ").Replace("i", string.Empty);
                link.Quality = ParseQuality(node.InnerText);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../td[3]").Trim(), node.GetTextValue("../../../td[4]").Trim())
                             + (node.GetHtmlValue("../..//img[@title='VIP']") != null ? ", VIP Uploader" : string.Empty)
                             + (node.GetHtmlValue("../..//img[@title='Trusted']") != null ? ", Trusted Uploader" : string.Empty);

                yield return link;
            }
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            release = release.Replace((char)160, '.').Replace((char)32, '.');

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
