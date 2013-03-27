namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Timer = System.Timers.Timer;

    /// <summary>
    /// Provides methods for monitoring files.
    /// </summary>
    public static class Library
    {
        /// <summary>
        /// Gets or sets the list of episode-mapped downloaded files.
        /// </summary>
        /// <value>
        /// The list of episode-mapped downloaded files.
        /// </value>
        public static Dictionary<int, List<string>> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Library" /> is currently searching for files.
        /// </summary>
        /// <value>
        ///   <c>true</c> if currently searching for files; otherwise, <c>false</c>.
        /// </value>
        public static bool Indexing { get; set; }

        private static FileSystemWatcher[] _fsw;
        private static Dictionary<int, List<string>> _tmp;
        private static ConcurrentQueue<Tuple<WatcherChangeTypes, string[]>> _queue;
        private static Thread _qthd;
        private static Timer _stmr;

        /// <summary>
        /// Initializes the <see cref="Library" /> class.
        /// </summary>
        static Library()
        {
            Files = new Dictionary<int, List<string>>();
            LoadList();
        }

        /// <summary>
        /// Initializes the library.
        /// </summary>
        public static void Initialize()
        {
            if (Files.Count != 0)
            {
                foreach (var ep in Files)
                {
                    var files = ep.Value.ToList();

                    foreach (var file in files)
                    {
                        try
                        {
                            if (!File.Exists(file))
                            {
                                ep.Value.Remove(file);
                            }
                        }
                        catch { }
                    }
                }
            }

            _stmr = new Timer
                        {
                            AutoReset = false,
                            Interval = 3000
                        };
            _stmr.Elapsed += (sender, args) => SaveList();

            StartWatching();
            SearchForFiles();
        }

        /// <summary>
        /// Creates and starts <c>FileSystemWatcher</c> instances on the download paths.
        /// </summary>
        public static void StartWatching()
        {
            if (_fsw != null && _fsw.Length != 0)
            {
                for (var i = 0; i < _fsw.Length; i++)
                {
                    try
                    {
                        if (_fsw[i] != null)
                        {
                            _fsw[i].EnableRaisingEvents = false;
                            _fsw[i].Dispose();
                        }
                    }
                    catch { }
                }
            }

            if (_qthd != null && _qthd.IsAlive)
            {
                try { _qthd.Abort(); } catch { }
            }

            var paths = Settings.Get<List<string>>("Download Paths");

            _fsw   = new FileSystemWatcher[paths.Count];
            _queue = new ConcurrentQueue<Tuple<WatcherChangeTypes, string[]>>();
            _qthd  = new Thread(ProcessChangesQueue);

            for (var i = 0; i < paths.Count; i++)
            {
                try
                {
                    _fsw[i] = new FileSystemWatcher(paths[i]);

                    _fsw[i].Created += FileSystemWatcher_OnCreated;
                    _fsw[i].Renamed += FileSystemWatcher_OnRenamed;
                    _fsw[i].Deleted += FileSystemWatcher_OnDeleted;
                    _fsw[i].Error   += FileSystemWatcher_OnError;

                    _fsw[i].EnableRaisingEvents = true;
                }
                catch { }
            }
        }

        /// <summary>
        /// Handles the OnCreated event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="fileSystemEventArgs">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _queue.Enqueue(Tuple.Create(fileSystemEventArgs.ChangeType, new[] { fileSystemEventArgs.FullPath }));
            
            if (_qthd == null || !_qthd.IsAlive)
            {
                _qthd = new Thread(ProcessChangesQueue);
                try { _qthd.Start(); } catch { }
            }
        }

        /// <summary>
        /// Handles the OnRenamed event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="renamedEventArgs">The <see cref="RenamedEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            _queue.Enqueue(Tuple.Create(renamedEventArgs.ChangeType, new[] { renamedEventArgs.OldFullPath, renamedEventArgs.FullPath }));

            if (_qthd == null || !_qthd.IsAlive)
            {
                _qthd = new Thread(ProcessChangesQueue);
                try { _qthd.Start(); } catch { }
            }
        }

        /// <summary>
        /// Handles the OnDeleted event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="fileSystemEventArgs">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _queue.Enqueue(Tuple.Create(fileSystemEventArgs.ChangeType, new[] { fileSystemEventArgs.FullPath }));
            
            if (_qthd == null || !_qthd.IsAlive)
            {
                _qthd = new Thread(ProcessChangesQueue);
                try { _qthd.Start(); } catch { }
            }
        }

        /// <summary>
        /// Handles the OnError event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="errorEventArgs">The <see cref="ErrorEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnError(object sender, ErrorEventArgs errorEventArgs)
        {
            new Thread(StartWatching).Start();
        }

        /// <summary>
        /// Pops events recursively from the file system changes queue and processes it.
        /// </summary>
        private static void ProcessChangesQueue()
        {
            Tuple<WatcherChangeTypes, string[]> evt;
            if (!_queue.TryDequeue(out evt) || evt == null || Indexing)
            {
                return;
            }

            try
            {
                switch (evt.Item1)
                {
                    case WatcherChangeTypes.Created:
                        {
                            CheckFile(evt.Item2[0]);
                        }
                        break;

                    case WatcherChangeTypes.Renamed:
                        {
                            CheckFile(evt.Item2[1]);

                            lock (Files)
                            {
                                foreach (var ep in Files)
                                {
                                    if (ep.Value.Contains(evt.Item2[0]))
                                    {
                                        ep.Value.Remove(evt.Item2[0]);
                                        break;
                                    }
                                }
                            }
                        }

                        if (!Indexing)
                        {
                            if (_stmr.Enabled)
                            {
                                _stmr.Stop();
                            }

                            _stmr.Start();

                            try
                            {
                                if (MainWindow.Active != null && MainWindow.Active.activeGuidesPage != null && MainWindow.Active.activeGuidesPage._activeShowID != 0)
                                {
                                    MainWindow.Active.Run(MainWindow.Active.activeGuidesPage.Refresh);
                                }
                            }
                            catch { }
                        }
                        break;

                    case WatcherChangeTypes.Deleted:
                        {
                            lock (Files)
                            {
                                foreach (var ep in Files)
                                {
                                    if (ep.Value.Contains(evt.Item2[0]))
                                    {
                                        ep.Value.Remove(evt.Item2[0]);
                                        break;
                                    }
                                }
                            }

                            if (!Indexing)
                            {
                                if (_stmr.Enabled)
                                {
                                    _stmr.Stop();
                                }

                                _stmr.Start();

                                try
                                {
                                    if (MainWindow.Active != null && MainWindow.Active.activeGuidesPage != null && MainWindow.Active.activeGuidesPage._activeShowID != 0)
                                    {
                                        MainWindow.Active.Run(MainWindow.Active.activeGuidesPage.Refresh);
                                    }
                                }
                                catch { }
                            }
                        }
                        break;
                }
            }
            catch { }

            ProcessChangesQueue();
        }

        /// <summary>
        /// Searches for matching files in the download paths.
        /// </summary>
        public static void SearchForFiles()
        {
            var fs = new FileSearch(Settings.Get<List<string>>("Download Paths"), CheckFile);

            Indexing = true;

            _tmp = new Dictionary<int, List<string>>();

            fs.BeginSearch();
            fs.SearchThread.Join(TimeSpan.FromMinutes(5));

            if (fs.SearchThread.IsAlive)
            {
                // searching for more than 5 minutes, kill it
                try { fs.SearchThread.Abort(); } catch { }
            }

            Files = _tmp;

            Indexing = false;

            SaveList();

            try
            {
                if (MainWindow.Active != null && MainWindow.Active.activeGuidesPage != null && MainWindow.Active.activeGuidesPage._activeShowID != 0)
                {
                    MainWindow.Active.Run(MainWindow.Active.activeGuidesPage.Refresh);
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks the file name for a match.
        /// </summary>
        /// <param name="file">The file name to check.</param>
        /// <returns>
        /// This function will always return <c>false</c>, because matches are inserted locally.
        /// </returns>
        private static bool CheckFile(string file)
        {
            if (!ShowNames.Regexes.KnownVideo.IsMatch(file) || ShowNames.Regexes.SampleVideo.IsMatch(file))
            {
                return false;
            }

            var name = Path.GetFileName(file);
            var dirs = Path.GetDirectoryName(file) ?? string.Empty;
            var pf   = FileNames.Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

            if (pf.DbEpisode != null)
            {
                var dict = Indexing ? _tmp : Files;
                List<string> list;

                if (dict.TryGetValue(pf.DbEpisode.ID, out list))
                {
                    if (!list.Contains(file))
                    {
                        list.Add(file);
                    }
                }
                else
                {
                    dict[pf.DbEpisode.ID] = new List<string> { file };
                }

                if (!Indexing)
                {
                    if (_stmr.Enabled)
                    {
                        _stmr.Stop();
                    }

                    _stmr.Start();

                    try
                    {
                        if (MainWindow.Active != null && MainWindow.Active.activeGuidesPage != null && MainWindow.Active.activeGuidesPage._activeShowID == pf.DbEpisode.Show.ID)
                        {
                            MainWindow.Active.Run(MainWindow.Active.activeGuidesPage.Refresh);
                        }
                    }
                    catch { }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the any data associated to a path.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void RemovePath(string path)
        {
            if (Indexing) return;

            foreach (var ep in Files)
            {
                var files = ep.Value.ToList();

                foreach (var file in files)
                {
                    if (file.StartsWith(path))
                    {
                        ep.Value.Remove(path);
                    }
                }
            }

            foreach (var fsw in _fsw)
            {
                try
                {
                    if (fsw != null && fsw.Path == path)
                    {
                        fsw.EnableRaisingEvents = false;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Adds a new path to the library.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void AddPath(string path)
        {
            if (Indexing) return;

            var i = _fsw.Length;
            Array.Resize(ref _fsw, _fsw.Length + 1);

            try
            {
                _fsw[i] = new FileSystemWatcher(path);

                _fsw[i].Created += FileSystemWatcher_OnCreated;
                _fsw[i].Renamed += FileSystemWatcher_OnRenamed;
                _fsw[i].Deleted += FileSystemWatcher_OnDeleted;
                _fsw[i].Error   += FileSystemWatcher_OnError;

                _fsw[i].EnableRaisingEvents = true;
            }
            catch { }
        }

        /// <summary>
        /// Saves the file list to the database.
        /// </summary>
        public static void SaveList()
        {
            var path = Path.Combine(Signature.InstallPath, @"misc\library");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)1);
                bw.Write((uint)DateTime.Now.ToUnixTimestamp());
                bw.Write((uint)Files.Count);

                foreach (var show in Files)
                {
                    bw.Write(show.Key);
                    bw.Write((uint)show.Value.Count);

                    foreach (var file in show.Value)
                    {
                        bw.Write(file ?? string.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the file list from the database.
        /// </summary>
        public static void LoadList()
        {
            var path = Path.Combine(Signature.InstallPath, @"misc\library");

            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                var ver = br.ReadByte();
                var upd = br.ReadUInt32();
                var cnt = br.ReadUInt32();

                for (var i = 0; i < cnt; i++)
                {
                    var sid = br.ReadInt32();
                    var snr = br.ReadUInt32();

                    List<string> lst;

                    if (!Files.TryGetValue(sid, out lst))
                    {
                        Files[sid] = lst = new List<string>();
                    }

                    for (var j = 0; j < snr; j++)
                    {
                        var fn = br.ReadString();

                        if (!string.IsNullOrWhiteSpace(fn) && File.Exists(fn))
                        {
                            lst.Add(fn);
                        }
                    }
                }
            }
        }
    }
}
