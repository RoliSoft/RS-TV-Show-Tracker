namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping Serienjunkies.
    /// </summary>
    [TestFixture]
    public class Serienjunkies : DownloadSearchEngine, ILinkExpander<Link>
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Serienjunkies";
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
                return "http://serienjunkies.org/";
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
                return Site + "media/img/favicon.ico";
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
                return Utils.DateTimeToVersion("2012-04-20 12:34 AM");
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
                return Types.DirectHTTP;
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
                return new ExternalDownloader();
            }
        }

        /// <summary>
        /// A list of TV shows which are differently noted on Serienjunkies.
        /// </summary>
        public static Dictionary<string, string> AlternativeNames = new Dictionary<string, string>
            {
                { "house", "dr-house" }
            };

        /// <summary>
        /// Gets a value indicating whether this site is deprecated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if deprecated; otherwise, <c>false</c>.
        /// </value>
        public override bool Deprecated
        {
            get
            {
                // "Sorry, this website is not available in your country."
                return true;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var parts = ShowNames.Parser.Split(query);
                parts[0] = Regex.Replace(parts[0].ToLower(), @"[^a-z0-9\s]", string.Empty);
                parts[0] = Regex.Replace(parts[0], @"\s+", "-");

            if (AlternativeNames.ContainsKey(parts[0]))
            {
                parts[0] = AlternativeNames[parts[0]];
            }

            Regex episode = null;

            if (parts.Length != 1)
            {
                episode = ShowNames.Parser.GenerateEpisodeRegexes(query);
            }

            var html  = Utils.GetHTML(Site + "serie/" + parts[0] + "/");
            var links = html.DocumentNode.SelectNodes("//strong[text()='Download:']/..");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var title = node.GetTextValue("strong").Trim();

                if (episode != null && !episode.IsMatch(title))
                {
                    continue;
                }

                var permlnk = node.GetNodeAttributeValue("../../h2/a[1]", "href");
                var slinks  = node.SelectNodes("a");

                var infoNode = node.PreviousSibling;
                var testFor  = new[] { "Dauer:", "Größe:", "Sprache:" };
                var sizergx  = Match.Empty;
                while (infoNode != null)
                {
                    if (testFor.Contains((infoNode.GetTextValue("strong") ?? string.Empty).Trim()))
                    {
                        sizergx = Regex.Match(infoNode.InnerText ?? string.Empty, @"(\d+(?:[,\.]\d+)?)\s*([KMG]B)", RegexOptions.IgnoreCase);
                        break;
                    }

                    infoNode = infoNode.PreviousSibling;
                }

                var size = string.Empty;
                if (sizergx.Success)
                {
                    size = sizergx.Groups[1].Value + " " + sizergx.Groups[2].Value.ToUpper();
                }

                foreach (var snode in slinks)
                {
                    var link = new Link(this);

                    link.Release = title;
                    link.InfoURL = permlnk;
                    link.FileURL = snode.GetAttributeValue("href");
                    link.Size    = size;
                    link.Quality = FileNames.Parser.ParseQuality(link.Release);

                    var hostrgx = Regex.Match(snode.GetTextValue("./following-sibling::text()[1]") ?? string.Empty, @"\s([a-z0-9\-]+)\.");
                    if (hostrgx.Success)
                    {
                        link.Infos = hostrgx.Groups[1].Value.ToUppercaseFirst();
                    }

                    yield return link;
                }
            }
        }

        /// <summary>
        /// Extracts the direct links from the supplied link.
        /// </summary>
        /// <param name="link">The protected link.</param>
        /// <returns>Direct links.</returns>
        public string ExpandLinks(Link link)
        {
            var html = Utils.GetHTML(link.FileURL);

            var captcha = html.DocumentNode.GetNodeAttributeValue("//td/img[contains(@src, '/secure/')]", "src");
            var session = html.DocumentNode.GetNodeAttributeValue("//input[@name='s']", "value");

            if (captcha == null && session == null)
            {
                goto extract;
            }

            var sectext = string.Empty;

            MainWindow.Active.Run(() =>
                {
                    var cw  = new CaptchaWindow(Name, "http://download.serienjunkies.org" + captcha, 100, 60);
                    var res = cw.ShowDialog();

                    if (res.HasValue && res.Value)
                    {
                        sectext = cw.Solution;
                    }
                });

            if (string.IsNullOrWhiteSpace(sectext))
            {
                return link.FileURL;
            }

            html = Utils.GetHTML(link.FileURL, "s=" + session + "&c=" + sectext + "&action=Download");

        extract:
            var links = html.DocumentNode.SelectNodes("//form[contains(@action, '/go-')]");

            if (links == null)
            {
                return link.FileURL;
            }

            var urls = string.Empty;

            foreach (var node in links)
            {
                var url = node.GetAttributeValue("action");

                if (string.IsNullOrWhiteSpace(url)) continue;

                Utils.GetHTML(
                    url,
                    request:  r => r.AllowAutoRedirect = false,
                    response: r => url = r.Headers[HttpResponseHeader.Location]
                );

                urls += url + "\0";
            }

            return urls.TrimEnd('\0');
        }
    }
}
