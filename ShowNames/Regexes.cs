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
                { "Sci-Fi Science: Physics of the Impossible", "Sci Fi Science"   },
                { "$#*! My Dad Says",                          "Shit My Dad Says" },
                { "Bleep My Dad Says",                         "Shit My Dad Says" },
                { "Don't Trust the Bitch in Apartment 23",     "Apartment 23"     },
                { "Don't Trust the B---- in Apartment 23",     "Apartment 23"     },
            };

        /// <summary>
        /// List of TV shows which have a name so weird they need a hand-written regular expression.
        /// </summary>
        public static readonly Dictionary<string, string> Pregenerated = new Dictionary<string, string>
            {
                { "Apartment 23", @"(?:\b|_)(?:DON(?:\\?['`’\._])?T[^A-Z0-9]+TRUST[^A-Z0-9]+THE[^A-Z0-9]+B(?:ITCH|\-+)?[^A-Z0-9]+IN[^A-Z0-9]+)?APARTMENT[^A-Z0-9]+23(?:\b|_)" },
                { "Hacktion Újratöltve", "(?:\b|_)HACKTION[^A-Z0-9]+[UÚú]JRAT[OÖö]LTVE(?:\b|_)" },
            };

        /// <summary>
        /// Removes everything except letters, numbers and parentheses.
        /// </summary>
        public static readonly Regex SpecialChars  = new Regex(@"([^A-Z0-9\s\(\)\0])");

        /// <summary>
        /// Matches 2000-2099 in brackets with leading space.
        /// </summary>
        public static readonly Regex NewYear       = new Regex(@"\s\(20\d{2}\)");

        /// <summary>
        /// Matches 1800-2999 in brackets.
        /// </summary>
        public static readonly Regex Year          = new Regex(@"\((1[89]\d{2}|2\d{3})\)");

        /// <summary>
        /// Matches US, UK and AU in brackets with leading space.
        /// </summary>
        public static readonly Regex Countries     = new Regex(@"\s\((US|UK|AU)\)");

        /// <summary>
        /// Matches contractions in the english language.
        /// </summary>
        public static readonly Regex Contractions  = new Regex(@"(?<=[A-z0-9])['`’\._](?=(?:S|VE|NT|RE|EM|LL)\b)");

        /// <summary>
        /// Matches an ampersand delimited by word boundaries.
        /// </summary>
        public static readonly Regex Ampersand     = new Regex(@"(?<=[\s_\-])&(?=[\s_\-])");

        /// <summary>
        /// Matches the four most common words in the english language. (and, the, of)
        /// </summary>
        public static readonly Regex Common        = new Regex(@"\b(AND\s?|THE\s?|OF\s?)\b");

        /// <summary>
        /// Matches a single character surrounded by word boundaries, except if it is the first character or has a null placeholder.
        /// </summary>
        public static readonly Regex OneChar       = new Regex(@"(?<!^)\b(?<!\0)([A-Z]\s?)\b");

        /// <summary>
        /// Matches strings starting with "the".
        /// </summary>
        public static readonly Regex StrictCommon  = new Regex(@"^THE\b");

        /// <summary>
        /// Matches any single letter with the exclusion of "a" and "i".
        /// </summary>
        public static readonly Regex StrictOneChar = new Regex(@"(?<!^)\b(?<!\0)[B-HJ-Z]\b");

        /// <summary>
        /// Matches any whitespace.
        /// </summary>
        public static readonly Regex Whitespace    = new Regex(@"\s+");

        /// <summary>
        /// Matches any part numbering in the episode title.
        /// </summary>
        public static readonly Regex PartText      = new Regex(@"(?:,? part \d{1,2}(?: of \d{1,2})| \((?:part ?)?\d{1,2}\))\s*$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches video file extensions, including popular archives.
        /// </summary>
        public static readonly Regex KnownVideo    = new Regex(@"\.(avi|mkv|mp4|ts|wmv|rar|zip|001)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches subtitle file extensions.
        /// </summary>
        public static readonly Regex KnownSubtitle = new Regex(@"\.(srt|sub|ass|ssa|smi|usf)$", RegexOptions.IgnoreCase);

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
        public static readonly Regex Group         = new Regex(@"\-\s?(?<group>[a-z0-9&]+)(?:\.[a-z0-9]+)?$", RegexOptions.IgnoreCase);
        
        /// <summary>
        /// Matches a group name if a release name starts with it.
        /// </summary>
        public static readonly Regex StartGroup    = new Regex(@"^(?<group>(?:[a-z0-9&]+\-|(?:\[[^\]]+\]\s*|\([^\)]+\)\s*)+))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Simple regular expression to detect S00E00 and 0x00 at the end of a query.
        /// </summary>
        public static readonly Regex Numbering     = new Regex(@"\s+((?:S[0-9]{2}(?:E[0-9]{2})?)|(?:[0-9]{1,2}x[0-9]{1,2}))", RegexOptions.IgnoreCase);

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
