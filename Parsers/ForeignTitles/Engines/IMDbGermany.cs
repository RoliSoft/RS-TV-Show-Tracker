namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off imdb.de.
    /// </summary>
    public abstract class IMDbGermany : IMDbInternational
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "de";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IMDbGermany"/> class.
        /// </summary>
        public IMDbGermany()
            : base("de")
        {
        }
    }
}
