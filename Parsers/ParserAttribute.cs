namespace RoliSoft.TVShowTracker.Parsers
{
    using System;

    /// <summary>
    /// Provides metadata information for the parser engines.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ParserAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the developer.
        /// </summary>
        /// <value>The developer.</value>
        public string Developer { get; set; }
        
        /// <summary>
        /// Gets or sets the revision date.
        /// </summary>
        /// <value>The revision date.</value>
        public DateTime Revision { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserAttribute"/> class.
        /// </summary>
        /// <param name="developer">The developer.</param>
        /// <param name="revision">The revision date.</param>
        public ParserAttribute(string developer, DateTime revision)
        {
            Developer = developer;
            Revision  = revision;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserAttribute"/> class.
        /// </summary>
        /// <param name="developer">The developer.</param>
        /// <param name="revision">The revision date.</param>
        public ParserAttribute(string developer, string revision)
        {
            Developer = developer;
            Revision  = DateTime.Parse(revision);
        }
    }
}
