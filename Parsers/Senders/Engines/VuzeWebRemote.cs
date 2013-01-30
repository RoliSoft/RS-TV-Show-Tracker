namespace RoliSoft.TVShowTracker.Parsers.Senders.Engines
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides support for sending files to the Vuze Web Remote.
    /// </summary>
    public class VuzeWebRemote : SenderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Vuze Web Remote";
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
                return "http://www.vuze.com/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/vuze.png";
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
                return Utils.DateTimeToVersion("2013-01-29 8:28 PM");
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
            byte[] data;

            using (var fs = File.OpenRead(path))
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            {
                sw.WriteLine("--AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e");
                sw.WriteLine("Content-Disposition: form-data; name=\"torrent_files[]\"; filename=\"file.torrent\"");
                sw.WriteLine("Content-Type: application/x-bittorrent");
                sw.WriteLine();
                sw.Flush();
                fs.CopyTo(ms);
                sw.WriteLine();
                sw.WriteLine("--AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e--");
                sw.Flush();

                data = ms.ToArray();
            }

            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/transmission/upload?paused=false", data, request: r =>
                {
                    r.Credentials = Login;
                    r.ContentType = "multipart/form-data; boundary=AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e";
                });

            CheckResponse(req);
        }

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        public override void SendLink(string link)
        {
            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/transmission/rpc", "{\"method\":\"torrent-add\",\"arguments\":{\"paused\":\"false\",\"filename\":\"" + link + "\"}}", request: r => r.Credentials = Login);

            CheckResponse(req);
        }

        /// <summary>
        /// Checks the server's response.
        /// </summary>
        /// <param name="resp">The response.</param>
        /// <exception cref="System.Exception">Invalid response received from the server.</exception>
        private void CheckResponse(string resp)
        {
            if (!Regex.IsMatch(resp, @"(?:""result"":|<h1>200: OK</h1>)"))
            {
                throw new Exception("Invalid response received from the server.");
            }

            var err = Regex.Match(resp, @"""result"":\s*""error:\s*(.*?)""");
            if (err.Success)
            {
                throw new Exception("Error received from the server: " + err.Groups[1].Value);
            }
        }
    }
}
