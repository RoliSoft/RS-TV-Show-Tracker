namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
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
        HTTP,
        /// <summary>
        /// A link which represents the existance of a scene release.
        /// </summary>
        PreDB
    }
}
