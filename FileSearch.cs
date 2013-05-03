namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Dependencies.USNJournal;
    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Provides file search for finding the episodes on the disk.
    /// </summary>
    public class FileSearch
    {
        /// <summary>
        /// Occurs when a file search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<string>>> FileSearchDone;

        /// <summary>
        /// Occurs when the progress of the file search has changed.
        /// </summary>
        public event EventHandler<EventArgs<string>> FileSearchProgressChanged;

        /// <summary>
        /// Gets or sets the found files.
        /// </summary>
        /// <value>The found files.</value>
        public List<string> Result
        {
            get { return _files; }
            set { _files = value; }
        }

        /// <summary>
        /// Gets or sets the file checking function.
        /// </summary>
        /// <value>The file checking function.</value>
        public Func<string, bool> CheckFile
        {
            get { return _checkFile; }
            set { _checkFile = value; }
        }

        /// <summary>
        /// Gets the search task.
        /// </summary>
        /// <value>The search task.</value>
        public Task SearchTask
        {
            get { return _task; }
        }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        /// <value>The cancellation token.</value>
        public CancellationTokenSource Cancellation
        {
            get { return _cts; }
        }

        private Task _task;
        private CancellationTokenSource _cts;
        private Dictionary<string, List<string>> _paths;
        private List<string> _files;
        private Regex[] _titleRegex, _episodeRegex;
        private Func<string, bool> _checkFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="episode">The episode to search for.</param>
        public FileSearch(IEnumerable<string> paths, Episode episode)
        {
            _checkFile    = StandardCheckFile;
            _titleRegex   = new[] { episode.Show.GenerateRegex() };
            _episodeRegex = new[] { episode.GenerateRegex() };

            InitStartPaths(paths);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="episodes">The episodes to search for.</param>
        public FileSearch(IEnumerable<string> paths, IList<Episode> episodes)
        {
            _checkFile    = StandardCheckFile;
            _titleRegex   = new Regex[episodes.Count];
            _episodeRegex = new Regex[episodes.Count];

            for (var i = 0; i < episodes.Count; i++)
            {
                _titleRegex[i]   = episodes[i].Show.GenerateRegex();
                _episodeRegex[i] = episodes[i].GenerateRegex();
            }

            InitStartPaths(paths);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="show">The show to search for.</param>
        public FileSearch(IEnumerable<string> paths, TVShow show)
        {
            _checkFile    = StandardCheckFile;
            _titleRegex   = new[] { show.GenerateRegex() };
            _episodeRegex = new[] { default(Regex) };

            InitStartPaths(paths);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="shows">The shows to search for.</param>
        public FileSearch(IEnumerable<string> paths, IList<TVShow> shows)
        {
            _checkFile    = StandardCheckFile;
            _titleRegex   = new Regex[shows.Count];
            _episodeRegex = new Regex[shows.Count];

            for (var i = 0; i < shows.Count; i++)
            {
                _titleRegex[i] = shows[i].GenerateRegex();
            }

            InitStartPaths(paths);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch" /> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="checkFile">The file checking function.</param>
        public FileSearch(IEnumerable<string> paths, Func<string, bool> checkFile)
        {
            _checkFile = checkFile;

            InitStartPaths(paths);
        }

        /// <summary>
        /// Groups the start paths into <see cref="_paths"/>.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        private void InitStartPaths(IEnumerable<string> paths)
        {
            _paths = new Dictionary<string, List<string>>();

            foreach (var path in paths)
            {
                var match = Regex.Match(path, @"^([A-Za-z]{1,2}):\\");
                var drive = match.Success ? match.Groups[1].Value : string.Empty;

                if (!_paths.ContainsKey(drive))
                {
                    _paths[drive] = new List<string>();
                }

                _paths[drive].Add(path);
            }
        }

        /// <summary>
        /// Begins the search asynchronously.
        /// </summary>
        public void BeginSearch()
        {
            _cts  = new CancellationTokenSource();
            _task = Task.Factory.StartNew(Search, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// Cancels the asynchronous search.
        /// </summary>
        public void CancelSearch()
        {
            _cts.Cancel();

            try
            {
                if (!_task.Wait(500))
                {
                    Log.Warn("Unable to cancel FileSearch._task after 500ms. Killing it.");

                    _task.Dispose();
                }
            }
            catch (AggregateException ex)
            {
                Log.Warn("Aggregate exceptions upon FileSearch._task completion.", ex);
            }
            catch (Exception ex)
            {
                Log.Warn("Error while canceling FileSearch._task.", ex);
            }
        }

        /// <summary>
        /// Starts the search.
        /// </summary>
        private void Search()
        {
            _files = new List<string>();

            foreach (var paths in _paths)
            {
                _cts.Token.ThrowIfCancellationRequested();

                DriveInfo drive = null;

                if (paths.Key != string.Empty)
                {
                    try
                    {
                        drive = new DriveInfo(paths.Key);
                        var s = drive.AvailableFreeSpace; // make sure disk is ready
                    }
                    catch (DriveNotFoundException) { continue; }
                    catch (IOException)            { continue; }
                    catch (Exception ex)
                    {
                        MainWindow.HandleUnexpectedException(ex);
                    }
                }

                if (drive != null && drive.DriveFormat == "NTFS" && Settings.Get<bool>("Search NTFS MFT records") && Utils.IsAdmin)
                {
                    try
                    {
                        ScanNtfsMftForFile(drive, paths.Value.Count == 1 && paths.Value[0].Length == 3 ? null : paths.Value);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        MainWindow.HandleUnexpectedException(ex);
                    }
                }
                else
                {
                    foreach (var path in paths.Value)
                    {
                        FileSearchProgressChanged.Fire(this, "Searching recursively for matching files in " + path + "...");

                        ScanDirectoryForFile(path);
                    }
                }
            }

            FileSearchDone.Fire(this, _files);
        }

        /// <summary>
        /// Scans the directory recursively for a matching file.
        /// </summary>
        /// <param name="path">The path to start the search from.</param>
        private void ScanDirectoryForFile(string path)
        {
            _cts.Token.ThrowIfCancellationRequested();

            // search for matching files
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    try
                    {
                        if (_checkFile(file))
                        {
                            _files.Add(file);
                        }
                    }
                    catch (PathTooLongException)        { }
                    catch (SecurityException)           { }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException)  { }
                    catch (Exception ex)
                    {
                        Log.Error("Error while checking file.", ex);
                    }
                }
            }
            catch (IOException)                 { }
            catch (SecurityException)           { }
            catch (UnauthorizedAccessException) { }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error("Error while enumerating files.", ex);
            }

            _cts.Token.ThrowIfCancellationRequested();

            // WE MUST GO DEEPER!
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        continue;
                    }

                    ScanDirectoryForFile(dir);
                }
            }
            catch (IOException)                 { }
            catch (SecurityException)           { }
            catch (UnauthorizedAccessException) { }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error("Error while enumerating directories.", ex);
            }
        }

        /// <summary>
        /// Scans the NTFS Master File Table entries for a matching file.
        /// </summary>
        /// <param name="drive">The partition to scan.</param>
        /// <param name="paths">The paths to which the search should be limited.</param>
        private void ScanNtfsMftForFile(DriveInfo drive, IEnumerable<string> paths = null)
        {
            FileSearchProgressChanged.Fire(this, "Reading the MFT records of the " + drive.Name[0] + " partition...");

            IEnumerable<string> list;

            Log.Debug("Reading the MFT records of the " + drive.Name[0] + " partition...");

            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                var usn = new NtfsUsnJournal(drive);

                _cts.Token.ThrowIfCancellationRequested();
                list = usn.GetParsedPaths(ShowNames.Regexes.KnownVideo, paths);

                _cts.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error("Error while reading the MFT records of the " + drive.Name[0] + " partition.", ex);
                return;
            }

            FileSearchProgressChanged.Fire(this, "Searching for matching files in the " + drive.Name[0] + " partition...");

            foreach (var file in list)
            {
                _cts.Token.ThrowIfCancellationRequested();

                try
                {
                    if (_checkFile(file))
                    {
                        _files.Add(file);
                    }
                }
                catch (PathTooLongException)        { }
                catch (SecurityException)           { }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException)  { }
                catch (Exception ex)
                {
                    Log.Error("Error while checking file.", ex);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified file is a match and inserts it into <see cref="_files" /> if it is.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        ///   <c>true</c> if matched; otherwise <c>false</c>.
        /// </returns>
        private bool StandardCheckFile(string file)
        {
            var name = Path.GetFileName(file);
            var dirs = Path.GetDirectoryName(file) ?? string.Empty;

            for (var i = 0; i < _titleRegex.Length; i++)
            {
                if (ShowNames.Parser.IsMatch(dirs + @"\" + name, _titleRegex[i], _episodeRegex[i]))
                {
                    var pf = FileNames.Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

                    if (pf.Success)
                    {
                        if (Log.IsTraceEnabled) Log.Trace("Identified file " + name + " as " + pf + ".");

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
