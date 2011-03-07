namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides methods to work with TV show names.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Gets the root words from a show name.
        /// </summary>
        /// <param name="show">The show's name.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <returns>List of the required words.</returns>
        public static string[] GetRoot(string show, bool removeCommon = true)
        {
            // see if the show has a different name
            show = show.Trim();
            if (Regexes.Exclusions.ContainsKey(show))
            {
                show = Regexes.Exclusions[show];
            }

            // the CLR is optimized for uppercase string matching
            show = show.ToUpper();

            // remove special characters
            show = Regexes.SpecialChars.Replace(show, " ").Trim();

            // remove year if the show started later than 2000
            if (Regexes.NewYear.IsMatch(show))
            {
                show = Regexes.NewYear.Replace(show, string.Empty);
            }

            // remove country
            if (Regexes.Countries.IsMatch(show))
            {
                show = Regexes.Countries.Replace(show, string.Empty);
            }

            // remove common words and single characters
            if (removeCommon)
            {
                show = Regexes.Common.Replace(show, string.Empty);
                show = Regexes.OneChar.Replace(show.Trim(), string.Empty);
            }
            else
            {
                show = Regexes.StrictCommon.Replace(show, string.Empty);
                show = Regexes.StrictOneChar.Replace(show.Trim(), string.Empty);
            }

            // split it up
            var parts = Regexes.Whitespace.Split(Regexes.Whitespace.Replace(show, " ").Trim());

            return parts;
        }

        /// <summary>
        /// Normalizes the specified TV show name.
        /// </summary>
        /// <param name="show">The show name.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <returns>Normalized name.</returns>
        public static string Normalize(string show, bool removeCommon = true)
        {
            var episode = string.Empty;

            if (Regexes.Numbering.IsMatch(show))
            {
                var tmp = Regexes.Numbering.Split(show);
                show    = tmp[0];
                episode = tmp[1];
            }

            var parts = String.Join(" ", GetRoot(show, removeCommon)).ToLower();

            return episode.Length != 0
                   ? parts + " " + episode
                   : parts;
        }

        /// <summary>
        /// Splits the name of the show and the episode number into two strings. Supports S00E00 and 0x00 formats.
        /// </summary>
        /// <returns>Show name and episode number separated.</returns>
        public static string[] Split(string query)
        {
            return Regexes.Numbering.Split(query);
        }

        /// <summary>
        /// Determines whether the specified release name matches with the specified show and episode.
        /// </summary>
        /// <param name="query">The show and episode number.</param>
        /// <param name="release">The release name.</param>
        /// <returns>
        /// 	<c>true</c> if the specified release matches the specified show and episode; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string query, string release)
        {
            var show = Split(query);
            return IsMatch(show[0], show.Length != 1 ? show[1] : string.Empty, release);
        }

        /// <summary>
        /// Determines whether the specified release name matches with the specified show and episode.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="release">The release name.</param>
        /// <returns>
        /// 	<c>true</c> if the specified release matches the specified show and episode; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string name, string episode, string release)
        {
            var title    = GetRoot(name);
            var episodes = new[] {
                                   episode, // S02E14
                                   episode.Replace("E", "\bE"), // S02.E14
                                   Regex.Replace(episode, "S0?([0-9]{1,2})E([0-9]{1,2})", "$1X$2", RegexOptions.IgnoreCase), // 2x14
                                   Regex.Replace(episode, "S0?([0-9]{1,2})E([0-9]{1,2})", "$1$2", RegexOptions.IgnoreCase) // 214
                                 };

            return title.All(part => Regex.IsMatch(release, @"\b" + part + @"\b", RegexOptions.IgnoreCase)) // does it have all the title words?
                && episodes.Any(ep => Regex.IsMatch(release, @"\b" + ep + @"\b", RegexOptions.IgnoreCase)); // is it the episode we want?
        }

        /// <summary>
        /// Extracts the episode number.
        /// </summary>
        /// <param name="name">The show name with episode.</param>
        /// <returns>Episode in specified format.</returns>
        public static ShowEpisode ExtractEpisode(string name)
        {
            var m = Regexes.AdvNumbering.Match(name);

            if (m.Success)
            {
                if (m.Groups["em"].Success)
                {
                    return new ShowEpisode
                       {
                           Season        = m.Groups["s"].Value.ToInteger(),
                           Episode       = m.Groups["em"].Value.ToInteger(),
                           SecondEpisode = m.Groups["e"].Value.ToInteger()
                       };
                }
                else
                {
                    return new ShowEpisode
                       {
                           Season  = m.Groups["s"].Value.ToInteger(),
                           Episode = m.Groups["e"].Value.ToInteger()
                       };
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the episode number into the specified format.
        /// </summary>
        /// <param name="name">The show name with episode.</param>
        /// <param name="format">The format which will be passed to <c>string.Format()</c>.</param>
        /// <returns>Episode in specified format.</returns>
        public static string ExtractEpisode(string name, string format = "S{0:00}E{1:00}")
        {
            var ep = ExtractEpisode(name);

            return ep != null
                   ? string.Format(format, ep.Season, ep.Episode)
                   : string.Empty;
        }

        /// <summary>
        /// Replaces the episode notation into the specified format.
        /// </summary>
        /// <param name="query">The show name with episode.</param>
        /// <param name="format">The format, see <c>ExtractEpisode()</c> for this parameter.</param>
        /// <param name="normalize">if set to <c>true</c> it will also normalize the name.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <returns>Show name with new episode notation.</returns>
        public static string ReplaceEpisode(string query, string format = "S{0:00}E{1:00}", bool normalize = false, bool removeCommon = true)
        {
            if (normalize)
            {
                query = Normalize(query, removeCommon);
            }

            return Split(query)[0] + " " + ExtractEpisode(query, format);
        }

        /// <summary>
        /// Generates regular expressions for matching a huge variety of episode numberings.
        /// </summary>
        /// <param name="episode">The raw episode.</param>
        /// <returns>List of regular expressions.</returns>
        public static Regex GenerateEpisodeRegexes(string episode)
        {
            return GenerateEpisodeRegexes(ExtractEpisode(episode));
        }

        /// <summary>
        /// Generates regular expressions for matching a huge variety of episode numberings.
        /// </summary>
        /// <param name="episode">The extracted episode.</param>
        /// <returns>List of regular expressions.</returns>
        public static Regex GenerateEpisodeRegexes(ShowEpisode episode)
        {
            var regexes = new[]
                {
                    // S02E14
                    @"S{0:00}E{1:00}",
                    // S02E13-14
                    @"S{0:00}E(?:[0-9]{{1,2}}\-E?){1:00}",
                    // S02.E14
                    @"S{0:00}.E{1:00}",
                    // S02.E13-14
                    @"S{0:00}.E(?:[0-9]{{1,2}}.?\-.?E?){1:00}",
                    // 2x14
                    @"{0:0}X{1:00}",
                    // 2x13-14
                    @"{0:0}X(?:[0-9]{{1,2}}\-){1:00}",
                    // 214
                    @"{0:0}{1:00}",
                };

            var expr = regexes.Aggregate(@"\b(?:", (current, format) => current + (format.FormatWith(episode.Season, episode.Episode) + "|")).TrimEnd('|') + @")\b";

            return new Regex(expr, RegexOptions.IgnoreCase);
        }
    }
}
