namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
                        if (!File.Exists(file))
                        {
                            ep.Value.Remove(file);
                        }
                    }
                }
            }

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
                    _fsw[i].EnableRaisingEvents = false;
                    _fsw[i].Dispose();
                }
            }

            var paths = Settings.Get<List<string>>("Download Paths");

            _fsw = new FileSystemWatcher[paths.Count];

            for (var i = 0; i < paths.Count; i++)
            {
                _fsw[i] = new FileSystemWatcher(paths[i]);

                _fsw[i].Created += (s, a) => CheckFile(a.FullPath);
                _fsw[i].Renamed += (s, a) =>
                    {
                        foreach (var ep in Files)
                        {
                            if (ep.Value.Contains(a.OldFullPath))
                            {
                                ep.Value.Remove(a.OldFullPath);
                                CheckFile(a.FullPath);
                                return;
                            }
                        }
                    };
                _fsw[i].Deleted += (s, a) =>
                    {
                        foreach (var ep in Files)
                        {
                            if (ep.Value.Contains(a.FullPath))
                            {
                                ep.Value.Remove(a.FullPath);
                                return;
                            }
                        }
                    };
                _fsw[i].Error += (s, a) => StartWatching();

                _fsw[i].EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Searches for matching files in the download paths.
        /// </summary>
        public static void SearchForFiles()
        {
            var fs = new FileSearch(Settings.Get<List<string>>("Download Paths"), CheckFile);

            Indexing = true;

            fs.BeginSearch();
            fs.SearchThread.Join(TimeSpan.FromMinutes(5));

            if (fs.SearchThread.IsAlive)
            {
                // searching for more than 5 minutes, kill it
                fs.SearchThread.Abort();
            }

            Indexing = false;

            SaveList();
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
            var name = Path.GetFileName(file);
            var dirs = Path.GetDirectoryName(file) ?? string.Empty;
            var pf   = FileNames.Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

            if (pf.DbEpisode != null)
            {
                List<string> list;
                if (Files.TryGetValue(pf.DbEpisode.ID, out list))
                {
                    if (!list.Contains(file))
                    {
                        list.Add(file);
                    }
                }
                else
                {
                    Files[pf.DbEpisode.ID] = new List<string> { file };
                }

                if (!Indexing)
                {
                    SaveList();
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
                if (fsw.Path == path)
                {
                    fsw.EnableRaisingEvents = false;
                }
            }
        }

        /// <summary>
        /// Adds a new path to the library.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void AddPath(string path)
        {
            var i = _fsw.Length;
            Array.Resize(ref _fsw, _fsw.Length + 1);

            _fsw[i] = new FileSystemWatcher(path);

            _fsw[i].Created += (s, a) => CheckFile(a.FullPath);
            _fsw[i].Renamed += (s, a) =>
            {
                foreach (var ep in Files)
                {
                    if (ep.Value.Contains(a.OldFullPath))
                    {
                        ep.Value.Remove(a.OldFullPath);
                        CheckFile(a.FullPath);
                        return;
                    }
                }
            };
            _fsw[i].Deleted += (s, a) =>
            {
                foreach (var ep in Files)
                {
                    if (ep.Value.Contains(a.FullPath))
                    {
                        ep.Value.Remove(a.FullPath);
                        return;
                    }
                }
            };
            _fsw[i].Error += (s, a) => StartWatching();

            _fsw[i].EnableRaisingEvents = true;

            var fs = new FileSearch(new[] { path }, CheckFile);

            Indexing = true;

            fs.BeginSearch();
            fs.SearchThread.Join(TimeSpan.FromMinutes(5));

            if (fs.SearchThread.IsAlive)
            {
                fs.SearchThread.Abort();
            }

            Indexing = false;

            SaveList();
        }

        /// <summary>
        /// Saves the file list to the database.
        /// </summary>
        public static void SaveList()
        {
            var path = Path.Combine(Signature.FullPath, @"misc\library");

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
                    if (show.Value.Count == 0)
                    {
                        continue;
                    }

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
            var path = Path.Combine(Signature.FullPath, @"misc\library");

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
