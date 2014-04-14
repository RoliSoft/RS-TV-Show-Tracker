using System.Text.RegularExpressions;

namespace RoliSoft.TVShowTracker.Downloaders.Engines
{
    using System;
    using System.Threading;

    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Provides a modified HTTP downloader to get the direct download link from the AlienSubtitles API.
    /// </summary>
    public class AlienSubtitlesDownloader : IDownloader
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
                url = (link as Subtitle).FileURL;
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

            var info = Utils.GetJSON("http://aliensubtitles.com/?d=" + Regex.Match(url, "/download#([0-9a-z]+)").Groups[1].Value + "&a=3a2677106d44d238f13ba200dd9ff53454af87a6");
            
            DownloadProgressChanged.Fire(this, 25);

            // check download link

            if (info["url"] == null)
            {
                DownloadFileCompleted.Fire(this, null, null, null);
                return;
            }

            // pass the rest of the work to HTTPDownloader

            _dl = new HTTPDownloader();

            _dl.DownloadProgressChanged += (s, e) => DownloadProgressChanged.Fire(this, e.Data);
            _dl.DownloadFileCompleted   += (s, e) => DownloadFileCompleted.Fire(this, e.First, e.Second, e.Third);

            _dl.Download((string)info["url"], target, token);
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
