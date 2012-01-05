namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

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
        /// Initializes a new instance of the <see cref="XMLTV"/> class.
        /// </summary>
        /// <param name="file">The XMLTV file.</param>
        public XMLTV(string file)
        {
            Channels   = new Dictionary<string, string>();
            Programmes = new List<Programme>();

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
                
                var prog = new Programme
                    {
                        Channel = channel.Value,
                        Title   = title[0].Value.Trim(),
                        AirDate = airdate
                    };

                if (descr.Count != 0)
                {
                    prog.Description = descr[0].Value.Trim();

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

                        prog.Description = Regex.Replace(prog.Description, @"^\s*" + Regex.Escape(prog.Title) + @"\s*(?:\([^\)]+\)\s*)?(?:\-\s*)?", string.Empty, RegexOptions.IgnoreCase);
                    }
                }

                Programmes.Add(prog);
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
            public DateTime AirDate { get; set; }
        }
    }
}
