namespace RoliSoft.TVShowTracker.Parsers.Senders.Engines
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides support for sending files to the Mipony Web UI.
    /// </summary>
    public class MiponyWebUI : SenderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Mipony Web UI";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://www.mipony.net/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "pack://application:,,,/RSTVShowTracker;component/Images/mipony.png";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2013-01-30 0:43 AM");
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.DirectHTTP;
            }
        }

        /// <summary>
        /// Gets or sets the name of the sender.
        /// </summary>
        /// <value>The name of the sender.</value>
        public override string Title { get; set; }

        /// <summary>
        /// Gets or sets the location of the receiving API.
        /// </summary>
        /// <value>The location of the receiving API.</value>
        public override string Location { get; set; }

        /// <summary>
        /// Gets or sets the login credentials.
        /// </summary>
        /// <value>The login credentials.</value>
        public override NetworkCredential Login { get; set; }

        /// <summary>
        /// Sends the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="status">The callback to report status to.</param>
        public override void SendFile(string path, Action<string> status = null)
        {
            throw new NotSupportedException("Files cannot be sent to this type.");
        }

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        /// <param name="status">The callback to report status to.</param>
        public override void SendLink(string link, Action<string> status = null)
        {
            if (status != null)
            {
                status("Checking status of " + Title + "...");
            }

            var init = Utils.GetURL(Location);

            if (init.Contains("frmLogin"))
            {
                if (status != null)
                {
                    status("Logging in to " + Title + "...");
                }

                Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/Login.asp", "Password=" + Utils.EncodeURL(Login.Password) + "&button=OK");
            }

            if (status != null)
            {
                status("Sending links to linkgrabber in " + Title + "...");
            }

            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/addlinks.asp", "textLinks=" + Utils.EncodeURL(link.Replace("\0", "\r\n")) + "&op=addLinks");
            var mc = Regex.Matches(req, @"name=[""']file_(\d+)");

            if (mc.Count == 0)
            {
                throw new Exception("The linkgrabber is empty or all the links were offline/unrecognized.");
            }

            if (status != null)
            {
                status("Accepting links from the linkgrabber in " + Title + "...");
            }

            var post = mc.Cast<Match>().Aggregate(string.Empty, (c, m) => c + ("file_" + m.Groups[1].Value + "=on&")) + "op=downloadLinks";
            Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/downloads.asp", post);
        }
    }
}
