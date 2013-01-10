namespace RoliSoft.TVShowTracker
{
    using RoliSoft.TVShowTracker.Parsers.Subtitles;

    /// <summary>
    /// Extended class of the original Subtitle class for the listview.
    /// </summary>
    public class SubtitleItem : Subtitle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleItem"/> class.
        /// </summary>
        /// <param name="subtitle">The subtitle.</param>
        public SubtitleItem(Subtitle subtitle) : base(subtitle.Source)
        {
            Release     = subtitle.Release;
            InfoURL     = subtitle.InfoURL;
            FileURL     = subtitle.FileURL;
            HINotations = subtitle.HINotations;
            Corrected   = subtitle.Corrected;
            Language    = subtitle.Language;
        }

        /// <summary>
        /// Gets a value indicating whether the HI notation icon is visible.
        /// </summary>
        public string HINotationVisible
        {
            get
            {
                return HINotations ? "Visible" : "Collapsed";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the corrected icon is visible.
        /// </summary>
        public string CorrectedVisible
        {
            get
            {
                return Corrected ? "Visible" : "Collapsed";
            }
        }

        /// <summary>
        /// Gets the image of the language.
        /// </summary>
        /// <value>The language's image.</value>
        public string LanguageImage
        {
            get
            {
                return "/RSTVShowTracker;component/Images/" + (Language != "null" ? "flag-" + Language : "unknown") + ".png";
            }
        }

        /// <summary>
        /// Gets the name of the language.
        /// </summary>
        /// <value>The language's name.</value>
        public string LanguageName
        {
            get
            {
                return Language != "null" ? Languages.List[Language] : "Unknown";
            }
        }
    }
}
