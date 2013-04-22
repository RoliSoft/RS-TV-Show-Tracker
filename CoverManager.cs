namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;

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
                return Path.Combine(Signature.InstallPath, "covers/");
            }
        }

        /// <summary>
        /// Initializes the <see cref="CoverManager"/> class.
        /// </summary>
        static CoverManager()
        {
            if (!Directory.Exists(Location))
            {
                try
                {
                    Directory.CreateDirectory(Location);
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to create cover directory to " + Location, ex);
                }
            }
        }

        /// <summary>
        /// Gets the local cover location of the specified show.
        /// </summary>
        /// <param name="show">The show to get the cover location for.</param>
        /// <returns>
        /// Local location of the cover.
        /// </returns>
        public static string GetCoverLocation(string show)
        {
            return Path.Combine(Location, Utils.CreateSlug(show, false) + ".jpg");
        }

        /// <summary>
        /// Gets the cover of the specified show.
        /// </summary>
        /// <param name="show">The show to get covers for.</param>
        /// <param name="status">The method to call when reporting a status change.</param>
        /// <returns>
        /// Cover of the specified show or null.
        /// </returns>
        public static string GetCover(string show, Action<string> status)
        {
            var cover = GetCoverLocation(show);

            if (File.Exists(cover))
            {
                goto success;
            }

            Log.Info("Getting cover for " + show + "...");

            string url;

            // try to find it on The TVDB

            status("Searching for cover on The TVDB...");
            try
            {
                if ((url = GetCoverFromTVDB(show)) != null)
                {
                    status("Downloading cover from " + new Uri(url).Host + "...");
                    if (DownloadCover(url, cover))
                    {
                        goto success;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while searching for cover for " + show + " on TVDB.", ex);
            }

            // try to find it on IMDb

            status("Searching for cover on IMDb...");
            try
            {
                if ((url = GetCoverFromIMDb(show)) != null)
                {
                    status("Downloading cover from " + new Uri(url).Host + "...");
                    if (DownloadCover(url, cover))
                    {
                        goto success;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while searching for cover for " + show + " on IMDb.", ex);
            }

            // try to find it on Amazon

            status("Searching for cover on Amazon...");
            try
            {
                if ((url = GetCoverFromAmazon(show)) != null)
                {
                    status("Downloading cover from " + new Uri(url).Host + "...");
                    if (DownloadCover(url, cover))
                    {
                        goto success;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while searching for cover for " + show + " on Amazon.", ex);
            }

            // we were out of luck, but fuck that, we'll draw our own cover! with blackjack. and hookers. in fact, forget the cover.

            status("Drawing a cover...");
            DrawCover(show, cover);

          success:
            return cover;
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
            Log.Debug("Getting cover for " + show + " from TVDB...");

            var tvdb = new TVDB();
            var res  = tvdb.GetID(show).ToList();

            if (res.Count != 0)
            {
                var guide = tvdb.GetData(res[0].ID);

                if (!string.IsNullOrWhiteSpace(guide.Cover))
                {
                    return guide.Cover;
                }
                else
                {
                    Log.Debug("TVDB doesn't have a cover associated to " + show + ".");
                }
            }
            else
            {
                Log.Debug("No shows were found on TVDB matching " + show + ".");
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
            Log.Debug("Getting cover for " + show + " from IMDb...");

            var imdb = new IMDb();
            var res  = imdb.GetID(show).ToList();

            if (res.Count != 0 && !string.IsNullOrWhiteSpace(res[0].Cover) && !res[0].Cover.EndsWith("/tv_series.gif"))
            {
                return Regex.Replace(res[0].Cover, @"@@.+\.", "@@.");
            }
            else
            {
                Log.Debug("No shows or covers were found on IMDb matching " + show + ".");
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
            Log.Debug("Getting cover for " + show + " from Amazon...");

            var html = Utils.GetHTML("http://www.amazon.com/gp/search/ref=sr_in_-2_p_n_format_browse-bi_5?rh=n%3A2625373011%2Ck%3A%2Cp_n_format_browse-bin%3A2650304011%7C2650305011%7C2650307011%7C2650308011%7C2650310011%7C2650309011&bbn=2625373011&ie=UTF8&qid=1324847845&rnid=2650303011&keywords=" + Utils.EncodeURL(show));
            var imgs = html.DocumentNode.SelectNodes("//img[@class='productImage' or @alt='Product Details']");

            if (imgs != null && html.DocumentNode.SelectSingleNode("//h1[@id='noResultsTitle']") == null)
            {
                var src = imgs[0].GetAttributeValue("src");

                if (src != null)
                {
                    return Regex.Replace(src, @"\._[^_]+_\.", "._SCRM_.");
                }
                else
                {
                    Log.Debug("Amazon entries don't have a cover associated to " + show + ".");
                }
            }
            else
            {
                Log.Debug("No shows were found on Amazon matching " + show + ".");
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
            Log.Debug("Downloading cover " + url + " to " + path + "...");

            try
            {
                new WebClient().DownloadFile(url, path);
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to download cover due to exception.", ex);
                return false;
            }

            return File.Exists(path) && new FileInfo(path).Length != 0;
        }

        /// <summary>
        /// Draws a cover for the show if none were found.
        /// </summary>
        /// <param name="title">The title of the show.</param>
        /// <param name="path">The path where to store the cover image.</param>
        private static void DrawCover(string title, string path)
        {
            Log.Debug("Drawing a cover for " + title + "...");

            var help = "covers/" + Utils.CreateSlug(title, false) + ".jpg";
            var font = new Font("Calibri", 72, FontStyle.Bold);
            var fon2 = new Font("Calibri", 48);
            var imag = new Bitmap(1, 1);
            var grap = Graphics.FromImage(imag);
            var size = grap.MeasureString(title, font);
            var siz2 = grap.MeasureString(help, fon2);
            var nwsz = size.Width > siz2.Width ? size.Width : siz2.Width;

            imag.Dispose();
            grap.Dispose();

            imag = new Bitmap((int)nwsz + 100, (int)((nwsz + 100) * 1.47));
            grap = Graphics.FromImage(imag);

            grap.Clear(Color.Black);

            grap.TextRenderingHint = TextRenderingHint.AntiAlias;
            grap.DrawString(title, font, Brushes.White, (int)((imag.Width - size.Width) / 2), (int)((imag.Height - size.Height) / 4));
            grap.DrawString(help, fon2, Brushes.DimGray, (int)((imag.Width - siz2.Width) / 2), (int)(((imag.Height - siz2.Height) - ((imag.Height - siz2.Height) / 4))));

            var ima2 = new Bitmap(680, 1000);
            var gra2 = Graphics.FromImage(ima2);

            gra2.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gra2.DrawImage(imag, 0, 0, 680, 1000);

            grap.Dispose();
            imag.Dispose();

            try
            {
                ima2.Save(path, ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while saving made-up cover for " + title + ".", ex);
            }

            gra2.Dispose();
            ima2.Dispose();
        }

        /// <summary>
        /// Tests the search.
        /// </summary>
        [Test]
        public static void TestSearch()
        {
            var show = "House";
            Console.Write("TVDB:   ");
            Console.WriteLine(GetCoverFromTVDB(show));
            Console.Write("IMDb:   ");
            Console.WriteLine(GetCoverFromIMDb(show));
            Console.Write("Amazon: ");
            Console.WriteLine(GetCoverFromAmazon(show));
        }
    }
}
