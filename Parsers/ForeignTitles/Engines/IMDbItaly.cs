namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off imdb.it.
    /// </summary>
    public class IMDbItaly : IMDbInternational
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "it";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IMDbItaly"/> class.
        /// </summary>
        public IMDbItaly()
            : base("it")
        {
        }
    }
}
