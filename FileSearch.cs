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

    /// <summary>
    /// Provides file search for finding the episodes on the disk.
    /// </summary>
    public class FileSearch
    {
        /// <summary>
        /// Occurs when a file search is done.
        /// </summary>
        public event EventHandler<EventArgs> FileSearchDone;

        /// <summary>
        /// Gets the paths where the search will begin.
        /// </summary>
        /// <value>The start paths.</value>
        public Dictionary<string, List<string>> StartPaths { get; internal set; }

        /// <summary>
        /// Gets the name of the show and the episode number.
        /// </summary>
        /// <value>The name of the show and the episode number.</value>
        public string ShowQuery { get; internal set; }

        /// <summary>
        /// Gets the search thread.
        /// </summary>
        /// <value>The search thread.</value>
        public Thread SearchThread { get; internal set; }

        /// <summary>
        /// Gets or sets the files found by this class.
        /// </summary>
        /// <value>The files.</value>
        public List<string> Files { get; set; }

        private readonly string[] _titleParts;
        private readonly Regex _episodeRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="paths">The paths where to start the search.</param>
        /// <param name="show">The show name.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="airdate">The airdate.</param>
        public FileSearch(IEnumerable<string> paths, string show, string episode, DateTime? airdate = null)
        {
            _titleParts   = Database.GetReleaseName(show, replaceApostrophes: @"['`’\._]?");
            _episodeRegex = ShowNames.Parser.GenerateEpisodeRegexes(episode, airdate);

            ShowQuery  = show + " " + episode;
            Files      = new List<string>();
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
                        ScanDirectoryForFile(path);
                    }
                }
            }

            FileSearchDone.Fire(this);
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
                        if (IsMatch(file))
                        {
                            Files.Add(file);
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
            var usn  = new NtfsUsnJournal(drive);
            var list = usn.GetParsedPaths(ShowNames.Regexes.KnownVideo, paths);

            foreach (var file in list)
            {
                try
                {
                    if (IsMatch(file))
                    {
                        Files.Add(file);
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

            if (ShowNames.Parser.IsMatch(dirs + @"\" + name, _titleParts, _episodeRegex) && !Files.Contains(file))
            {
                var pf = FileNames.Parser.ParseFile(name, dirs.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries), false);

                return pf.Success && _titleParts.SequenceEqual(Database.GetReleaseName(pf.Show, replaceApostrophes: @"['`’\._]?"));
            }

            return false;
        }
    }
}
