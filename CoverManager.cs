namespace RoliSoft.TVShowTracker
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using Parsers.Guides.Engines;

    /// <summary>
    /// Provides methods for getting and using covers.
    /// </summary>
    public static class CoverManager
    {
        /// <summary>
        /// Gets the location of the covers.
        /// </summary>
        public static string Location
        {
            get
            {
                return Path.Combine(Signature.FullPath, "covers/");
            }
        }

        /// <summary>
        /// Initializes the <see cref="CoverManager"/> class.
        /// </summary>
        static CoverManager()
        {
            if (!Directory.Exists(Location))
            {
                try { Directory.CreateDirectory(Location); } catch { }
            }
        }

        /// <summary>
        /// Gets the cover of the specified show.
        /// </summary>
        /// <param name="show">The show to get covers for.</param>
        /// <param name="status">The method to call when reporting a status change.</param>
        /// <returns>
        /// Cover of the specified show or null.
        /// </returns>
        public static Uri GetCover(string show, Action<string> status)
        {
            var clean = Utils.CreateSlug(show, false);
            var cover = Path.Combine(Location, clean + ".jpg");

            if (File.Exists(cover))
            {
                goto success;
            }

            string url;

            // try to find it on The TVDB

            status("Searching for cover on The TVDB...");
            if ((url = GetCoverFromTVDB(show)) != null)
            {
                status("Downloading cover from " + new Uri(url).Host + "...");
                if (DownloadCover(url, cover))
                {
                    goto success;
                }
            }

            // try to find it on IMDb

            status("Searching for cover on IMDb...");
            if ((url = GetCoverFromIMDb(show)) != null)
            {
                status("Downloading cover from " + new Uri(url).Host + "...");
                if (DownloadCover(url, cover))
                {
                    goto success;
                }
            }

            // try to find it on Amazon

            status("Searching for cover on Amazon...");
            if ((url = GetCoverFromAmazon(show)) != null)
            {
                status("Downloading cover from " + new Uri(url).Host + "...");
                if (DownloadCover(url, cover))
                {
                    goto success;
                }
            }

            return null;

          success:
            return new Uri(cover, UriKind.Absolute);
        }

        /// <summary>
        /// Gets the cover for the specified show from The TVDB.
        /// </summary>
        /// <param name="show">The show to get the cover for.</param>
        /// <returns>
        /// URL to the image on TVDB's server.
        /// </returns>
        public static string GetCoverFromTVDB(string show)
        {
            var tvdb = new TVDB();
            var res  = tvdb.GetID(show).ToList();

            if (res.Count != 0)
            {
                var guide = tvdb.GetData(res[0].ID);

                if (!string.IsNullOrWhiteSpace(guide.Cover))
                {
                    return guide.Cover;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the cover for the specified show from IMDb.
        /// </summary>
        /// <param name="show">The show to get the cover for.</param>
        /// <returns>
        /// URL to the image on IMDb's server.
        /// </returns>
        public static string GetCoverFromIMDb(string show)
        {
            var imdb = new IMDb();
            var res  = imdb.GetID(show).ToList();

            if (res.Count != 0 && !string.IsNullOrWhiteSpace(res[0].Cover) && !res[0].Cover.EndsWith("/tv_series.gif"))
            {
                return Regex.Replace(res[0].Cover, @"@@.+\.", "@@.");
            }

            return null;
        }

        /// <summary>
        /// Gets the cover for the specified show from Amazon.
        /// </summary>
        /// <param name="show">The show to get the cover for.</param>
        /// <returns>
        /// URL to the image on Amazon's server.
        /// </returns>
        public static string GetCoverFromAmazon(string show)
        {
            var html = Utils.GetHTML("http://www.amazon.com/gp/search/ref=sr_in_-2_p_n_format_browse-bi_5?rh=n%3A2625373011%2Ck%3A%2Cp_n_format_browse-bin%3A2650304011%7C2650305011%7C2650307011%7C2650308011%7C2650310011%7C2650309011&bbn=2625373011&ie=UTF8&qid=1324847845&rnid=2650303011&keywords=" + Utils.EncodeURL(show));
            var imgs = html.DocumentNode.SelectNodes("//img[@class='productImage' or @alt='Product Details']");

            if (imgs != null && html.DocumentNode.SelectSingleNode("//h1[@id='noResultsTitle']") == null)
            {
                var src = imgs[0].GetAttributeValue("src");

                if (src != null)
                {
                    return Regex.Replace(src, @"\._[^_]+_\.", "._SCRM_.");
                }
            }

            return null;
        }

        /// <summary>
        /// Downloads the specified cover.
        /// </summary>
        /// <param name="url">The URL to the cover image.</param>
        /// <param name="path">The path where to store the cover image.</param>
        /// <returns>
        ///   <c>true</c> if the download was successful; otherwise, <c>false</c>.
        /// </returns>
        private static bool DownloadCover(string url, string path)
        {
            try
            {
                new WebClient().DownloadFile(url, path);
            }
            catch
            {
                return false;
            }

            return File.Exists(path) && new FileInfo(path).Length != 0;
        }
    }
}
