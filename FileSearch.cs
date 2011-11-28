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
        /// Occurs when the progress of the file search has changed.
        /// </summary>
        public event EventHandler<EventArgs<string>> FileSearchProgressChanged;

        /// <summary>
        /// Gets the paths where the search will begin.
        /// </summary>
        /// <value>The start paths.</value>
        public Dictionary<string, List<string>> StartPaths { get; internal set; }

        /// <summary>
        /// Gets the search thread.
        /// </summary>
        /// <value>The search thread.</value>
        public Thread SearchThread { get; internal set; }

        private readonly Episode[] _episodes;
        private readonly Regex[] _titleRegex, _episodeRegex;

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
        /// Groups the start paths into <see cref="StartPaths"/>.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        private void InitStartPaths(IEnumerable<string> paths)
        {
            StartPaths = new Dictionary<string, List<string>>();

            foreach (var path in paths)
            {
                var match = Regex.Match(path, @"^([A-Za-z]{1,2}):\\");
                var drive = match.Success ? match.Groups[1].Value : string.Empty;

                if (!StartPaths.ContainsKey(drive))
                {
                    StartPaths[drive] = new List<string>();
                }

                StartPaths[drive].Add(path);
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
            var files = new List<string>();

            foreach (var paths in StartPaths)
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
                        ScanNtfsMftForFile(drive, files, paths.Value.Count == 1 && paths.Value[0].Length == 3 ? null : paths.Value);
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

                        ScanDirectoryForFile(path, files);
                    }
                }
            }

            FileSearchDone.Fire(this, files);
        }

        /// <summary>
        /// Scans the directory recursively for a matching file.
        /// </summary>
        /// <param name="path">The path to start the search from.</param>
        /// <param name="files">The reference to a <c>List[string]</c> object where the files will be added.</param>
        private void ScanDirectoryForFile(string path, List<string> files)
        {
            // search for matching files
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try
                    {
                        if (IsMatch(file))
                        {
                            files.Add(file);
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

                    ScanDirectoryForFile(dir, files);
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
        /// <param name="files">The reference to a <c>List[string]</c> object where the files will be added.</param>
        /// <param name="paths">The paths to which the search should be limited.</param>
        private void ScanNtfsMftForFile(DriveInfo drive, List<string> files, IEnumerable<string> paths = null)
        {
            FileSearchProgressChanged.Fire(this, "Reading the MFT records of the " + drive.Name[0] + " partition...");

            var usn  = new NtfsUsnJournal(drive);
            var list = usn.GetParsedPaths(ShowNames.Regexes.KnownVideo, paths);

            FileSearchProgressChanged.Fire(this, "Searching for matching files in the " + drive.Name[0] + " partition...");

            foreach (var file in list)
            {
                try
                {
                    if (IsMatch(file))
                    {
                        files.Add(file);
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
        }

        /// <summary>
        /// Determines whether the specified file is a match.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        ///   <c>true</c> if the specified file is a match; otherwise, <c>false</c>.
        /// </returns>
        private bool IsMatch(string file)
        {
            var name = Path.GetFileName(file);
            var dirs = Path.GetDirectoryName(file) ?? string.Empty;

            if (ShowNames.Parser.IsMatch((dirs + @"\" + name).ToUpper(), _titleRegex[0], _episodeRegex[0]))
            {
                var pf = FileNames.Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

                return pf.Success;
            }

            return false;
        }
    }
}
