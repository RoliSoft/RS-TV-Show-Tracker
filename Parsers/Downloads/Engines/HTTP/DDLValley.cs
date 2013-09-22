namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using HtmlAgilityPack;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;

    /// <summary>
    /// Provides support for scraping DDLValley.
    /// </summary>
    [TestFixture]
    public class DDLValley : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "DDLValley";
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
                return "http://www.ddlvalley.eu/";
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
                return Utils.DateTimeToVersion("2012-12-31 0:01 AM");
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
        /// Gets a value indicating whether this site is deprecated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if deprecated; otherwise, <c>false</c>.
        /// </value>
        public override bool Deprecated
        {
            get
            {
                // RSS doesn't serve full content anymore.
                // Fate to be decided.
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
            if (Deprecated)
            {
                yield break;
            }

            var req = Utils.GetURL(Site + "search/" + Utils.EncodeURL(query) + "/feed")
                            .Replace("<![CDATA[", string.Empty)
                            .Replace("]]>", string.Empty)
                            .Replace("×", "x");

            var html = new HtmlDocument();
            html.LoadHtml(req);

            var links = html.DocumentNode.SelectNodes("//item");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var infourl = (node.GetTextValue("comments") ?? string.Empty).Replace("#comments", string.Empty); // can't get <link>
                var titles = HtmlEntity.DeEntitize(node.GetTextValue("title")).Split(new[] { " & " }, StringSplitOptions.RemoveEmptyEntries);
                var ps = node.SelectNodes("description/p");
                var idx = -1;
                var type = 0;

                foreach (var p in ps)
                {
                    if (p.ChildNodes.Count == 0)
                    {
                        continue;
                    }

                    if (p.ChildNodes[0].Attributes.FirstOrDefault(a => a.Name == "class" && a.Value == "info2") != null)
                    {
                        idx++;
                        type = 1;

                        continue;
                    }

                    if (p.ChildNodes[0].Attributes.FirstOrDefault(a => a.Name == "class" && a.Value == "info3") != null)
                    {
                        if (idx == -1)
                        {
                            idx++;
                        }

                        type = 2;

                        continue;
                    }

                    if (type == 0)
                    {
                        continue;
                    }

                    var hoster = p.GetTextValue("strong");
                    var hrefs = p.SelectNodes("a");

                    if (hrefs == null || string.IsNullOrWhiteSpace(hoster))
                    {
                        continue;
                    }

                    if (idx == -1)
                    {
                        idx++;
                    }

                    var link = new Link(this);

                    link.Release = titles.Length > idx ? titles[idx] : titles.Last();
                    link.InfoURL = infourl;
                    link.FileURL = string.Join("\0", hrefs.Select(x => x.GetAttributeValue("href")));
                    link.Infos = hoster.ToLower().ToUppercaseFirst();
                    link.Quality = FileNames.Parser.ParseQuality(link.Release);

                    yield return link;
                }
            }
        }
    }
}