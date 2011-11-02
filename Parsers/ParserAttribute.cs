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
        /// Gets or sets the version number.
        /// </summary>
        /// <value>The version number.</value>
        public Version Version { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserAttribute"/> class.
        /// </summary>
        public ParserAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserAttribute"/> class.
        /// </summary>
        /// <param name="developer">The developer.</param>
        /// <param name="revision">The version number or date time.</param>
        public ParserAttribute(string developer, string revision)
        {
            Developer = developer;
            Version   = ParseRevision(revision);
        }

        /// <summary>
        /// Parses the revision string in the attribute.
        /// </summary>
        /// <param name="revision">The revision containing a version number or a date time.</param>
        /// <returns>
        /// Extracted version number.
        /// </returns>
        private Version ParseRevision(string revision)
        {
            Version ver;
            if (Version.TryParse(revision, out ver))
            {
                return ver;
            }

            DateTime dt;
            if (DateTime.TryParse(revision, out dt))
            {
                return Utils.DateTimeToVersion(dt);
            }

            return null;
        }
    }
}
