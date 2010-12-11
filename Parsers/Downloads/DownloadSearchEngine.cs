namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Occurs when a download link search is done.
    /// </summary>
    public delegate void DownloadSearchDone(string name, List<DownloadSearchEngine.Link> links);

    /// <summary>
    /// Occurs when a download link search has encountered an error.
    /// </summary>
    public delegate void DownloadSearchError(string name, string message, string detailed = null);

    /// <summary>
    /// Represents a download link search engine.
    /// </summary>
    public abstract class DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public abstract string Icon { get; }

        /// <summary>
        /// Gets a value indicating whether the site requires cookies to authenticate.
        /// </summary>
        /// <value><c>true</c> if requires cookies; otherwise, <c>false</c>.</value>
        public abstract bool RequiresCookies { get; }

        /// <summary>
        /// Gets or sets the cookies used to access the site.
        /// </summary>
        /// <value>The cookies in the same format in which <c>alert(document.cookie)</c> returns in a browser.</value>
        public string Cookies { get; set; }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public abstract Types Type { get; }

        /// <summary>
        /// Occurs when a download link search is done.
        /// </summary>
        public event DownloadSearchDone DownloadSearchDone;

        /// <summary>
        /// Occurs when a download link search has encountered an error.
        /// </summary>
        public event DownloadSearchError DownloadSearchError;

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public abstract List<Link> Search(string query);

        private Thread _job;

        /// <summary>
        /// Searches for download links on the service asynchronously.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        public void SearchAsync(string query)
        {
            if (_job != null)
            {
                _job.Abort();
            }

            _job = new Thread(() =>
                {
                    try
                    {
                        var list = Search(query);
                        DownloadSearchDone(Name, list);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ThreadAbortException)
                        {
                            return;
                        }

                        DownloadSearchError(Name, "There was an error while searching for download links.", ex.Message);
                    }
                });
            _job.Start();
        }

        /// <summary>
        /// Cancels the active asynchronous search.
        /// </summary>
        public void CancelAsync()
        {
            if (_job != null)
            {
                _job.Abort();
                _job = null;
            }
        }

        /// <summary>
        /// Represents the supported link types.
        /// </summary>
        public enum Types
        {
            /// <summary>
            /// A link to a BitTorrent .torrent file.
            /// </summary>
            Torrent,
            /// <summary>
            /// A link to a Usenet .nzb file.
            /// </summary>
            Usenet,
            /// <summary>
            /// A link to a page which contains HTTP links to the file. Usually RapidShare (and similar) links.
            /// </summary>
            Http
        }

        /// <summary>
        /// Represents a download link.
        /// </summary>
        public class Link
        {
            /// <summary>
            /// Gets or sets the name of the site.
            /// </summary>
            /// <value>The site.</value>
            public string Site { get; set; }

            /// <summary>
            /// Gets or sets the release name.
            /// </summary>
            /// <value>The release name.</value>
            public string Release { get; set; }

            /// <summary>
            /// Gets or sets the quality of the video.
            /// </summary>
            /// <value>The quality.</value>
            public Qualities Quality { get; set; }

            /// <summary>
            /// Gets or sets the size of the file.
            /// </summary>
            /// <value>The size.</value>
            public string Size { get; set; }

            /// <summary>
            /// Gets or sets the type of the URL.
            /// </summary>
            /// <value>The type.</value>
            public Types Type { get; set; }

            /// <summary>
            /// Gets or sets the URL to the subtitle.
            /// </summary>
            /// <value>The URL.</value>
            public string URL { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the URL is a direct link to the download.
            /// </summary>
            /// <value>
            /// 	<c>true</c> if the URL is a direct link; otherwise, <c>false</c>.
            /// </value>
            public bool IsLinkDirect { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Link"/> class.
            /// </summary>
            public Link()
            {
                IsLinkDirect = true;
            }

            /// <summary>
            /// Represents the supported qualities of a file.
            /// </summary>
            public enum Qualities
            {
                /// <summary>
                /// Unspecified or undetected.
                /// </summary>
                Unknown,
                /// <summary>
                /// A TV rip; usually NTSC or PAL size and low-bitrate.
                /// </summary>
                TVRip,
                /// <summary>
                /// A widescreen but low-resolution and bitrate video.
                /// </summary>
                HDTV_XviD,
                /// <summary>
                /// A high-resolution (but not necessarily widescreen) video.
                /// </summary>
                HR_x264,
                /// <summary>
                /// A 1280x720 high-definition video captured from DVB.
                /// </summary>
                HDTV_720p,
                /// <summary>
                /// A 1280x720 high-definition video ripped from a Blu-Ray disc.
                /// </summary>
                BluRay_720p,
                /// <summary>
                /// A 1280x720 high-definition video downloaded from a legal source; usually iTunes.
                /// </summary>
                WebDL_720p,
                /// <summary>
                /// A 1920x1080 high-definition video captured from DVB.
                /// </summary>
                HDTV_1080,
                /// <summary>
                /// A 1920x1080 high-definition video ripped from a Blu-Ray disc.
                /// </summary>
                BluRay_1080,
                /// <summary>
                /// A 1920x1080 high-definition video downloaded from a legal source; usually iTunes.
                /// </summary>
                WebDL_1080
            }
        }
    }
}
