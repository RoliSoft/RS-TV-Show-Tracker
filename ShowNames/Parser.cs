namespace RoliSoft.TVShowTracker.ShowNames
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides methods to work with TV show names.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Generates a regular expression for matching the show's name.
        /// </summary>
        /// <param name="show">The show's name.</param>
        /// <returns>
        /// Regular expression which matches to the show's name.
        /// </returns>
        public static Regex GenerateTitleRegex(string show)
        {
            // see if the show has a different name
            show = show.Trim();
            if (Regexes.Exclusions.ContainsKey(show))
            {
                show = Regexes.Exclusions[show];
            }

            // see if the show already has a hand-written regex
            if (Regexes.Pregenerated.ContainsKey(show))
            {
                return new Regex(Regexes.Pregenerated[show], RegexOptions.IgnoreCase);
            }

            // the CLR is optimized for uppercase string matching
            show = show.ToUpper();

            // replace apostrophes which occur in contractions to a null placeholder
            show = Regexes.Contractions.Replace(show, "\0");

            // replace "&" to "and"
            show = Regexes.Ampersand.Replace(show, "AND");

            // remove special characters
            show = Regexes.SpecialChars.Replace(show, " ").Trim();

            // remove parentheses
            //show = show.Replace("(", string.Empty).Replace(")", string.Empty);

            // make year optional
            show = Regexes.Year.Replace(show, m => "(?:" + m.Groups[1].Value + ")?");

            // make common words and single characters optional
            show = Regexes.Common.Replace(show, m => "(?:" + m.Groups[1].Value + ")?");
            show = Regexes.OneChar.Replace(show, m => "(?:" + m.Groups[1].Value + ")?");

            // replace null placeholder for apostrophes
            show = show.Replace("\0", @"(?:\\?['`’\._])?");

            // replace whitespace to non-letter matcher
            show = Regexes.Whitespace.Replace(show.Trim(), "[^A-Z0-9]+");

            // quick fix for ending optional tags
            show = show.Replace("[^A-Z0-9]+(?:", "[^A-Z0-9]*(?:").Replace(")?(?:", ")?[^A-Z0-9]*(?:");

            // add boundary restrictions
            show = @"(?:\b|_)" + show + @"(?:\b|_)";

            return new Regex(show, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Removes unnecessary stuff from a show's name.
        /// </summary>
        /// <param name="show">The show's name.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <returns>
        /// Clean title.
        /// </returns>
        public static string CleanTitle(string show, bool removeCommon = true)
        {
            // see if the show has a different name
            show = show.Trim();
            if (Regexes.Exclusions.ContainsKey(show))
            {
                show = Regexes.Exclusions[show];
            }

            // the CLR is optimized for uppercase string matching
            show = show.ToUpper();

            // replace apostrophes which occur in contractions to a null placeholder
            show = Regexes.Contractions.Replace(show, "\0");

            // remove special characters
            show = Regexes.SpecialChars.Replace(show, " ").Trim();

            // remove year if the show started later than 2000
            if (Regexes.NewYear.IsMatch(show))
            {
                show = Regexes.NewYear.Replace(show, string.Empty);
            }

            // remove parentheses
            show = show.Replace("(", string.Empty).Replace(")", string.Empty);

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

            // replace null placeholder for apostrophes
            show = show.Replace("\0", "'");

            // replace subsequent whitespace
            show = Regexes.Whitespace.Replace(show, " ").Trim();

            return show.ToLower().ToUppercaseWords();
        }

        /// <summary>
        /// Removes unnecessary stuff from a show's name.
        /// </summary>
        /// <param name="show">The show's name including episode notation.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <returns>
        /// Clean title with episode notation.
        /// </returns>
        public static string CleanTitleWithEp(string show, bool removeCommon = true)
        {
            var episode = string.Empty;

            if (Regexes.Numbering.IsMatch(show))
            {
                var tmp = Regexes.Numbering.Split(show);
                show    = tmp[0];
                episode = tmp[1];
            }

            var parts = CleanTitle(show, removeCommon);

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
            return IsMatch(release, Database.GetReleaseName(name), !string.IsNullOrEmpty(episode) ? GenerateEpisodeRegexes(episode) : null, false);
        }

        /// <summary>
        /// Determines whether the specified release name matches with the specified show and episode.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="titleRegex">The title regex.</param>
        /// <param name="episodeRegex">The episode regex.</param>
        /// <param name="onlyVideo">if set to <c>true</c> returns false for files that are not video.</param>
        /// <returns>
        ///   <c>true</c> if the specified release matches the specified show and episode; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string name, Regex titleRegex, Regex episodeRegex = null, bool onlyVideo = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (onlyVideo && (!Regexes.KnownVideo.IsMatch(name) || Regexes.SampleVideo.IsMatch(name)))
            {
                return false;
            }

            if (episodeRegex != null && !episodeRegex.IsMatch(name))
            {
                return false;
            }

            return titleRegex.IsMatch(name);
        }

        /// <summary>
        /// Extracts the episode number.
        /// </summary>
        /// <param name="name">The show name with episode.</param>
        /// <returns>Episode in specified format.</returns>
        public static ShowEpisode ExtractEpisode(string name)
        {
            return ExtractEpisode(Regexes.AdvNumbering.Match(name));
        }

        /// <summary>
        /// Extracts the episode number.
        /// </summary>
        /// <param name="m">The regex match.</param>
        /// <returns>Episode in specified format.</returns>
        public static ShowEpisode ExtractEpisode(Match m)
        {
            if (m.Success)
            {
                if (m.Groups["y"].Success)
                {
                    return new ShowEpisode(new DateTime(m.Groups["y"].Value.ToInteger(), m.Groups["m"].Value.ToInteger(), m.Groups["d"].Value.ToInteger()));
                }

                int e;
                var se = new ShowEpisode();

                if (m.Groups["s"].Success)
                {
                    se.Season = m.Groups["s"].Value.ToInteger();
                }
                else
                {
                    se.Season = 1;
                }

                if (m.Groups["em"].Success)
                {
                    if (int.TryParse(m.Groups["em"].Value, out e))
                    {
                        se.Episode = e;
                    }
                    else
                    {
                        se.Episode = Utils.RomanToNumber(m.Groups["em"].Value);
                    }

                    if (int.TryParse(m.Groups["e"].Value, out e))
                    {
                        se.SecondEpisode = e;
                    }
                    else
                    {
                        se.SecondEpisode = Utils.RomanToNumber(m.Groups["e"].Value);
                    }
                }
                else
                {
                    if (int.TryParse(m.Groups["e"].Value, out e))
                    {
                        se.Episode = e;
                    }
                    else
                    {
                        se.Episode = Utils.RomanToNumber(m.Groups["e"].Value);
                    }
                }

                return se;
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
                query = CleanTitleWithEp(query, removeCommon);
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
        /// <param name="episode">The raw episode.</param>
        /// <param name="airdate">The airdate.</param>
        /// <returns>
        /// List of regular expressions.
        /// </returns>
        public static Regex GenerateEpisodeRegexes(string episode, DateTime? airdate = null)
        {
            var ep = ExtractEpisode(episode);
            ep.AirDate = airdate;
            return GenerateEpisodeRegexes(ep);
        }

        /// <summary>
        /// Generates regular expressions for matching a huge variety of episode numberings.
        /// </summary>
        /// <param name="episode">The extracted episode.</param>
        /// <returns>List of regular expressions.</returns>
        public static Regex GenerateEpisodeRegexes(ShowEpisode episode)
        {
            return GenerateEpisodeRegexes(episode.Season.ToString(), episode.Episode.ToString(), episode.AirDate);
        }

        /// <summary>
        /// Generates regular expressions for matching a huge variety of episode numberings.
        /// </summary>
        /// <param name="season">The season number or expression.</param>
        /// <param name="episode">The episode number or expression.</param>
        /// <param name="airdate">The airdate.</param>
        /// <param name="generateExtractor">if set to <c>true</c> generates an expression which universally matches any season/episode.</param>
        /// <returns>
        /// List of regular expressions.
        /// </returns>
        public static Regex GenerateEpisodeRegexes(string season = null, string episode = null, DateTime? airdate = null, bool generateExtractor = false)
        {
            if (generateExtractor)
            {
                season  = @"(?<s>\d{1,2})";
                episode = @"(?<e>\d{1,3})";
            }

            var regexes = new List<string>
                {
                    // S[0]2[.]E[P][13-]14
                    @"S0?{0}[^0-9]?EP?(?:(?<em>\d{{1,2}})[\-E_](?:EP?)?)?0?{1}(?:[E_].+)?".FormatWith(season, episode),
                    // 2x[13-]14
                    @"0?{0}x(?:(?<em>\d{{1,2}})\-)?0?{1}".FormatWith(season, episode),
                    // [S[e[ason|ries]]] 1[ - ]E[p[isode]] [1 - ]2
                    @"S(?:e(?:ason|ries)?)?[^0-9]?0?{0}[^a-z0-9]*E(?:p(?:isode|\.)?)?[^0-9]?(?:(?<em>\d{{1,2}})[^a-z0-9]{{1,3}})?0?{1}".FormatWith(season, episode)
                };

            if (Settings.Get("Enable Shortest Notation", true))
            {
                regexes.Add(
                    // 213
                    @"(?<x>{0}{1:00})(?:(?=[\.\s_])|$)".FormatWith(season, generateExtractor ? (object)@"(?<e>\d{2})" : (object)episode.ToInteger())
                );
            }

            if (airdate.HasValue)
            {
                regexes.Add(
                    // 2011-03-14
                    @"(?:{0}\.0?{1}\.0?{2}|{0}\-0?{1}\-0?{2}|{0}_0?{1}_0?{2}|{0}\s0?{1}\s0?{2})".FormatWith(airdate.Value.Year, airdate.Value.Month, airdate.Value.Day)
                );
            }
            else if (generateExtractor)
            {
                regexes.Add(@"(?:(?<y>\d{4})\.(?<m>\d{1,2})\.(?<d>\d{1,2})|(?<y>\d{4})\-(?<m>\d{1,2})\-(?<d>\d{1,2})|(?<y>\d{4})_(?<m>\d{1,2})_(?<d>\d{1,2})|(?<y>\d{4})\s(?<m>\d{1,2})\s(?<d>\d{1,2}))");
            }

            if (generateExtractor || season == "1")
            {
                var roman = generateExtractor
                            ? @"(?<e>[IVXLCDM]+)"
                            : Utils.NumberToRoman(episode.ToInteger());

                regexes.AddRange(new[]
                    {
                        // E[P][13-]14
                        @"(?<!(?:Season|Series|S).*\d{{1,2}}.*)EP?(?:(?<em>\d{{1,2}})\-(?:EP?)?)?0?{0}".FormatWith(episode),
                        // [E[p[isode]]|P[ar]t|Vol[ume]][ ][9|IX]
                        @"(?<!(?:Season|Series|S).*\d{{1,2}}.*)(?:E(?:p(?:isode)?)?|P(?:ar)?t|Vol(?:ume)?)[^a-z0-9]?(?:(?:0?(?<em>\d{{1,2}})|(?<em>[IVXLCDM]+))[^a-z0-9]{{1,3}})?(?:0?{0}|{1})".FormatWith(episode, roman)
                    });
            }

            var expr = regexes.Aggregate(@"(?:\b|_)(" + (!generateExtractor ? "?:" : string.Empty), (current, format) => current + (format + "|")).TrimEnd('|') + @")(?:\b|_)";

            return new Regex(expr, RegexOptions.IgnoreCase);
        }
    }
}
