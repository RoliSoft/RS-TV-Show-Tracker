namespace RoliSoft.TVShowTracker
{
    using System.IO;

    /// <summary>
    /// Represents a file on the list view.
    /// </summary>
    public class LogListViewItem
    {
        /// <summary>
        /// Gets or sets the original log entry.
        /// </summary>
        /// <value>The original log entry.</value>
        public Log.LogItem Item { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        /// <value>The time.</value>
        public string Time { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public string Location { get; set; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message
        {
            get
            {
                return Item.Message;
            }
        }

        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <value>The level.</value>
        public string Level
        {
            get
            {
                return Item.Level.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogListViewItem" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        public LogListViewItem(Log.LogItem item)
        {
            Item     = item;
            Time     = item.Time.ToString("HH:mm:ss");
            Location = Path.GetFileName(item.File) + "/" + item.Method + "():" + item.Line;

            switch (item.Level)
            {
                default:
                case Log.Level.None:  Icon = "/RSTVShowTracker;component/Images/unchecked.png";         break;
                case Log.Level.Trace: Icon = "/RSTVShowTracker;component/Images/bug.png";               break;
                case Log.Level.Debug: Icon = "/RSTVShowTracker;component/Images/information-white.png"; break;
                case Log.Level.Info:  Icon = "/RSTVShowTracker;component/Images/information.png";       break;
                case Log.Level.Warn:  Icon = "/RSTVShowTracker;component/Images/exclamation.png";       break;
                case Log.Level.Error: Icon = "/RSTVShowTracker;component/Images/exclamation-red.png";   break;
                case Log.Level.Fatal: Icon = "/RSTVShowTracker;component/Images/fire.png";              break;
            }
        }
    }
}
