namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off port.hr.
    /// </summary>
    public class PORTCroatia : PORTNetwork
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "hr";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PORTCroatia"/> class.
        /// </summary>
        public PORTCroatia()
            : base("hr")
        {
        }
    }
}
