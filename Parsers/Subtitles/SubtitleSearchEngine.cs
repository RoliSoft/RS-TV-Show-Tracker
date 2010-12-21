namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Represents a subtitle search engine.
    /// </summary>
    public abstract class SubtitleSearchEngine
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
        /// Occurs when a subtitle search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<Subtitle>>> SubtitleSearchDone;

        /// <summary>
        /// Occurs when a subtitle search has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, string>> SubtitleSearchError;

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public abstract List<Subtitle> Search(string query);

        private Thread _job;

        /// <summary>
        /// Searches for subtitles on the service asynchronously.
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
                        SubtitleSearchDone.Fire(this, list);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ThreadAbortException)
                        {
                            return;
                        }

                        SubtitleSearchError.Fire(this, "There was an error while searching for subtitles.", ex.Message);
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
        /// Represents a subtitle.
        /// </summary>
        public class Subtitle
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
            /// Gets or sets the language of the subtitle.
            /// </summary>
            /// <value>The language.</value>
            public Languages Language { get; set; }

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
            /// Initializes a new instance of the <see cref="Subtitle"/> class.
            /// </summary>
            public Subtitle()
            {
                IsLinkDirect = true;
            }

            /// <summary>
            /// Represents the languages the search engines were designed to support.
            /// </summary>
            public enum Languages
            {
                /// <summary>
                /// Not specified or not recognized
                /// </summary>
                Unknown,
                /// <summary>
                /// English
                /// </summary>
                English,
                /// <summary>
                /// Hungarian - magyar
                /// </summary>
                Hungarian,
                /// <summary>
                /// Romanian - română
                /// </summary>
                Romanian
            }
        }
    }
}
