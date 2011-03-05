namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Dialogs;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.FileNames;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>FileSearch</c> class.
    /// </summary>
    public class FileSearchTaskDialog
    {
        private TaskDialog _td;
        private FileSearch _fs;
        private string _show, _episode;
        private volatile bool _active;

        /// <summary>
        /// Searches for the specified show and its episode.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        public void Search(string show, string episode)
        {
            _show    = show;
            _episode = episode;

            var path = Settings.Get("Download Path");

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                var spnctd = new TaskDialog
                    {
                        Icon            = TaskDialogStandardIcon.Error,
                        Caption         = "Search path not configured",
                        InstructionText = "Search path not configured",
                        Text            = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'.",
                        Cancelable      = true
                    };

                spnctd.Show();
                return;
            }

            _td = new TaskDialog();

            _td.Caption         = "Searching...";
            _td.InstructionText = show + " " + episode;
            _td.Text            = "Searching for the episode...";
            _td.StandardButtons = TaskDialogStandardButtons.Cancel;
            _td.Cancelable      = true;
            _td.ProgressBar     = new TaskDialogProgressBar { State = TaskDialogProgressBarState.Marquee };
            _td.Closing        += TaskDialogClosing;

            _active = true;
            new Thread(() =>
                {
                    Thread.Sleep(500);

                    if (_active)
                    {
                        try { _td.Show(); } catch (NullReferenceException) { }
                    }
                }).Start();

            _fs = new FileSearch(path, show, episode);

            _fs.FileSearchDone += FileSearchDone;
            _fs.BeginSearch();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the Closing event of the TaskDialog control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.WindowsAPICodePack.Dialogs.TaskDialogClosingEventArgs"/> instance containing the event data.</param>
        private void TaskDialogClosing(object sender, TaskDialogClosingEventArgs e)
        {
            if (e.TaskDialogResult == TaskDialogResult.Cancel)
            {
                _active = false;

                _fs.CancelSearch();
            }
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchDone</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void FileSearchDone(object sender, EventArgs e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);
            try { _td.Close(TaskDialogResult.Ok); } catch { }

            switch (_fs.Files.Count)
            {
                case 0:
                    var nfftd = new TaskDialog();

                    nfftd.Icon            = TaskDialogStandardIcon.Error;
                    nfftd.Caption         = "No files found";
                    nfftd.InstructionText = _show + " " + _episode;
                    nfftd.Text            = "No files were found for this episode.";
                    nfftd.StandardButtons = TaskDialogStandardButtons.Ok;
                    nfftd.Cancelable      = true;

                    nfftd.Show();
                    break;

                case 1:
                    Utils.Run(_fs.Files[0]);
                    break;

                default:
                    var mfftd = new TaskDialog();

                    mfftd.Caption         = "Multiple files found";
                    mfftd.InstructionText = _show + " " + _episode;
                    mfftd.Text            = "Multiple files were found for this episode:";
                    mfftd.StandardButtons = TaskDialogStandardButtons.Cancel;
                    mfftd.Cancelable      = true;

                    foreach (var file in _fs.Files)
                    {
                        var tmp     = file;
                        var fi      = new FileInfo(file);
                        var quality = Parser.ParseQuality(file);
                        var instr   = string.Empty;

                        if (quality != Parsers.Downloads.Qualities.Unknown)
                        {
                            instr = quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                        }

                        instr += Utils.GetFileSize(fi.Length)
                               + Environment.NewLine
                               + fi.DirectoryName;

                        var fd = new TaskDialogCommandLink
                            {
                                Text        = fi.Name,
                                Instruction = instr
                            };
                        fd.Click += (s, r) =>
                            {
                                try   { mfftd.Close(TaskDialogResult.Ok); }
                                catch { }
                                Utils.Run(tmp);
                            };

                        mfftd.Controls.Add(fd);
                    }

                    mfftd.Show();
                    break;
            }
        }
    }
}
