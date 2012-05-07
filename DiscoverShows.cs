namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Threading;

    using HtmlAgilityPack;

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

        private HashSet<string> _results;

        /// <summary>
        /// Starts the search for TV shows recursively starting from the specified path.
        /// </summary>
        /// <param name="path">The paths where to start the search.</param>
        /// <returns>
        /// The list of discovered TV shows.
        /// </returns>
        public List<string> DiscoverFromPath(string path)
        {
            _results = new HashSet<string>();

            ScanDirectoryForFile(path);

            return _results.ToList();
        }

        /// <summary>
        /// Starts the search for TV shows in a HTML document.
        /// </summary>
        /// <param name="url">The URL of the HTML document to get and parse.</param>
        /// <returns>
        /// The list of discovered TV shows.
        /// </returns>
        public List<string> DiscoverFromHTML(string url)
        {
            _results = new HashSet<string>();

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

            var html  = Utils.GetHTML(url);
            var nodes = html.DocumentNode.SelectNodes("//.");

            foreach (var node in nodes)
            {
                var text = HtmlEntity.DeEntitize(node.InnerText).Trim();

                if (string.IsNullOrWhiteSpace(text)) continue;

                var slug = Utils.CreateSlug(text);

                foreach (var show in FileNames.Parser.AllKnownTVShows.Where(x => !string.IsNullOrWhiteSpace(x.Slug) && x.Slug != "episodes" && x.Slug != "popular"))
                {
                    if (show.Slug == slug)
                    {
                        _results.Add(show.Title);
                        break;
                    }
                }
            }

            return _results.ToList();
        }

        /// <summary>
        /// Starts the asynchronous search for TV shows recursively starting from the specified path.
        /// </summary>
        /// <param name="path">The paths where to start the search.</param>
        public void BeginAsyncFileDiscovery(string path)
        {
            SearchThread = new Thread(() => DiscoveryDone.Fire(this, DiscoverFromPath(path)));
            SearchThread.Start();
        }

        /// <summary>
        /// Starts the asynchronous search for TV shows in a HTML document.
        /// </summary>
        /// <param name="url">The URL of the HTML document to get and parse.</param>
        public void BeginAsyncHTMLDiscovery(string url)
        {
            SearchThread = new Thread(() => DiscoveryDone.Fire(this, DiscoverFromHTML(url)));
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
            var pf = FileNames.Parser.ParseFile(Path.GetFileName(file), Path.GetDirectoryName(file).Split(Path.DirectorySeparatorChar), false);
            if (pf.Success)
            {
                _results.Add(pf.Show);
            }
        }
    }
}
