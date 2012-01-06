namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off imdb.es.
    /// </summary>
    public abstract class IMDbSpain : IMDbInternational
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "es";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IMDbSpain"/> class.
        /// </summary>
        public IMDbSpain()
            : base("es")
        {
        }
    }
}
