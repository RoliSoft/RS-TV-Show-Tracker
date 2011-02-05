namespace RoliSoft.TVShowTracker.Downloaders
{
    using System;
    using System.Net;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Provides a simple HTTP downloader.
    /// </summary>
    public class HTTPDownloader : IDownloader
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
        /// This can be an URL in a string or a <c>Link</c>/<c>Subtitle</c> object.
        /// </param>
        /// <param name="target">The target location.</param>
        /// <param name="token">The user token.</param>
        public void Download(object link, string target, string token = null)
        {
            var wc  = new Utils.SmarterWebClient();
            Uri uri;

            if (link is string)
            {
                uri = new Uri(link as string);
            }
            else if (link is Link)
            {
                uri = new Uri((link as Link).URL);

                if (!string.IsNullOrWhiteSpace((link as Link).Source.Cookies))
                {
                    wc.Headers[HttpRequestHeader.Cookie] = (link as Link).Source.Cookies;
                }
            }
            else if (link is Subtitle)
            {
                uri = new Uri((link as Subtitle).URL);
            }
            else
            {
                throw new Exception("The link object is an unsupported type.");
            }

            wc.Headers[HttpRequestHeader.Referer] = "http://" + uri.DnsSafeHost + "/";
            wc.DownloadProgressChanged           += (s, e) => DownloadProgressChanged.Fire(this, e.ProgressPercentage);
            wc.DownloadFileCompleted             += (s, e) => DownloadFileCompleted.Fire(this, (e.UserState as string[])[0], (s as Utils.SmarterWebClient).FileName, null);

            wc.DownloadFileAsync(uri, target, new[] { target, token ?? string.Empty });
        }
    }
}
