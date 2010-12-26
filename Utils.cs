namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>
        /// Gets the Unix epoch date. (1970-01-01 00:00:00)
        /// </summary>
        /// <value>The Unix epoch.</value>
        public static DateTime UnixEpoch
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Appends a unit to a number and makes it plural if the number is not 1.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>Formatted number.</returns>
        public static string FormatNumber(int number, string unit)
        {
            return number + " " + unit + (number != 1 ? "s" : string.Empty);
        }

        /// <summary>
        /// Runs the specified process.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="arguments">The arguments.</param>
        public static void Run(string process, string arguments = null)
        {
            try { Process.Start(process, arguments); } catch { }
        }

        /// <summary>
        /// Runs the specified process, waits until it finishes and returns the console content.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>Console output.</returns>
        public static string RunAndRead(string process, string arguments = null)
        {
            var sb = new StringBuilder();
            var p  = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo           =
                        {
                            FileName               = process,
                            Arguments              = arguments,
                            UseShellExecute        = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            CreateNoWindow         = true
                        }
                };

            p.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sb.AppendLine(e.Data);
                    }
                };
            p.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sb.AppendLine(e.Data);
                    }
                };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            return sb.ToString();
        }

        /// <summary>
        /// Googles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        [Obsolete("Google is continuously changing their APIs and putting limits on them. The AJAX API used here is deprecated since November 1st, 2010, however it will still work for a limited time. Use Bing instead.")]
        public static string Google(string query)
        {
            var search = GetURL("http://www.google.com/uds/GwebSearch?callback=google.search.WebSearch.RawCompletion&context=0&lstkp=0&rsz=small&hl=en&source=gsc&gss=.com&sig=22c4e39868158a22aac047a2c138a780&q=" + Uri.EscapeUriString(query) + "&gl=www.google.com&qid=12a9cb9d0a6870d28&key=AIzaSyA5m1Nc8ws2BbmPRwKu5gFradvD_hgq6G0&v=1.0");
            var json   = JObject.Parse(search.Remove(0, "google.search.WebSearch.RawCompletion('0',".Length));

            return json["results"].HasValues
                   ? json["results"][0]["unescapedUrl"].Value<string>()
                   : string.Empty;
        }

        /// <summary>
        /// Bings (...it just doesn't sound right...) the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>First link on the search result or empty string.</returns>
        public static string Bing(string query)
        {
            var search = GetURL("http://api.bing.net/json.aspx?AppId=072CCFDBC52FB4552FF96CE87A95F8E9DE30C37B&Query=" + Uri.EscapeUriString(query) + "&Sources=Web&Version=2.0&Market=en-us&Adult=Off&Web.Count=1&Web.Offset=0&Web.Options=DisableHostCollapsing+DisableQueryAlterations");
            var json   = JObject.Parse(search);

            return json["SearchResponse"]["Web"]["Total"].Value<int>() != 0
                   ? json["SearchResponse"]["Web"]["Results"][0]["Url"].Value<string>()
                   : string.Empty;
        }

        /// <summary>
        /// Downloads the specified URL and parses it with HtmlAgilityPack.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <returns>Remote page's parsed content.</returns>
        public static HtmlDocument GetHTML(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(
                GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent)
            );

            return doc;
        }

        /// <summary>
        /// Downloads the specified URL into a string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <returns>Remote page's content.</returns>
        public static string GetURL(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null)
        {
            ServicePointManager.Expect100Continue = false;

            var req       = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout   = 10000;
            req.UserAgent = userAgent ?? "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.7.39 Version/11.00";

            if (!string.IsNullOrWhiteSpace(postData))
            {
                req.Method        = "POST";
                req.ContentType   = "application/x-www-form-urlencoded";
                req.ContentLength = encoding != null
                                    ? encoding.GetByteCount(postData)
                                    : Encoding.ASCII.GetByteCount(postData);
            }

            if (!string.IsNullOrWhiteSpace(cookies))
            {
                req.CookieContainer = new CookieContainer();

                foreach (var kv in Regex.Replace(cookies.TrimEnd(';'), @";\s*", ";")
                                   .Split(';')
                                   .Where(cookie => cookie != null)
                                   .Select(cookie => cookie.Split('=')))
                {
                    req.CookieContainer.Add(new Cookie(kv[0], kv[1], "/", new Uri(url).Host));
                }
            }

            if (!string.IsNullOrWhiteSpace(postData))
            {
                using (var sw = new StreamWriter(req.GetRequestStream(), encoding ?? Encoding.ASCII) { AutoFlush = true })
                {
                    sw.Write(postData);
                }
            }

            if (!autoDetectEncoding)
            {
                using (var sr = new StreamReader(req.GetResponse().GetResponseStream(), encoding ?? Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
            else
            {
                var rs = req.GetResponse().GetResponseStream();
                var ms = new MemoryStream();
                byte[] bs;

                int read;
                do
                {
                    bs = new byte[8192];
                    read = rs.Read(bs, 0, bs.Length);
                    ms.Write(bs, 0, read);
                } while (read > 0);

                bs = ms.ToArray();

                var rgx = Regex.Match(Encoding.ASCII.GetString(bs), @"charset=([^""]+)", RegexOptions.IgnoreCase);
                var eenc = "utf-8";

                if (rgx.Success)
                {
                    eenc = rgx.Groups[1].Value;

                    if (eenc == "iso-8859-1") // .NET won't recognize iso-8859-1
                    {
                        eenc = "windows-1252";
                    }
                }

                return Encoding.GetEncoding(eenc).GetString(bs);
            }
        }

        /// <summary>
        /// Modify the specified URL to go through Coral CDN.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static string Coralify(string url)
        {
            return Regex.Replace(url, @"(/{2}[^/]+)/", @"$1.nyud.net/");
        }

        /// <summary>
        /// Gets the unique user identifier or generates one if absent.
        /// </summary>
        /// <returns>Unique ID.</returns>
        public static string GetUUID()
        {
            if (string.IsNullOrWhiteSpace(Signature.FullPath))
            {
                return string.Empty;
            }

            var uid = Database.Setting("uid");

            if (string.IsNullOrWhiteSpace(uid))
            {
                uid = Guid.NewGuid().ToString();
                Database.Setting("uid", uid);
            }

            return uid;
        }

        /// <summary>
        /// Gets the full path to a random file.
        /// </summary>
        /// <param name="extension">The extension of the file.</param>
        /// <returns>Full path to random file.</returns>
        public static string GetRandomFileName(string extension = null)
        {
            return Path.GetTempPath() + Path.PathSeparator + Path.GetRandomFileName() + (extension != null ? "." + extension : string.Empty);
        } 
        
        /// <summary>
        /// Gets the size of the file in human-readable format.
        /// </summary>
        /// <param name="bytes">The size.</param>
        /// <returns>Transformed file size.</returns>
        public static string GetFileSize(long bytes)
        {
            var size = "0 bytes";

            if (bytes >= 1073741824.0)
            {
                size = String.Format("{0:0.00}", bytes / 1073741824.0) + " GB";
            }
            else if (bytes >= 1048576.0)
            {
                size = String.Format("{0:0.00}", bytes / 1048576.0) + " MB";
            }
            else if (bytes >= 1024.0)
            {
                size = String.Format("{0:0.00}", bytes / 1024.0) + " kB";
            }
            else if (bytes > 0 && bytes < 1024.0)
            {
                size = bytes + " bytes";
            }

            return size;
        }
    }
}