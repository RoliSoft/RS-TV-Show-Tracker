namespace RoliSoft.TVShowTracker.Parsers.Senders
{
    using System.Net;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Represents a sender engine.
    /// </summary>
    public abstract class SenderEngine : ParserEngine
    {
        /// <summary>
        /// Gets or sets the name of the sender.
        /// </summary>
        /// <value>The name of the sender.</value>
        public abstract string Title { get; set; }

        /// <summary>
        /// Gets or sets the location of the receiving API.
        /// </summary>
        /// <value>The location of the receiving API.</value>
        public abstract string Location { get; set; }

        /// <summary>
        /// Gets or sets the login credentials.
        /// </summary>
        /// <value>The login credentials.</value>
        public abstract NetworkCredential Login { get; set; }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public abstract Types Type { get; }

        /// <summary>
        /// Sends the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public abstract void SendFile(string path);

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        public abstract void SendLink(string link);
    }
}
