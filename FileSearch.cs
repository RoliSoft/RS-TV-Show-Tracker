namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading;

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
        public string[] StartPaths { get; internal set; }

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
        public FileSearch(string[] paths, string show, string episode)
        {
            _titleParts   = Database.GetReleaseName(show);
            _episodeRegex = ShowNames.Parser.GenerateEpisodeRegexes(episode);

            ShowQuery  = show + " " + episode;
            StartPaths = paths;
            Files      = new List<string>();
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
            foreach (var path in StartPaths)
            {
                ScanDirectoryForFile(path);
            }

            FileSearchDone.Fire(this);
        }

        /// <summary>
        /// Scans the directory recursively for an episode.
        /// </summary>
        /// <param name="path">The path.</param>
        private void ScanDirectoryForFile(string path)
        {
            // search for matching files
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    var name = Path.GetFileName(file);
                    var dirs = Path.GetDirectoryName(file) ?? string.Empty;

                    if (ShowNames.Parser.IsMatch(dirs + @"\" + name, _titleParts, _episodeRegex) && !Files.Contains(file))
                    {
                        var pf = FileNames.Parser.ParseFile(name, dirs.Split(Path.DirectorySeparatorChar), false);
                        if (pf.Success && _titleParts.SequenceEqual(ShowNames.Parser.GetRoot(pf.Show)))
                        {
                            Files.Add(file);
                        }
                    }
                }
            }
            catch (PathTooLongException)        { }
            catch (SecurityException)           { }
            catch (UnauthorizedAccessException) { }
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
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }
        }
    }
}
