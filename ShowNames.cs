namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides methods to work with TV show names.
    /// </summary>
    public static class ShowNames
    {
        private static readonly Regex _removeSpecialChars = new Regex(@"([^a-z0-9\s]|\bthe\b|\band\b)", RegexOptions.IgnoreCase),
                                      _episodeNumber      = new Regex(@"\s+(?=S[0-9]{1,2}E[0-9]{1,2})", RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, string> _sceneNames = new Dictionary<string, string>
            {
                { "House, M.D.", "House" },
                { "Battlestar Galactica (2003)", "Battlestar Galactica" },
                { "Supernatural (2005)", "Supernatural" },
                { "The Universe", "The Universe" },
                { "The Simpsons", "The Simpsons" } // this is NOT an unnecessary item. without this, the regex would run and the "The " would be removed.
            };
        
        /// <summary>
        /// Normalizes the specified TV show name.
        /// </summary>
        /// <param name="show">The show name.</param>
        /// <returns>Normalized name.</returns>
        public static string Normalize(string show)
        {
            show = show.Trim();

            string episode = null;

            if (_episodeNumber.IsMatch(show))
            {
                var tmp = _episodeNumber.Split(show);
                show    = tmp.First();
                episode = tmp.Last();
            }

            show = _sceneNames.ContainsKey(show) ? _sceneNames[show] : _removeSpecialChars.Replace(show, string.Empty);

            return show + (!string.IsNullOrWhiteSpace(episode) ? " " + episode : string.Empty);
        }

        /// <summary>
        /// Splits the name of the show and the episode number into two strings. Supports S00E00 and 0x00 formats.
        /// </summary>
        /// <returns>Show name and episode number separated.</returns>
        public static string[] Split(string query)
        {
            var info = Regex.Match(query, @"^(?<show>.*?)\s*(?<episode>(?:S[0-9]{2}E[0-9]{2})|(?:[0-9]{1,2}x[0-9]{1,2}))", RegexOptions.IgnoreCase);

            return new[]
                {
                    info.Groups["show"].Value,
                    info.Groups["episode"].Value
                };
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
            var title    = Normalize(name).Split(' ');
            var episodes = new[] { episode, // S02E14
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
            var m = Regex.Match(name, @"(
                                          # S01E01, S01E01-02, S01E01-E02, S01E01E02
                                           S(?<s>[0-9]{1,2})(\.|\s|\-)?E([0-9]{1,2}(?!\-(?:1080|720|480))(\-E?|E))?(?<e>[0-9]{1,2})|
                                          # 1x01, 1x01-02
                                           (?<s>[0-9]{1,2})x([0-9]{1,2}\-)?(?<e>[0-9]{1,2})
                                        )",
                                  RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

            if (m.Success)
            {
                return new ShowEpisode 
                    {
                        Season  = m.Groups["s"].Value.ToInteger(),
                        Episode = m.Groups["e"].Value.ToInteger()
                    };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts the episode number into the specified format.
        /// </summary>
        /// <param name="name">The show name with episode.</param>
        /// <param name="format">The format: 0 for S00E00; 1 for 0x00.</param>
        /// <returns>Episode in specified format.</returns>
        public static string ExtractEpisode(string name, int format)
        {
            var ep = ExtractEpisode(name);

            if (ep != null)
            {
                switch (format)
                {
                    default:
                        return "S" + ep.Season.ToString("00") + "E" + ep.Episode.ToString("00");
                    case 1:
                        return ep.Season.ToString("0") + "x" + ep.Episode.ToString("00");
                }
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Replaces the episode notation into the specified format.
        /// </summary>
        /// <param name="query">The show name with episode.</param>
        /// <param name="format">The format, see <c>ExtractEpisode()</c> for this parameter.</param>
        /// <param name="normalize">if set to <c>true</c> it will also normalize the name.</param>
        /// <returns>Show name with new episode notation.</returns>
        public static string ReplaceEpisode(string query, int format, bool normalize = false)
        {
            return (normalize
                   ? Normalize(Split(query)[0])
                   : Split(query)[0])
                     + " " + ExtractEpisode(query, format);
        }
    }

    /// <summary>
    /// Provides support for comparing shows for Linq functions.
    /// </summary>
    public class ShowEqualityComparer : IEqualityComparer<string>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object of type <paramref name="T"/> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T"/> to compare.</param>
        public bool Equals(string x, string y)
        {
            return ShowNames.Normalize(x).ToUpper().Replace(" ", string.Empty) == ShowNames.Normalize(y).ToUpper().Replace(" ", string.Empty);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(string obj)
        {
            return ShowNames.Normalize(obj).ToUpper().Replace(" ", string.Empty).GetHashCode();
        }
    }

    /// <summary>
    /// Represents an episode.
    /// </summary>
    public class ShowEpisode
    {
        /// <summary>
        /// Gets or sets the season of the episode.
        /// </summary>
        /// <value>The season.</value>
        public int Season { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        /// <value>The episode.</value>
        public int Episode { get; set; }
    }
}
