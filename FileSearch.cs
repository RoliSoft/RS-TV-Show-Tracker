namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        /// Gets the path where the search begins.
        /// </summary>
        /// <value>The start path.</value>
        public string StartPath { get; internal set; }

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
        /// <param name="path">The path where to start the search.</param>
        /// <param name="show">The show name.</param>
        /// <param name="episode">The episode number.</param>
        public FileSearch(string path, string show, string episode)
        {
            _titleParts   = Database.GetReleaseName(show);
            _episodeRegex = ShowNames.Parser.GenerateEpisodeRegexes(episode);

            ShowQuery = show + " " + episode;
            StartPath = path;
            Files     = new List<string>();
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
            // start
            try
            {
                ScanDirectoryForFile(StartPath);
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            // fire event
            FileSearchDone.Fire(this);
        }

        /// <summary>
        /// Scans the directory recursively for an episode.
        /// </summary>
        /// <param name="path">The path.</param>
        private void ScanDirectoryForFile(string path)
        {
            // search for matching files
            foreach (var file in Directory.GetFiles(path))
            {
                var name = Path.GetFileName(file);
                var dir  = Path.GetFileName(Path.GetDirectoryName(file));

                if (ShowNames.Parser.IsMatch(dir + @"\" + name, _titleParts, _episodeRegex) && !Files.Contains(file))
                {
                    var pf = FileNames.Parser.ParseFile(name);
                    if ((pf.Success && _titleParts.SequenceEqual(ShowNames.Parser.GetRoot(pf.Show))) || // is the show extracted from the file name the exact same?
                        ((pf = FileNames.Parser.ParseFile(dir)).Success && _titleParts.SequenceEqual(ShowNames.Parser.GetRoot(pf.Show)))) // or the one extracted from the directory name?
                    {
                        Files.Add(file);
                    }
                }
            }

            // search for matching directory names
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    continue;
                }

                ScanDirectoryForFile(dir);
            }
        }
    }
}
