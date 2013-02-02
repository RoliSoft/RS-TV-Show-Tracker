namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.LinkCheckers;

    /// <summary>
    /// Extended class of the original Link class to handle the context menu items.
    /// </summary>
    public class LinkItem : Link, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkItem"/> class.
        /// </summary>
        /// <param name="link">The link.</param>
        public LinkItem(Link link) : base(link.Source)
        {
            Release = link.Release;
            Quality = link.Quality;
            Size    = link.Size;
            InfoURL = link.InfoURL;
            FileURL = link.FileURL;
            Infos   = link.Infos;
            Color   = "White";

            if (!Signature.IsActivated)
            {
                return;
            }

            switch (Source.Type)
            {
                case Types.Torrent:
                    if (Infos.StartsWith("0 seed") && Settings.Get("Fade Dead Torrents", true))
                    {
                        Color = "#50FFFFFF";
                    }
                    else if (Infos.Contains("Free") && Settings.Get<bool>("Highlight Free Torrents"))
                    {
                        Color = "GreenYellow";
                    }
                    break;

                case Types.Usenet:
                    var ret = Settings.Get("Usenet Retention", 0);

                    if (ret != 0 && Infos.Contains("day") && int.Parse(Infos.Replace(",", string.Empty).Split(" ".ToCharArray()).First()) > ret)
                    {
                        Color = "#50FFFFFF";
                    }
                    break;

                case Types.DirectHTTP:
                    var typ = Settings.Get("One-Click Hoster List Type", "white");
                    var lst = Settings.Get<List<string>>("One-Click Hoster List");

                    if (typ == "white")
                    {
                        if (string.IsNullOrWhiteSpace(FileURL) || !lst.Any(d => FileURL.Contains(d)))
                        {
                            Color = "#50FFFFFF";
                        }
                    }
                    else if (typ == "black")
                    {
                        if (string.IsNullOrWhiteSpace(FileURL) || lst.Any(d => FileURL.Contains(d)))
                        {
                            Color = "#50FFFFFF";
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public string Color { get; set; }

        /// <summary>
        /// Gets the image of the link type.
        /// </summary>
        /// <value>The type's image.</value>
        public string TypeImage
        {
            get
            {
                switch (Source.Type)
                {
                    case Types.Torrent:
                        return "/RSTVShowTracker;component/Images/torrent.png";

                    case Types.Usenet:
                        return "/RSTVShowTracker;component/Images/usenet.png";

                    case Types.PreDB:
                        return "/RSTVShowTracker;component/Images/globe.png";

                    default:
                        return "/RSTVShowTracker;component/Images/filehoster.png";
                }
            }
        }

        /// <summary>
        /// Gets the friendly name of the link quality.
        /// </summary>
        /// <value>The quality's friendly name.</value>
        public string QualityText
        {
            get
            {
                return Quality.GetAttribute<DescriptionAttribute>().Description;
            }
        }

        /// <summary>
        /// Gets the HD icon, if the quality is HD.
        /// </summary>
        /// <value>The HD icon.</value>
        public string HDIcon
        {
            get
            {
                switch (Quality)
                {
                    case Qualities.BluRay1080p:
                    case Qualities.WebDL1080p:
                    case Qualities.HDTV1080i:
                        return "/RSTVShowTracker;component/Images/hd_1080.png";

                    case Qualities.BluRay720p:
                    case Qualities.WebDL720p:
                    case Qualities.HDTV720p:
                        return "/RSTVShowTracker;component/Images/hd_720.png";

                    default:
                        return "/RSTVShowTracker;component/Images/empty.png";
                }
            }
        }

        /// <summary>
        /// Checks whether this link is available.
        /// </summary>
        public void CheckLink()
        {
            if (string.IsNullOrWhiteSpace(FileURL))
            {
                return;
            }

            var checker = Extensibility.GetNewInstances<LinkCheckerEngine>().FirstOrDefault(x => x.CanCheck(FileURL));

            if (checker == null)
            {
                return;
            }

            var inf = Infos;

            if (!string.IsNullOrWhiteSpace(inf))
            {
                inf += ", ";
            }

            Infos = inf + "Checking...";
            MainWindow.Active.Run(() => { try { PropertyChanged(this, new PropertyChangedEventArgs("Infos")); } catch { } });

            try
            {
                var result = checker.Check(FileURL.Split('\0').First());

                if (!result)
                {
                    Color = "#50FFFFFF";
                }

                Infos = inf + "Link is " + (result ? "online" : "broken");
                MainWindow.Active.Run(() =>
                    {
                        try
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs("Infos"));
                            PropertyChanged(this, new PropertyChangedEventArgs("Color"));
                        } catch { }
                    });
            }
            catch
            {
                Infos = inf + "Check error";
                MainWindow.Active.Run(() => { try { PropertyChanged(this, new PropertyChangedEventArgs("Infos")); } catch { } });
            }
        }
    }
}
