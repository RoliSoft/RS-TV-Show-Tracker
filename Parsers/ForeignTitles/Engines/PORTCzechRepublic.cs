namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off port.cz.
    /// </summary>
    public class PORTCzechRepublic : PORTNetwork
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "cs";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PORTCzechRepublic"/> class.
        /// </summary>
        public PORTCzechRepublic()
            : base("cz")
        {
        }
    }
}
