namespace RoliSoft.TVShowTracker.Downloaders.Engines
{
    using System;
    using System.IO;
    using System.Threading;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides a modified HTTP downloader to create the NZB from ID on BinSearch.
    /// </summary>
    public class BinSearchDownloader : IDownloader
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
            else if (link is Link)
            {
                url = (link as Link).FileURL;
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
            var parts = url.Split(';');
            var nzb   = Utils.GetURL(parts[0], parts[1], encoding: new Utils.Base64Encoding());

            DownloadProgressChanged.Fire(this, 75);

            File.WriteAllBytes(target, Convert.FromBase64String(nzb));

            DownloadProgressChanged.Fire(this, 100);
            DownloadFileCompleted.Fire(this, target, parts[2], token ?? string.Empty);
        }

        /// <summary>
        /// Cancels the asynchronous download.
        /// </summary>
        public void CancelAsync()
        {
            try { _thd.Abort(); } catch { }
        }
    }
}
