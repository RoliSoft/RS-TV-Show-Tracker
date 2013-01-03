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

    using Parsers.Guides;

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
        /// Gets the URL to the plugin's icon.
        /// </summary>
        /// <value>The location of the plugin's icon.</value>
        public override string Icon
        {
            get
            {
                return "http://wiki.xmltv.org/favicon.ico";
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
            var settings = Settings.Get<List<Dictionary<string, object>>>("XMLTV");

            if (settings == null || !settings.Any())
            {
                yield break;
            }

            foreach (var setting in settings)
            {
                if (!setting.ContainsKey("Name") || !(setting["Name"] is string) || !setting.ContainsKey("File") || !(setting["File"] is string))
                {
                    continue;
                }
                
                yield return new XMLTVConfiguration(this)
                                 {
                                     Name           = (string) setting["Name"],
                                     File           = (string) setting["File"],
                                     Language       = setting.ContainsKey("Language") && setting["Language"] is string ? (string) setting["Language"] : string.Empty,
                                     AdvHuRoParse   = setting.ContainsKey("Advanced Parsing") && setting["Advanced Parsing"] is bool && (bool) setting["Advanced Parsing"],
                                     UseMappedNames = setting.ContainsKey("Use English Titles") && setting["Use English Titles"] is bool && (bool) setting["Use English Titles"],
                                     TZCorrection   = setting.ContainsKey("Time Zone Correction") && setting["Time Zone Correction"] is double ? (double) setting["Time Zone Correction"] : 0,
                                 };
            }
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
            var xconfig = (XMLTVConfiguration) config;
            var listing = (List<XMLTVProgramme>) null;
            var cache   = Path.Combine(Path.GetTempPath(), "XMLTV-" + (xconfig).File.GetHashCode().ToString("X") + "-" + (xconfig).Language + ".bin");
            
            if (File.Exists(cache) && File.GetLastWriteTime(cache) > File.GetLastWriteTime(xconfig.File))
            {
                try
                {
                    using (var file = File.OpenRead(cache))
                    {
                        listing = Serializer.Deserialize<List<XMLTVProgramme>>(file);
                    }
                }
                catch
                {
                    listing = null;
                }
            }

            if (listing == null)
            {
                listing = Filter(ParseFile(xconfig), xconfig).ToList();
            }

            using (var file = File.Create(cache))
            {
                Serializer.Serialize(file, listing);
            }

            foreach (var prog in listing.Where(p => p.Airdate.AddHours(xconfig.TZCorrection) > DateTime.Now))
            {
                if (xconfig.AdvHuRoParse && prog.Description.StartsWith(prog.Name))
                {
                    var ep = Regex.Match(prog.Description, @"(?:(?:(?<sr>[IVXLCDM]+)\.(?: évad)?\s*/\s*)?(?<e>\d{1,3})\. rész|(?<s>\d{1,2})\. évad(?:,? (?<e>\d{1,3})\. rész)?|(?:sezonul (?<s>\d{1,2}), )?episodul (?<e>\d{1,3})|^\s*" + Regex.Escape(prog.Name) + @"\s+(?<e>\d{1,3})\.(?:,\s+)?)");

                    prog.Description = Regex.Replace(prog.Description, @"^\s*" + Regex.Escape(prog.Name) + @"\s*(\d{1,3}\.(?:,\s+|$))?(?:\([^\)]+\)\s*)?(?:\(ism\.\)\s*)?(?:(?:\d{1,2}|[IVXLCDM]+)\. évad(?:\s*[,/] \d{1,3}\. rész(?:, )?)?)?(?:\-\s*)?(?:\((?:reluare|St)\)\s*)?(?: Utána: .+| Feliratozva .+)?", string.Empty, RegexOptions.IgnoreCase);

                    if (ep.Success)
                    {
                        prog.Number = string.Empty;

                        if (ep.Groups["sr"].Success)
                        {
                            prog.Number = "S" + Utils.RomanToNumber(ep.Groups["sr"].Value).ToString("00");

                            if (ep.Groups["e"].Success)
                            {
                                prog.Number += " #" + int.Parse(ep.Groups["e"].Value);
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

                if (xconfig.UseMappedNames)
                {
                    prog.Name = prog.Show.Name;
                }

                if (xconfig.TZCorrection != 0)
                {
                    prog.Airdate = prog.Airdate.AddHours(xconfig.TZCorrection);
                }

                yield return prog;
            }
        }

        /// <summary>
        /// Parses the specified XMLTV file.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>
        /// List of extracted programmes.
        /// </returns>
        public IEnumerable<XMLTVProgramme> ParseFile(XMLTVConfiguration config)
        {
            var channels = new Dictionary<string, string[]>();
            var document = XDocument.Load(config.File);

            foreach (var channel in document.Descendants("channel"))
            {
                var id   = channel.Attribute("id");
                var name = channel.Descendants("display-name").ToList();
                var url  = channel.Descendants("url").ToList();

                if (id == null || !name.Any())
                {
                    continue;
                }

                channels.Add(id.Value, new[] { name[0].Value, url.Any() ? url.Last().Value : null });
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

                var prog = new XMLTVProgramme
                    {
                        Channel  = channels[channel.Value][0],
                        Name     = title[0].Value.Trim(),
                        Airdate  = airdate,
                        URL      = channels[channel.Value][1]
                    };

                if (descr.Any())
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
        public IEnumerable<XMLTVProgramme> Filter(IEnumerable<XMLTVProgramme> programmes, XMLTVConfiguration config)
        {
            var regexes = new Dictionary<Regex, TVShow>();

            foreach (var show in Database.TVShows)
            {
                regexes.Add(new Regex(@"(^|:\s+)" + Regex.Escape(show.Value.Name) + @"(?!(?:[:,0-9]| \- |\s*[a-z&]))", RegexOptions.IgnoreCase), show.Value);

                if (!string.IsNullOrWhiteSpace(config.Language) && config.Language.Length == 2)
                {
                    var foreign = show.Value.GetForeignTitle(config.Language);

                    if (!string.IsNullOrWhiteSpace(foreign))
                    {
                        regexes.Add(new Regex(@"(^|:\s+)" + Regex.Escape(foreign.RemoveDiacritics()) + @"(?!(?:[:,0-9]| \- |\s*[a-z&]))", RegexOptions.IgnoreCase), show.Value);
                    }
                }
            }

            foreach (var prog in programmes.OrderBy(p => p.Airdate))
            {
                foreach (var regex in regexes)
                {
                    if (regex.Key.IsMatch(prog.Name.RemoveDiacritics()))
                    {
                        prog.ShowID = regex.Value.ID;

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
            /// Gets or sets the timezone correction information.
            /// </summary>
            /// <value>
            /// The timezone correction information.
            /// </value>
            public double TZCorrection { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="XMLTVConfiguration"/> class.
            /// </summary>
            /// <param name="plugin">The plugin.</param>
            public XMLTVConfiguration(LocalProgrammingPlugin plugin) : base(plugin)
            {
            }
        }

        /// <summary>
        /// Represents a programme in the XMLTV listing.
        /// </summary>
        [ProtoContract]
        public class XMLTVProgramme : Programme
        {
            /// <summary>
            /// Gets or sets the ID of the show in the database.
            /// </summary>
            /// <value>
            /// The ID of the show in the database.
            /// </value>
            [ProtoMember(short.MaxValue)]
            public int ShowID
            {
                get { return base.Show.ID; }
                set { base.Show = Database.TVShows[value]; }
            }

            /// <summary>
            /// Gets or sets the name of the show.
            /// </summary>
            /// <value>
            /// The name of the show.
            /// </value>
            [ProtoMember(1)]
            public new string Name
            {
                get { return base.Name; }
                set { base.Name = value; }
            }

            /// <summary>
            /// Gets or sets the episode number.
            /// </summary>
            /// <value>
            /// The episode number.
            /// </value>
            [ProtoMember(2)]
            public new string Number
            {
                get { return base.Number; }
                set { base.Number = value; }
            }

            /// <summary>
            /// Gets or sets the description.
            /// </summary>
            /// <value>
            /// The description.
            /// </value>
            [ProtoMember(3)]
            public new string Description
            {
                get { return base.Description; }
                set { base.Description = value; }
            }

            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            /// <value>
            /// The channel.
            /// </value>
            [ProtoMember(4)]
            public new string Channel
            {
                get { return base.Channel; }
                set { base.Channel = value; }
            }

            /// <summary>
            /// Gets or sets the airdate.
            /// </summary>
            /// <value>
            /// The airdate.
            /// </value>
            [ProtoMember(5)]
            public new DateTime Airdate
            {
                get { return base.Airdate; }
                set { base.Airdate = value; }
            }

            /// <summary>
            /// Gets or sets an URL to the listing or episode information.
            /// </summary>
            /// <value>
            /// The URL to the listing or episode information.
            /// </value>
            [ProtoMember(6)]
            public string URL
            {
                get { return base.URL; }
                set { base.URL = value; }
            }
        }
    }
}
