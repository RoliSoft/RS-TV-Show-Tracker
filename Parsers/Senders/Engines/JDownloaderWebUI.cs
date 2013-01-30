namespace RoliSoft.TVShowTracker.Parsers.Senders.Engines
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides support for sending files to the JDownloader Web UI.
    /// </summary>
    public class JDownloaderWebUI : SenderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "JDownloader Web UI";
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
                return "http://jdownloader.org/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/jdownloader.png";
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
                return Utils.DateTimeToVersion("2013-01-30 0:52 AM");
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
        public override void SendFile(string path)
        {
            throw new NotSupportedException("Files cannot be sent to this type.");
        }

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        public override void SendLink(string link)
        {
            Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/link_adder.tmpl", "do=Add&addlinks=" + Utils.EncodeURL(link.Replace("\0", "\r\n")), request: r => r.Credentials = Login);

            Thread.Sleep(100);

            var check = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/link_adder.tmpl", request: r => r.Credentials = Login);

            if (!check.Contains("LinkGrabber still Running!") && !check.Contains("value=\"Unchecked\""))
            {
                return;
            }

            var done = false;

            for (var i = 0; !done && i < 120; i++)
            {
                Thread.Sleep(250);
                check = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/link_adder.tmpl", request: r => r.Credentials = Login);
                done = !check.Contains("LinkGrabber still Running!") && !check.Contains("value=\"Unchecked\"");
            }

            if (!done)
            {
                return;
            }

            var post = "do=Submit&checkallbox=on&selected_dowhat_link_adder=add&"
                     + Regex.Matches(check, @"(?<key>adder_package_name_(?<num>\d))"" value=""(?<value>[^""]+)""").Cast<Match>().Aggregate(string.Empty, (c, m) => c + ("package_all_add=" + m.Groups["num"].Value + "&" + m.Groups["key"].Value + "=" + m.Groups["value"].Value + "&"))
                     + Regex.Matches(check, @"package_single_add"" value=""([^""]+)""").Cast<Match>().Aggregate(string.Empty, (c, m) => c + "package_single_add=" + m.Groups[1].Value.Replace(" ", "+") + "&");
            
            Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/index.tmpl", post, request: r => r.Credentials = Login);
            Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/index.tmpl", "do=start&autoreconnect=on&selected_dowhat_index=activate", request: r => r.Credentials = Login);
        }
    }
}
