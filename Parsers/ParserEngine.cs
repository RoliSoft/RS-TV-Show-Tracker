namespace RoliSoft.TVShowTracker.Parsers
{
    /// <summary>
    /// Represents a parser engine.
    /// </summary>
    [Parser]
    public abstract class ParserEngine : IPlugin
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public abstract string Site { get; }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public virtual string Icon
        {
            get
            {
                return Site + "favicon.ico";
            }
        }
    }
}
