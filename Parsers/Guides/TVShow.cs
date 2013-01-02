namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Represents a TV show in the guide.
    /// </summary>
    public class TVShow
    {
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
        /// Gets or sets the episode list.
        /// </summary>
        /// <value>The episode list.</value>
        public List<Episode> Episodes { get; set; }

        /// <summary>
        /// Saves this object to the stream.
        /// </summary>
        /// <param name="info">The destination stream.</param>
        public void Save(Stream info)
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
            }
        }

        /// <summary>
        /// Saves this object to the stream.
        /// </summary>
        /// <param name="info">The destination stream.</param>
        /// <param name="list">The destination stream for episode listing.</param>
        /// <param name="desc">The destination stream for descriptions.</param>
        public void Save(Stream info, Stream list, Stream desc)
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
            }

            using (var bw = new BinaryWriter(list))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)Episodes.Count);

                foreach (var episode in Episodes)
                {
                    episode.Save(list, desc);
                }
            }
        }

        /// <summary>
        /// Loads an object from the stream.
        /// </summary>
        /// <param name="info">The source stream.</param>
        /// <returns>
        /// Deserialized object.
        /// </returns>
        public static TVShow Load(Stream info)
        {
            var show = new TVShow();

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
            }

            return show;
        }

        /// <summary>
        /// Loads an object from the stream.
        /// </summary>
        /// <param name="info">The source stream.</param>
        /// <param name="list">The source stream for episode listing.</param>
        /// <returns>
        /// Deserialized object.
        /// </returns>
        public static TVShow Load(Stream info, Stream list)
        {
            var show = new TVShow();

            using (var br1 = new BinaryReader(info))
            using (var br2 = new BinaryReader(list))
            {
                var sver = br1.ReadByte();
                var supd = br1.ReadUInt32();

                show.Source      = br1.ReadString();
                show.SourceID    = br1.ReadString();
                show.Title       = br1.ReadString();
                show.Description = br1.ReadString();
                show.Cover       = br1.ReadString();
                show.Airing      = br1.ReadBoolean();
                show.AirTime     = br1.ReadString();
                show.AirDay      = br1.ReadString();
                show.Network     = br1.ReadString();
                show.Runtime     = br1.ReadByte();
                show.TimeZone    = br1.ReadString();
                show.Language    = br1.ReadString();
                show.URL         = br1.ReadString();

                var ever = br2.ReadByte();
                var eupd = br2.ReadUInt32();
                var epnr = (int)br2.ReadUInt32();

                show.Episodes = new List<Episode>(epnr);

                for (var i = 0; i < epnr; i++)
                {
                    show.Episodes.Add(Episode.Load(list));
                }
            }

            return show;
        }

        /// <summary>
        /// Represents an episode in a TV show.
        /// </summary>
        public class Episode
        {
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

            private uint _pos;

            /// <summary>
            /// Saves this object to the stream.
            /// </summary>
            /// <param name="list">The destination stream for episode listing.</param>
            /// <param name="desc">The destination stream for descriptions.</param>
            public void Save(Stream list, Stream desc)
            {
                using (var bw1 = new BinaryWriter(list))
                using (var bw2 = new BinaryWriter(desc))
                {
                    if (list.Length == 0)
                    {
                        bw1.Write((byte)1);
                        bw1.Write((uint)DateTime.Now.ToUnixTimestamp());
                        bw1.Write((uint)1);
                    }

                    bw1.Write((ushort)Season);
                    bw1.Write((ushort)Number);
                    bw1.Write((uint)Airdate.ToUnixTimestamp());
                    bw1.Write(Title);

                    if (desc.Length == 0)
                    {
                        bw2.Write((byte)1);
                        bw2.Write((uint)DateTime.Now.ToUnixTimestamp());
                    }

                    bw1.Write((uint)desc.Position);
                    bw2.Write(URL);
                    bw2.Write(Picture);
                    bw2.Write(Summary);
                }
            }

            /// <summary>
            /// Loads an object from the stream.
            /// </summary>
            /// <param name="list">The source stream for episode listing.</param>
            /// <returns>
            /// Deserialized object.
            /// </returns>
            public static Episode Load(Stream list)
            {
                var ep = new Episode();

                using (var br = new BinaryReader(list))
                {
                    ep.Season  = br.ReadUInt16();
                    ep.Number  = br.ReadUInt16();
                    ep.Airdate = ((double)br.ReadUInt32()).GetUnixTimestamp();
                    ep.Title   = br.ReadString();
                    ep._pos    = br.ReadUInt32();
                }

                return ep;
            }

            /// <summary>
            /// Loads an object from the stream.
            /// </summary>
            /// <param name="list">The source stream for episode listing.</param>
            /// <param name="desc">The destination stream for descriptions.</param>
            /// <returns>
            /// Deserialized object.
            /// </returns>
            public static Episode Load(Stream list, Stream desc)
            {
                var ep = new Episode();

                using (var br1 = new BinaryReader(list))
                {
                    ep.Season  = br1.ReadUInt16();
                    ep.Number  = br1.ReadUInt16();
                    ep.Airdate = ((double)br1.ReadUInt32()).GetUnixTimestamp();
                    ep.Title   = br1.ReadString();

                    desc.Position = br1.ReadUInt32();

                    using (var br2 = new BinaryReader(desc))
                    {
                        ep.URL     = br2.ReadString();
                        ep.Picture = br2.ReadString();
                        ep.Summary = br2.ReadString();
                    }
                }

                return ep;
            }
        }
    }
}
