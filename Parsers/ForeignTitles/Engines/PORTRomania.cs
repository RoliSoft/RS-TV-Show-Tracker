namespace RoliSoft.TVShowTracker.Parsers.ForeignTitles.Engines
{
    /// <summary>
    /// Provides support for extracting titles off port.ro.
    /// </summary>
    public class PORTRomania : PORTNetwork
    {
        /// <summary>
        /// Gets the ISO 639-1 code of the language this engine provides titles for.
        /// </summary>
        /// <value>The ISO 639-1 code of the language this engine provides titles for.</value>
        public override string Language
        {
            get
            {
                return "ro";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PORTRomania"/> class.
        /// </summary>
        public PORTRomania()
            : base("ro")
        {
        }
    }
}
