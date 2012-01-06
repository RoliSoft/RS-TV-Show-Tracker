namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Tables;

    /// <summary>
    /// Provides support for parsing XMLTV files and mapping the programming to shows in your database.
    /// </summary>
    public class XMLTV
    {
        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>
        /// The channels.
        /// </value>
        public Dictionary<string, string> Channels { get; set; }

        /// <summary>
        /// Gets or sets the programmes.
        /// </summary>
        /// <value>
        /// The programmes.
        /// </value>
        public List<Programme> Programmes { get; set; }

        /// <summary>
        /// Gets or sets the language of the titles in the programming.
        /// </summary>
        /// <value>
        /// The language of the titles in the programming.
        /// </value>
        public string Language { get; set; }

        /// <summary>
        /// Loads the programming from the specified file.
        /// </summary>
        /// <param name="file">The XMLTV file.</param>
        /// <param name="language">The language of the titles in the programming.</param>
        public void LoadFrom(string file, string language)
        {
            Channels   = new Dictionary<string, string>();
            Programmes = new List<Programme>();
            Language   = language;

            var doc = XDocument.Load(file);

            foreach (var channel in doc.Descendants("channel"))
            {
                var id   = channel.Attribute("id");
                var name = channel.Descendants("display-name").ToList();

                if (id == null || !name.Any())
                {
                    continue;
                }

                Channels.Add(id.Value, name[0].Value);
            }

            foreach (var programme in doc.Descendants("programme"))
            {
                var channel = programme.Attribute("channel");
                var start   = programme.Attribute("start");
                var title   = programme.Descendants("title").ToList();
                var descr   = programme.Descendants("desc").ToList();

                if (channel == null || start == null || !title.Any())
                {
                    continue;
                }

                DateTime airdate;

                if (!DateTime.TryParseExact(start.Value, "yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out airdate))
                {
                    continue;
                }

                if (DateTime.Now > airdate)
                {
                    continue;
                }

                var prog = new Programme
                    {
                        Channel  = Channels[channel.Value],
                        Title    = title[0].Value.Trim(),
                        Airdate  = airdate
                    };

                if (descr.Count != 0)
                {
                    prog.Description = descr[0].Value.Trim();
                }

                Programmes.Add(prog);
            }
        }

        /// <summary>
        /// Filters any show that is not in the main database and returns the programming ordered by airdate.
        /// </summary>
        /// <returns>
        /// Filtered and ordered XMLTV programming.
        /// </returns>
        public IEnumerable<Programme> GetListing()
        {
            var regexes  = new Dictionary<Regex, TVShow>();

            foreach (var show in Database.TVShows)
            {
                regexes.Add(new Regex(@"(^|:\s+)" + Regex.Escape(show.Value.Name) + @"(?!(?:[:,0-9]| \- |\s*[a-z]))", RegexOptions.IgnoreCase), show.Value);

                var foreign = show.Value.GetForeignTitle(Language);

                if (!string.IsNullOrWhiteSpace(foreign))
                {
                    regexes.Add(new Regex(@"(^|:\s+)" + Regex.Escape(foreign.RemoveDiacritics()) + @"(?!(?:[:,0-9]| \- |\s*[a-z]))", RegexOptions.IgnoreCase), show.Value);
                }
            }

            foreach (var prog in Programmes.OrderBy(p => p.Airdate))
            {
                foreach (var regex in regexes)
                {
                    if (regex.Key.IsMatch(prog.Title.RemoveDiacritics()))
                    {
                        if (prog.Description.StartsWith(prog.Title))
                        {
                            #region Explanation
                            /*
                             * This fix is specific to port.hu/ro and tv_huro_grab: remove the title from the description.
                             * For example, the listing for Discovery Science Romania looks like this:
                             * 
                             *   <title lang="ro">Morgan Freeman şi spaţiul cosmic</title>
                             *   <desc  lang="ro">Morgan Freeman şi spaţiul cosmic (f. s. doc.) - Există viaţă după moarte?</desc>
                             * 
                             * Similarly, Discovery Science Hungary has this as well:
                             * 
                             *   <title lang="hu">Morgan Freeman: a féreglyukon át</title>
                             *   <desc  lang="hu">Morgan Freeman: a féreglyukon át (ism. sor.) - Van-e élet a halál után?</desc>
                             * 
                             * We can't split by by the separator, because those aren't always included. See Discover HD Romania:
                             * 
                             *   <title lang="ro">Through the Wormhole with Morgan Freeman</title>
                             *   <desc  lang="ro">Through the Wormhole with Morgan Freeman Is There Life After Death?</desc>
                             * 
                             * This fix removes anything but the episode's title from the description.
                             */
                            #endregion

                            var ep = Regex.Match(prog.Description, @"(?:(?:(?<sr>[IVXLCDM]+)\.\s*/\s*)?(?<e>\d{1,3})\. rész|(?<s>\d{1,2})\. évad(?:, (?<e>\d{1,3})\. rész)?|(?:sezonul (?<s>\d{1,2}), )?episodul (?<e>\d{1,3}))");

                            prog.Description = Regex.Replace(prog.Description, @"^\s*" + Regex.Escape(prog.Title) + @"\s*(?:\([^\)]+\)\s*)?(?:\(ism\.\)\s*)?(?:\d{1,2}\. évad(?:, \d{1,3}\. rész(?:, )?)?)?(?:\-\s*)?(?:\(reluare\)\s*)?", string.Empty, RegexOptions.IgnoreCase);

                            if (ep.Success)
                            {
                                prog.Number = string.Empty;

                                if (ep.Groups["sr"].Success)
                                {
                                    prog.Number = "S" + Utils.RomanToNumber(ep.Groups["sr"].Value).ToString("00");

                                    if (ep.Groups["e"].Success)
                                    {
                                        prog.Number += "/" + int.Parse(ep.Groups["e"].Value);
                                    }
                                }
                                else
                                {
                                    if (ep.Groups["s"].Success)
                                    {
                                        prog.Number = "S" + int.Parse(ep.Groups["s"].Value).ToString("00");
                                    }
                                    else if (ep.Groups["e"].Success)
                                    {
                                        prog.Number += "#" + int.Parse(ep.Groups["e"].Value);
                                    }
                                    if (ep.Groups["s"].Success && ep.Groups["e"].Success)
                                    {
                                        prog.Number += "E" + int.Parse(ep.Groups["e"].Value).ToString("00");
                                    }
                                }
                            }
                        }

                        prog.Title = regex.Value.Name;

                        yield return prog;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Represents a programming in an XMLTV listing.
        /// </summary>
        public class Programme
        {
            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>
            /// The title.
            /// </value>
            public string Title { get; set; }

            /// <summary>
            /// Gets or sets the episode number.
            /// </summary>
            /// <value>
            /// The episode number.
            /// </value>
            public string Number { get; set; }

            /// <summary>
            /// Gets or sets the description.
            /// </summary>
            /// <value>
            /// The description.
            /// </value>
            public string Description { get; set; }

            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            /// <value>
            /// The channel.
            /// </value>
            public string Channel { get; set; }

            /// <summary>
            /// Gets or sets the airdate.
            /// </summary>
            /// <value>
            /// The airdate.
            /// </value>
            public DateTime Airdate { get; set; }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return Title + " [" + Channel + "]";
            }
        }
    }
}
