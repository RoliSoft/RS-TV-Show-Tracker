namespace RoliSoft.TVShowTracker.Parsers.Social
{
    /// <summary>
    /// Represents a social site.
    /// </summary>
    public abstract class SocialEngine : ParserEngine
    {
        /// <summary>
        /// Gets the default status format.
        /// </summary>
        public abstract string DefaultStatusFormat { get; }

        /// <summary>
        /// Posts the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public abstract void PostMessage(string message);
    }
}
