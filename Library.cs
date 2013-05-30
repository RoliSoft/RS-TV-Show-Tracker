namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.FileNames;

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
        public static Dictionary<int, HashSet<string>> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Library" /> is currently searching for files.
        /// </summary>
        /// <value>
        ///   <c>true</c> if currently searching for files; otherwise, <c>false</c>.
        /// </value>
        public static volatile bool Indexing;

        private static FileSystemWatcher[] _fsw;
        private static Dictionary<int, HashSet<string>> _tmp;
        private static BlockingCollection<Tuple<WatcherChangeTypes, string[]>> _producer;
        private static Task _consumer;
        private static Timer _stmr;
        private static volatile bool _isStarting;

        /// <summary>
        /// Initializes the <see cref="Library" /> class.
        /// </summary>
        static Library()
        {
            Files = new Dictionary<int, HashSet<string>>();

            try
            {
                LoadList();
            }
            catch (Exception ex)
            {
                Log.Warn("Error while loading library. It will be regenerated on next search.", ex);
            }
        }

        /// <summary>
        /// Initializes the library.
        /// </summary>
        public static void Initialize()
        {
            if (!Signature.IsActivated)
            {
                return;
            }

            if (Files.Count != 0)
            {
                Log.Debug("Library data loaded; checking to see if files still exist.");

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
                    Interval  = 3000
                };
            _stmr.Elapsed += (sender, args) =>
                {
                    try
                    {
                        SaveList();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Error while saving library.", ex);
                    }

                    if (UPnP.IsRunning)
                    {
                        try
                        {
                            UPnP.RebuildList();
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("Error while rebuilding UPnP/DLNA library.", ex);
                        }
                    }
                };

            SearchForFiles();
        }

        /// <summary>
        /// Creates and starts <c>FileSystemWatcher</c> instances on the download paths.
        /// </summary>
        public static void StartWatching()
        {
            if (_isStarting || !Signature.IsActivated)
            {
                return;
            }

            _isStarting = true;

            Log.Debug(((_fsw != null && _fsw.Length != 0) ? "Res" : "S") + "tarting file system watchers...");

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

            var paths = Settings.Get<List<string>>("Download Paths");

            if (_producer != null)
            {
                _producer.CompleteAdding();
            }

            _fsw      = new FileSystemWatcher[paths.Count];
            _producer = new BlockingCollection<Tuple<WatcherChangeTypes, string[]>>();
            _consumer = new Task(ProcessChangesQueue, TaskCreationOptions.LongRunning);

            _consumer.Start();

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
                catch (Exception ex)
                {
                    Log.Warn("Error while creating file system watcher for " + paths[i] + ".", ex);
                }
            }

            _isStarting = false;
        }

        /// <summary>
        /// Handles the OnCreated event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="fileSystemEventArgs">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (Log.IsTraceEnabled) Log.Trace("File created: " + fileSystemEventArgs.FullPath);

            _producer.Add(Tuple.Create(WatcherChangeTypes.Created, new[] { fileSystemEventArgs.FullPath }));
        }

        /// <summary>
        /// Handles the OnRenamed event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="renamedEventArgs">The <see cref="RenamedEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            if (Log.IsTraceEnabled) Log.Trace("File renamed: " + renamedEventArgs.OldFullPath + " -> " + renamedEventArgs.FullPath);

            _producer.Add(Tuple.Create(WatcherChangeTypes.Deleted, new[] { renamedEventArgs.OldFullPath }));
            _producer.Add(Tuple.Create(WatcherChangeTypes.Created, new[] { renamedEventArgs.FullPath }));
        }

        /// <summary>
        /// Handles the OnDeleted event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="fileSystemEventArgs">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (Log.IsTraceEnabled) Log.Trace("File deleted: " + fileSystemEventArgs.FullPath);

            _producer.Add(Tuple.Create(WatcherChangeTypes.Deleted, new[] { fileSystemEventArgs.FullPath }));
        }

        /// <summary>
        /// Handles the OnError event of the FileSystemWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="errorEventArgs">The <see cref="ErrorEventArgs" /> instance containing the event data.</param>
        private static void FileSystemWatcher_OnError(object sender, ErrorEventArgs errorEventArgs)
        {
            Log.Warn("Error occurred while watching specified directories.", errorEventArgs.GetException());

            StartWatching();
        }

        /// <summary>
        /// Consumes the file system changes blocking collection.
        /// </summary>
        private static void ProcessChangesQueue()
        {
            var prod = _producer;

            try
            {
                foreach (var evt in prod.GetConsumingEnumerable())
                {
                    try
                    {
                        switch (evt.Item1)
                        {
                            case WatcherChangeTypes.Created:
                            {
                                var success = false;

                                if (File.Exists(evt.Item2[0]))
                                {
                                    success = CheckFile(evt.Item2[0]);

                                    if (success)
                                    {
                                        Log.Debug("Added file " + Path.GetFileName(evt.Item2[0]) + " to the library.");
                                    }
                                }
                                else if (Directory.Exists(evt.Item2[0]))
                                {
                                    Log.Debug("Scanning new directory " + Path.GetFileName(evt.Item2[0]) + " for episodes...");

                                    if (_producer.Count < 10)
                                    {
                                        Log.Trace("Queue.Count < 10, waiting 5s before initiating directory scan...");
                                        Thread.Sleep(5000);
                                    }

                                    var fs = new FileSearch(new[] {evt.Item2[0]}, CheckFile);

                                    fs.FileSearchDone += (sender, args) =>
                                    {
                                        if (args.Data == null || args.Data.Count == 0)
                                        {
                                            Log.Debug("No episodes found in " + evt.Item2[0] + ".");
                                        }
                                        else
                                        {
                                            success = true;

                                            foreach (var file in args.Data)
                                            {
                                                Log.Debug("Added file " + Path.GetFileName(file) + " to the library.");
                                            }
                                        }
                                    };

                                    fs.BeginSearch();

                                    fs.Cancellation.CancelAfter(TimeSpan.FromMinutes(5));

                                    try
                                    {
                                        if (!fs.SearchTask.Wait(TimeSpan.FromMinutes(5.5)))
                                        {
                                            fs.SearchTask.Dispose();
                                        }
                                    }
                                    catch (AggregateException ex)
                                    {
                                        if (ex.InnerException is OperationCanceledException)
                                        {
                                            Log.Error("Directory scan timed out after 5 minutes of file searching.");
                                        }
                                        else
                                        {
                                            Log.Warn("Aggregate exceptions upon FileSearch._task completion.", ex);
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Warn(evt.Item2[0] + " has been deleted since the creation event was fired.");
                                }

                                if (success && !Indexing)
                                {
                                    DataChanged();
                                }
                            }
                                break;

                            case WatcherChangeTypes.Deleted:
                            {
                                var success = false;

                                foreach (var ep in Files)
                                {
                                    if (ep.Value.Contains(evt.Item2[0]))
                                    {
                                        ep.Value.Remove(evt.Item2[0]);
                                        success = true;
                                        Log.Debug("Removed file " + Path.GetFileName(evt.Item2[0]) + " from the library.");
                                        break;
                                    }
                                }

                                if (!success)
                                {
                                    foreach (var ep in Files)
                                    {
                                        var lst = ep.Value.ToList();

                                        foreach (var file in lst)
                                        {
                                            if (file.StartsWith(evt.Item2[0] + Path.DirectorySeparatorChar))
                                            {
                                                ep.Value.Remove(file);
                                                success = true;
                                                Log.Debug("Removed file " + Path.GetFileName(file) + " from the library.");
                                            }
                                        }
                                    }
                                }

                                if (success && !Indexing)
                                {
                                    DataChanged();
                                }
                            }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Error while processing change " + evt.Item1 + " for " + evt.Item2.FirstOrDefault() + ".", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Exception upon producer/consumer collection enumeration.", ex);
            }
            finally
            {
                if (prod != null)
                {
                    prod.Dispose();
                }
            }
        }

        /// <summary>
        /// Searches for matching files in the download paths.
        /// </summary>
        public static void SearchForFiles()
        {
            if (!Signature.IsActivated)
            {
                return;
            }

            if (Indexing)
            {
                Log.Warn("A library update operation is currently running, cannot start a new one.");
                return;
            }

            Log.Info("Starting library update...");

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

            if (_producer != null)
            {
                _producer.CompleteAdding();
                _producer.Dispose();
                _producer = null;
            }

            var st = DateTime.Now;
            var fs = new FileSearch(Settings.Get<List<string>>("Download Paths"), CheckFile);

            Indexing = true;

            _tmp = new Dictionary<int, HashSet<string>>();

            fs.BeginSearch();

            fs.Cancellation.CancelAfter(TimeSpan.FromMinutes(5));

            try
            {
                if (!fs.SearchTask.Wait(TimeSpan.FromMinutes(5.5)))
                {
                    fs.SearchTask.Dispose();
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    Log.Error("Library update timed out after 5 minutes of file searching. Please optimize your download paths, and, if possible, please refrain from using network-attached paths.");
                }
                else
                {
                    Log.Warn("Aggregate exceptions upon FileSearch._task completion.", ex);
                }
            }

            Files = _tmp;

            Indexing = false;

            Log.Info("Library update finished in " + (DateTime.Now - st).TotalSeconds + "s.");

            StartWatching();
            DataChanged(false);
        }

        /// <summary>
        /// Propagates data changes.
        /// </summary>
        /// <param name="delaySave">if set to <c>true</c> the database commit will be delayed for 3 seconds.</param>
        private static void DataChanged(bool delaySave = true)
        {
            try
            {
                if (MainWindow.Active != null && MainWindow.Active.activeGuidesPage != null && MainWindow.Active.activeGuidesPage._activeShowID != 0)
                {
                    MainWindow.Active.Run(MainWindow.Active.activeGuidesPage.Refresh);
                }
            }
            catch { }

            if (delaySave)
            {
                if (_stmr.Enabled)
                {
                    _stmr.Stop();
                }

                _stmr.Start();
            }
            else
            {
                try
                {
                    SaveList();
                }
                catch (Exception ex)
                {
                    Log.Warn("Error while saving library.", ex);
                }

                if (UPnP.IsRunning)
                {
                    try
                    {
                        UPnP.RebuildList();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Error while rebuilding UPnP/DLNA library.", ex);
                    }
                }
            }
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
            var pf   = Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

            if (pf.DbEpisode == null)
            {
                if (Log.IsTraceEnabled && !string.IsNullOrWhiteSpace(pf.Show)) Log.Trace("Identified file " + name + " as " + pf + " but discarded it.");
                return false;
            }

            var dict = Indexing ? _tmp : Files;
            HashSet<string> list;

            if (Log.IsTraceEnabled) Log.Trace("Identified file " + name + " as " + pf + ".");

            if (dict.TryGetValue(pf.DbEpisode.ID, out list))
            {
                if (!list.Contains(file))
                {
                    list.Add(file);
                }
            }
            else
            {
                dict[pf.DbEpisode.ID] = new HashSet<string> { file };
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

            return true;
        }

        /// <summary>
        /// Removes the any data associated to a path.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void RemovePath(string path)
        {
            if (Indexing) return;

            Log.Info("Removing path from library: " + path);

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

            Log.Info("Adding path to library: " + path);

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
            catch (Exception ex)
            {
                Log.Warn("Error while creating file system watcher for " + path + ".", ex);
            }
        }

        /// <summary>
        /// Saves the file list to the database.
        /// </summary>
        public static void SaveList()
        {
            Log.Debug("Saving library...");

            var path = Path.Combine(Signature.InstallPath, @"misc\library");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Log.Debug("$InstallPath\\misc does not exist, creating it...");

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                catch (Exception ex)
                {
                    Log.Error("Error while creating directory $InstallPath\\misc, library will not be saved.", ex);
                    return;
                }
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

                    HashSet<string> lst;

                    if (!Files.TryGetValue(sid, out lst))
                    {
                        Files[sid] = lst = new HashSet<string>();
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
