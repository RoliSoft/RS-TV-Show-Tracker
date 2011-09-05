namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.FileNames;
    using RoliSoft.TVShowTracker.Parsers.Social;

    using Parser = RoliSoft.TVShowTracker.ShowNames.Parser;

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
        /// Gets the list of open file handles for the given processes.
        /// </summary>
        /// <param name="processes">The processes' executable name.</param>
        /// <returns>List of the open files.</returns>
        public static List<FileInfo> GetHandleList(IEnumerable<string> processes)
        {
            var sb = new StringBuilder();

            foreach (var process in processes)
            {
                sb.AppendLine(Utils.RunAndRead(Path.Combine(Signature.FullPath, "handle.exe"), "-accepteula -p " + process, true));
            }

            return Regex.Matches(sb.ToString(), @"(?:D|\-)\)\s+(.+)(?:\r|$)")
                   .Cast<Match>()
                   .Select(m => m.Groups[1].Value.Trim())
                   .Distinct(StringComparer.CurrentCultureIgnoreCase)
                   .Select(f => new FileInfo(f))
                   .ToList();
        }

        /// <summary>
        /// Checks for open files on the specified processes and marks them if recognized.
        /// </summary>
        public static void CheckOpenFiles()
        {
            if (!File.Exists(Path.Combine(Signature.FullPath, "handle.exe")))
            {
                return;
            }

            var procs = new List<string>();
            procs.AddRange(Settings.Get<IEnumerable<string>>("Processes to Monitor"));
            procs.AddRange(Utils.GetDefaultVideoPlayers().Select(Path.GetFileName));

            if (procs.Count() == 0)
            {
                return;
            }

            var files = GetHandleList(procs.Distinct(StringComparer.CurrentCultureIgnoreCase));

            foreach (var show in Database.TVShows)
            {
                var titleParts   = Parser.GetRoot(show.Value.Name);
                var releaseParts = !string.IsNullOrWhiteSpace(show.Value.Release) ? show.Value.Release.Split(' ') : null;

                foreach (var file in files)
                {
                    if (Parser.IsMatch(file.DirectoryName + @"\" + file.Name, titleParts) || (releaseParts != null && Parser.IsMatch(file.DirectoryName + @"\" + file.Name, releaseParts)))
                    {
                        var pf = FileNames.Parser.ParseFile(file.Name, file.DirectoryName.Split(Path.DirectorySeparatorChar), false);
                        if (pf.Success && Database.GetReleaseName(show.Value.Name).SequenceEqual(Database.GetReleaseName(pf.Show))) // or the one extracted from the directory name?
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
                                MarkAsSeen(show.Value.ShowID, pf);
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
                var epid = Database.GetEpisodeID(showid, file.Episode.Season, epnr);
                if (epid == int.MinValue)
                {
                    continue;
                }

                if (@new = (Database.Query("select * from tracking where showid = ? and episodeid = ?", showid, epid).Count == 0))
                {
                    Database.Execute("insert into tracking values (?, ?)", showid, epid);

                    Database.Trackings.Add(epid);
                    Database.Episodes.First(e => e.EpisodeID == epid).Watched = true;
                }
            }

            if (@new)
            {
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
            foreach (var engine in typeof(SocialEngine).GetDerivedTypes().Select(type => Activator.CreateInstance(type) as SocialEngine))
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

                if (Settings.Get("Post to " + engine.Name + " only new", true) && (DateTime.Now - file.Airdate).TotalDays > 21)
                {
                    continue;
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
