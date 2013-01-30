namespace RoliSoft.TVShowTracker.Parsers.Senders.Engines
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides support for sending files to the Deluge Web UI.
    /// </summary>
    public class DelugeWebUI : SenderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Deluge Web UI";
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
                return "http://deluge-torrent.org/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/deluge.png";
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
                return Utils.DateTimeToVersion("2013-01-29 10:41 PM");
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
                return Types.Torrent;
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
            var file = Convert.ToBase64String(File.ReadAllBytes(path));
            var token = GetToken();
            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/json", "{\"id\":" + token.Item1 + ",\"method\":\"core.add_torrent_file\",\"params\":[\"" + Path.GetFileNameWithoutExtension(path) + ".torrent\",\"" + file + "\",{}]}", token.Item2);

            CheckResponse(req);
        }

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        public override void SendLink(string link)
        {
            var token = GetToken();
            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/json", "{\"id\":" + token.Item1 + ",\"method\":\"core.add_torrent_magnet\",\"params\":[\"" + link + "\",{}]}", token.Item2);

            CheckResponse(req);
        }

        /// <summary>
        /// Gets the _session_id cookie from the server.
        /// </summary>
        /// <returns>
        /// _session_id cookie for future requests.
        /// </returns>
        private Tuple<int, string> GetToken()
        {
            var id = Utils.Rand.Next(1000, 999999);
            var cookies = string.Empty;

            Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/json", "{\"id\":" + id + ",\"method\":\"auth.login\",\"params\":[\"" + Login.Password + "\"]}",
                response: r => cookies = Utils.EatCookieCollection(r.Cookies));

            if (string.IsNullOrWhiteSpace(cookies))
            {
                throw new Exception("Server didn't send _session_id.");
            }

            return new Tuple<int, string>(id, cookies);
        }

        /// <summary>
        /// Checks the server's response.
        /// </summary>
        /// <param name="resp">The response.</param>
        /// <exception cref="System.Exception">Invalid response received from the server.</exception>
        private void CheckResponse(string resp)
        {
            if (!resp.Contains("\"result\":"))
            {
                throw new Exception("Invalid response received from the server.");
            }

            var err = Regex.Match(resp, @"""error"":\s*""?(.*?)[""}]");
            if (err.Success && err.Groups[1].Value != "null")
            {
                throw new Exception("Error received from the server: " + err.Groups[1].Value);
            }
        }
    }
}
