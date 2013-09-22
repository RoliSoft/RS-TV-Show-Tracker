namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using RoliSoft.TVShowTracker.FileNames;
    using RoliSoft.TVShowTracker.Parsers.Social;
    using RoliSoft.TVShowTracker.Dependencies.DetectOpenFiles;
    using RoliSoft.TVShowTracker.Parsers.Guides;

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
            Func<List<int>, IEnumerable<string>> getFilesLockedBy;

            var method = Settings.Get("Process Monitoring Method", "Internal");

            switch (method)
            {
                default:
                case "Internal":
                    Log.Trace("Using internal unsafe NtQuerySystemInformation()-based implementation as the process monitoring method.");
                    getFilesLockedBy = DetectOpenFiles.UnsafeGetFilesLockedBy; 
                   break;

                case "Sysinternals":
                   Log.Trace("Using third-party application Sysinternals Handle as the process monitoring method.");
                    getFilesLockedBy = DetectOpenFiles.SysinternalsGetFilesLockedBy;
                    break;

                case "NirSoft":
                    Log.Trace("Using third-party application NirSoft OpenedFilesView as the process monitoring method.");
                    getFilesLockedBy = DetectOpenFiles.NirSoftGetFilesLockedBy;
                    break;
            }

            foreach (var file in getFilesLockedBy(pids))
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

                if (fi.Exists)
                {
                    yield return fi;
                }
            }
        }

        /// <summary>
        /// Gets the list of window titles for the given process IDs.
        /// </summary>
        /// <param name="pids">The process IDs.</param>
        /// <returns>List of the window titles.</returns>
        public static IEnumerable<string> GetWindownTitles(List<int> pids)
        {
            foreach (var proc in Process.GetProcesses())
            {
                if (pids.Contains(proc.Id))
                {
                    var title = default(string);

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                        {
                            title = proc.MainWindowTitle;
                        }
                        else
                        {
                            Log.Debug("Process " + proc.ProcessName + " (" + proc.Id + ") has an empty window title.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Error while getting window title for " + proc.ProcessName + " (" + proc.Id + ")", ex);
                    }

                    if (title != null)
                    {
                        yield return title;
                    }
                }
            }
        }

        /// <summary>
        /// Checks for open files on the specified processes and marks them if recognized.
        /// </summary>
        public static void CheckOpenFiles()
        {
            if (!Settings.Get("Monitor Processes", true))
            {
                return;
            }

            Log.Debug("Checking for open files...");

            var alt    = Settings.Get("Process Monitoring Method", "Internal") == "WindowTitle";
            var netmon = Settings.Get<bool>("Monitor Network Shares");

            if (!alt && !Utils.IsAdmin)
            {
                Log.Info("You are not running with administrator rights but requested file handle monitoring.");
            }

            var procs = new List<string>();
            procs.AddRange(Settings.Get<List<string>>("Processes to Monitor"));

            try
            {
                procs.AddRange(Utils.GetDefaultVideoPlayers().Select(Path.GetFileName));
            }
            catch (Exception ex)
            {
                Log.Warn("Error while getting list of default video players.", ex);
            }

            if (Log.IsTraceEnabled) Log.Trace("Requested processes:", procs);

            if (!procs.Any() && !netmon && !UPnP.IsRunning)
            {
                Log.Debug("No processes specified to monitor and network share monitoring is disabled.");
                return;
            }

            var pids = GetPIDs(procs).Distinct().ToList();

            if (Log.IsTraceEnabled) Log.Trace("Running PIDs:", pids);

            if (!pids.Any() && !netmon && !UPnP.IsRunning)
            {
                Log.Debug("Unable to get at least one PID for the specified processes and network share monitoring is disabled.");
                return;
            }

            var files  = new List<FileInfo>();
            var titles = new List<string>();

            if (pids.Any())
            {
                if (!alt)
                {
                    using (var mre = new ManualResetEvent(false))
                    {
                        var thd = new Thread(() =>
                            {
                                try
                                {
                                    files.AddRange(GetHandleList(pids));
                                }
                                finally
                                {
                                    mre.Set();
                                }
                            })
                            {
                                IsBackground = true
                            };

                        thd.Start();

                        if (!mre.WaitOne(TimeSpan.FromMinutes(3)))
                        {
                            Log.Error("Process monitoring method timed out after 3 minutes. Please consider switching implementations from the Settings menu.");

                            try
                            {
                                thd.Interrupt();
                                thd.Abort();
                            } catch { }
                        }
                    }

                    if (files.Count != 0)
                    {
                        Log.Debug(Utils.FormatNumber(files.Count, "file handle", true) + " open for the PID" + (pids.Count != 1 ? "s" : string.Empty) + ": " + string.Join(", ", pids).TrimEnd(", ".ToCharArray()) + ".");
                    }
                }
                else
                {
                    titles.AddRange(GetWindownTitles(pids));

                    if (titles.Count != 0)
                    {
                        Log.Debug(Utils.FormatNumber(files.Count, "window title", true) + " found for the PID" + (pids.Count != 1 ? "s" : string.Empty) + ": " + string.Join(", ", pids).TrimEnd(", ".ToCharArray()) + ".");
                    }

                    if (Log.IsTraceEnabled) Log.Trace("Window titles:", titles);
                }
            }

            if (Signature.IsActivated && UPnP.IsRunning)
            {
                try
                {
                    var cnt = files.Count;

                    files.AddRange(UPnP.GetActiveTransfers());

                    if (files.Count != cnt)
                    {
                        Log.Debug(Utils.FormatNumber(files.Count - cnt, "file handle", true) + " open through the UPnP/DLNA server.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Cannot get active transfer list from the UPnP/DLNA server due to an exception.", ex);
                }
            }

            if (Signature.IsActivated && netmon)
            {
                try
                {
                    var cnt = files.Count;

                    files.AddRange(NetworkShares.GetActiveTransfers());

                    if (files.Count != cnt)
                    {
                        Log.Debug(Utils.FormatNumber(files.Count - cnt, "file handle", true) + " open through Windows file sharing.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Cannot get active network share transfer list from Windows due to an exception.", ex);
                }
            }

            if (Log.IsTraceEnabled) Log.Trace("Open file handles:", files.Select(fi => fi.FullName));

            if (files.Count == 0 && titles.Count == 0)
            {
                Log.Debug("No " + (alt ? "window titles" : "file handles open") + " for the specified processes (" + (pids.Count == 0 ? "none running" : "PID" + (pids.Count != 1 ? "s" : string.Empty) + ": " + string.Join(", ", pids).TrimEnd(", ".ToCharArray())) + ") and services.");
                return;
            }

            foreach (var show in Database.TVShows)
            {
                var titleRegex   = Parser.GenerateTitleRegex(show.Value.Title, show.Value);
                var releaseExpr  = string.Empty;
                var releaseRegex = show.Value.Data.TryGetValue("regex", out releaseExpr) && !string.IsNullOrWhiteSpace(releaseExpr) ? new Regex(releaseExpr) : null;

                foreach (var file in files)
                {
                    if (Parser.IsMatch(file.DirectoryName + @"\" + file.Name, titleRegex) || (releaseRegex != null && Parser.IsMatch(file.DirectoryName + @"\" + file.Name, releaseRegex)))
                    {
                        var pf = FileNames.Parser.ParseFile(file.Name, file.DirectoryName.Split(Path.DirectorySeparatorChar), false);
                        if (pf.Success && show.Value.Title == pf.Show)
                        {
                            Log.Debug("Identified open file " + file.Name + " as " + pf + ".");

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

                foreach (var title in titles)
                {
                    if (Parser.IsMatch(title, titleRegex, null, false) || (releaseRegex != null && Parser.IsMatch(title, releaseRegex, null, false)))
                    {
                        var pf = FileNames.Parser.ParseFile(title, null, false);
                        if (pf.Success && show.Value.Title == pf.Show)
                        {
                            Log.Debug("Identified window title " + title + " as " + pf + ".");

                            if (!OpenFiles.Contains(pf.ToString()))
                            {
                                OpenFiles.Add(pf.ToString());
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
                Log.Debug("Marking " + file + " as seen.");
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
                Log.Debug("Not posting " + file + " to social networks because it is not a recent episode.");
                return;
            }

            var listed = Settings.Get("Post restrictions list", new List<int>())
                                 .Contains(Database.TVShows.Values.First(x => x.Title == file.Show).ID);

            switch (Settings.Get("Post restrictions list type", "black"))
            {
                case "black":
                    if (listed)
                    {
                        Log.Debug("Not posting " + file + " to social networks because the show is blacklisted.");
                        return;
                    }
                    break;

                case "white":
                    if (!listed)
                    {
                        Log.Debug("Not posting " + file + " to social networks because the show is not whitelisted.");
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
                        Log.Debug("Not posting " + file + " to " + engine.Name + " because it required OAuth tokens are missing.");
                        continue;
                    }
                }


                var format = Settings.Get(engine.Name + " Status Format", engine.DefaultStatusFormat);
                if (string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                try
                {
                    engine.PostMessage(FileNames.Parser.FormatFileName(format, file));
                    Log.Debug("Successfully posted " + file + " to " + engine.Name + ".");
                }
                catch (Exception ex)
                {
                    Log.Warn("Unhandled exception while posting " + file + " to " + engine.Name + ".", ex);
                }
            }
        }
    }
}
