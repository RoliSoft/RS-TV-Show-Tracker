namespace RoliSoft.TVShowTracker.ShowNames
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Contains regular expressions which are used when working with show names.
    /// </summary>
    public static class Regexes
    {
        /// <summary>
        /// List of TV shows which have a different or shorter scene release name.
        /// </summary>
        public static readonly Dictionary<string, string> Exclusions = new Dictionary<string, string>
            {
                { "Sci-Fi Science: Physics of the Impossible", "Sci Fi Science" },
            };

        /// <summary>
        /// Removes everything except letters, numbers and parentheses.
        /// </summary>
        public static readonly Regex SpecialChars  = new Regex(@"([^A-Z0-9\s\(\)])");

        /// <summary>
        /// Matches 2000-2099 in brackets with leading space.
        /// </summary>
        public static readonly Regex NewYear       = new Regex(@"\s\(20\d{2}\)");

        /// <summary>
        /// Matches US, UK and AU in brackets with leading space.
        /// </summary>
        public static readonly Regex Countries     = new Regex(@"\s\((US|UK|AU)\)");

        /// <summary>
        /// Matches the four most common words in the english language. (and, the, of, a)
        /// </summary>
        public static readonly Regex Common        = new Regex(@"\b(AND|THE|OF|A)\b");

        /// <summary>
        /// Matches a single character surrounded by word boundaries, except if it is the first character.
        /// </summary>
        public static readonly Regex OneChar       = new Regex(@"(?<!^)\b[A-Z]\b");

        /// <summary>
        /// Matches strings starting with "the".
        /// </summary>
        public static readonly Regex StrictCommon  = new Regex(@"^THE\b");

        /// <summary>
        /// Matches any single letter with the exclusion of "a" and "i".
        /// </summary>
        public static readonly Regex StrictOneChar = new Regex(@"(?<!^)\b[B-HJ-Z]\b");

        /// <summary>
        /// Matches any whitespace.
        /// </summary>
        public static readonly Regex Whitespace    = new Regex(@"\s+");

        /// <summary>
        /// Matches any part numbering in the episode title.
        /// </summary>
        public static readonly Regex PartText      = new Regex(@"(?:,? part \d{1,2}(?: of \d{1,2})| \((?:part ?)?\d{1,2}\))\s*$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches video file extensions.
        /// </summary>
        public static readonly Regex KnownVideo    = new Regex(@"\.(avi|mkv|mp4|ts|wmv)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches if the specified file name is only a sample of the video.
        /// </summary>
        public static readonly Regex SampleVideo   = new Regex(@"(^|[\.\-\s])sample[\.\-\s]", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches a proper notation in a release name.
        /// </summary>
        public static readonly Regex Proper        = new Regex(@"\b(PROPER|REPACK|RERIP|REAL|FINAL)\b");

        /// <summary>
        /// Matches a group name in a release name.
        /// </summary>
        public static readonly Regex Group         = new Regex(@"\-(?<group>[a-z0-9&]+)(?:\.[a-z0-9]+)?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Simple regular expression to detect S00E00 and 0x00 at the end of a query.
        /// </summary>
        public static readonly Regex Numbering     = new Regex(@"\s+((?:S[0-9]{2}E[0-9]{2})|(?:[0-9]{1,2}x[0-9]{1,2}))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches release names which are season packs.
        /// </summary>
        public static readonly Regex VolNumbering  = new Regex(@"\s*[\.\-_]*\s*(?:Complete|(?:S(?:e(?:ason|ries)?)?|Pa(?:rt|ck)|Disc)\s*.?\s*0?\d{1,2}|\((?![0-9]{4}\)).*\)).*$", RegexOptions.IgnoreCase);

        /// <summary>
        /// More advanced regular expression to detect season and episode number in various forms in a string.
        /// </summary>
        public static readonly Regex AdvNumbering  = Parser.GenerateEpisodeRegexes(generateExtractor: true);
    }
}
