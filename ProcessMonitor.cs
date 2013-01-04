namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using FileNames;
    using Parsers.Social;
    using Dependencies.DetectOpenFiles;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    using Parser = ShowNames.Parser;

    /// <summary>
    /// Provides methods to monitor a given process for open file handles.
    /// </summary>
    public static class ProcessMonitor
    {
        /// <summary>
        /// Gets or sets the list of files that have been seen to be open by the specified process.
        /// </summary>
        /// <value>The open files.</value>
        public static List<string> OpenFiles { get; set; }

        /// <summary>
        /// Initializes the <see cref="ProcessMonitor"/> class.
        /// </summary>
        static ProcessMonitor()
        {
            OpenFiles = new List<string>();
        }

        /// <summary>
        /// Gets the process IDs of the specified file names.
        /// </summary>
        /// <param name="names">The process names.</param>
        /// <returns>List of PIDs.</returns>
        public static IEnumerable<int> GetPIDs(List<string> names)
        {
            foreach (var proc in Process.GetProcesses())
            {
                foreach (var name in names)
                {
                    if (proc.ProcessName.Equals(name.Replace(".exe", string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return proc.Id;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of open file handles for the given process IDs.
        /// </summary>
        /// <param name="pids">The process IDs.</param>
        /// <returns>List of the open files.</returns>
        public static IEnumerable<FileInfo> GetHandleList(List<int> pids)
        {
            foreach (var file in new DetectOpenFiles.OpenFiles(pids))
            {
                FileInfo fi;

                try
                {
                    fi = new FileInfo(file);
                }
                catch
                {
                    continue;
                }

                yield return fi;
            }
        }

        /// <summary>
        /// Checks for open files on the specified processes and marks them if recognized.
        /// </summary>
        public static void CheckOpenFiles()
        {
            var procs = new List<string>();
            procs.AddRange(Settings.Get<List<string>>("Processes to Monitor"));
            try { procs.AddRange(Utils.GetDefaultVideoPlayers().Select(Path.GetFileName)); } catch { }

            if (!procs.Any())
            {
                return;
            }

            var pids = GetPIDs(procs).Distinct().ToList();

            if (!pids.Any())
            {
                return;
            }

            var files = GetHandleList(pids).ToList();

            foreach (var show in Database.TVShows)
            {
                var titleRegex   = Parser.GenerateTitleRegex(show.Value.Name);
                var releaseRegex = !string.IsNullOrWhiteSpace(show.Value.Release) ? new Regex(show.Value.Release) : null;

                foreach (var file in files)
                {
                    if (Parser.IsMatch(file.DirectoryName + @"\" + file.Name, titleRegex) || (releaseRegex != null && Parser.IsMatch(file.DirectoryName + @"\" + file.Name, releaseRegex)))
                    {
                        var pf = FileNames.Parser.ParseFile(file.Name, file.DirectoryName.Split(Path.DirectorySeparatorChar), false);
                        if (pf.Success && show.Value.Name == pf.Show)
                        {
                            if (!OpenFiles.Contains(file.ToString()))
                            {
                                // add to open files list
                                // 5 minutes later we'll check again, and if it's still open we'll mark it as seen
                                // the reason for this is that an episode will be marked as seen only if you're watching it for more than 10 minutes (5 minute checks twice)

                                OpenFiles.Add(file.ToString());
                            }
                            else
                            {
                                MarkAsSeen(show.Value.ID, pf);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Marks the specified episode as seen.
        /// </summary>
        /// <param name="showid">The ID of the show.</param>
        /// <param name="file">The identified file.</param>
        public static void MarkAsSeen(int showid, ShowFile file)
        {
            var @new = false;
            var eps  = file.Episode.SecondEpisode.HasValue
                       ? Enumerable.Range(file.Episode.Episode, (file.Episode.SecondEpisode.Value - file.Episode.Episode + 1)).ToArray()
                       : new[] { file.Episode.Episode };

            foreach (var epnr in eps)
            {
                Episode ep;
                if (Database.TVShows[showid].EpisodeByID.TryGetValue(file.Episode.Season * 1000 + epnr, out ep))
                {
                    if (!ep.Watched)
                    {
                        @new = ep.Watched = true;
                    }
                }
            }

            if (@new)
            {
                Database.TVShows[showid].SaveTracking();
                PostToSocial(file);
            }

            MainWindow.Active.DataChanged();
        }

        /// <summary>
        /// Updates the status message on configures social networks.
        /// </summary>
        /// <param name="file">The identified file.</param>
        public static void PostToSocial(ShowFile file)
        {
            if (Settings.Get("Post only recent", true) && (DateTime.Now - file.Airdate).TotalDays > 21)
            {
                return;
            }

            var listed = Settings.Get("Post restrictions list", new List<int>())
                                 .Contains(Database.TVShows.Values.First(x => x.Name == file.Show).ID);

            switch (Settings.Get("Post restrictions list type", "black"))
            {
                case "black":
                    if (listed)
                    {
                        return;
                    }
                    break;

                case "white":
                    if (!listed)
                    {
                        return;
                    }
                    break;
            }

            foreach (var engine in Extensibility.GetNewInstances<SocialEngine>())
            {
                if (!Settings.Get<bool>("Post to " + engine.Name))
                {
                    continue;
                }

                if (engine is OAuthEngine)
                {
                    var tokens = Settings.Get<List<string>>(engine.Name + " OAuth");

                    if (tokens != null && tokens.Count != 0)
                    {
                        ((OAuthEngine)engine).Tokens = tokens;
                    }
                    else
                    {
                        continue;
                    }
                }


                var format = Settings.Get(engine.Name + " Status Format", engine.DefaultStatusFormat);
                if (string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                try { engine.PostMessage(FileNames.Parser.FormatFileName(format, file)); } catch { }
            }
        }
    }
}
