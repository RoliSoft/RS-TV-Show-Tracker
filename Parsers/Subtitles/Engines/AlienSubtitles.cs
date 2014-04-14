namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Downloaders.Engines;
    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Provides support for the AlienSubtitles API.
    /// </summary>
    [TestFixture]
    public class AlienSubtitles : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "AlienSubtitles";
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
                return "http://aliensubtitles.com/";
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
                return Utils.DateTimeToVersion("2014-04-14 9:59 AM");
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
                return new AlienSubtitlesDownloader();
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

            if (Regexes.Numbering.IsMatch(query))
            {
                show     = Utils.EncodeURL(Parser.Split(query)[0]);
                var epnr = Parser.ExtractEpisode(query) ?? new ShowEpisode();
                season   = epnr.Season.ToString();
                episode  = epnr.Episode.ToString();
            }
            else
            {
                show = Utils.EncodeURL(query);
            }

            var json = Utils.GetJSON(Site + "?q={0}&n={1}&e={2}&a=3a2677106d44d238f13ba200dd9ff53454af87a6".FormatWith(show, season, episode));
            
            if ((int)json["count"] == 0)
            {
                yield break;
            }

            foreach (var node in json["results"])
            {
                var sub = new Subtitle(this);

                sub.Release  = node["title"] + (node["season"] != null ? " S" + ((int)node["season"]).ToString("00") + (node["episode"] != null ? "E" + ((int)node["episode"]).ToString("00") : string.Empty) : string.Empty);
                sub.Language = Languages.Parse((string)node["language"]);
                sub.InfoURL  = (string)node["iurl"];
                sub.FileURL  = (string)node["durl"];

                foreach (string tag in node["tags"])
                {
                    switch (tag)
                    {
                        case "hi": sub.HINotations = true; break;
                        case "cr": sub.Corrected = true;   break;
                    }
                }

                var sd = false;
                foreach (string scene in node["scene"])
                {
                    if (string.IsNullOrWhiteSpace(scene)) continue;

                    if (!sd)
                    {
                        sub.Release += " - ";
                        sd = true;
                    }
                    else
                    {
                        sub.Release += "/";
                    }

                    sub.Release += scene.Trim();
                }

                yield return sub;
            }
        }
    }
}
