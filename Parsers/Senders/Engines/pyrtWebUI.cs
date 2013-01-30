namespace RoliSoft.TVShowTracker.Parsers.Senders.Engines
{
    using System;
    using System.IO;
    using System.Net;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides support for sending files to the pyrt Web UI.
    /// </summary>
    public class pyrtWebUI : SenderEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "pyrt Web UI";
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
                return "https://github.com/mountainpenguin/pyrt";
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
                return Utils.DateTimeToVersion("2013-01-29 11:36 PM");
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
            var cookies = string.Empty;
            var login = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/", "password=" + Utils.EncodeURL(Login.Password), response: r => cookies = Utils.EatCookieCollection(r.Cookies));

            if (login.Contains("Incorrect Password"))
            {
                throw new Exception("Unable to login with the specified credentials.");
            }

            byte[] data;

            using (var fs = File.OpenRead(path))
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            {
                sw.WriteLine("--AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e");
                sw.WriteLine("Content-Disposition: form-data; name=\"request\"");
                sw.WriteLine();
                sw.WriteLine("upload_torrent");
                sw.WriteLine("--AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e");
                sw.WriteLine("Content-Disposition: form-data; name=\"start\"");
                sw.WriteLine();
                sw.WriteLine("on");
                sw.WriteLine();
                sw.WriteLine("--AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e");
                sw.WriteLine("Content-Disposition: form-data; name=\"torrent\"; filename=\"" + Path.GetFileNameWithoutExtension(path) + ".torrent\"");
                sw.WriteLine("Content-Type: application/x-bittorrent");
                sw.WriteLine();
                sw.Flush();
                fs.CopyTo(ms);
                sw.WriteLine();
                sw.WriteLine("--AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e--");
                sw.Flush();

                data = ms.ToArray();
            }

            var req = Utils.GetURL(Location.TrimEnd("/".ToCharArray()) + "/ajax", data, cookies, request: r =>
                {
                    r.Credentials = Login;
                    r.ContentType = "multipart/form-data; boundary=AJAX-----------------------d41d8cd98f00b204e9800998ecf8427e";
                });

            if (!req.Contains("Redirect"))
            {
                throw new Exception("There was an error while sending the torrent.");
            }
        }

        /// <summary>
        /// Sends the specified link.
        /// </summary>
        /// <param name="link">The link to send.</param>
        public override void SendLink(string link)
        {
            throw new NotSupportedException("Magnet links are not supported as this is an antiquated UI.");
        }
    }
}
