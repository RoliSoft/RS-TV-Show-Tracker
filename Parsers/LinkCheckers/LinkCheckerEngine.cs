namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers
{
    using System;
    using System.Threading;

    using NUnit.Framework;

    /// <summary>
    /// Represents a link checker engine.
    /// </summary>
    public abstract class LinkCheckerEngine : ParserEngine
    {
        /// <summary>
        /// Occurs when a link check is done.
        /// </summary>
        public event EventHandler<EventArgs<bool>> LinkCheckerDone;

        /// <summary>
        /// Occurs when a link check has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception>> LinkCheckerError;

        /// <summary>
        /// Checks the availability of the link on the service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if the link is available; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool Check(string url);

        /// <summary>
        /// Determines whether this instance can check the availability of the link on the specified service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if this instance can check the specified service; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanCheck(string url);

        private Thread _job;

        /// <summary>
        /// Checks the availability of the link on the service asynchronously.
        /// </summary>
        /// <param name="url">The link to check.</param>
        public void CheckAsync(string url)
        {
            CancelAsync();

            _job = new Thread(() =>
                {
                    try
                    {
                        var avail = Check(url);
                        LinkCheckerDone.Fire(this, avail);
                    }
                    catch (Exception ex)
                    {
                        LinkCheckerError.Fire(this, "There was an error while checking the link for availability.", ex);
                    }
                });
            _job.Start();
        }

        /// <summary>
        /// Cancels the active asynchronous check.
        /// </summary>
        public void CancelAsync()
        {
            if (_job != null)
            {
                _job.Abort();
                _job = null;
            }
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public abstract void Test();
    }
}
