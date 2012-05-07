namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading;

    using ProtoBuf;

    /// <summary>
    /// Various functions to discover TV shows in a specified input.
    /// </summary>
    public class DiscoverShows
    {
        /// <summary>
        /// Occurs when a discovery is done.
        /// </summary>
        public event EventHandler<EventArgs<List<string>>> DiscoveryDone;

        /// <summary>
        /// Gets the search thread.
        /// </summary>
        /// <value>
        /// The search thread.
        /// </value>
        public Thread SearchThread { get; internal set; }

        /// <summary>
        /// Gets or sets the list of regexes from the list of all known shows.
        /// </summary>
        /// <value>
        /// The list of regexes from the list of all known shows.
        /// </value>
        public Dictionary<FileNames.Parser.KnownTVShow, Regex> ShowRegexes { get; set; }

        private HashSet<string> _results;

        /// <summary>
        /// Starts the search for TV shows recursively starting from the specified path.
        /// </summary>
        /// <param name="path">The paths where to start the search.</param>
        /// <returns>
        /// The list of discovered TV shows.
        /// </returns>
        public List<string> DiscoverFiles(string path)
        {
            GenerateShowRegexes();
            
            _results = new HashSet<string>();

            ScanDirectoryForFile(path);

            return _results.ToList();
        }

        /// <summary>
        /// Starts the asynchronous search for TV shows recursively starting from the specified path.
        /// </summary>
        /// <param name="path">The paths where to start the search.</param>
        public void BeginAsyncFileDiscovery(string path)
        {
            SearchThread = new Thread(() => DiscoveryDone.Fire(this, DiscoverFiles(path)));
            SearchThread.Start();
        }

        /// <summary>
        /// Cancels the asynchronous discovery.
        /// </summary>
        public void CancelAsync()
        {
            try { SearchThread.Abort(); } catch { }
        }

        /// <summary>
        /// Generates a list of regexes from the list of all known shows.
        /// </summary>
        private void GenerateShowRegexes()
        {
            if (FileNames.Parser.AllKnownTVShows.Count == 0)
            {
                var fn = Path.Combine(Path.GetTempPath(), "AllKnownTVShows.bin");

                if (File.Exists(fn) && new FileInfo(fn).Length != 0)
                {
                    using (var file = File.OpenRead(fn))
                    {
                        try { FileNames.Parser.AllKnownTVShows = Serializer.Deserialize<List<FileNames.Parser.KnownTVShow>>(file); } catch { }
                    }
                }
                else
                {
                    try { FileNames.Parser.GetAllKnownTVShows(); } catch { }
                }
            }

            ShowRegexes = new Dictionary<FileNames.Parser.KnownTVShow, Regex>();

            foreach (var show in FileNames.Parser.AllKnownTVShows.Where(x => !string.IsNullOrWhiteSpace(x.Slug)).GroupBy(x => x.Slug).Select(x => x.First()))
            {
                ShowRegexes.Add(show, ShowNames.Parser.GenerateTitleRegex(show.Title));
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
        /// Determines whether the specified file is a match and inserts it into <see cref="_results"/> if it is.
        /// </summary>
        /// <param name="file">The file.</param>
        private void CheckFile(string file)
        {
            var name = Path.GetFileName(file);
            var dirs = Path.GetDirectoryName(file) ?? string.Empty;

            foreach (var show in ShowRegexes)
            {
                if (ShowNames.Parser.IsMatch(dirs + @"\" + name, show.Value))
                {
                    _results.Add(show.Key.Title);
                    break;
                }
            }
        }
    }
}
