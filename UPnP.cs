namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows;

    using OpenSource.UPnP;
    using OpenSource.UPnP.AV.CdsMetadata;
    using OpenSource.UPnP.AV.MediaServer.DV;

    using RoliSoft.TVShowTracker.Parsers.Downloads;
    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;
    using RoliSoft.TVShowTracker.TaskDialogs;

    /// <summary>
    /// Provides support for streaming the library to UPnP/DLNA-compatible devices.
    /// </summary>
    public static class UPnP
    {
        private static DeviceInfo _di;
        private static MediaServerDevice _ms;
        private static UPnPDeviceWatcher _dw;
        private static Image _icon, _icon2;
        private static Icon _favicon;
        private static List<string> _mimes; 

        /// <summary>
        /// Gets or sets a value indicating whether this server is running.
        /// </summary>
        /// <value><c>true</c> if this server is running; otherwise, <c>false</c>.</value>
        public static bool IsRunning { get; set; }

        /// <summary>
        /// Initializes static members of the <see cref="UPnP"/> class.
        /// </summary>
        static UPnP()
        {
            _di = new DeviceInfo
                {
                    AllowRemoteContentManagement = true,
                    FriendlyName                 = Signature.Software + " on " + Environment.MachineName,
                    Manufacturer                 = Signature.Developer,
                    ManufacturerURL              = "http://lab.rolisoft.net/tvshowtracker.html",
                    ModelName                    = "RS TV Show Tracker",
                    ModelDescription             = "Provides access to TV shows tracked by " + Signature.Software + " on " + Environment.MachineName,
                    ModelURL                     = "http://lab.rolisoft.net/tvshowtracker.html",
                    ModelNumber                  = Signature.Version,
                    LocalRootDirectory           = "",
                    SearchCapabilities           = "dc:title,dc:creator,upnp:class,upnp:album,res@protocolInfo,res@size,res@bitrate",
                    SortCapabilities             = "dc:title,dc:creator,upnp:class,upnp:album",
                    EnableSearch                 = true,
                    CacheTime                    = 1800,
                    CustomUDN                    = "",
                    INMPR03                      = true
                };

            MediaObject.ENCODE_UTF8 = false;
        }

        /// <summary>
        /// Starts the UPnP server.
        /// </summary>
        public static void Start()
        {
            if (IsRunning)
            {
                Stop();
            }

            if (!Signature.IsActivated)
            {
                return;
            }

            _ms = new MediaServerDevice(_di, null, true, "http-get:*:*:*", "");
            _dw = new UPnPDeviceWatcher(_ms._Device);

            var dv = _ms.GetUPnPDevice();

            if (_favicon == null)
            {
                _favicon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/RSTVShowTracker;component/tv.ico")).Stream);
                _icon    = new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/tv48.png")).Stream);
                _icon2   = new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/tv64.png")).Stream);
            }

            dv.favicon = _favicon;
            dv.Icon    = _icon;
            dv.Icon2   = _icon2;

            _ms.Start();

            IsRunning = true;

            _mimes = new List<string>();

            RebuildList();
        }

        /// <summary>
        /// Stops the UPnP server.
        /// </summary>
        public static void Stop()
        {
            IsRunning = false;

            _ms.Stop();
            _ms.Dispose();
            _ms = null;
            _dw = null;
        }

        /// <summary>
        /// Resets the hierarchy from the root.
        /// </summary>
        public static void RebuildList()
        {
            if (!Signature.IsActivated && IsRunning)
            {
                Stop();
                return;
            }

            MediaBuilder.SetNextID(0);

            var allEps = DvMediaBuilder.CreateContainer(new MediaBuilder.container("-- All Episodes --") { IsRestricted = true });
            var unwEps = DvMediaBuilder.CreateContainer(new MediaBuilder.container("-- Unwatched Episodes --") { IsRestricted = true });

            _ms.Root.AddObject(allEps, true);
            _ms.Root.AddObject(unwEps, true);

            var shows = new Dictionary<int, DvMediaContainer>();

            foreach (var tvs in Database.TVShows.OrderBy(t => t.Value.Title))
            {
                shows[tvs.Key] = DvMediaBuilder.CreateContainer(new MediaBuilder.container(tvs.Value.Title) { IsRestricted = true });
            }

            foreach (var item in Library.Files)
            {
                var sid = (int)Math.Floor((double)item.Key / 1000 / 1000);

                TVShow show;
                if (!Database.TVShows.TryGetValue(sid, out show))
                {
                    continue;
                }

                Episode ep;
                if (!show.EpisodeByID.TryGetValue(item.Key - (sid * 1000 * 1000), out ep))
                {
                    continue;
                }

                var txt = string.Format("S{0:00}E{1:00}: {2}", ep.Season, ep.Number, ep.Title);

                foreach (var file in Library.Files[item.Key])
                {
                    if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(file).ToLower()))
                    {
                        continue;
                    }

                    var ltxt = txt;
                    var q = FileNames.Parser.ParseQuality(file);

                    if (q != Qualities.Unknown)
                    {
                        ltxt += " [" + q.GetAttribute<DescriptionAttribute>().Description + "]";
                    }
                    else
                    {
                        var ed = FileNames.Parser.ParseEdition(file);

                        if (ed != Editions.Unknown)
                        {
                            ltxt += " [" + ed.GetAttribute<DescriptionAttribute>().Description + "]";
                        }
                        else
                        {
                            ltxt += " [" + Path.GetExtension(file) + "]";
                        }
                    }

                    allEps.AddBranch(CreateObject(show.Title + " " + ltxt, file));

                    if (!ep.Watched)
                    {
                        unwEps.AddBranch(CreateObject(show.Title + " " + ltxt, file));
                    }

                    shows[sid].AddBranch(CreateObject(ltxt, file));
                }
            }

            foreach (var tvs in shows)
            {
                if (tvs.Value.ChildCount != 0)
                {
                    _ms.Root.AddObject(tvs.Value, true);
                }
            }
        }

        /// <summary>
        /// Enumerates the currently transfered files.
        /// </summary>
        /// <returns>List of active transfer names.</returns>
        public static IEnumerable<FileInfo> GetActiveTransfers()
        {
            foreach (MediaServerDevice.HttpTransfer t in _ms.HttpTransfers)
            {
                yield return (FileInfo)t.Resource.Tag;
            }
        }

        /// <summary>
        /// Creates a media object.
        /// </summary>
        /// <param name="title">The title of the object.</param>
        /// <param name="file">The full path to the file.</param>
        /// <returns>Media object.</returns>
        private static IDvMedia CreateObject(string title, string file)
        {
            var fi = new FileInfo(file);
            var media = DvMediaBuilder.CreateItem(new MediaBuilder.item(title));

            string mime, mediaClass;
            MimeTypes.ExtensionToMimeType(fi.Extension, out mime, out mediaClass);

            var resInfo = new ResourceBuilder.VideoItem
                {
                    contentUri   = MediaResource.AUTOMAPFILE + fi.FullName,
                    protocolInfo = new ProtocolInfoString("http-get:*:" + mime + ":*"),
                    size         = new _ULong((ulong)fi.Length)
                };

            var res = DvResourceBuilder.CreateResource(resInfo, true);
            res.Tag = fi;

            media.AddResource(res);

            if (_mimes.Contains(mime) == false)
            {
                _mimes.Add(mime);
                var ps = new ProtocolInfoString[_mimes.Count];
                for (var i = 0; i < _mimes.Count; i++)
                {
                    ps[i] = new ProtocolInfoString("http-get:*:" + _mimes[i] + ":*");
                }
                _ms.SourceProtocolInfoSet = ps;
            }

            return media;
        }
    }
}
