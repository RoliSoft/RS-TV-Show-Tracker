namespace RoliSoft.TVShowTracker.Parsers
{
    /// <summary>
    /// An interface for classes which can't provide direct links by default, usually due to some sort of link protection.
    /// </summary>
    /// <typeparam name="T">The type of the link.</typeparam>
    public interface ILinkExpander<T>
    {
        /// <summary>
        /// Extracts the direct links from the supplied link.
        /// </summary>
        /// <param name="link">The protected link.</param>
        /// <returns>Direct links.</returns>
        string ExpandLinks(T link);
    }
}
