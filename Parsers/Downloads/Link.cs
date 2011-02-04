namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System.ComponentModel;

    /// <summary>
    /// Represents a download link.
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Gets the source of the download link.
        /// </summary>
        /// <value>The site.</value>
        public DownloadSearchEngine Source { get; internal set; }

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
        /// <param name="source">The source of this download link.</param>
        public Link(DownloadSearchEngine source)
        {
            Source       = source;
            IsLinkDirect = true;
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
        HTTP
    }

    /// <summary>
    /// Represents the supported qualities of a file.
    /// </summary>
    public enum Qualities
    {
        /// <summary>
        /// Unspecified or undetected.
        /// </summary>
        [Description("Unknown")]
        Unknown,
        /// <summary>
        /// A TV rip; usually NTSC or PAL size and low-bitrate.
        /// </summary>
        [Description("TV Rip")]
        TVRip,
        /// <summary>
        /// A widescreen but low-resolution and bitrate video.
        /// </summary>
        [Description("HDTV XviD")]
        HDTVXviD,
        /// <summary>
        /// A high-resolution (but not necessarily widescreen) video.
        /// </summary>
        [Description("HiRes x264")]
        HRx264,
        /// <summary>
        /// A 1280x720 high-definition video captured from DVB.
        /// </summary>
        [Description("HDTV 720p")]
        HDTV720p,
        /// <summary>
        /// A 1280x720 high-definition video downloaded from a legal source; usually iTunes.
        /// </summary>
        [Description("Web-Dl 720p")]
        WebDL720p,
        /// <summary>
        /// A 1280x720 high-definition video ripped from a Blu-Ray disc.
        /// </summary>
        [Description("Blu-ray 720p")]
        BluRay720p,
        /// <summary>
        /// A 1920x1080 high-definition video captured from DVB.
        /// </summary>
        [Description("HDTV 1080i")]
        HDTV1080i,
        /// <summary>
        /// A 1920x1080 high-definition video downloaded from a legal source; usually iTunes.
        /// </summary>
        [Description("Web-Dl 1080p")]
        WebDL1080p,
        /// <summary>
        /// A 1920x1080 high-definition video ripped from a Blu-Ray disc.
        /// </summary>
        [Description("Blu-ray 1080p")]
        BluRay1080p
    }
}
