namespace RoliSoft.TVShowTracker.Downloaders.Engines
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

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

        private Thread _thd;
        private HTTPDownloader _dl;

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
                url = (link as Subtitle).InfoURL;
            }
            else
            {
                throw new Exception("The link object is an unsupported type.");
            }

            _thd = new Thread(() => InternalDownload(url, target, token ?? string.Empty));
            _thd.Start();
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

            var dllink    = "http://subscene.com" + Regex.Match(info, "href=\"([^\"]+)\".*?id=\"downloadButton\"", RegexOptions.IgnoreCase).Groups[1].Value;

            // pass the rest of the work to HTTPDownloader

            _dl = new HTTPDownloader();

            _dl.DownloadProgressChanged += (s, e) => DownloadProgressChanged.Fire(this, e.Data);
            _dl.DownloadFileCompleted   += (s, e) => DownloadFileCompleted.Fire(this, e.First, e.Second, e.Third);

            _dl.Download(dllink, target, token);
        }

        /// <summary>
        /// Cancels the asynchronous download.
        /// </summary>
        public void CancelAsync()
        {
            try { _dl.CancelAsync(); } catch { }
            try { _thd.Abort(); } catch { }
        }
    }
}
