namespace RoliSoft.TVShowTracker.Downloaders
{
    using System;

    /// <summary>
    /// Interface to download an URL provided by a parser.
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// Occurs when a file download completes.
        /// </summary>
        event EventHandler<EventArgs<string, string, string>> DownloadFileCompleted;

        /// <summary>
        /// Occurs when the download progress changes.
        /// </summary>
        event EventHandler<EventArgs<int>> DownloadProgressChanged;

        /// <summary>
        /// Asynchronously downloads the specified link.
        /// </summary>
        /// <param name="link">
        /// The object containing the link.
        /// This can be an URL in a string or a <c>Link</c>/<c>Subtitle</c> object.
        /// </param>
        /// <param name="target">The target location.</param>
        /// <param name="token">The user token.</param>
        void Download(object link, string target, string token = null);
    }
}