namespace RoliSoft.TVShowTracker.Downloaders.Engines
{
    using System;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Provides an external downloader. (Translation: sends the URL to the browser, let the browser do the job)
    /// </summary>
    public class ExternalDownloader : IDownloader
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
            string url;

            if (link is string)
            {
                url = link as string;
            }
            else if (link is Link)
            {
                url = (link as Link).FileURL;
            }
            else if (link is Subtitle)
            {
                url = (link as Subtitle).FileURL;
            }
            else
            {
                throw new Exception("The link object is an unsupported type.");
            }

            // we need to check if the URL is really an HTTP link.
            // if we don't do this, the software could be exploited into running any command
            if (!Regex.IsMatch(url, @"(https?|ftp)://", RegexOptions.IgnoreCase))
            {
                throw new Exception("The specified URL doesn't look like a HTTP/FTP link.");
            }

            DownloadProgressChanged.Fire(this, 50);

            Utils.Run(url);

            DownloadProgressChanged.Fire(this, 100);
            DownloadFileCompleted.Fire(this, null, null, "LaunchedBrowser");
        }

        /// <summary>
        /// Cancels the asynchronous download.
        /// </summary>
        public void CancelAsync()
        {
        }
    }
}
