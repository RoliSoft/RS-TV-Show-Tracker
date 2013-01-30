namespace RoliSoft.TVShowTracker.Parsers.Senders.Engines
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides support for sending files to the Transmission Web UI.
    /// </summary>
    public class TransmissionWebUI : SenderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Transmission Web UI";
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
                return "http://www.transmissionbt.com/";
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
                return Utils.DateTimeToVersion("2013-01-29 9:24 PM");
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
            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/transmission/rpc", "{\"method\":\"torrent-add\",\"arguments\":{\"paused\":\"false\",\"metainfo\":\"" + file + "\"}}", request: r =>
                {
                    r.Credentials = Login;
                    r.Headers["X-Transmission-Session-Id"] = token;
                });

            CheckResponse(req);
        }

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        public override void SendLink(string link)
        {
            var token = GetToken();
            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/transmission/rpc", "{\"method\":\"torrent-add\",\"arguments\":{\"paused\":\"false\",\"filename\":\"" + link + "\"}}", request: r =>
                {
                    r.Credentials = Login;
                    r.Headers["X-Transmission-Session-Id"] = token;
                });

            CheckResponse(req);
        }

        /// <summary>
        /// Gets the X-Transmission-Session-Id from the server.
        /// </summary>
        /// <returns>
        /// X-Transmission-Session-Id for future requests.
        /// </returns>
        private string GetToken()
        {
            var sessid = string.Empty;

            try
            {
                Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/transmission/rpc",
                    request: r => r.Credentials = Login,
                    response: r => sessid = r.GetResponseHeader("X-Transmission-Session-Id"));
            }
            catch (WebException ex)
            {
                sessid = ex.Response.Headers["X-Transmission-Session-Id"];
            }

            if (string.IsNullOrWhiteSpace(sessid))
            {
                throw new Exception("Server didn't send X-Transmission-Session-Id.");
            }

            return sessid;
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

            var err = Regex.Match(resp, @"""result"":\s*""(.*?)""");
            if (err.Success && err.Groups[1].Value != "success")
            {
                throw new Exception("Error received from the server: " + err.Groups[1].Value);
            }
        }
    }
}
