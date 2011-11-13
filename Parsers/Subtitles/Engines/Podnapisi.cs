namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping Podnapisi.
    /// </summary>
    [TestFixture]
    public class Podnapisi : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Podnapisi";
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
                return "http://simple.podnapisi.net/";
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
                return Utils.DateTimeToVersion("2011-08-07 1:29 AM");
            }
        }

        /// <summary>
        /// Returns an <c>IDownloader</c> object which can be used to download the URLs provided by this parser.
        /// </summary>
        /// <value>The downloader.</value>
        public override IDownloader Downloader
        {
            get
            {
                return new PodnapisiDownloader();
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            string show, season = string.Empty, episode = string.Empty;

            if (ShowNames.Regexes.Numbering.IsMatch(query))
            {
                show     = Uri.EscapeUriString(ShowNames.Parser.Split(query)[0]);
                var epnr = ShowNames.Parser.ExtractEpisode(query);
                season   = epnr.Season.ToString();
                episode  = epnr.Episode.ToString();
            }
            else
            {
                show = Uri.EscapeUriString(query);
            }

            var html = Utils.GetHTML(Site + "en/ppodnapisi/search?tbsl=3&asdp=1&sK={0}&sM=&sJ=0&sO=asc&sS=time&submit=Search&sTS={1}&sTE={2}&sY=&sR=&sT=1".FormatWith(show, season, episode));
            var subs = html.DocumentNode.SelectNodes("//tr[@class='a' or @class='b']");
            
            if (subs == null)
            {
                yield break;
            }

            var pg2 = html.DocumentNode.GetNodeAttributeValue("//span[@class='strani']/a[1]", "href");

            if (!string.IsNullOrWhiteSpace(pg2))
            {
                var html2 = Utils.GetHTML(Site.TrimEnd('/') + HtmlEntity.DeEntitize(pg2));
                var subs2 = html2.DocumentNode.SelectNodes("//tr[@class='a' or @class='b']");

                if (subs2 != null)
                {
                    foreach (var sub2 in subs2)
                    {
                        subs.Add(sub2);
                    }
                }
            }
            
            foreach (var node in subs)
            {
                var sub = new Subtitle(this);

                sub.Release = node.GetNodeAttributeValue("td[1]/span/span", "title");
                if (!string.IsNullOrWhiteSpace(sub.Release) && Regex.IsMatch(sub.Release, @"[^A-Za-z]"))
                {
                    sub.Release = sub.Release.Trim().Split(' ')[0];
                }
                else
                {
                    sub.Release = node.GetTextValue("td[1]/a[2]") + " ";

                    var epinfo = node.GetTextValue("td[1]/span[@class='opis']").Replace("&nbsp;", string.Empty);
                    if (Regex.IsMatch(epinfo, @"[^A-Za-z]"))
                    {
                        sub.Release += epinfo;
                    }
                    else
                    {
                        sub.Release += node.GetTextValue("td[1]/span[@class='opis'][2]").Replace("&nbsp;", string.Empty);
                    }

                    sub.Release = Regex.Replace(sub.Release, @"\s*Season: (\d{1,2}) Episode: (\d{1,2}),?\s*", m => string.Format(" S{0:00}E{1:00}", m.Groups[1].Value.ToInteger(), m.Groups[2].Value.ToInteger()));
                }

                if (node.SelectSingleNode("td[1]/img[contains(@src, 'h.gif')]") != null)
                {
                    sub.Release += "/HD";
                }

                if (node.SelectSingleNode("td[1]/img[contains(@src, 'n.gif')]") != null)
                {
                    sub.Release = Subscene.HINotationRegex.Replace(sub.Release, string.Empty);
                    sub.HINotations = true;
                }

                sub.Language = Languages.Parse(node.GetNodeAttributeValue("td[1]/a/img", "title"));
                sub.URL      = Site.TrimEnd('/') + node.GetNodeAttributeValue("td[1]/a[2]", "href");

                yield return sub;
            }
        }
    }
}
