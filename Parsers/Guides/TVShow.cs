namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents a TV show in the guide.
    /// </summary>
    public class TVShow
    {
        /// <summary>
        /// Gets or sets the show ID.
        /// </summary>
        /// <value>
        /// The show ID.
        /// </value>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the row ID.
        /// </summary>
        /// <value>
        /// The row ID.
        /// </value>
        public int RowID { get; set; }

        /// <summary>
        /// Gets or sets the title of the show.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the genre of the show, comma separated if multiple are provided.
        /// </summary>
        /// <value>The genre.</value>
        public string Genre { get; set; }

        /// <summary>
        /// Gets or sets the description of the show.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL location of a DVD cover.
        /// </summary>
        /// <value>The URL of the cover.</value>
        public string Cover { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TVShow"/> is airing.
        /// </summary>
        /// <value><c>true</c> if airing; otherwise, <c>false</c>.</value>
        public bool Airing { get; set; }

        /// <summary>
        /// Gets or sets the time when the show is aired usually.
        /// </summary>
        /// <value>The air time.</value>
        public string AirTime { get; set; }

        /// <summary>
        /// Gets or sets the day when the show is aired usually.
        /// </summary>
        /// <value>The air day.</value>
        public string AirDay { get; set; }

        /// <summary>
        /// Gets or sets the name of the network on which the show is aired.
        /// </summary>
        /// <value>The network.</value>
        public string Network { get; set; }

        /// <summary>
        /// Gets or sets the length of an episode.
        /// </summary>
        /// <value>The runtime.</value>
        public int Runtime { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the airdates.
        /// </summary>
        /// <value>The timezone of the airdates.</value>
        public string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the language of the show's episode listing.
        /// </summary>
        /// <value>The episode listing's language.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the URL to the show's page on the site.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the source ID.
        /// </summary>
        /// <value>The source ID.</value>
        public string SourceID { get; set; }

        /// <summary>
        /// Gets or sets the key-value store associated with this TV show.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<string, string> Data { get; set; }

        /// <summary>
        /// Gets or sets the episode list.
        /// </summary>
        /// <value>The episode list.</value>
        public List<Episode> Episodes { get; set; }

        /// <summary>
        /// Saves this object to the stream.
        /// </summary>
        /// <param name="info">The destination stream.</param>
        /// <param name="conf">The destination stream for associated key-value store.</param>
        /// <param name="seen">The destination stream for episode tracking.</param>
        /// <param name="desc">The destination stream for descriptions.</param>
        public void Save(Stream info, Stream conf, Stream seen = null, Stream desc = null)
        {
            using (var bw = new BinaryWriter(info))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write(Source);
                bw.Write(SourceID);
                bw.Write(Title);
                bw.Write(Description);
                bw.Write(Cover);
                bw.Write(Airing);
                bw.Write(AirTime);
                bw.Write(AirDay);
                bw.Write(Network);
                bw.Write((byte)Runtime);
                bw.Write(TimeZone);
                bw.Write(Language);
                bw.Write(URL);
                bw.Write((ushort)Episodes.Count);
            }

            if (seen != null)
            {
                using (var bw = new BinaryWriter(seen))
                {
                    bw.Write((byte)1);
                    bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                    bw.Write((ushort)0);
                }
            }

            var scnt = 0;

            foreach (var episode in Episodes)
            {
                if (episode.Watched)
                {
                    scnt++;
                }

                episode.Save(info, seen, desc);
            }

            if (seen != null)
            {
                seen.Position = 5;

                using (var bw = new BinaryWriter(seen))
                {
                    bw.Write((ushort)scnt);
                }
            }

            Data["showid"] = ID.ToString();
            Data["rowid"]  = RowID.ToString();

            using (var bw = new BinaryWriter(conf))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((ushort)Data.Count);

                foreach (var kv in Data)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value);
                }
            }
        }

        /// <summary>
        /// Loads an object from the stream.
        /// </summary>
        /// <param name="info">The source stream.</param>
        /// <param name="conf">The source stream for associated key-value store.</param>
        /// <param name="desc">The source stream for descriptions.</param>
        /// <returns>
        /// Deserialized object.
        /// </returns>
        public static TVShow Load(Stream info, Stream conf, Stream desc = null) // TODO load seen back
        {
            var show = new TVShow();
            int epnr;

            using (var br = new BinaryReader(info))
            {
                var sver = br.ReadByte();
                var supd = br.ReadUInt32();

                show.Source      = br.ReadString();
                show.SourceID    = br.ReadString();
                show.Title       = br.ReadString();
                show.Description = br.ReadString();
                show.Cover       = br.ReadString();
                show.Airing      = br.ReadBoolean();
                show.AirTime     = br.ReadString();
                show.AirDay      = br.ReadString();
                show.Network     = br.ReadString();
                show.Runtime     = br.ReadByte();
                show.TimeZone    = br.ReadString();
                show.Language    = br.ReadString();
                show.URL         = br.ReadString();

                epnr = br.ReadUInt16();
            }

            show.Episodes = new List<Episode>(epnr);

            for (var i = 0; i < epnr; i++)
            {
                show.Episodes.Add(Episode.Load(show, info, desc));
            }

            using (var br = new BinaryReader(conf))
            {
                var dver = br.ReadByte();
                var dupd = br.ReadUInt32();
                var dcnt = br.ReadUInt16();

                show.Data = new Dictionary<string, string>();

                for (var i = 0; i < dcnt; i++)
                {
                    show.Data[br.ReadString()] = br.ReadString();
                }
            }

            show.ID    = int.Parse(show.Data["showid"]);
            show.RowID = int.Parse(show.Data["rowid"]);

            return show;
        }

        /// <summary>
        /// Generates a regular expression which matches this show's name.
        /// </summary>
        /// <returns>
        /// A regular expression which matches this show's name.
        /// </returns>
        public Regex GenerateRegex()
        {
            string regex;
            if (Data.TryGetValue("regex", out regex) && !string.IsNullOrWhiteSpace(regex))
            {
                return new Regex(regex, RegexOptions.IgnoreCase);
            }
            else
            {
                return ShowNames.Parser.GenerateTitleRegex(Title);
            }
        }

        /// <summary>
        /// Gets the foreign title.
        /// </summary>
        /// <param name="language">The ISO 639-1 code of the language.</param>
        /// <param name="askRemote">if set to <c>true</c> lab.rolisoft.net's API will be asked then a foreign title provider engine.</param>
        /// <param name="statusCallback">The method to call to report a status change.</param>
        /// <returns>
        /// Foreign title or <c>null</c>.
        /// </returns>
        public string GetForeignTitle(string language, bool askRemote = false, Action<string> statusCallback = null)
        {
            return Database.GetForeignTitle(ID, language, askRemote, statusCallback);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} [{1}]", Title, ID);
        }
    }
}
