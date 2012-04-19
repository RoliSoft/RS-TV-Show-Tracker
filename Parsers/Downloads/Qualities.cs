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
        /// A screener, digital distribution copy or pre-air release; the quality is lower than what's going to air and might not have post-processing.
        /// </summary>
        [Description("Screener")]
        Screener,

        /// <summary>
        /// An SDTV rip, possibly from an analogue source; the resolution is usually NTSC or PAL and low-bitrate.
        /// </summary>
        [Description("SDTV XviD")]
        SDTVRip,

        /// <summary>
        /// A VHS rip; the resolution is usually NTSC or PAL. The only reason this is after SDTVRip, is because this might contain uncut/uncensored material.
        /// </summary>
        [Description("VHS-Rip XviD")]
        VHSRipXviD,

        /// <summary>
        /// A VHS rip encoded with an x264 encoder, as if it'd make a difference.
        /// </summary>
        [Description("VHS-Rip x264")]
        VHSRipx264,

        /// <summary>
        /// A web rip, usually the .FLV file served from the show's site encoded with XviD. It has a pretty low quality. Don't confuse with Web-DL, which is far more superior.
        /// </summary>
        [Description("Web-Rip XviD")]
        WebRipXviD,

        /// <summary>
        /// A web rip, usually the .FLV file served from the show's site encoded with x264. It has a pretty low quality. Don't confuse with Web-DL, which is far more superior.
        /// </summary>
        [Description("Web-Rip x264")]
        WebRipx264,

        /// <summary>
        /// A widescreen but low-resolution and bitrate video.
        /// </summary>
        [Description("HDTV XviD")]
        HDTVXviD,

        /// <summary>
        /// An XviD-encoded video downloaded from a legal source; usually iTunes.
        /// </summary>
        [Description("Web-DL XviD")]
        WebDLXviD,

        /// <summary>
        /// A DVD rip; the resolution is usually NTSC or PAL.
        /// </summary>
        [Description("DVD-Rip XviD")]
        DVDRipXviD,

        /// <summary>
        /// A Blu-ray-rip encoded with XviD; the resolution is usually lower than 720p.
        /// </summary>
        [Description("BD-Rip XviD")]
        BDRipXviD,

        /// <summary>
        /// A widescreen and high-resolution (but not high-definition) video.
        /// </summary>
        [Description("HDTV x264")]
        HDTVx264,

        /// <summary>
        /// A high-resolution DVD rip.
        /// </summary>
        [Description("DVD-Rip x264")]
        DVDRipx264,

        /// <summary>
        /// A high-resolution (but not high-definition) video.
        /// </summary>
        [Description("High-Res x264")]
        HRx264,

        /// <summary>
        /// A DVD disk. (Not ripped!)
        /// </summary>
        [Description("DVD")]
        DVD,

        /// <summary>
        /// An 852x480 or 720x480 high-definition video captured from DVB.
        /// </summary>
        [Description("HDTV 480p")]
        HDTV480p,

        /// <summary>
        /// An 852x480 or 720x480 high-definition video downloaded from a legal source; usually iTunes.
        /// </summary>
        [Description("Web-DL 480p")]
        WebDL480p,

        /// <summary>
        /// An 852x480 or 720x480 high-definition video ripped from a Blu-Ray disc.
        /// </summary>
        [Description("Blu-ray 480p")]
        BluRay480p,

        /// <summary>
        /// A 1024x576 high-definition video captured from DVB.
        /// </summary>
        [Description("HDTV 576p")]
        HDTV576p,

        /// <summary>
        /// A 1024x576 high-definition video downloaded from a legal source; usually iTunes.
        /// </summary>
        [Description("Web-DL 576p")]
        WebDL576p,

        /// <summary>
        /// A 1024x576 high-definition video ripped from a Blu-Ray disc.
        /// </summary>
        [Description("Blu-ray 576p")]
        BluRay576p,

        /// <summary>
        /// A 1280x720 high-definition video captured from DVB.
        /// </summary>
        [Description("HDTV 720p")]
        HDTV720p,

        /// <summary>
        /// A 1280x720 high-definition video downloaded from a legal source; usually iTunes.
        /// </summary>
        [Description("Web-DL 720p")]
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
        [Description("Web-DL 1080p")]
        WebDL1080p,

        /// <summary>
        /// A 1920x1080 high-definition video ripped from a Blu-Ray disc.
        /// </summary>
        [Description("Blu-ray 1080p")]
        BluRay1080p
    }
}
