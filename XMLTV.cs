namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using ProtoBuf;
    using Tables;

    /// <summary>
    /// Provides support for parsing XMLTV files and mapping the programming to shows in your database.
    /// </summary>
    public class XMLTV : LocalProgrammingPlugin
    {
        /// <summary>
        /// Gets or sets the name of the plugin.
        /// </summary>
        /// <value>The name of the plugin.</value>
        public override string Name
        {
            get
            {
                return "XMLTV";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2012-01-07 5:13 AM");
            }
        }

        /// <summary>
        /// Gets a list of available programming configurations.
        /// </summary>
        /// <returns>The list of available programming configurations.</returns>
        public override IEnumerable<Configuration> GetConfigurations()
        {
            return new List<XMLTVConfiguration>
                {
                    new XMLTVConfiguration(this)
                        {
                            Name         = "Romania",
                            File         = @"C:\Users\RoliSoft\Desktop\xmltv-0.5.61-win32\guide_ro.xml",
                            Language     = "ro",
                            AdvHuRoParse = true
                        },
                    new XMLTVConfiguration(this)
                        {
                            Name         = "Hungary",
                            File         = @"C:\Users\RoliSoft\Desktop\xmltv-0.5.61-win32\guide_hu.xml",
                            Language     = "hu",
                            AdvHuRoParse = true
                        }
                };
        }

        /// <summary>
        /// Gets a list of upcoming episodes in your area ordered by airdate.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns>
        /// List of upcoming episodes in your area.
        /// </returns>
        public override IEnumerable<Programme> GetListing(Configuration config)
        {
            var cache = Path.Combine(Path.GetTempPath(), "XMLTV-" + ((XMLTVConfiguration)config).File.GetHashCode() + "-" + ((XMLTVConfiguration)config).Language + ".bin");

            if (File.Exists(cache) && File.GetLastWriteTime(cache) > File.GetLastWriteTime(((XMLTVConfiguration)config).File))
            {
                using (var file = File.OpenRead(cache))
                {
                    return Serializer.Deserialize<List<Programme>>(file).Where(p => p.Airdate > DateTime.Now);
                }
            }

            var listing = Filter(ParseFile((XMLTVConfiguration)config), (XMLTVConfiguration)config).ToList();

            using (var file = File.Create(cache))
            {
                Serializer.Serialize(file, listing);
            }

            return listing.Where(p => p.Airdate > DateTime.Now);
        }

        /// <summary>
        /// Parses the specified XMLTV file.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>
        /// List of extracted programmes.
        /// </returns>
        public IEnumerable<Programme> ParseFile(XMLTVConfiguration config)
        {
            var channels = new Dictionary<string, string>();
            var document = XDocument.Load(config.File);

            foreach (var channel in document.Descendants("channel"))
            {
                var id   = channel.Attribute("id");
                var name = channel.Descendants("display-name").ToList();

                if (id == null || !name.Any())
                {
                    continue;
                }

                channels.Add(id.Value, name[0].Value);
            }

            foreach (var programme in document.Descendants("programme"))
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

                var prog = new Programme
                    {
                        Channel  = channels[channel.Value],
                        Show     = title[0].Value.Trim(),
                        Airdate  = airdate
                    };

                if (descr.Count != 0)
                {
                    prog.Description = descr[0].Value.Trim();
                }

                yield return prog;
            }
        }

        /// <summary>
        /// Filters the specified list of programmes and orders them by airdate.
        /// </summary>
        /// <param name="programmes">The full extracted list of programmes.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>
        /// List of filtered and ordered programmes.
        /// </returns>
        public IEnumerable<Programme> Filter(IEnumerable<Programme> programmes, XMLTVConfiguration config)
        {
            var regexes = new Dictionary<Regex, TVShow>();

            foreach (var show in Database.TVShows)
            {
                regexes.Add(new Regex(@"(^|:\s+)" + Regex.Escape(show.Value.Name) + @"(?!(?:[:,0-9]| \- |\s*[a-z]))", RegexOptions.IgnoreCase), show.Value);

                var foreign = show.Value.GetForeignTitle(config.Language);

                if (!string.IsNullOrWhiteSpace(foreign))
                {
                    regexes.Add(new Regex(@"(^|:\s+)" + Regex.Escape(foreign.RemoveDiacritics()) + @"(?!(?:[:,0-9]| \- |\s*[a-z]))", RegexOptions.IgnoreCase), show.Value);
                }
            }

            foreach (var prog in programmes.OrderBy(p => p.Airdate))
            {
                foreach (var regex in regexes)
                {
                    if (regex.Key.IsMatch(prog.Show.RemoveDiacritics()))
                    {
                        if (config.AdvHuRoParse && prog.Description.StartsWith(prog.Show))
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

                            var ep = Regex.Match(prog.Description, @"(?:(?:(?<sr>[IVXLCDM]+)\.\s*/\s*)?(?<e>\d{1,3})\. rész|(?<s>\d{1,2})\. évad(?:,? (?<e>\d{1,3})\. rész)?|(?:sezonul (?<s>\d{1,2}), )?episodul (?<e>\d{1,3}))");

                            prog.Description = Regex.Replace(prog.Description, @"^\s*" + Regex.Escape(prog.Show) + @"\s*(?:\([^\)]+\)\s*)?(?:\(ism\.\)\s*)?(?:\d{1,2}\. évad(?:, \d{1,3}\. rész(?:, )?)?)?(?:\-\s*)?(?:\(reluare\)\s*)?", string.Empty, RegexOptions.IgnoreCase);

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

                        if (config.UseMappedNames)
                        {
                            prog.Show = regex.Value.Name;
                        }

                        yield return prog;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Represents an XMLTV configuration.
        /// </summary>
        public class XMLTVConfiguration : Configuration
        {
            /// <summary>
            /// Gets or sets the location of the XML file.
            /// </summary>
            /// <value>
            /// The location of the XML file.
            /// </value>
            public string File { get; set; }

            /// <summary>
            /// Gets or sets the language of the titles in the programming.
            /// </summary>
            /// <value>
            /// The language of the titles in the programming.
            /// </value>
            public string Language { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether advanced parsing is enabled.
            /// </summary>
            /// <remarks>
            /// Advanced parsing will remove irrelevant information from the description field and
            /// extract the episode notation. This only works for hungarian and romanian listing.
            /// </remarks>
            /// <value>
            ///   <c>true</c> if advanced parsing is enabled; otherwise, <c>false</c>.
            /// </value>
            public bool AdvHuRoParse { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the mapped english names are used or the original ones found in the listing.
            /// </summary>
            /// <value>
            ///   <c>true</c> if the mapped english names are used; otherwise, <c>false</c>.
            /// </value>
            public bool UseMappedNames { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="XMLTVConfiguration"/> class.
            /// </summary>
            /// <param name="plugin">The plugin.</param>
            public XMLTVConfiguration(LocalProgrammingPlugin plugin) : base(plugin)
            {
            }
        }
    }
}
