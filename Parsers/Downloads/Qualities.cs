namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System.ComponentModel;

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
