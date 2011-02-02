namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides methods to work with TV show names.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Gets the root words from a show name.
        /// </summary>
        /// <param name="show">The show's name.</param>
        /// <returns>List of the required words.</returns>
        public static string[] GetRoot(string show)
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
            show = Regexes.Common.Replace(show, string.Empty);
            show = Regexes.OneChar.Replace(show.Trim(), string.Empty);

            // split it up
            var parts = Regexes.Whitespace.Split(Regexes.Whitespace.Replace(show, " ").Trim());

            return parts;
        }

        /// <summary>
        /// Normalizes the specified TV show name.
        /// </summary>
        /// <param name="show">The show name.</param>
        /// <returns>Normalized name.</returns>
        public static string Normalize(string show)
        {
            var episode = string.Empty;

            if (Regexes.Numbering.IsMatch(show))
            {
                var tmp = Regexes.Numbering.Split(show);
                show = tmp[0];
                episode = tmp[1];
            }

            var parts = String.Join(" ", GetRoot(show)).ToLower();

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
            return IsMatch(show[0], show[1], release);
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
            var title = GetRoot(name);
            var episodes = new[] {
                                   episode, // S02E14
                                   episode.Replace("E", ".E"), // S02.E14
                                   Regex.Replace(episode, "S0?([0-9]{1,2})E([0-9]{1,2})", ".$1X$2.", RegexOptions.IgnoreCase), // 2x14
                                   Regex.Replace(episode, "S0?([0-9]{1,2})E([0-9]{1,2})", ".$1$2.", RegexOptions.IgnoreCase) // 214
                                 };

            return title.All(part => Regex.IsMatch(release, @"\b" + part + @"\b", RegexOptions.IgnoreCase)) // does it have all the title words?
                && episodes.Any(ep => release.ToUpper().Contains(ep)) // is it the episode we want?
                   ;
        }

        /// <summary>
        /// Extracts the episode number.
        /// </summary>
        /// <param name="name">The show name with episode.</param>
        /// <returns>Episode in specified format.</returns>
        public static ShowEpisode ExtractEpisode(string name)
        {
            var m = Regexes.AdvNumbering.Match(name);
            return m.Success
                   ? new ShowEpisode
                       {
                           Season = m.Groups["s"].Value.ToInteger(),
                           Episode = m.Groups["e"].Value.ToInteger()
                       }
                   : null;
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
        /// <returns>Show name with new episode notation.</returns>
        public static string ReplaceEpisode(string query, string format = "S{0:00}E{1:00}", bool normalize = false)
        {
            return (normalize
                   ? Normalize(Split(query)[0])
                   : Split(query)[0])
                     + " " + ExtractEpisode(query, format);
        }
    }
}
