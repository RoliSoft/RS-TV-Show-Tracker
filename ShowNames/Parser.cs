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
        /// Gets the root words from a show name.
        /// </summary>
        /// <param name="show">The show's name.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <param name="replaceApostrophes">The character to replace apostrophes to.</param>
        /// <returns>
        /// List of the required words.
        /// </returns>
        public static string[] GetRoot(string show, bool removeCommon = true, string replaceApostrophes = null)
        {
            // see if the show has a different name
            show = show.Trim();
            if (Regexes.Exclusions.ContainsKey(show))
            {
                show = Regexes.Exclusions[show];
            }

            // the CLR is optimized for uppercase string matching
            show = show.ToUpper();

            // remove apostrophes which occur in contractions
            show = Regexes.Contractions.Replace(show, replaceApostrophes ?? string.Empty);

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
        /// <param name="replaceApostrophes">The character to replace apostrophe to.</param>
        /// <returns>Normalized name.</returns>
        public static string Normalize(string show, bool removeCommon = true, string replaceApostrophes = null)
        {
            var episode = string.Empty;

            if (Regexes.Numbering.IsMatch(show))
            {
                var tmp = Regexes.Numbering.Split(show);
                show    = tmp[0];
                episode = tmp[1];
            }

            var parts = String.Join(" ", Database.GetReleaseName(show, removeCommon, replaceApostrophes)).ToLower();

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
        /// <param name="titleParts">The title parts.</param>
        /// <param name="episodeRegex">The episode regex.</param>
        /// <param name="onlyVideo">if set to <c>true</c> returns false for files that are not video.</param>
        /// <returns>
        /// 	<c>true</c> if the specified release matches the specified show and episode; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string name, string[] titleParts, Regex episodeRegex = null, bool onlyVideo = true)
        {
            return !string.IsNullOrWhiteSpace(name)
                && (!onlyVideo || (Regexes.KnownVideo.IsMatch(name) && !Regexes.SampleVideo.IsMatch(name)))
                && (episodeRegex == null || episodeRegex.IsMatch(name))
                && titleParts.All(part =>
                    {
                        if (part.First() == '[' && part.Last() == ']')
                        {
                            return true;
                        }

                        return Regex.IsMatch(name, @"(?:\b|_)" + part + @"(?:\b|_)", RegexOptions.IgnoreCase);
                    });
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
                    @"{0}x(?:(?<em>\d{{1,2}})\-)?0?{1}".FormatWith(season, episode),
                    // [S[e[ason|ries]]] 1[ - ]E[p[isode]] [1 - ]2
                    @"S(?:e(?:ason|ries)?)?[^0-9]?0?{0}[^a-z0-9]*E(?:p(?:isode|\.)?)?[^0-9]?(?:(?<em>\d{{1,2}})[^a-z0-9]{{1,3}})?0?{1}".FormatWith(season, episode),
                    // 213; must be followed by quality notation
                    @"(?:{0}{1:00})[\.\s_](?:\d{{3,4}}[ip]|hdtv|xvid)".FormatWith(generateExtractor ? @"(?<s>\d)" : season, generateExtractor ? (object)episode : (object)episode.ToInteger())
                };

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

        /// <summary>
        /// Names the sequence equals.
        /// </summary>
        /// <param name="first">The first array.</param>
        /// <param name="second">The second array, with optional elements.</param>
        /// <returns>
        ///   <c>true</c> if the names match; otherwise, <c>false</c>.
        /// </returns>
        public static bool NameSequenceEquals(IEnumerable<string> first, IEnumerable<string> second)
        {
            using (var e1 = first.GetEnumerator())
            using (var e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!e2.MoveNext())
                    {
                        return false;
                    }

                compare:
                    if (e1.Current != e2.Current.Trim('[', ']'))
                    {
                        if (e2.Current.First() == '[' && e2.Current.Last() == ']')
                        {
                            if (!e2.MoveNext())
                            {
                                return false;
                            }

                            goto compare;
                        }
                        
                        return false;
                    }
                }

            remaining:
                if (e2.MoveNext())
                {
                    if (e2.Current.First() == '[' && e2.Current.Last() == ']')
                    {
                        goto remaining;
                    }

                    return false;
                }
            }

            return true; 
        }
    }
}
