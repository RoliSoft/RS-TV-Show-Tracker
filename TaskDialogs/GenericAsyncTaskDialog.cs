namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend and runs asynchronously any anonymous method.
    /// </summary>
    public class GenericAsyncTaskDialog
    {
        private TaskDialog _td;
        private Result _res;
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
            _title = title;
            _message = message;
            _run = run;
            _callback = callback;
            _cancellation = cancellation;
        }

        /// <summary>
        /// Calls the underlying method and displays a taskdialog.
        /// </summary>
        public void Run()
        {
            _td = new TaskDialog
                {
                    Title           = "Working...",
                    Instruction     = _title,
                    Content         = _message,
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;
            
            _active = true;
            new Thread(() => { _res = _td.Show().CommonButton; }).Start();

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

                    if (_td != null && _td.IsShowing)
                    {
                        _td.SimulateButtonClick(-1);
                    }

                    if (_res == Result.Cancel)
                    {
                        if (_cancellation != null)
                        {
                            _cancellation();
                        }
                    }
                    else if (_callback != null)
                    {
                        _callback();
                    }
                }).Start();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the Destroyed event of the _td control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TaskDialogDestroyed(object sender, EventArgs e)
        {
            if (_active && (_res == Result.Cancel || (e is ClickEventArgs && (e as ClickEventArgs).ButtonID == 2)))
            {
                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                _active = false;
                _res    = Result.Cancel;

                try { _thd.Abort(); } catch { }
                _thd = null;

                if (_cancellation != null)
                {
                    _cancellation();
                }
            }
        }
    }
}
