namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.FileNames;
    using RoliSoft.TVShowTracker.Parsers.Guides;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>FileSearch</c> class.
    /// </summary>
    public class FileSearchTaskDialog
    {
        private FileSearch _fs;
        private Episode _ep;
        private string _tdstr;
        private volatile bool _active;

        /// <summary>
        /// Searches for the specified show and its episode.
        /// </summary>
        /// <param name="episode">The episode to search for.</param>
        public void Search(Episode episode)
        {
            _ep = episode;

            var paths = Settings.Get<List<string>>("Download Paths");

            if (paths.Count == 0)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Search path not configured",
                        MainInstruction         = "Search path not configured",
                        Content                 = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });
                return;
            }

            _active = true;
            _tdstr = "Searching for the episode...";
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Searching...",
                    MainInstruction         = string.Format("{0} S{1:00}E{2:00}", _ep.Show.Name, _ep.Season, _ep.Number),
                    Content                 = "Searching for the episode...",
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp)
                            {
                                dialog.SetProgressBarMarquee(true, 0);
                                showmbp = true;
                            }

                            dialog.SetContent(_tdstr);

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try { _fs.CancelSearch(); } catch { }
                                }

                                return false;
                            }

                            if (!_active)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();

            _fs = new FileSearch(paths, _ep);

            _fs.FileSearchDone            += FileSearchDone;
            _fs.FileSearchProgressChanged += FileSearchProgressChanged;
            _fs.BeginSearch();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchProgressChanged</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="string"/> instance containing the event data.</param>
        private void FileSearchProgressChanged(object sender, EventArgs<string> e)
        {
            _tdstr = e.Data;
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchDone</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoliSoft.TVShowTracker.EventArgs&lt;System.Collections.Generic.List&lt;System.String&gt;&gt;"/> instance containing the event data.</param>
        private void FileSearchDone(object sender, EventArgs<List<string>> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (e.Data == null)
            {
                return;
            }

            switch (e.Data.Count)
            {
                case 0:
                    TaskDialog.Show(new TaskDialogOptions
                        {
                            MainIcon                = VistaTaskDialogIcon.Error,
                            Title                   = "No files found",
                            MainInstruction         = string.Format("{0} S{1:00}E{2:00}", _ep.Show.Name, _ep.Season, _ep.Number),
                            Content                 = "No files were found for this episode.",
                            ExpandedInfo            = "If you have the episode in the specified download folder but the search fails, it might be because the file name slightly differs.\r\nIf this is the case, click on the 'Guides' tab, select this show, click on the wrench icon and then follow the instructions under the 'Custom release name' section.",
                            AllowDialogCancellation = true,
                            CustomButtons           = new[] { "OK" }
                        });
                    break;

                case 1:
                    if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(e.Data[0]).ToLower()))
                    {
                        new OpenArchiveTaskDialog().OpenArchive(e.Data[0]);
                    }
                    else
                    {
                        Utils.Run(e.Data[0]);
                    }
                    break;

                default:
                    var mfftd = new TaskDialogOptions
                        {
                            Title                   = "Multiple files found",
                            MainInstruction         = string.Format("{0} S{1:00}E{2:00}", _ep.Show.Name, _ep.Season, _ep.Number),
                            Content                 = "Multiple files were found for this episode:",
                            AllowDialogCancellation = true,
                            CommandButtons          = new string[e.Data.Count + 1]
                        };

                    var i = 0;
                    for (; i < e.Data.Count; i++)
                    {
                        var fi      = new FileInfo(e.Data[i]);
                        var quality = Parser.ParseQuality(e.Data[i]);
                        var instr   = fi.Name + "\n";

                        if (quality != Parsers.Downloads.Qualities.Unknown)
                        {
                            instr += quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                        }

                        mfftd.CommandButtons[i] = instr + Utils.GetFileSize(fi.Length) + "\n" + fi.DirectoryName;
                    }

                    mfftd.CommandButtons[i] = "None of the above";

                    var res = TaskDialog.Show(mfftd);

                    if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value < e.Data.Count)
                    {
                        if (OpenArchiveTaskDialog.SupportedArchives.Contains(Path.GetExtension(e.Data[res.CommandButtonResult.Value]).ToLower()))
                        {
                            new OpenArchiveTaskDialog().OpenArchive(e.Data[res.CommandButtonResult.Value]);
                        }
                        else
                        {
                            Utils.Run(e.Data[res.CommandButtonResult.Value]);
                        }
                    }
                    break;
            }
        }
    }
}
