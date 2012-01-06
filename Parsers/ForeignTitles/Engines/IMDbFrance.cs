namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off imdb.fr.
    /// </summary>
    public class IMDbFrance : IMDbInternational
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "fr";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IMDbFrance"/> class.
        /// </summary>
        public IMDbFrance()
            : base("fr")
        {
        }
    }
}
