namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    using System.ComponentModel;

    /// <summary>
    /// Represents the supported editions of a file.
    /// </summary>
    public enum Editions
    {
        /// <summary>
        /// Unspecified or undetected.
        /// </summary>
        [Description("Unknown")]
        Unknown,

        /// <summary>
        /// The edition which was sent to critics via a DVD or digitally distributed copy or leaked otherwise.
        /// This video might not have post-processing and not be the same as the episode which is going to air.
        /// </summary>
        [Description("Screener")]
        Screener,

        /// <summary>
        /// The edition which was seen on TV for the first time. These are the XviD and 720p HDTV or PDTV releases.
        /// </summary>
        [Description("TV")]
        TV,

        /// <summary>
        /// The edition which can be downloaded from a legal source, like iTunes, after the episode aired.
        /// Although it usually doesn't contain extra scenes, if the original broadcast had a recap, this
        /// edition probably doesn't; also, between the scenes where the ads were in the original broadcast,
        /// there is a 2-5 sec black screen or TV show logo, which is enough to desync subtitles.
        /// </summary>
        [Description("Web-DL")]
        WebDL,

        /// <summary>
        /// The edition which is released on DVDs and Blu-rays. It might contain
        /// several seconds of extra scenes and it is most likely uncensored.
        /// </summary>
        [Description("Retail")]
        Retail
    }
}
