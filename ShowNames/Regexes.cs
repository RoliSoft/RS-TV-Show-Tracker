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
        /// Simple regular expression to detect S00E00 and 0x00 at the end of a query.
        /// </summary>
        public static readonly Regex Numbering     = new Regex(@"\s+((?:S[0-9]{2}E[0-9]{2})|(?:[0-9]{1,2}x[0-9]{1,2}))", RegexOptions.IgnoreCase);

        /// <summary>
        /// More advanced regular expression to detect season and episode number in various forms in a string.
        /// </summary>
        public static readonly Regex AdvNumbering  = new Regex(@"(
                                                                   # S01E01, S01E01-02, S01E01-E02, S01E01E02
                                                                    S(?<s>[0-9]{1,2})(\.|\s|\-)?E([0-9]{1,2}(?!\-(?:1080|720|480))(\-E?|E))?(?<e>[0-9]{1,2})|
                                                                   # 1x01, 1x01-02
                                                                    (?<s>[0-9]{1,2})x([0-9]{1,2}\-)?(?<e>[0-9]{1,2})
                                                                 )", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
    }
}
