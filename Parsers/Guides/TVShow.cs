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
        public int RowID
        {
            get
            {
                if (_rowId.HasValue)
                {
                    return _rowId.Value;
                }

                return (int)(_rowId = int.Parse(Data["rowid"]));
            }
            set
            {
                _rowId = value;

                if (Data != null)
                {
                    Data["rowid"] = value.ToString();
                }
            }
        }

        /// <summary>
        /// Gets or sets the title of the show.
        /// </summary>
        /// <value>The title.</value>
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;

                if (Data != null)
                {
                    Data["title"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of the show.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the genre of the show, comma separated if multiple are provided.
        /// </summary>
        /// <value>The genre.</value>
        public string Genre { get; set; }

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
        /// Gets or sets the episode list by ID.
        /// </summary>
        /// <value>The episode list.</value>
        public Dictionary<int, Episode> EpisodeByID { get; set; }

        /// <summary>
        /// Gets or sets the directory where the show is stored.
        /// </summary>
        /// <value>The directory.</value>
        public string Directory { get; set; }

        private int? _rowId;
        private string _title;

        /// <summary>
        /// Saves the episode tracking information.
        /// </summary>
        public void SaveTracking()
        {
            using (var fs = File.OpenWrite(Path.Combine(Directory, "seen")))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((ushort)0);

                var scnt = 0;
                foreach (var episode in Episodes)
                {
                    if (episode.Watched)
                    {
                        bw.Write((byte)episode.Season);

                        if (episode.Number < 255)
                        {
                            bw.Write((byte)episode.Number);
                        }
                        else
                        {
                            bw.Write((byte)255);
                            bw.Write((byte)(episode.Number - 255));
                        }

                        scnt++;
                    }
                }

                bw.Seek(5, SeekOrigin.Begin);
                bw.Write((ushort)scnt);
            }
        }

        /// <summary>
        /// Saves the associated key-value store.
        /// </summary>
        public void SaveData()
        {
            using (var fs = File.OpenWrite(Path.Combine(Directory, "conf")))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((ushort)Data.Count);

                foreach (var kv in Data)
                {
                    bw.Write(kv.Key ?? string.Empty);
                    bw.Write(kv.Value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Saves this object.
        /// </summary>
        /// <remarks>
        /// This will NOT save episode tracking information.
        /// Call <c>SaveTracking()</c> if you need that as well.
        /// </remarks>
        public void Save()
        {
            using (var info = File.OpenWrite(Path.Combine(Directory, "info")))
            using (var conf = File.OpenWrite(Path.Combine(Directory, "conf")))
            using (var inbw = new BinaryWriter(info))
            using (var cobw = new BinaryWriter(conf))
            {
                inbw.Write((byte)1);
                inbw.Write((uint)DateTime.Now.ToUnixTimestamp());
                inbw.Write(Title ?? string.Empty);
                inbw.Write(Source ?? string.Empty);
                inbw.Write(SourceID ?? string.Empty);
                inbw.Write(Description ?? string.Empty);
                inbw.Write(Genre ?? string.Empty);
                inbw.Write(Cover ?? string.Empty);
                inbw.Write(Airing);
                inbw.Write(AirTime ?? string.Empty);
                inbw.Write(AirDay ?? string.Empty);
                inbw.Write(Network ?? string.Empty);
                inbw.Write((byte)Runtime);
                inbw.Write(TimeZone ?? string.Empty);
                inbw.Write(Language ?? string.Empty);
                inbw.Write(URL ?? string.Empty);
                inbw.Write((ushort)Episodes.Count);

                foreach (var episode in Episodes)
                {
                    episode.Save(inbw);
                }

                Data["showid"] = ID.ToString();
                Data["rowid"]  = RowID.ToString();

                cobw.Write((byte)1);
                cobw.Write((uint)DateTime.Now.ToUnixTimestamp());
                cobw.Write((ushort)Data.Count);

                foreach (var kv in Data)
                {
                    cobw.Write(kv.Key ?? string.Empty);
                    cobw.Write(kv.Value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Loads an object from the directory.
        /// </summary>
        /// <param name="dir">The source directory.</param>
        /// <returns>
        /// Deserialized object.
        /// </returns>
        public static TVShow Load(string dir)
        {
            var show = new TVShow { Directory = dir };

            using (var info = File.OpenRead(Path.Combine(dir, "info")))
            using (var conf = File.OpenRead(Path.Combine(dir, "conf")))
            using (var seen = File.OpenRead(Path.Combine(dir, "seen")))
            using (var inbr = new BinaryReader(info))
            using (var cobr = new BinaryReader(conf))
            using (var sebr = new BinaryReader(seen))
            {
                int epnr;

                var sver = inbr.ReadByte();
                var supd = inbr.ReadUInt32();

                show.Title       = inbr.ReadString();
                show.Source      = inbr.ReadString();
                show.SourceID    = inbr.ReadString();
                show.Description = inbr.ReadString();
                show.Genre       = inbr.ReadString();
                show.Cover       = inbr.ReadString();
                show.Airing      = inbr.ReadBoolean();
                show.AirTime     = inbr.ReadString();
                show.AirDay      = inbr.ReadString();
                show.Network     = inbr.ReadString();
                show.Runtime     = inbr.ReadByte();
                show.TimeZone    = inbr.ReadString();
                show.Language    = inbr.ReadString();
                show.URL         = inbr.ReadString();

                epnr = inbr.ReadUInt16();

                var dver = cobr.ReadByte();
                var dupd = cobr.ReadUInt32();
                var dcnt = cobr.ReadUInt16();

                show.Data = new Dictionary<string, string>();

                for (var i = 0; i < dcnt; i++)
                {
                    show.Data[cobr.ReadString()] = cobr.ReadString();
                }

                show.ID     = int.Parse(show.Data["showid"]);
                show._rowId = int.Parse(show.Data["rowid"]);

                string ctitle;
                if (show.Data.TryGetValue("title", out ctitle))
                {
                    show._title = ctitle;
                }

                show.Episodes    = new List<Episode>(epnr);
                show.EpisodeByID = new Dictionary<int, Episode>();

                for (var i = 0; i < epnr; i++)
                {
                    var ep = Episode.Load(show, inbr);
                    show.Episodes.Add(ep);
                    show.EpisodeByID[ep.Season * 1000 + ep.Number] = ep;
                }

                var tver = sebr.ReadByte();
                var tupd = sebr.ReadUInt32();
                var tcnt = sebr.ReadUInt16();

                for (var i = 0; i < tcnt; i++)
                {
                    var sn = sebr.ReadByte();
                    var en = sebr.ReadByte();

                    if (en == 255)
                    {
                        en += sebr.ReadByte();
                    }

                    try { show.EpisodeByID[sn * 1000 + en].Watched = true; } catch (KeyNotFoundException) { }
                }
            }

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
