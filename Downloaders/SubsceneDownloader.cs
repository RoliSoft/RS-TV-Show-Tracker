namespace RoliSoft.TVShowTracker.Downloaders
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Ionic.Zip;

    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Provides a modified HTTP downloader to circumvent subscene.com's protection.
    /// </summary>
    public class SubsceneDownloader : IDownloader
    {
        /// <summary>
        /// Occurs when a file download completes.
        /// </summary>
        public event EventHandler<EventArgs<string, string, string>> DownloadFileCompleted;

        /// <summary>
        /// Occurs when the download progress changes.
        /// </summary>
        public event EventHandler<EventArgs<int>> DownloadProgressChanged;

        /// <summary>
        /// Asynchronously downloads the specified link.
        /// </summary>
        /// <param name="link">
        /// The object containing the link.
        /// This class only supports strings and <c>Subtitle</c>.
        /// </param>
        /// <param name="target">The target location.</param>
        /// <param name="token">The user token.</param>
        public void Download(object link, string target, string token = null)
        {
            string url;

            if (link is string)
            {
                url = link as string;
            }
            else if (link is Subtitle)
            {
                url = (link as Subtitle).URL;
            }
            else
            {
                throw new Exception("The link object is an unsupported type.");
            }

            new Thread(() => InternalDownload(url, target, token ?? string.Empty)).Start();
        }

        /// <summary>
        /// Downloads the specified subtitle from subscene.
        /// </summary>
        /// <param name="url">The URL of the subtitle page.</param>
        /// <param name="target">The target location.</param>
        /// <param name="token">The user token.</param>
        private void InternalDownload(string url, string target, string token)
        {
            // get the info page

            var info = Utils.GetURL(url);
            
            DownloadProgressChanged.Fire(this, 25);

            // extract required info

            var dllink    = "http://subscene.com/" + Regex.Match(info, @"\(new WebForm_PostBackOptions\([^\n\r\t]+?\/([^\n\r\t]+?)&quot;, false, true\)\)", RegexOptions.IgnoreCase).Groups[1].Value;
            var viewstate = Regex.Match(info, @"<input type=""hidden"" name=""__VIEWSTATE"" id=""__VIEWSTATE"" value=""([^\n\r\t]*?)"" />", RegexOptions.IgnoreCase).Groups[1].Value;
            var prevpage  = Regex.Match(info, @"<input type=""hidden"" name=""__PREVIOUSPAGE"" id=""__PREVIOUSPAGE"" value=""([^\n\r\t]*?)"" />", RegexOptions.IgnoreCase).Groups[1].Value;
            var subid     = Regex.Match(info, @"<input type=""hidden"" name=""subtitleId"" id=""subtitleId"" value=""(\d+?)"" />", RegexOptions.IgnoreCase).Groups[1].Value;
            var typeid    = Regex.Match(info, @"<input type=""hidden"" name=""typeId"" value=""([^\n\r\t]{3,15})"" />", RegexOptions.IgnoreCase).Groups[1].Value;
            var filmid    = Regex.Match(info, @"<input type=""hidden"" name=""filmId"" value=""(\d+?)"" />", RegexOptions.IgnoreCase).Groups[1].Value;

            // build POST data

            var post = "__EVENTTARGET=s$lc$bcr$downloadLink&__EVENTARGUMENT=&__VIEWSTATE={0}&__PREVIOUSPAGE={1}&subtitleId={2}&typeId={3}&filmId={4}".FormatWith(viewstate, prevpage, subid, typeid, filmid);
            var pbin = Encoding.ASCII.GetBytes(post);

            // download file

            var req               = (HttpWebRequest)WebRequest.Create(dllink);
            req.UserAgent         = "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.7.39 Version/11.00";
            req.ContentType       = "application/x-www-form-urlencoded";
            req.Method            = "POST";
            req.Referer           = url;
            req.Headers["Origin"] = "http://subscene.com";
            req.ContentLength     = pbin.Length;

            using (var rs = req.GetRequestStream())
            {
                rs.Write(pbin, 0, pbin.Length);
            }

            var resp = (HttpWebResponse)req.GetResponse();

            using (var rstream = resp.GetResponseStream())
            {
                using (var fstream = new FileStream(target, FileMode.Create))
                {
                    var buffer = new byte[1024];
                    int read;

                    while ((read = rstream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fstream.Write(buffer, 0, read);
                    }
                }
            }

            DownloadProgressChanged.Fire(this, 100);

            // extract subtitle file

            string fn;
            using (var mstream = new MemoryStream())
            {
                using (var zip = ZipFile.Read(target))
                {
                    fn = zip.Entries[0].FileName;
                    zip.Entries[0].Extract(mstream);
                }

                File.Delete(target);
                File.WriteAllBytes(target, mstream.ToArray());
            }

            DownloadFileCompleted.Fire(this, target, fn, token);
        }
    }
}
