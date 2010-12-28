namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for scraping tvtorrents.com.
    /// </summary>
    [Parser("RoliSoft", "2010-12-09 2:48 AM")]
    public class TvTorrents : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TvTorrents";
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
                return "http://www.tvtorrents.com/favicon.ico";
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
                return true;
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
            var show  = ShowNames.Split(query)[0];
            var html  = Utils.GetHTML("http://tvtorrents.com/loggedin/search.do?search=" + Uri.EscapeUriString(show), cookies: Cookies);
            var links = html.DocumentNode.SelectNodes("//table[2]/tr/td[3]");

            if (links == null)
            {
                yield break;
            }

            var hash    = Regex.Match(html.DocumentNode.InnerHtml, "hash='(.*?)';").Groups[1].Value;
            var digest  = Regex.Match(html.DocumentNode.InnerHtml, "digest='(.*?)';").Groups[1].Value;
            var episode = ShowNames.ExtractEpisode(query, 1);
            
            foreach (var node in links)
            {
                if(!string.IsNullOrWhiteSpace(episode) && !node.SelectSingleNode("a").InnerText.StartsWith(episode))
                {
                    continue;
                }

                yield return new Link
                    {
                        Site    = Name,
                        Release = Regex.Replace(node.InnerText, @"\b([0-9]{1,2})x([0-9]{1,2})\b", new MatchEvaluator(me => "S" + me.Groups[1].Value.ToInteger().ToString("00") + "E" + me.Groups[2].Value.ToInteger().ToString("00")), RegexOptions.IgnoreCase),
                        URL     = "http://torrent.tvtorrents.com/FetchTorrentServlet?info_hash=" + node.SelectSingleNode("a").GetAttributeValue("href", string.Empty).Split('=').Last() + "&digest=" + digest + "&hash=" + hash,
                        Size    = node.SelectSingleNode("../td[5]").GetAttributeValue("title", string.Empty).Replace("Torrent is ", String.Empty).Replace("b", "B"),
                        Quality = node.SelectSingleNode("a").InnerText.Contains("(720p .mkv)") ? Qualities.HDTV720p : node.SelectSingleNode("a").InnerText.Contains(" .mkv)") ? Qualities.HRx264 : Qualities.HDTVXviD,
                        Type    = Types.Torrent
                   };
            }
        }
    }
}
