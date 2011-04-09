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
        /// A link to a page which contains one or more links to the file which is usually located on a one-click hosting service.
        /// </summary>
        HTTP,
        /// <summary>
        /// A direct link to a file on a one-click hosting service.
        /// </summary>
        DirectHTTP,
        /// <summary>
        /// A link which represents the existance of a scene release.
        /// </summary>
        PreDB
    }
}
