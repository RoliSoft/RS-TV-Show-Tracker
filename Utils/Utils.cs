namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Compat.Web;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;
    using System.Xml;
    using System.Xml.Linq;

    using HtmlAgilityPack;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using Newtonsoft.Json;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>
        /// Gets or sets a fast pseudo-random number generator.
        /// </summary>
        /// <value>The fast pseudo-random number generator.</value>
        public static Random Rand { get; set; }

        /// <summary>
        /// Gets or sets a cryptographically strong pseudo-random number generator.
        /// </summary>
        /// <value>The cryptographically strong pseudo-random number generator.</value>
        public static RNGCryptoServiceProvider CryptoRand { get; set; }

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
        /// Gets a value indicating whether the operating system is Windows 7 or newer.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the OS is Windows 7 or newer; otherwise, <c>false</c>.
        /// </value>
        public static bool Is7
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                     ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1) ||
                       Environment.OSVersion.Version.Major >= 6);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the software has administrator rights.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the software has administrator rights; otherwise, <c>false</c>.
        /// </value>
        public static bool IsAdmin
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Gets the name of the operating system.
        /// </summary>
        /// <value>The OS.</value>
        public static string OS
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32S:
                        return "Windows 3.1";

                    case PlatformID.Win32Windows:
                        switch (Environment.OSVersion.Version.Minor)
                        {
                            case 0:
                                return "Windows 95";

                            case 10:
                                return Environment.OSVersion.Version.Revision.ToString() == "2222A"
                                       ? "Windows 98 Second Edition"
                                       : "Windows 98";

                            case 90:
                                return "Windows ME";
                        }
                        break;

                    case PlatformID.Win32NT:
                        switch (Environment.OSVersion.Version.Major)
                        {
                            case 3:
                                return "Windows NT 3.51";

                            case 4:
                                return "Windows NT 4.0";

                            case 5:
                                switch (Environment.OSVersion.Version.Minor)
                                {
                                    case 0:
                                        return "Windows 2000";

                                    case 1:
                                        return "Windows XP";

                                    case 2:
                                        return "Windows 2003";
                                }
                                break;

                            case 6:
                                switch (Environment.OSVersion.Version.Minor)
                                {
                                    case 0:
                                        return "Windows Vista";

                                    case 1:
                                        return "Windows 7";
                                }
                                break;
                        }
                        break;

                    case PlatformID.WinCE:
                        return "Windows CE";

                    case PlatformID.Unix:
                        return "Unix";
                }

                return "Unknown OS";
            }
        }

        /// <summary>
        /// A list of certificates trusted by this software, even if the operating system's validation failed.
        /// </summary>
        public static Dictionary<string, string> TrustedCertificates = new Dictionary<string, string>
            {
                {
                    "E=support@cacert.org, CN=CA Cert Signing Authority, OU=http://www.cacert.org, O=Root CA",
                    "3082020A0282020100CE22C0E2467DEC3628075096F2A033408C4BF13B663F31E56B0236DBD67CF6F1888F4E7736054195F909F012CF46867360B76E7EE8C05864AECDB0AD45170C63FA670AE8D6D2BF3EE798C4F04CFAE003BB355D6C21DE9E20D9BACD66323772FAF708F5C7CD58C98EE70E5EEA3EFE1CA1140A156C86845B64662A7AA94B5379F588A27BEE2F0A612B8DB27E4D56A513ECEADA929EAC44411E5860650566F8C044BDCB94F7427E0BF76568985105F0F30591041D1B1782ECC857BBC36B7A88F1B072CC255B2091EC1602128F32E9171848D0C7052E023042B8259C056B3FAA3AA7EB5348F7E8D2B60798DC1BC6347F7FC91C827A05582B085BF338A2AB175D66C998D79E108BA2D2DD749AF7710C7260DFCD6F98339D9634763E247A92B00E951E6FE6A0453847AAD741ED4AB712F6D71B838A0F2ED809B659D7AA04FFD2937D682EDD8B4BAB58BA2F8DEA95A7A0C35489A5FBDB8B51229DB2C3BE11BE2C91868B9678AD20D38A2F1A3FC6D051658721B11901657F451C87F57CD0414C4F299821FD331F750C0451FA1977DBD4141CEE81C31DF598B769069122DD0050CC8131AC12077B38DA685BE62BD47EC95FADE8EB724CF301E54B20BF9AA657CA9100018BA1752137B5630D673E464F702067CEC5D659DB02E0F0D2CBCDBA62B79041E8DD20E429BC642942C822DC789AFF43EC981B09514B5A5AC271F1C4CB73A9E5A10B0203010001"
                }
            };

        /// <summary>
        /// A list of hostnames for which to ignore invalid SSL certificate errors.
        /// </summary>
        public static List<string> IgnoreInvalidCertificatesFor = new List<string>(); 

        /// <summary>
        /// Initializes the <see cref="Utils"/> class.
        /// </summary>
        static Utils()
        {
            Rand       = new Random();
            CryptoRand = new RNGCryptoServiceProvider();

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertificate;
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
        /// <param name="elevate">if set to <c>true</c> the process will be elevated if the invoker is not under admin.</param>
        /// <returns>Console output.</returns>
        public static string RunAndRead(string process, string arguments = null, bool elevate = false)
        {
            var sb = new StringBuilder();
            var p  = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo           =
                        {
                            FileName               = process,
                            Arguments              = arguments ?? string.Empty,
                            UseShellExecute        = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            CreateNoWindow         = true
                        }
                };

            if (elevate)
            {
                if (!IsAdmin)
                {
                    p.StartInfo.Verb = "runas";
                }
            }

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

            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            }
            catch (Win32Exception) { }

            return sb.ToString();
        }

        /// <summary>
        /// Encodes a URL string.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string EncodeURL(string str)
        {
            return Uri.EscapeDataString(str);
        }

        /// <summary>
        /// Converts a string that has been encoded for transmission in a URL into a decoded string.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string DecodeURL(string str)
        {
            return HttpUtility.UrlDecode(str);
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
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static HtmlDocument GetHTML(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var doc = new HtmlDocument();
            var htm = GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent, timeout, headers, proxy, request, response);
            doc.LoadHtml(htm);

            return doc;
        }

        /// <summary>
        /// Downloads the specified URL and parses it as an XML.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static XDocument GetXML(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var xml = GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent, timeout, headers, proxy, request, response);

            return XDocument.Parse(xml);
        }

        /// <summary>
        /// Downloads the specified URL and parses it as an XML.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static XmlDocument GetXML2(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var xml = GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent, timeout, headers, proxy, request, response);
            var doc = new XmlDocument();

            doc.LoadXml(xml);

            return doc;
        }

        /// <summary>
        /// Downloads the specified URL and parses it as a JSON whose members can be dynamically accessed and casted.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static dynamic GetJSON(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var json = GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent, timeout, headers, proxy, request, response);

            return JsonConvert.DeserializeObject(json);
        }

        /// <summary>
        /// Downloads the specified URL and parses it as a JSON similar to type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The type to use when deserializing.</typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static T GetJSON<T>(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var json = GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent, timeout, headers, proxy, request, response);

            return JsonConvert.DeserializeObject<T>(json);
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
        /// <param name="timeout">The requrest timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's content.
        /// </returns>
        public static string GetURL(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            var domain = new Uri(url).Host.Replace("www.", string.Empty);

            object proxyId;
            if (proxy == null && Settings.Get<Dictionary<string, object>>("Proxied Domains").TryGetValue(domain, out proxyId))
            {
                proxy = (string)Settings.Get<Dictionary<string, object>>("Proxies")[(string)proxyId];
            }

            if (proxy != null)
            {
                var proxyUri = new Uri(proxy.Replace("$domain.", string.Empty));

                switch (proxyUri.Scheme.ToLower())
                {
                    case "http":
                        if (proxy.Contains("$url"))
                        {
                            req = (HttpWebRequest)WebRequest.Create(proxy.Replace("$url", EncodeURL(url)));
                        }
                        else if (proxy.Contains("$domain") && proxy.Contains("$path"))
                        {
                            req = (HttpWebRequest)WebRequest.Create(proxy.Replace("$domain", req.RequestUri.DnsSafeHost).Replace("$path", req.RequestUri.AbsolutePath));
                        }
                        else
                        {
                            req.Proxy = new WebProxy(proxyUri.Host + ":" + proxyUri.Port);
                        }
                        break;

                    case "socks4":
                    case "socks4a":
                    case "socks5":
                        var tunnel = new HttpToSocks { RemoteProxy = HttpToSocks.Proxy.ParseUri(proxyUri) };
                        tunnel.Listen();
                        req.Proxy = (WebProxy)tunnel.LocalProxy;
                        break;
                }

                req.Timeout += 20000;
            }
            
            req.Timeout   = timeout;
            req.UserAgent = userAgent ?? "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.7.39 Version/11.00";
            req.ConnectionGroupName    = Guid.NewGuid().ToString();
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (!string.IsNullOrWhiteSpace(postData))
            {
                req.Method                    = "POST";
                req.ContentType               = "application/x-www-form-urlencoded";
                req.AllowWriteStreamBuffering = true;
            }

            req.CookieContainer = new CookieContainer();

            if (!string.IsNullOrWhiteSpace(cookies))
            {
                foreach (var kv in Regex.Replace(cookies.TrimEnd(';'), @";\s*", ";")
                                   .Split(';')
                                   .Where(cookie => cookie != null)
                                   .Select(cookie => cookie.Split('=')))
                {
                    req.CookieContainer.Add(new Cookie(kv[0], kv[1], "/", req.Address.Host));
                }
            }

            if (headers != null && headers.Count != 0)
            {
                foreach (var header in headers)
                {
                    req.Headers[header.Key] = header.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(postData))
            {
                using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                {
                    sw.Write(postData);
                    sw.Flush();
                }
            }

            if (request != null)
            {
                request(req);
            }

            var resp = (HttpWebResponse)req.GetResponse();
            var rstr = resp.GetResponseStream();

            if (response != null)
            {
                response(resp);
            }

            if (!autoDetectEncoding)
            {
                if (encoding is Base64Encoding)
                {
                    using (var ms = new MemoryStream())
                    {
                        int read;
                        do
                        {
                            var bs = new byte[8192];
                            read = rstr.Read(bs, 0, bs.Length);
                            ms.Write(bs, 0, read);
                        }
                        while (read > 0);

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
                else
                {
                    using (var sr = new StreamReader(rstr, encoding ?? Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            else
            {
                byte[] bs;

                using (var ms = new MemoryStream())
                {
                    int read;
                    do
                    {
                        bs = new byte[8192];
                        read = rstr.Read(bs, 0, bs.Length);
                        ms.Write(bs, 0, read);
                    }
                    while (read > 0);

                    bs = ms.ToArray();
                }

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
        /// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>A Boolean value that determines whether the specified certificate is accepted for authentication.</returns>
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            foreach (var element in chain.ChainElements)
            {
                if (TrustedCertificates.ContainsKey(element.Certificate.Subject)
                 && TrustedCertificates[element.Certificate.Subject] == element.Certificate.GetPublicKeyString())
                {
                    return true;
                }
            }

            if (sender is HttpWebRequest && IgnoreInvalidCertificatesFor.Contains(Regex.Replace((sender as HttpWebRequest).Host, @"^www\.", string.Empty)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Modify the specified URL to go through Coral CDN.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Coralified URL.</returns>
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
            return Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), extension));
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

        /// <summary>
        /// Replaces UTF-8 characters with \uXXXX format.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>Pure ASCII text.</returns>
        public static string EscapeUTF8(string text)
        {
            return Regex.Replace(text, @"[^\u0000-\u007F]", m => string.Format(@"\u{0:x4}", (int)m.Value[0]));
        }

        /// <summary>
        /// Sets the application's progress bar state and/or progress on the Windows 7 taskbar.
        /// </summary>
        /// <param name="progress">The progress of the progress bar from 1 to 100.</param>
        /// <param name="state">The state of the progress bar behind the icon.</param>
        public static void Win7Taskbar(int? progress = null, TaskbarProgressBarState? state = null)
        {
            MainWindow.Active.Run(() =>
                {
                    if (!MainWindow.Active.IsVisible)
                    {
                        return;
                    }

                    try
                    {
                        if (state.HasValue)
                        {
                            TaskbarManager.Instance.SetProgressState(state.Value, MainWindow.Active);
                        }

                        if (progress.HasValue)
                        {
                            TaskbarManager.Instance.SetProgressValue(progress.Value, 100, MainWindow.Active);
                        }
                    }
                    catch (PlatformNotSupportedException) { }
                });
        }

        /// <summary>
        /// Gets the default application for the specified extension.
        /// </summary>
        /// <param name="extension">The extension with a leading dot.</param>
        /// <returns>The path of the associated application.</returns>
        public static string GetApplicationForExtension(string extension)
        {
            // get prog id

            var extkey = Registry.ClassesRoot.OpenSubKey(extension);

            if (extkey == null)
            {
                return string.Empty;
            }

            var appid = extkey.GetValue(null) as string;

            if (appid == null)
            {
                return string.Empty;
            }

            extkey.Close();

            // get application

            var appkey = Registry.ClassesRoot.OpenSubKey(appid + @"\shell\open\command");

            if (appkey == null)
            {
                return string.Empty;
            }

            var cmd = appkey.GetValue(null) as string;

            if (cmd == null)
            {
                return string.Empty;
            }

            appkey.Close();

            if (cmd.StartsWith("\""))
            {
                var cmds = Regex.Split(cmd, "\"([^\"]+)");
                if (cmds.Length > 1)
                {
                    cmd = cmds[1];
                }
            }

            return cmd.Trim(" \"'".ToCharArray());
        }

        /// <summary>
        /// Extracts the icon for a specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>WPF-compatible small icon.</returns>
        public static BitmapSource ExtractIcon(string path)
        {
            try
            {
                var largeIcon = IntPtr.Zero;
                var smallIcon = IntPtr.Zero;

                Interop.ExtractIconExW(path, 0, ref largeIcon, ref smallIcon, 1);
                Interop.DestroyIcon(largeIcon);

                return Imaging.CreateBitmapSourceFromHBitmap(Icon.FromHandle(smallIcon).ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the name and small icon of the specified executable.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Tuple containing the name and icon.</returns>
        public static Tuple<string, BitmapSource> GetExecutableInfo(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var name = FileVersionInfo.GetVersionInfo(path).ProductName;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = new FileInfo(path).Name.ToUppercaseFirst().Replace(".exe", string.Empty);
            }

            var icon = ExtractIcon(path);

            return new Tuple<string, BitmapSource>(name, icon);
        }

        /// <summary>
        /// Gets path of applications which are associated to common video extensions.
        /// </summary>
        /// <returns>List of default video players.</returns>
        public static string[] GetDefaultVideoPlayers()
        {
            return new[] { ".avi", ".mkv", ".ts", ".mp4", ".wmv" }
                   .Select(GetApplicationForExtension)
                   .Where(app => !string.IsNullOrWhiteSpace(app))
                   .Distinct()
                   .ToArray();
        }

        /// <summary>
        /// Gets a regular expression which matches illegal characters in file names.
        /// </summary>
        /// <value>Regex for illegal characters.</value>
        public static Regex InvalidFileNameChars = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars().Where(c => c != '\\' && c != '/').ToArray())) + "]");

        /// <summary>
        /// Removes illegal characters from a file name.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <returns>Sanitized file name.</returns>
        public static string SanitizeFileName(string file)
        {
            return InvalidFileNameChars.Replace(file, string.Empty);
        }

        /// <summary>
        /// Determines whether the specified file is in use by another process.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        ///   <c>true</c> if the file is in use by another process; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }

        /// <summary>
        /// Serializes the specified object to a byte array.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Serialized object.</returns>
        public static byte[] SerializeObject(object obj)
        {
            var bf = new BinaryFormatter();

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the specified byte array to an object of type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">Type of the serialized object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <returns>Unserialized object.</returns>
        public static T DeserializeObject<T>(byte[] obj) where T : class
        {
            var bf = new BinaryFormatter();

            using (var ms = new MemoryStream(obj))
            {
                return bf.Deserialize(ms) as T;
            }
        }

        /// <summary>
        /// Converts the specified arabic numeral to a roman numeral.
        /// </summary>
        /// <param name="number">The arabic numeral.</param>
        /// <returns>Roman numeral.</returns>
        public static string NumberToRoman(int number)
        {
            if (number == 0) return "N";

            var arabic = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            var roman  = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

            var result = new StringBuilder();

            for (var i = 0; i < 13; i++)
            {
                while (number >= arabic[i])
                {
                    number -= arabic[i];
                    result.Append(roman[i]);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts the specified roman number into an arabic numeral.
        /// </summary>
        /// <param name="roman">The roman numeral.</param>
        /// <returns>Arabic numeral.</returns>
        public static int RomanToNumber(string roman)
        {
            roman = roman.ToUpper().Trim();

            if (roman == "N") return 0;

            var pairs = new Dictionary<char, int>
                {
                    { 'I', 1    },
                    { 'V', 5    },
                    { 'X', 10   },
                    { 'L', 50   },
                    { 'C', 100  },
                    { 'D', 500  },
                    { 'M', 1000 }
                };

            int i = 0, value = 0;
            while (i < roman.Length)
            {
                var digit = pairs[roman[i]];

                if (i < roman.Length - 1)
                {
                    var next = pairs[roman[i + 1]];

                    if (next > digit)
                    {
                        digit = next - digit;
                        i++;
                    }
                }

                value += digit;

                i++;
            }

            return value;
        }

        /// <summary>
        /// Encrypts the specified text with AES-256.
        /// </summary>
        /// <param name="secret">The text to be encrypted.</param>
        /// <param name="password">The password.</param>
        /// <returns>
        /// Base64-encoded encrypted text.
        /// </returns>
        public static string Encrypt(string secret, string password)
        {
            var raw = Encoding.UTF8.GetBytes(secret);
            var pdb = new Rfc2898DeriveBytes(password, BitConverter.GetBytes(Math.PI));

            using (var ms  = new MemoryStream())
            using (var alg = Rijndael.Create())
            {
                alg.Mode = CipherMode.CBC;
                alg.Key  = pdb.GetBytes(32);
                alg.IV   = pdb.GetBytes(16);

                using (var cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(raw, 0, raw.Length);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Decrypts the specified text with AES-256.
        /// </summary>
        /// <param name="secret">The Base64-encoded encrypted text.</param>
        /// <param name="password">The password.</param>
        /// <returns>
        /// Decrypted text.
        /// </returns>
        public static string Decrypt(string secret, string password)
        {
            var enc = Convert.FromBase64String(secret);
            var pdb = new Rfc2898DeriveBytes(password, BitConverter.GetBytes(Math.PI));

            using (var ms  = new MemoryStream())
            using (var alg = Rijndael.Create())
            {
                alg.Mode = CipherMode.CBC;
                alg.Key  = pdb.GetBytes(32);
                alg.IV   = pdb.GetBytes(16);

                using (var cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(enc, 0, enc.Length);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Retrieves a table that contains a list of TCP endpoints available to the application.
        /// </summary>
        /// <returns>
        /// List of raw TCP endpoints.
        /// </returns>
        public static List<Interop.TcpRow> GetExtendedTCPTable()
        {
            var rows = new List<Interop.TcpRow>();
            var tcpTable = IntPtr.Zero;
            var tcpTableLength = 0;

            if (Interop.GetExtendedTcpTable(tcpTable, ref tcpTableLength, false, 2, Interop.TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);

                    if (Interop.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, 2, Interop.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        var table = (Interop.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(Interop.TcpTable));
                        var rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));

                        for (int i = 0; i < table.length; ++i)
                        {
                            rows.Add((Interop.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(Interop.TcpRow)));
                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(Interop.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// Extracts the key-values in a query string into a dictionary.
        /// </summary>
        /// <param name="qs">The query string.</param>
        /// <returns>
        /// Extracted key-values.
        /// </returns>
        public static Dictionary<string, string> ParseQueryString(string qs)
        {
            var dic = new Dictionary<string, string>();
            var mc  = Regex.Matches(qs, @"&?(?<key>[^=]+)=(?<value>[^&$]+)");

            foreach (Match m in mc)
            {
                dic[m.Groups["key"].Value] = Utils.DecodeURL(m.Groups["value"].Value);
            }

            return dic;
        }

        /// <summary>
        /// Creates a slug from the specified title.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="extraSlim">if set to <c>true</c> spaces, "and", "the", "of" and "a" will be removed.</param>
        /// <param name="separator">The separator to use when <c>extraSlim</c> is set to <c>false</c>.</param>
        /// <returns>
        /// Slug.
        /// </returns>
        public static string CreateSlug(string title, bool extraSlim = true, string separator = "-")
        {
            // remove HTML entities
            title = HtmlEntity.DeEntitize(title);

            // remove diacritics. don't ask why this works, it just does.
            title = Encoding.ASCII.GetString(Encoding.GetEncoding("Cyrillic").GetBytes(title));

            if (extraSlim)
            {
                // remove stopwords, year, and special characters including spaces
                title = Regex.Replace(title.ToLower(), @"(\s\(20\d{2}\)|\s20\d{2}$|\b(and|the|of|a)\b|[^a-z0-9])", string.Empty).Trim();
            }
            else
            {
                // remove year and special characters
                title = Regex.Replace(title.ToLower(), @"(\s\(20\d{2}\)|[^a-z0-9\s])", string.Empty).Trim();

                // replace space to specified separator
                title = Regex.Replace(title, @"\s\s*", separator);
            }

            return title;
        }

        /// <summary>
        /// Parses the age string returned by a Usenet indexer.
        /// </summary>
        /// <param name="age">The age string as returned by the indexer.</param>
        /// <returns>
        /// Normalized age string.
        /// </returns>
        public static string ParseAge(string age)
        {
            var res = Regex.Match(age, @"(\d+(?:\.\d+)?)\s*([mhdwy])", RegexOptions.IgnoreCase);
            double nr;

            if (!res.Success || !double.TryParse(res.Groups[1].Value, out nr))
            {
                return age;
            }

            switch (res.Groups[2].Value.ToLower())
            {
                case "m":
                    return FormatNumber((int)nr, "minute") + " old";

                case "h":
                    return FormatNumber((int)nr, "hour") + " old";

                case "d":
                    return FormatNumber((int)nr, "day") + " old";

                case "w":
                    return FormatNumber((int)(nr * 7), "day") + " old";

                case "y":
                    return FormatNumber((int)(nr * 365.242199), "day") + " old";

                default:
                    return age;
            }
        }

        /// <summary>
        /// Converts the specified DateTime a Usenet indexer-style relative age.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <returns>A string value based on the relative date
        /// of the datetime as compared to the current date.</returns>
        public static string DetermineAge(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
     
            if (timeSpan <= TimeSpan.FromSeconds(60))
            {
                return FormatNumber(timeSpan.Seconds, "second") + " old";
            }

            if (timeSpan <= TimeSpan.FromMinutes(60))
            {
                return FormatNumber(timeSpan.Minutes, "minute") + " old";
            }

            if (timeSpan <= TimeSpan.FromHours(24))
            {
                return FormatNumber(timeSpan.Hours, "hour") + " old";
            }

            if (timeSpan <= TimeSpan.FromDays(30))
            {
                return FormatNumber(timeSpan.Days, "day") + " old";
            }

            if (timeSpan <= TimeSpan.FromDays(365))
            {
                return FormatNumber((timeSpan.Days / 30), "month") + " old";
            }

            return FormatNumber((timeSpan.Days / 365), "year") + " old";
        }

        /// <summary>
        /// Converts a date and time to a version number.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="major">The major version number to use.</param>
        /// <param name="minor">The minor version number to use.</param>
        /// <returns>
        /// A date-based version number.
        /// </returns>
        public static Version DateTimeToVersion(string date, int major = 2, int minor = 0)
        {
            return DateTimeToVersion(DateTime.Parse(date), major, minor);
        }

        /// <summary>
        /// Converts a date and time to a version number.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="major">The major version number to use.</param>
        /// <param name="minor">The minor version number to use.</param>
        /// <returns>
        /// A date-based version number.
        /// </returns>
        public static Version DateTimeToVersion(DateTime date, int major = 2, int minor = 0)
        {
            var diff = date - new DateTime(2000, 1, 1, 1, 0, 0);

            return new Version(
                major,
                minor,
                (int)Math.Floor(diff.TotalDays),
                (int)Math.Round((diff.Subtract(TimeSpan.FromDays(Math.Floor(diff.TotalDays)))).TotalSeconds / 2)
            );
        }

        /// <summary>
        /// Converts a version number to a date and time.
        /// </summary>
        /// <param name="version">The date-based version number.</param>
        /// <returns>
        /// Date and time extracted from the versio number.
        /// </returns>
        public static DateTime VersionToDateTime(string version)
        {
            return VersionToDateTime(Version.Parse(version));
        }

        /// <summary>
        /// Converts a version number to a date and time.
        /// </summary>
        /// <param name="version">The date-based version number.</param>
        /// <returns>
        /// Date and time extracted from the versio number.
        /// </returns>
        public static DateTime VersionToDateTime(Version version)
        {
            return new DateTime(2000, 1, 1, 1, 0, 0).AddDays(version.Build).AddSeconds(version.Revision * 2);
        }

        /// <summary>
        /// Transforms a <c>CookieCollection</c> object into a cookie string.
        /// </summary>
        /// <param name="cookies">The wierdly-stored cookies.</param>
        /// <param name="removeSession">if set to <c>true</c> temporary cookies (such as <c>PHPSESSID</c>) will be removed.</param>
        /// <returns>
        /// String of cookie key-values.
        /// </returns>
        public static string EatCookieCollection(CookieCollection cookies, bool removeSession = false)
        {
            if (cookies == null || cookies.Count == 0)
            {
                return string.Empty;
            }

            var cookiez = new StringBuilder();

            foreach (Cookie cookie in cookies)
            {
                if (removeSession && (cookie.Name == "PHPSESSID" || cookie.Name == "JSESSIONID" || cookie.Value == "deleted"))
                {
                    continue;
                }

                if (cookiez.Length != 0)
                {
                    cookiez.Append("; ");
                }

                cookiez.Append(cookie.Name + "=" + cookie.Value);
            }

            return cookiez.ToString();
        }

        /// <summary>
        /// A custom encoding to denote Base64-encoded content.
        /// </summary>
        public class Base64Encoding : ASCIIEncoding
        {
        }
    }
}