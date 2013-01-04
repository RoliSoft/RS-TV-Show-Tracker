namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents an episode in a TV show.
    /// </summary>
    public class Episode
    {
        /// <summary>
        /// Gets or sets the TV show associated with this episode.
        /// </summary>
        /// <value>The show.</value>
        public TVShow Show { get; private set; }

        /// <summary>
        /// Gets or sets the episode ID.
        /// </summary>
        /// <value>The episode ID.</value>
        public int ID { get; private set; }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        /// <value>The season.</value>
        public int Season { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        /// <value>The episode.</value>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this episode is marked as watched
        /// </summary>
        /// <value>
        ///   <c>true</c> if marked as watched; otherwise, <c>false</c>.
        /// </value>
        public bool Watched { get; set; }

        /// <summary>
        /// Gets or sets the date when the episode was first aired.
        /// </summary>
        /// <value>The air date.</value>
        public DateTime Airdate { get; set; }

        /// <summary>
        /// Gets or sets the title of the episode.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the title of the episode.
        /// </summary>
        /// <value>The title.</value>
        [Obsolete("Use Title.")]
        public string Name
        {
            get { return Title; }
            set { Title = value; }
        }

        /// <summary>
        /// Gets or sets the summary of the episode.
        /// </summary>
        /// <value>The summary.</value>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the URL to a screen capture of the episode.
        /// </summary>
        /// <value>The URL to the screen capture.</value>
        public string Picture { get; set; }

        /// <summary>
        /// Gets or sets the URL to the episode's page on the site.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        /// <summary>
        /// Saves this object to the stream.
        /// </summary>
        /// <param name="list">The destination stream for episode listing.</param>
        /// <param name="seen">The destination stream for episode tracking.</param>
        public void Save(Stream list, Stream seen)
        {
            using (var bw = new BinaryWriter(list))
            {
                bw.Write((byte)Season);

                if (Number < 255)
                {
                    bw.Write((byte)Number);
                }
                else
                {
                    bw.Write((byte)255);
                    bw.Write((byte)(Number - 255));
                }

                bw.Write((uint)Airdate.ToUnixTimestamp());
                bw.Write(Title);
                bw.Write(Summary);
                bw.Write(Picture);
                bw.Write(URL);
            }

            if (Watched)
            {
                using (var bw = new BinaryWriter(seen))
                {
                    bw.Write((byte)Season);

                    if (Number < 255)
                    {
                        bw.Write((byte)Number);
                    }
                    else
                    {
                        bw.Write((byte)255);
                        bw.Write((byte)(Number - 255));
                    }
                }
            }
        }

        /// <summary>
        /// Loads an object from the stream.
        /// </summary>
        /// <param name="show">The show associated with the episode.</param>
        /// <param name="inbr">The source reader for episode listing.</param>
        /// <returns>
        /// Deserialized object.
        /// </returns>
        internal static Episode Load(TVShow show, BinaryReader inbr)
        {
            var ep = new Episode();

            ep.Show   = show;
            ep.Season = inbr.ReadByte();
            ep.Number = inbr.ReadByte();
            ep.ID     = ep.Number + (ep.Season * 1000) + (ep.Show.ID * 1000 * 1000);

            if (ep.Number == 255)
            {
                ep.Number += inbr.ReadByte();
            }

            ep.Airdate = ((double)inbr.ReadUInt32()).GetUnixTimestamp();
            ep.Title   = inbr.ReadString();
            ep.Summary = inbr.ReadString();
            ep.Picture = inbr.ReadString();
            ep.URL     = inbr.ReadString();

            return ep;
        }

        /// <summary>
        /// Generates a regular expression which matches this episode's numbering.
        /// </summary>
        /// <returns>
        /// A regular expression which matches this episode's numbering.
        /// </returns>
        public Regex GenerateRegex()
        {
            return ShowNames.Parser.GenerateEpisodeRegexes(Season.ToString(), Number.ToString(), Airdate.ToOriginalTimeZone(Show.TimeZone));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} S{1:00}E{2:00} {3} {4}", Show.Title, Season, Number, Title, Watched ? "✓" : "✗");
        }
    }
}
