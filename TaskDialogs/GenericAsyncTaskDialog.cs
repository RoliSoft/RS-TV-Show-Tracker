namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend and runs asynchronously any anonymous method.
    /// </summary>
    public class GenericAsyncTaskDialog
    {
        private string _title, _message;
        private Action _run, _callback, _cancellation;
        private Thread _thd;
        private volatile bool _active;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericAsyncTaskDialog"/> class.
        /// </summary>
        /// <param name="title">The title to display on the taskdialog.</param>
        /// <param name="message">The message to display on the taskdialog.</param>
        /// <param name="run">The method to call.</param>
        /// <param name="callback">The method to call when the first call finished.</param>
        /// <param name="cancellation">The method to call when the first call was cancelled by the user.</param>
        public GenericAsyncTaskDialog(string title, string message, Action run, Action callback = null, Action cancellation = null)
        {
            _title        = title;
            _message      = message;
            _run          = run;
            _callback     = callback;
            _cancellation = cancellation;
        }

        /// <summary>
        /// Calls the underlying method and displays a taskdialog.
        /// </summary>
        public void Run()
        {
            _active = true;
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Working...",
                    MainInstruction         = _title,
                    Content                 = _message,
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
                                if (_active)
                                {
                                    try { _thd.Abort(); } catch { }

                                    if (_cancellation != null)
                                    {
                                        _cancellation();
                                    }
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

            _thd = new Thread(new ThreadStart(_run));
            _thd.Start();

            new Thread(() =>
                {
                    while (_thd.IsAlive)
                    {
                        Thread.Sleep(250);
                    }

                    _active = false;

                    Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                    if (_callback != null)
                    {
                        _callback();
                    }
                }).Start();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }
    }
}
