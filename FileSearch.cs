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

        private readonly IEnumerable<string> _titleParts, _episodeParts;
        private readonly Regex _knownVideoRegex, _sampleVideoRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSearch"/> class.
        /// </summary>
        /// <param name="path">The path where to start the search.</param>
        /// <param name="show">The show name.</param>
        /// <param name="episode">The episode number.</param>
        public FileSearch(string path, string show, string episode)
        {
            _titleParts   = ShowNames.Parser.GetRoot(show);
            _episodeParts = new[]
                {
                    // S02E14
                    episode,
                    // S02.E14
                    episode.Replace("E", ".E"),
                    // 2x14
                    Regex.Replace(episode, "S0?([0-9]{1,2})E([0-9]{1,2})", "$1X$2", RegexOptions.IgnoreCase),
                    // 214
                    Regex.Replace(episode, "S0?([0-9]{1,2})E([0-9]{1,2})", "$1$2", RegexOptions.IgnoreCase)
                };
            
            _knownVideoRegex  = new Regex(@"\.(avi|mkv|mp4|wmv)$", RegexOptions.IgnoreCase);
            _sampleVideoRegex = new Regex(@"(^|[\.\-\s])sample[\.\-\s]", RegexOptions.IgnoreCase);

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
                if (!string.IsNullOrWhiteSpace(name)
                 && _titleParts.All(part => Regex.IsMatch(name, @"\b" + part + @"\b", RegexOptions.IgnoreCase)) // does it have all the title words?
                 && _knownVideoRegex.IsMatch(name) // is it a known video file extension?
                 && !_sampleVideoRegex.IsMatch(name) // is it not a sample?
                 && _episodeParts.Any(ep => Regex.IsMatch(name, @"\b" + ep + @"\b", RegexOptions.IgnoreCase)) // is it the episode we want?
                 && !Files.Contains(file)) // and not in the array already?
                {
                    Files.Add(file);
                }
            }

            // search for matching directory names
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    continue;
                }

                if (_titleParts.All(part => Regex.IsMatch(dir, @"\b" + part + @"\b", RegexOptions.IgnoreCase)) // does it have all the title words?
                 && _episodeParts.Any(ep => Regex.IsMatch(dir, @"\b" + ep + @"\b", RegexOptions.IgnoreCase))) // is it the episode we want?
                {
                    // search for matching episodes inside the matching directory
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        if (_knownVideoRegex.IsMatch(file) // is it a video?
                         && !Files.Contains(file) // and not in the array already?
                         && !_sampleVideoRegex.IsMatch(file)) // and not just a sample?
                        {
                            Files.Add(file);
                        }
                    }
                }

                ScanDirectoryForFile(dir);
            }
        }
    }
}
