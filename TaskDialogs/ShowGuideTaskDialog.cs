namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using RoliSoft.TVShowTracker.Parsers.Guides;

    using TaskDialogInterop;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to editing TV shows.
    /// </summary>
    public class ShowGuideTaskDialog
    {
        private Thread _thd;
        private Guide _g;
        private List<ShowID> _ids;
        private bool _cancel;

        /// <summary>
        /// Searches for the specified show on the specified guide.
        /// </summary>
        /// <param name="guide">The guide.</param>
        /// <param name="show">The show.</param>
        /// <param name="lang">The language.</param>
        /// <param name="done">The method to call when finished.</param>
        public void Search(Guide guide, string show, string lang, Action<string, string, string> done)
        {
            _g = guide;

            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Searching...",
                    MainInstruction         = show.ToUppercaseWords(),
                    Content                 = "Searching on " + guide.Name + "...",
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

                            if (args.ButtonId != 0)
                            {
                                if (_thd != null && _thd.IsAlive && !_cancel)
                                {
                                    _thd.Abort();
                                }

                                return false;
                            }

                            if (_cancel)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();

            _thd = new Thread(() =>
                {
                    try
                    {
                        _ids = _g.GetID(show, lang).ToList();
                    }
                    catch (Exception ex)
                    {
                        _cancel = true;

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Error,
                                Title           = "Search error",
                                MainInstruction = show.ToUppercaseWords(),
                                Content         = "Error while searching on " + _g.Name + ".",
                                ExpandedInfo    = ex.Message,
                                CustomButtons   = new[] { "OK" }
                            });

                        done(null, null, null);
                        return;
                    }

                    _cancel = true;

                    if (_ids.Count == 0)
                    {
                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Warning,
                                Title           = "Search result",
                                MainInstruction = show.ToUppercaseWords(),
                                Content         = "Couldn't find the specified show on " + _g.Name + ".",
                                CustomButtons   = new[] { "OK" }
                            });

                        done(null, null, null);
                        return;
                    }

                    if (_ids.Count == 1)
                    {
                        done(_ids[0].ID, _ids[0].Title, _ids[0].Language);
                        return;
                    }

                    if (_ids.Count > 1)
                    {
                        if (_ids.Count > 5)
                        {
                            _ids = _ids.Take(5).ToList();
                        }

                        var td = new TaskDialogOptions
                            {
                                Title           = "Search result",
                                MainInstruction = show.ToUppercaseWords(),
                                Content         = "More than one show matched the search criteria on " + _g.Name + ":",
                                CommandButtons  = new string[_ids.Count + 1]
                            };
                        
                        var i = 0;
                        for (; i < _ids.Count; i++)
                        {
                            td.CommandButtons[i] = _ids[i].Title;
                        }

                        td.CommandButtons[i] = "None of the above";

                        var res = TaskDialog.Show(td);

                        if (res.CommandButtonResult.HasValue && res.CommandButtonResult.Value < _ids.Count)
                        {
                            done(_ids[res.CommandButtonResult.Value].ID, _ids[res.CommandButtonResult.Value].Title, _ids[res.CommandButtonResult.Value].Language);
                        }
                        else
                        {
                            done(null, null, null);
                        }
                    }
                });
            _thd.SetApartmentState(ApartmentState.STA);
            _thd.Start();
        }
    }
}
