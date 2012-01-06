namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off port.rs.
    /// </summary>
    public class PORTSerbia : PORTNetwork
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "sr";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PORTSerbia"/> class.
        /// </summary>
        public PORTSerbia()
            : base("rs")
        {
        }
    }
}
