namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Dependencies.USNJournal;
    using Tables;

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
        /// Occurs when a multiple file search is done.
        /// </summary>
        public event EventHandler<EventArgs<List<string>[]>> MultiFileSearchDone;

        /// <summary>
        /// Occurs when the progress of the file search has changed.
        /// </summary>
        public event EventHandler<EventArgs<string>> FileSearchProgressChanged;

        /// <summary>
        /// Gets the search thread.
        /// </summary>
        /// <value>The search thread.</value>
        public Thread SearchThread { get; internal set; }

        private Dictionary<string, List<string>> _paths;
        private List<string>[] _files; 
        private Episode[] _episodes;
        private Regex[] _titleRegex, _episodeRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="episode">The episode to search for.</param>
        public FileSearch(IEnumerable<string> paths, Episode episode)
        {
            _episodes     = new[] { episode };
            _titleRegex   = new[] { episode.Show.GenerateRegex() };
            _episodeRegex = new[] { episode.GenerateRegex() };

            InitStartPaths(paths);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="episodes">The episodes to search for.</param>
        public FileSearch(IEnumerable<string> paths, IEnumerable<Episode> episodes)
        {
            _episodes     = episodes.ToArray();
            _titleRegex   = new Regex[_episodes.Length];
            _episodeRegex = new Regex[_episodes.Length];

            for (var i = 0; i < _episodes.Length; i++)
            {
                _titleRegex[i]   = _episodes[i].Show.GenerateRegex();
                _episodeRegex[i] = _episodes[i].GenerateRegex();
            }

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
            SearchThread = new Thread(Search);
            SearchThread.Start();
        }

        /// <summary>
        /// Cancels the asynchronous search.
        /// </summary>
        public void CancelSearch()
        {
            try { SearchThread.Abort(); } catch { }
        }

        /// <summary>
        /// Starts the search.
        /// </summary>
        private void Search()
        {
            _files = new List<string>[_episodes.Length];

            for (var i = 0; i < _files.Length; i++)
            {
                _files[i] = new List<string>();
            }

            foreach (var paths in _paths)
            {
                DriveInfo drive = null;

                if (paths.Key != string.Empty)
                {
                    try
                    {
                        drive = new DriveInfo(paths.Key);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Active.HandleUnexpectedException(ex);
                    }
                }

                if (drive != null && drive.DriveFormat == "NTFS" && Settings.Get<bool>("Search NTFS MFT records") && Utils.IsAdmin)
                {
                    try
                    {
                        ScanNtfsMftForFile(drive, paths.Value.Count == 1 && paths.Value[0].Length == 3 ? null : paths.Value);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Active.HandleUnexpectedException(ex);
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

            if (_files.Length == 1)
            {
                FileSearchDone.Fire(this, _files[0]);
            }
            else
            {
                MultiFileSearchDone.Fire(this, _files);
            }
        }

        /// <summary>
        /// Scans the directory recursively for a matching file.
        /// </summary>
        /// <param name="path">The path to start the search from.</param>
        private void ScanDirectoryForFile(string path)
        {
            // search for matching files
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try
                    {
                        CheckFile(file);
                    }
                    catch (PathTooLongException)        { }
                    catch (SecurityException)           { }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException)  { }
                    catch (Exception ex)
                    {
                        MainWindow.Active.HandleUnexpectedException(ex);
                    }
                }
            }
            catch (PathTooLongException)        { }
            catch (SecurityException)           { }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException)  { }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            // WE MUST GO DEEPER!
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        continue;
                    }

                    ScanDirectoryForFile(dir);
                }
            }
            catch (PathTooLongException)        { }
            catch (SecurityException)           { }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException)  { }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
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

            var usn  = new NtfsUsnJournal(drive);
            var list = usn.GetParsedPaths(ShowNames.Regexes.KnownVideo, paths);

            FileSearchProgressChanged.Fire(this, "Searching for matching files in the " + drive.Name[0] + " partition...");

            foreach (var file in list)
            {
                try
                {
                    CheckFile(file);
                }
                catch (PathTooLongException)        { }
                catch (SecurityException)           { }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException)  { }
                catch (Exception ex)
                {
                    MainWindow.Active.HandleUnexpectedException(ex);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified file is a match and inserts it into <see cref="_files"/> if it is.
        /// </summary>
        /// <param name="file">The file.</param>
        private void CheckFile(string file)
        {
            var name = Path.GetFileName(file);
            var dirs = Path.GetDirectoryName(file) ?? string.Empty;

            for (var i = 0; i < _files.Length; i++)
            {
                if (ShowNames.Parser.IsMatch(dirs + @"\" + name, _titleRegex[i], _episodeRegex[i]))
                {
                    var pf = FileNames.Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

                    if (pf.Success)
                    {
                        _files[i].Add(file);
                        break;
                    }
                }
            }
        }
    }
}
