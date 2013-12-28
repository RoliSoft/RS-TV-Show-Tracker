namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Compat.Web;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security.AccessControl;
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

    using RoliSoft.TVShowTracker.ShowNames;

    using Formatting = Newtonsoft.Json.Formatting;

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
        public static bool IsNT6
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                     ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1) ||
                       Environment.OSVersion.Version.Major >= 6);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operating system is Windows 7.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the OS is Windows 7; otherwise, <c>false</c>.
        /// </value>
        public static bool Is7
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                      (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operating system is Windows 8 or 8.1.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the OS is Windows 8 or 8.1; otherwise, <c>false</c>.
        /// </value>
        public static bool Is8
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                      (Environment.OSVersion.Version.Major == 6 && (Environment.OSVersion.Version.Minor == 2 || Environment.OSVersion.Version.Minor == 3));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operating system is Windows XP.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the OS is Windows XP; otherwise, <c>false</c>.
        /// </value>
        public static bool IsXP
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                      (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor == 1);
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
                if (!_isAdmin.HasValue)
                {
                    _isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                }

                return _isAdmin.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current process is 64-bit.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the current process is 64-bit; otherwise, <c>false</c>.
        /// </value>
        public static bool Is64Bit
        {
            get
            {
                if (!_isX64.HasValue)
                {
                    _isX64 = (Marshal.SizeOf(typeof(IntPtr)) == 8);
                }

                return _isX64.Value;
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
                                        return "Windows XP 64-Bit Edition";
                                }
                                break;

                            case 6:
                                switch (Environment.OSVersion.Version.Minor)
                                {
                                    case 0:
                                        return "Windows Vista";

                                    case 1:
                                        return "Windows 7";

                                    case 2:
                                        return "Windows 8";

                                    case 3:
                                        return "Windows 8.1";
                                }
                                break;
                        }
                        break;

                    case PlatformID.WinCE:
                        return "Windows CE";

                    case PlatformID.Unix:
                        return "Unix";

                    case PlatformID.MacOSX:
                        return "Mac OS X " + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor;

                    case PlatformID.Xbox:
                        return "Xbox 360";
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

        private static bool? _isAdmin, _isX64;

        /// <summary>
        /// Initializes the <see cref="Utils"/> class.
        /// </summary>
        static Utils()
        {
            Rand       = new Random();
            CryptoRand = new RNGCryptoServiceProvider();

            WebRequest.DefaultWebProxy = null;
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
        /// <param name="addis">if set to <c>true</c> "is" or "are" will be appended.</param>
        /// <returns>Formatted number.</returns>
        public static string FormatNumber(int number, string unit, bool addis = false)
        {
            return number + " " + unit + (number != 1 ? "s" : string.Empty) + (addis ? " " + (number != 1 ? "are" : "is") : string.Empty);
        }

        /// <summary>
        /// Runs the specified process.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="arguments">The arguments.</param>
        public static void Run(string process, string arguments = null)
        {
            Log.Debug("Running process: {0} {1}", new[] { process, arguments });

            try
            {
                Process.Start(process, arguments);
            }
            catch (Win32Exception ex)
            {
                Log.Warn("Exception while running: " + process + " " + arguments, ex);
            }
        }

        /// <summary>
        /// Runs the specified process with admin rights.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="arguments">The arguments.</param>
        public static void RunElevated(string process, string arguments = null)
        {
            Log.Debug("Running process with elevation: runas {0} {1}", new[] { process, arguments });

            var pi = new ProcessStartInfo
                         {
                             Verb      = "runas",
                             FileName  = process,
                             Arguments = arguments
                         };

            try
            {
                Process.Start(pi);
            }
            catch (Win32Exception ex)
            {
                Log.Warn("Exception while running: " + process + " " + arguments, ex);
            }
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
            Log.Debug("Running process with I/O redirection{0} {1} {2}", new[] { elevate && !IsAdmin ? " and elevation: runas" : ":", process, arguments });

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
            catch (Win32Exception ex)
            {
                Log.Warn("Exception while running: " + process + " " + arguments, ex);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the specified path is writable by the Users group.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if the specified path is writable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUserWritable(string path)
        {
            try
            {
                var ds = Directory.GetAccessControl(path);
                var si = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                foreach (var rule in ds.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    if (((AuthorizationRule)rule).IdentityReference == si)
                    {
                        var rights = ((FileSystemAccessRule)rule);
                        if (rights.AccessControlType == AccessControlType.Allow)
                        {
                            if (rights.FileSystemRights == (rights.FileSystemRights | FileSystemRights.Modify))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Adds permission the specified path so it will be writable by the Users groups.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if the operation was successfull; otherwise, <c>false</c>.
        /// </returns>
        public static bool MakeUserWritable(string path)
        {
            try
            {
                var rule = new FileSystemAccessRule(@"BUILTIN\Users", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);

                var di = new DirectoryInfo(path);
                var ds = di.GetAccessControl(AccessControlSections.Access);

                var res = false;
                ds.ModifyAccessRule(AccessControlModification.Set, rule, out res);

                if (!res)
                {
                    return false;
                }

                res = false;
                ds.ModifyAccessRule(AccessControlModification.Add, rule, out res);

                if (!res)
                {
                    return false;
                }

                di.SetAccessControl(ds);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
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
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static HtmlDocument GetHTML(string url, object postData = null, string cookies = null, Encoding encoding = null, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var doc = new HtmlDocument();
            var htm = GetURL(url, postData, cookies, encoding, userAgent, timeout, headers, proxy, request, response);
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
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static XDocument GetXML(string url, object postData = null, string cookies = null, Encoding encoding = null, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var xml = GetURL(url, postData, cookies, encoding, userAgent, timeout, headers, proxy, request, response);

            return XDocument.Parse(xml);
        }

        /// <summary>
        /// Downloads the specified URL and parses it as an XML.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static XmlDocument GetXML2(string url, object postData = null, string cookies = null, Encoding encoding = null, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var xml = GetURL(url, postData, cookies, encoding, userAgent, timeout, headers, proxy, request, response);
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
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static dynamic GetJSON(string url, object postData = null, string cookies = null, Encoding encoding = null, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var json = GetURL(url, postData, cookies, encoding, userAgent, timeout, headers, proxy, request, response);

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
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's parsed content.
        /// </returns>
        public static T GetJSON<T>(string url, object postData = null, string cookies = null, Encoding encoding = null, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var json = GetURL(url, postData, cookies, encoding, userAgent, timeout, headers, proxy, request, response);

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Downloads the specified URL into a string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="timeout">The requrest timeout in milliseconds.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <param name="proxy">The proxy to use to connect.</param>
        /// <param name="request">The method to call with the request object before the request is made.</param>
        /// <param name="response">The method to call with the response object after the request was made.</param>
        /// <returns>
        /// Remote page's content.
        /// </returns>
        public static string GetURL(string url, object postData = null, string cookies = null, Encoding encoding = null, string userAgent = null, int timeout = 10000, Dictionary<string, string> headers = null, string proxy = null, Action<HttpWebRequest> request = null, Action<HttpWebResponse> response = null)
        {
            var id = Rand.Next(short.MaxValue);
            var st = DateTime.Now;
            var domain = new Uri(url).Host.Replace("www.", string.Empty);

            Log.Debug("HTTP#{0} {1} {2}", new[] { id.ToString(), postData != null ? "POST" : "GET", url });

            var req = (HttpWebRequest)WebRequest.Create(url);
            var proxyId = default(object);

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
                Log.Debug("HTTP#" + id + " [" + domain + "] is proxied through " + proxyId + " (" + proxyUri + ")");
            }
            else
            {
                req.Proxy = null;
            }
            
            req.Timeout   = timeout;
            req.UserAgent = userAgent ?? "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.7.39 Version/11.00";
            req.ConnectionGroupName    = Guid.NewGuid().ToString();
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if ((postData is string && !string.IsNullOrWhiteSpace(postData as string)) || (postData is byte[] && (postData as byte[]).Length != 0))
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

            if (postData is string && !string.IsNullOrWhiteSpace(postData as string))
            {
                using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                {
                    sw.Write(postData as string);
                    sw.Flush();
                }
            }
            else if (postData is byte[] && (postData as byte[]).Length != 0)
            {
                using (var bw = new BinaryWriter(req.GetRequestStream()))
                {
                    bw.Write(postData as byte[]);
                    bw.Flush();
                }
            }

            if (request != null)
            {
                request(req);
            }

            HttpWebResponse resp;

            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException ex)
            {
                Log.Warn("HTTP#" + id + " [" + domain + "] threw an exception, " + (ex.Response == null ? "without a response; rethrowing exception" : "with response; returning response") + ".", ex);

                if (ex.Response != null)
                {
                    resp = (HttpWebResponse)ex.Response;
                }
                else
                {
                    throw;
                }
            }

            var rstr = resp.GetResponseStream();

            if (response != null)
            {
                response(resp);
            }


            if (encoding is Base64Encoding)
            {
                byte[] res;

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

                    res = ms.ToArray();
                }

                Log.Debug("HTTP#" + id + " [" + domain + "] is " + GetFileSize(res.Length) + " and took " + (DateTime.Now - st).TotalSeconds + "s.");
                if (Log.IsTraceEnabled) Log.Trace("HTTP#" + id + " [" + domain + "] is " + resp.ContentType + ", dumping first 156 bytes", res.Take(156).ToArray());

                return Convert.ToBase64String(res);
            }
            else
            {
                using (var sr = new StreamReader(rstr, encoding ?? Encoding.UTF8))
                {
                    var str = sr.ReadToEnd();

                    Log.Debug("HTTP#" + id + " [" + domain + "] is " + GetFileSize(str.Length) + " and took " + (DateTime.Now - st).TotalSeconds + "s.");
                    if (Log.IsTraceEnabled) Log.Trace("HTTP#" + id + " [" + domain + "] is " + resp.ContentType + ", dumping text content" + Environment.NewLine + Regex.Replace(Regex.Replace(Regex.Replace(str, @"<\s*(script|style)[^>]*>.*?<\s*/\s*\1[^>]*>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase), "<[^>]+>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase).Replace("&quot;", "\"").Replace("&nbsp;", " "), @"\s\s*", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase));

                    return str;
                }
            }
        }

        /// <summary>
        /// Downloads the specified URL into a string using low-level sockets.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// Remote page's content.
        /// </returns>
        public static string GetFastURL(string url, int timeout = 10000)
        {
            var uri = new Uri(url);
            var req = Encoding.UTF8.GetBytes("GET " + uri.PathAndQuery + " HTTP/1.1\r\nHost: " + uri.DnsSafeHost + (!uri.IsDefaultPort ? ":" + uri.Port : string.Empty) + "\r\nConnection: close\r\nUser-Agent: Opera/9.80 (Windows NT 6.1; U; en) Presto/2.7.39 Version/11.00\r\n\r\n");
            var tcp = new TcpClient();

            tcp.NoDelay = true;

            tcp.Connect(uri.DnsSafeHost, uri.Port);

            using (var st = tcp.GetStream())
            {
                Stream ns;

                if (uri.Scheme == "https")
                {
                    var ssl = new SslStream(st);
                    ssl.AuthenticateAsClient(uri.DnsSafeHost);
                    ns = ssl;
                }
                else
                {
                    ns = st;
                }

                using (var sr = new StreamReader(ns))
                {
                    ns.Write(req, 0, req.Length);

                    var dnl = false;
                    while (!dnl)
                    {
                        dnl = sr.ReadLine() == string.Empty;
                    }

                    var str = sr.ReadToEnd();
                    return str;
                }
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
            string host;

            if (sender is HttpWebRequest)
            {
                host = Regex.Replace((sender as HttpWebRequest).Host, @"^www\.", string.Empty);
            }
            else
            {
                host = sender.ToString();
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                Log.Debug("HTTPS [" + host + "] " + certificate.Subject + " has been deemed valid by Windows.");
                return true;
            }

            foreach (var element in chain.ChainElements)
            {
                if (TrustedCertificates.ContainsKey(element.Certificate.Subject)
                 && TrustedCertificates[element.Certificate.Subject] == element.Certificate.GetPublicKeyString())
                {
                    Log.Debug("HTTPS [" + host + "] " + certificate.Subject + " contains a software-trusted certificate in its chain.");
                    return true;
                }
            }

            if (sender is HttpWebRequest && IgnoreInvalidCertificatesFor.Contains(host))
            {
                Log.Debug("HTTPS [" + host + "] " + certificate.Subject + " is invalid, but host is whitelisted.");
                return true;
            }

            Log.Warn("HTTPS [" + host + "] " + certificate.Subject + " is invalid, dropping connection.");
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
            if (string.IsNullOrWhiteSpace(Signature.InstallPath))
            {
                return string.Empty;
            }

            var uid = Database.Setting("uuid");

            if (string.IsNullOrWhiteSpace(uid))
            {
                uid = Guid.NewGuid().ToString();
                Database.Setting("uuid", uid);
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
            else if (cmd.Contains("\""))
            {
                var cmds = Regex.Split(cmd, "\"([^\"]+)");
                if (cmds.Length > 1)
                {
                    cmd = cmds[0];
                }
            }

            return cmd.Trim(" \"'".ToCharArray());
        }

        /// <summary>
        /// Gets the name and small icon of the specified executable.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="getIcon">if set to <c>true</c> the associated icon will be extracted.</param>
        /// <returns>
        /// Tuple containing the name and icon.
        /// </returns>
        public static Tuple<string, BitmapSource> GetExecutableInfo(string path, bool getIcon = true)
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

            var icon = default(BitmapSource);

            if (getIcon)
            {
                try { icon = Imaging.CreateBitmapSourceFromHIcon(Icons.ExtractOne(path, 0, Icons.SystemIconSize.Small).Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); } catch { }
            }

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
                {
                    stream.Close();
                }
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
        /// Encrypts the specified text with AES-256.
        /// </summary>
        /// <param name="type">The type the secret belongs to.</param>
        /// <param name="secrets">The secrets to be encrypted.</param>
        /// <returns>
        /// Base64-encoded encrypted text.
        /// </returns>
        public static string Encrypt(Type type, params string[] secrets)
        {
            return Encrypt(string.Join("\0", secrets), Signature.Software + '\0' + type.FullName + '\0' + GetUUID());
        }

        /// <summary>
        /// Encrypts the specified text with AES-256.
        /// </summary>
        /// <param name="plugin">The plugin the secret belongs to.</param>
        /// <param name="secrets">The secrets to be encrypted.</param>
        /// <returns>
        /// Base64-encoded encrypted text.
        /// </returns>
        public static string Encrypt(IPlugin plugin, params string[] secrets)
        {
            return Encrypt(plugin.GetType(), secrets);
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
        /// Decrypts the specified text with AES-256.
        /// </summary>
        /// <param name="type">The type the secret belongs to.</param>
        /// <param name="secret">The Base64-encoded encrypted text.</param>
        /// <returns>
        /// Decrypted texts.
        /// </returns>
        public static string[] Decrypt(Type type, string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return new[] { secret, secret };
            }

            return Decrypt(secret, Signature.Software + '\0' + type.FullName + '\0' + GetUUID()).Split(new[] { "\0" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Decrypts the specified text with AES-256.
        /// </summary>
        /// <param name="plugin">The plugin the secret belongs to.</param>
        /// <param name="secret">The Base64-encoded encrypted text.</param>
        /// <returns>
        /// Decrypted texts.
        /// </returns>
        public static string[] Decrypt(IPlugin plugin, string secret)
        {
            return Decrypt(plugin.GetType(), secret);
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
                dic[m.Groups["key"].Value] = DecodeURL(m.Groups["value"].Value);
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
                // replace & to "and"
                title = Regexes.Ampersand.Replace(title, "and");

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
        /// <param name="forceDay">if set to <c>true</c> the maximum unit will be days.</param>
        /// <returns>
        /// A string value based on the relative date
        /// of the datetime as compared to the current date.
        /// </returns>
        public static string DetermineAge(DateTime dateTime, bool forceDay = false)
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

            if (timeSpan <= TimeSpan.FromDays(30) || forceDay)
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
        /// Dumps the byte array into a readable format for debugging purposes.
        /// </summary>
        /// <param name="bytes">The byte array to dump.</param>
        /// <param name="bytesPerLine">The number of bytes to display on each line.</param>
        /// <param name="lowAscii">if set to <c>true</c> low ASCII characters, <code>0x00</code> - <code>0x1F</code>, will be represented with their counterpart Unicode symbols.</param>
        /// <returns>Readable hex dump.</returns>
        public static string HexDump(this byte[] bytes, int bytesPerLine = 26, bool lowAscii = false)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            var chars = "0123456789ABCDEF".ToCharArray();
            var firstHexCol = 8 + 2;
            var firstCharCol = firstHexCol + bytesPerLine * 3 + 1;
            var lineLength = firstCharCol + bytesPerLine + Environment.NewLine.Length;
            var line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
            var expectedLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;
            var sb = new StringBuilder(expectedLines * lineLength);

            for (var i = 0; i < bytes.Length; i += bytesPerLine)
            {
                line[0] = chars[(i >> 28) & 0xF];
                line[1] = chars[(i >> 24) & 0xF];
                line[2] = chars[(i >> 20) & 0xF];
                line[3] = chars[(i >> 16) & 0xF];
                line[4] = chars[(i >> 12) & 0xF];
                line[5] = chars[(i >> 8) & 0xF];
                line[6] = chars[(i >> 4) & 0xF];
                line[7] = chars[(i >> 0) & 0xF];

                var hexCol = firstHexCol;
                var charCol = firstCharCol;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (i + j >= bytes.Length)
                    {
                        line[hexCol] = ' ';
                        line[hexCol + 1] = ' ';
                        line[charCol] = ' ';
                    }
                    else
                    {
                        var b = bytes[i + j];
                        var c = (char)b;

                        if (b < 32)
                        {
                            if (lowAscii)
                            {
                                c = (char)(9216 + b);
                            }
                            else
                            {
                                c = '·';
                            }
                        }

                        line[hexCol] = chars[(b >> 4) & 0xF];
                        line[hexCol + 1] = chars[b & 0xF];
                        line[charCol] = c;
                    }

                    hexCol += 3;
                    charCol++;
                }

                sb.Append("  ");
                sb.Append(line);
            }

            return "Dump of byte[" + bytes.Length + "]:" + Environment.NewLine + sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Dumps the object into a readable format for debugging purposes.
        /// </summary>
        /// <param name="value">The object to dump.</param>
        /// <returns>
        /// Object in string format.
        /// </returns>
        public static string ObjDump(this object value)
        {
            if (value == null)
            {
                return "<null>";
            }
            
            var json = JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            
            return "Dump of type " + CleanTypeName(value.GetType().ToString()) + ":" + Environment.NewLine + json;
        }

        /// <summary>
        /// Cleans the name of the type.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <returns>
        /// Clean type name.
        /// </returns>
        public static string CleanTypeName(string name)
        {
            return Regex.Replace(Regex.Replace(name, @"System\.(?:[A-z]+\.)*", string.Empty), @"`\d{1,2}\[", "<").Replace(']', '>');
        }

        /// <summary>
        /// A custom encoding to denote Base64-encoded content.
        /// </summary>
        public class Base64Encoding : ASCIIEncoding
        {
        }

        /// <summary>
        /// Gets the stardates for Star Trek: The Original Series.
        /// </summary>
        /// <returns>Stardates for each episode.</returns>
        public static Dictionary<int, string> GetStarTrekTheOriginalSeriesStardates()
        {
            return new Dictionary<int, string>
                {
                    { 1*1000 + 1,	"1513.1" }, { 1*1000 + 2,	"1533.6" }, { 1*1000 + 3,	"1312.4" }, { 1*1000 + 4,	"1704.2" },
                    { 1*1000 + 5,	"1672.1" }, { 1*1000 + 6,	"1329.8" }, { 1*1000 + 7,	"2712.4" }, { 1*1000 + 8,	"2713.5" },
                    { 1*1000 + 9,	"2715.1" }, { 1*1000 + 10,	"1512.2" }, { 1*1000 + 11,	"3012.4" }, { 1*1000 + 12,	"3013.1" },
                    { 1*1000 + 13,	"2817.6" }, { 1*1000 + 14,	"1709.2" }, { 1*1000 + 15,	"3025.3" }, { 1*1000 + 16,	"2821.5" },
                    { 1*1000 + 17,	"2124.5" }, { 1*1000 + 18,	"3045.6" }, { 1*1000 + 19,	"3113.2" }, { 1*1000 + 20,	"2947.3" },
                    { 1*1000 + 21,	"3156.2" }, { 1*1000 + 22,	"3141.9" }, { 1*1000 + 23,	"3192.1" }, { 1*1000 + 24,	"3417.3" },
                    { 1*1000 + 25,	"3196.1" }, { 1*1000 + 26,	"3198.4" }, { 1*1000 + 27,	"3087.6" }, { 1*1000 + 28,	"3187.4" },
                    { 1*1000 + 29,	"3287.2" }, { 2*1000 + 1,	"3372.7" }, { 2*1000 + 2,	"3468.1" }, { 2*1000 + 3,	"3541.9" },
                    { 2*1000 + 4,	"3628.6" }, { 2*1000 + 5,	"3715.3" }, { 2*1000 + 6,	"4202.9" }, { 2*1000 + 7,	"3018.2" },
                    { 2*1000 + 8,	"4513.3" }, { 2*1000 + 9,	"3219.4" }, { 2*1000 + 10,	"3842.3" }, { 2*1000 + 11,	"3497.2" },
                    { 2*1000 + 12,	"3478.2" }, { 2*1000 + 13,	"3619.2" }, { 2*1000 + 14,	"3614.9" }, { 2*1000 + 15,	"4523.3" },
                    { 2*1000 + 16,	"3211.7" }, { 2*1000 + 17,	"4598.0" }, { 2*1000 + 18,	"4307.1" }, { 2*1000 + 19,	"4211.4" },
                    { 2*1000 + 20,	"4768.3" }, { 2*1000 + 21,	"2534.0" }, { 2*1000 + 22,	"4657.5" }, { 2*1000 + 23,	"4693.5" },
                    { 2*1000 + 24,	"4729.4" }, { 2*1000 + 25,	"4040.7" }, { 2*1000 + 26,	"4736.1" }, { 3*1000 + 1,	"5431.4" },
                    { 3*1000 + 2,	"5027.3" }, { 3*1000 + 3,	"4842.6" }, { 3*1000 + 4,	"5029.5" }, { 3*1000 + 5,	"5630.7" },
                    { 3*1000 + 6,	"4385.3" }, { 3*1000 + 7,	"4930.8" }, { 3*1000 + 8,	"5476.3" }, { 3*1000 + 9,	"5693.2" },
                    { 3*1000 + 10,	"5784.2" }, { 3*1000 + 11,	"5710.5" }, { 3*1000 + 12,	"5121.5" }, { 3*1000 + 13,	"4372.5" }, 
                    { 3*1000 + 14,	"5718.3" }, { 3*1000 + 15,	"5730.2" }, { 3*1000 + 16,	"5423.4" }, { 3*1000 + 17,	"5574.4" },
                    { 3*1000 + 18,	"5725.3" }, { 3*1000 + 19,	"5843.7" }, { 3*1000 + 20,	"5832.3" }, { 3*1000 + 21,	"5818.4" },
                    { 3*1000 + 22,	"5906.4" }, { 3*1000 + 23,	"5943.7" }, { 3*1000 + 24,	"5928.5" }
                };
        }
        
        /// <summary>
        /// Gets the stardates for Star Trek: The Animated Series.
        /// </summary>
        /// <returns>Stardates for each episode.</returns>
        public static Dictionary<int, string> GetStarTrekTheAnimatedSeriesStardates()
        {
            return new Dictionary<int, string>
                {
                    { 1*1000 + 1,	"5521.3" }, { 1*1000 + 2,	"5373.4" }, { 1*1000 + 3,	"5371.3" }, { 1*1000 + 4,	"5483.7" },
                    { 1*1000 + 5,	"5392.4" }, { 1*1000 + 6,	"5143.3" }, { 1*1000 + 7,	"5554.4" }, { 1*1000 + 8,	"1254.4" },
                    { 1*1000 + 9,	"5591.2" }, { 1*1000 + 10,	"4978.5" }, { 1*1000 + 11,	"5577.3" }, { 1*1000 + 12,	"5267.2" },
                    { 1*1000 + 13,	"5499.9" }, { 1*1000 + 14,	"4187.3" }, { 1*1000 + 15,	"5501.2" }, { 1*1000 + 16,	"5683.1" },
                    { 2*1000 + 1,	"6334.1" }, { 2*1000 + 2,	"7403.6" }, { 2*1000 + 3,	"3183.3" }, { 2*1000 + 4,	"5275.6" },
                    { 2*1000 + 5,	"6063.4" }, { 2*1000 + 6,	"6770.3" }
                };
        }
        
        /// <summary>
        /// Gets the stardates for Star Trek: The Next Generation.
        /// </summary>
        /// <returns>Stardates for each episode.</returns>
        public static Dictionary<int, string> GetStarTrekTheNextGenerationStardates()
        {
            return new Dictionary<int, string>
                {
                    { 1*1000 + 1,	"41153.7" }, { 1*1000 + 2,	"41153.7" }, { 1*1000 + 3,	"41209.2" }, { 1*1000 + 4,	"41235.2" },
                    { 1*1000 + 5,	"41386.4" }, { 1*1000 + 6,	"41263.1" }, { 1*1000 + 7,	"41249.3" }, { 1*1000 + 8,	"41255.6" },
                    { 1*1000 + 9,	"41723.9" }, { 1*1000 + 10,	"41590.5" }, { 1*1000 + 11,	"41294.5" }, { 1*1000 + 12,	"41997.7" },
                    { 1*1000 + 13,	"41242.4" }, { 1*1000 + 14,	"41636.9" }, { 1*1000 + 15,	"41365.9" }, { 1*1000 + 16,	"41309.5" },
                    { 1*1000 + 17,	"41509.1" }, { 1*1000 + 18,	"41463.9" }, { 1*1000 + 19,	"41416.2" }, { 1*1000 + 20,	"41503.7" },
                    { 1*1000 + 21,	"41798.2" }, { 1*1000 + 22,	"41699.8" }, { 1*1000 + 23,	"41601.3" }, { 1*1000 + 24,	"41697.9" },
                    { 1*1000 + 25,	"41775.5" }, { 1*1000 + 26,	"41986.0" }, { 2*1000 + 1,	"42073.1" }, { 2*1000 + 2,	"42193.6" },
                    { 2*1000 + 3,	"42286.3" }, { 2*1000 + 4,	"42402.7" }, { 2*1000 + 5,	"42477.2" }, { 2*1000 + 6,	"42437.5" },
                    { 2*1000 + 7,	"42494.8" }, { 2*1000 + 8,	"42506.5" }, { 2*1000 + 9,	"42523.7" }, { 2*1000 + 10,	"42568.8" },
                    { 2*1000 + 11,	"42609.1" }, { 2*1000 + 12,	"42625.4" }, { 2*1000 + 13,	"42679.2" }, { 2*1000 + 14,	"42686.4" },
                    { 2*1000 + 15,	"42695.3" }, { 2*1000 + 16,	"42761.3" }, { 2*1000 + 17,	"42779.1" }, { 2*1000 + 18,	"42823.2" },
                    { 2*1000 + 19,	"42859.2" }, { 2*1000 + 20,	"42901.3" }, { 2*1000 + 21,	"42923.4" }, { 2*1000 + 22,	"42976.1" },
                    { 3*1000 + 1,	"43125.8" }, { 3*1000 + 2,	"43133.3" }, { 3*1000 + 3,	"43152.4" }, { 3*1000 + 4,	"43173.5" },
                    { 3*1000 + 5,	"43198.7" }, { 3*1000 + 6,	"43205.6" }, { 3*1000 + 7,	"43349.2" }, { 3*1000 + 8,	"43385.6" },
                    { 3*1000 + 9,	"43421.9" }, { 3*1000 + 10,	"43462.5" }, { 3*1000 + 11,	"43489.2" }, { 3*1000 + 12,	"43510.7" },
                    { 3*1000 + 13,	"43539.1" }, { 3*1000 + 14,	"43610.4" }, { 3*1000 + 15,	"43625.2" }, { 3*1000 + 16,	"43657.0" },
                    { 3*1000 + 17,	"43685.2" }, { 3*1000 + 18,	"43714.1" }, { 3*1000 + 19,	"43745.2" }, { 3*1000 + 20,	"43779.3" },
                    { 3*1000 + 21,	"43807.4" }, { 3*1000 + 22,	"43872.2" }, { 3*1000 + 23,	"43917.4" }, { 3*1000 + 24,	"43930.7" },
                    { 3*1000 + 25,	"43957.2" }, { 3*1000 + 26,	"43989.1" }, { 4*1000 + 1,	"44001.4" }, { 4*1000 + 2,	"44012.3" },
                    { 4*1000 + 3,	"44085.7" }, { 4*1000 + 4,	"44143.7" }, { 4*1000 + 5,	"44161.2" }, { 4*1000 + 6,	"44215.2" },
                    { 4*1000 + 7,	"44246.3" }, { 4*1000 + 8,	"44286.5" }, { 4*1000 + 9,	"44307.3" }, { 4*1000 + 10,	"44356.9" },
                    { 4*1000 + 11,	"44390.1" }, { 4*1000 + 12,	"44429.6" }, { 4*1000 + 13,	"44474.5" }, { 4*1000 + 14,	"44502.7" },
                    { 4*1000 + 15,	"44558.6" }, { 4*1000 + 16,	"44614.6" }, { 4*1000 + 17,	"44631.2" }, { 4*1000 + 18,	"44664.5" },
                    { 4*1000 + 19,	"44704.2" }, { 4*1000 + 20,	"44741.9" }, { 4*1000 + 21,	"44769.2" }, { 4*1000 + 22,	"44805.3" },
                    { 4*1000 + 23,	"44821.3" }, { 4*1000 + 24,	"44885.5" }, { 4*1000 + 25,	"44932.3" }, { 4*1000 + 26,	"44995.3" },
                    { 5*1000 + 1,	"45021.3" }, { 5*1000 + 2,	"45047.2" }, { 5*1000 + 3,	"45076.3" }, { 5*1000 + 4,	"45122.3" },
                    { 5*1000 + 5,	"45156.1" }, { 5*1000 + 6,	"45208.2" }, { 5*1000 + 7,	"45236.4" }, { 5*1000 + 8,	"45245.8" },
                    { 5*1000 + 9,	"45349.1" }, { 5*1000 + 10,	"45376.3" }, { 5*1000 + 11,	"45397.3" }, { 5*1000 + 12,	"45429.3" },
                    { 5*1000 + 13,	"45470.1" }, { 5*1000 + 14,	"45494.2" }, { 5*1000 + 15,	"45571.2" }, { 5*1000 + 16,	"45587.3" },
                    { 5*1000 + 17,	"45614.6" }, { 5*1000 + 18,	"45652.1" }, { 5*1000 + 19,	"45703.9" }, { 5*1000 + 20,	"45733.6" },
                    { 5*1000 + 21,	"45761.3" }, { 5*1000 + 22,	"45852.1" }, { 5*1000 + 23,	"45854.2" }, { 5*1000 + 24,	"45092.4" },
                    { 5*1000 + 25,	"45944.1" }, { 5*1000 + 26,	"45959.1" }, { 6*1000 + 1,	"46001.3" }, { 6*1000 + 2,	"46041.1" },
                    { 6*1000 + 3,	"46071.6" }, { 6*1000 + 4,	"46125.3" }, { 6*1000 + 5,	"46154.2" }, { 6*1000 + 6,	"46192.3" },
                    { 6*1000 + 7,	"46235.7" }, { 6*1000 + 8,	"46271.5" }, { 6*1000 + 9,	"46307.2" }, { 6*1000 + 10,	"46357.4" },
                    { 6*1000 + 11,	"46360.8" }, { 6*1000 + 12,	"46424.1" }, { 6*1000 + 13,	"46461.3" }, { 6*1000 + 14,	"46519.1" },
                    { 6*1000 + 15,	"46548.8" }, { 6*1000 + 16,	"46578.4" }, { 6*1000 + 17,	"46759.2" }, { 6*1000 + 18,	"46682.4" },
                    { 6*1000 + 19,	"46693.1" }, { 6*1000 + 20,	"46731.5" }, { 6*1000 + 21,	"46778.1" }, { 6*1000 + 22,	"46830.1" },
                    { 6*1000 + 23,	"46852.2" }, { 6*1000 + 24,	"46915.2" }, { 6*1000 + 25,	"46944.2" }, { 6*1000 + 26,	"46982.1" },
                    { 7*1000 + 1,	"47025.4" }, { 7*1000 + 2,	"47120.4" }, { 7*1000 + 3,	"47215.5" }, { 7*1000 + 4,	"47135.2" },
                    { 7*1000 + 5,	"47160.1" }, { 7*1000 + 6,	"47225.7" }, { 7*1000 + 7,	"47254.1" }, { 7*1000 + 8,	"47304.2" },
                    { 7*1000 + 9,	"47310.2" }, { 7*1000 + 10,	"47410.2" }, { 7*1000 + 11,	"47391.2" }, { 7*1000 + 12,	"47457.1" },
                    { 7*1000 + 13,	"47423.9" }, { 7*1000 + 14,	"47495.3" }, { 7*1000 + 15,	"47566.7" }, { 7*1000 + 16,	"47611.2" },
                    { 7*1000 + 17,	"47615.2" }, { 7*1000 + 18,	"47622.1" }, { 7*1000 + 19,	"47653.2" }, { 7*1000 + 20,	"47751.2" },
                    { 7*1000 + 21,	"47779.4" }, { 7*1000 + 22,	"47829.1" }, { 7*1000 + 23,	"47869.2" }, { 7*1000 + 24,	"47941.7" },
                    { 7*1000 + 25,	"47988.1" }, { 7*1000 + 26,	"47988.1" }
                };
        }
        
        /// <summary>
        /// Gets the stardates for Star Trek: Deep Space Nine.
        /// </summary>
        /// <remarks>81 stardates are interpolated.</remarks>
        /// <returns>Stardates for each episode.</returns>
        public static Dictionary<int, string> GetStarTrekDeepSpaceNineStardates()
        {
            return new Dictionary<int, string>
                {
                    { 1*1000 + 1,	"46379.1" }, { 1*1000 + 2,	"46392.7" }, { 1*1000 + 3,	"46407.1" }, { 1*1000 + 4,	"46421.5" },
                    { 1*1000 + 5,	"46423.7" }, { 1*1000 + 6,	"46477.4" }, { 1*1000 + 7,	"46531.2" }, { 1*1000 + 8,	"46910.1" },
                    { 1*1000 + 9,	"46879.9" }, { 1*1000 + 10,	"46849.8" }, { 1*1000 + 11,	"46819.6" }, { 1*1000 + 12,	"46789.4" },
                    { 1*1000 + 13,	"46759.3" }, { 1*1000 + 14,	"46729.1" }, { 1*1000 + 15,	"46844.3" }, { 1*1000 + 16,	"46853.2" },
                    { 1*1000 + 17,	"46925.1" }, { 1*1000 + 18,	"46922.3" }, { 1*1000 + 19,	"46965.6" }, { 1*1000 + 20,	"47008.9" },
                    { 2*1000 + 1,	"47052.2" }, { 2*1000 + 2,	"47095.5" }, { 2*1000 + 3,	"47138.8" }, { 2*1000 + 4,	"47182.1" },
                    { 2*1000 + 5,	"47177.2" }, { 2*1000 + 6,	"47229.1" }, { 2*1000 + 7,	"47255.8" }, { 2*1000 + 8,	"47282.5" },
                    { 2*1000 + 9,	"47329.4" }, { 2*1000 + 10,	"47391.2" }, { 2*1000 + 11,	"47391.4" }, { 2*1000 + 12,	"47391.7" },
                    { 2*1000 + 13,	"47471.9" }, { 2*1000 + 14,	"47552.1" }, { 2*1000 + 15,	"47573.1" }, { 2*1000 + 16,	"47603.3" },
                    { 2*1000 + 17,	"47641.2" }, { 2*1000 + 18,	"47679.1" }, { 2*1000 + 19,	"47716.9" }, { 2*1000 + 20,	"47754.8" },
                    { 2*1000 + 21,	"47792.7" }, { 2*1000 + 22,	"47830.6" }, { 2*1000 + 23,	"47868.4" }, { 2*1000 + 24,	"47906.3" },
                    { 2*1000 + 25,	"47944.2" }, { 2*1000 + 26,	"48078.6" }, { 3*1000 + 1,	"48213.1" }, { 3*1000 + 2,	"48213.1" },
                    { 3*1000 + 3,	"48224.2" }, { 3*1000 + 4,	"48231.7" }, { 3*1000 + 5,	"48244.5" }, { 3*1000 + 6,	"48301.1" },
                    { 3*1000 + 7,	"48388.8" }, { 3*1000 + 8,	"48423.2" }, { 3*1000 + 9,	"48467.3" }, { 3*1000 + 10,	"48441.6" },
                    { 3*1000 + 11,	"48481.2" }, { 3*1000 + 12,	"48481.2" }, { 3*1000 + 13,	"48498.4" }, { 3*1000 + 14,	"48521.5" },
                    { 3*1000 + 15,	"48543.2" }, { 3*1000 + 16,	"48555.5" }, { 3*1000 + 17,	"48576.7" }, { 3*1000 + 18,	"48592.2" },
                    { 3*1000 + 19,	"48601.1" }, { 3*1000 + 20,	"48620.3" }, { 3*1000 + 21,	"48622.5" }, { 3*1000 + 22,	"48699.9" },
                    { 3*1000 + 23,	"48731.2" }, { 3*1000 + 24,	"48764.8" }, { 3*1000 + 25,	"48959.1" }, { 3*1000 + 26,	"48962.5" },
                    { 4*1000 + 1,	"49011.4" }, { 4*1000 + 2,	"49011.4" }, { 4*1000 + 3,	"49037.7" }, { 4*1000 + 4,	"49066.5" },
                    { 4*1000 + 5,	"49122.4" }, { 4*1000 + 6,	"49195.5" }, { 4*1000 + 7,	"49263.5" }, { 4*1000 + 8,	"49201.3" },
                    { 4*1000 + 9,	"49263.5" }, { 4*1000 + 10,	"49300.7" }, { 4*1000 + 11,	"49170.0" }, { 4*1000 + 12,	"49482.3" },
                    { 4*1000 + 13,	"49517.3" }, { 4*1000 + 14,	"49534.2" }, { 4*1000 + 15,	"49556.2" }, { 4*1000 + 16,	"49565.1" },
                    { 4*1000 + 17,	"49600.7" }, { 4*1000 + 18,	"49665.3" }, { 4*1000 + 19,	"49680.5" }, { 4*1000 + 20,	"49699.1" },
                    { 4*1000 + 21,	"49702.2" }, { 4*1000 + 22,	"49729.8" }, { 4*1000 + 23,	"49904.2" }, { 4*1000 + 24,	"49909.7" },
                    { 4*1000 + 25,	"49930.3" }, { 4*1000 + 26,	"49962.4" }, { 5*1000 + 1,	"50005.9" }, { 5*1000 + 2,	"50049.3" },
                    { 5*1000 + 3,	"50090.1" }, { 5*1000 + 4,	"50130.8" }, { 5*1000 + 5,	"50171.6" }, { 5*1000 + 6,	"4523.7" },
                    { 5*1000 + 7,	"50253.1" }, { 5*1000 + 8,	"50293.9" }, { 5*1000 + 9,	"50334.7" }, { 5*1000 + 10,	"50375.4" },
                    { 5*1000 + 11,	"50416.2" }, { 5*1000 + 12,	"50450.7" }, { 5*1000 + 13,	"50485.2" }, { 5*1000 + 14,	"50524.7" },
                    { 5*1000 + 15,	"50564.2" }, { 5*1000 + 16,	"50601.3" }, { 5*1000 + 17,	"50638.3" }, { 5*1000 + 18,	"50675.4" },
                    { 5*1000 + 19,	"50712.5" }, { 5*1000 + 20,	"50746.4" }, { 5*1000 + 21,	"50780.3" }, { 5*1000 + 22,	"50814.2" },
                    { 5*1000 + 23,	"50854.4" }, { 5*1000 + 24,	"50894.7" }, { 5*1000 + 25,	"50934.9" }, { 5*1000 + 26,	"50975.2" },
                    { 6*1000 + 1,	"51041.2" }, { 6*1000 + 2,	"51107.2" }, { 6*1000 + 3,	"51128.3" }, { 6*1000 + 4,	"51149.5" },
                    { 6*1000 + 5,	"51182.2" }, { 6*1000 + 6,	"51214.8" }, { 6*1000 + 7,	"51247.5" }, { 6*1000 + 8,	"51289.0" },
                    { 6*1000 + 9,	"51330.6" }, { 6*1000 + 10,	"51372.1" }, { 6*1000 + 11,	"51413.6" }, { 6*1000 + 12,	"51433.8" },
                    { 6*1000 + 13,	"51454.0" }, { 6*1000 + 14,	"51474.2" }, { 6*1000 + 15,	"51535.7" }, { 6*1000 + 16,	"51597.2" },
                    { 6*1000 + 17,	"51638.6" }, { 6*1000 + 18,	"51679.9" }, { 6*1000 + 19,	"51721.3" }, { 6*1000 + 20,	"51756.0" },
                    { 6*1000 + 21,	"51790.7" }, { 6*1000 + 22,	"51825.4" }, { 6*1000 + 23,	"51866.4" }, { 6*1000 + 24,	"51907.3" },
                    { 6*1000 + 25,	"51948.3" }, { 6*1000 + 26,	"52016.4" }, { 7*1000 + 1,	"52084.5" }, { 7*1000 + 2,	"52152.6" },
                    { 7*1000 + 3,	"52163.0" }, { 7*1000 + 4,	"52173.4" }, { 7*1000 + 5,	"52183.8" }, { 7*1000 + 6,	"52194.1" },
                    { 7*1000 + 7,	"52204.5" }, { 7*1000 + 8,	"52214.9" }, { 7*1000 + 9,	"52225.3" }, { 7*1000 + 10,	"52235.7" },
                    { 7*1000 + 11,	"52284.3" }, { 7*1000 + 12,	"52333.0" }, { 7*1000 + 13,	"52381.6" }, { 7*1000 + 14,	"52430.3" },
                    { 7*1000 + 15,	"52478.9" }, { 7*1000 + 16,	"52527.6" }, { 7*1000 + 17,	"52576.2" }, { 7*1000 + 18,	"52587.8" },
                    { 7*1000 + 19,	"52599.4" }, { 7*1000 + 20,	"52610.9" }, { 7*1000 + 21,	"52622.5" }, { 7*1000 + 22,	"52634.1" },
                    { 7*1000 + 23,	"52645.7" }, { 7*1000 + 24,	"52861.3" }, { 7*1000 + 26,	"52973.8" }, { 7*1000 + 25,	"52973.8" }
                };
        }

        /// <summary>
        /// Gets the stardates for Star Trek: Voyager.
        /// </summary>
        /// <remarks>51 stardates are interpolated.</remarks>
        /// <returns>Stardates for each episode.</returns>
        public static Dictionary<int, string> GetStarTrekVoyagerStardates()
        {
            return new Dictionary<int, string>
                {
                    { 1*1000 + 1,	"48315.6" }, { 1*1000 + 2,	"48315.6" }, { 1*1000 + 3,	"48439.7" }, { 1*1000 + 4,	"48486.1" },
                    { 1*1000 + 5,	"48532.4" }, { 1*1000 + 6,	"48546.2" }, { 1*1000 + 7,	"48579.4" }, { 1*1000 + 8,	"48601.4" },
                    { 1*1000 + 9,	"48623.5" }, { 1*1000 + 10,	"48642.5" }, { 1*1000 + 11,	"48658.2" }, { 1*1000 + 12,	"48693.2" },
                    { 1*1000 + 13,	"48734.2" }, { 1*1000 + 14,	"48784.2" }, { 1*1000 + 15,	"48832.1" }, { 1*1000 + 16,	"48846.5" },
                    { 2*1000 + 1,	"48975.1" }, { 2*1000 + 2,	"49005.3" }, { 2*1000 + 3,	"48892.1" }, { 2*1000 + 4,	"48921.3" },
                    { 2*1000 + 5,	"49011.0" }, { 2*1000 + 6,	"49039.8" }, { 2*1000 + 7,	"49068.5" }, { 2*1000 + 8,	"49037.2" },
                    { 2*1000 + 9,	"49211.5" }, { 2*1000 + 10,	"49164.8" }, { 2*1000 + 11,	"48423.0" }, { 2*1000 + 12,	"48727.8" },
                    { 2*1000 + 13,	"49032.6" }, { 2*1000 + 14,	"49337.4" }, { 2*1000 + 15,	"49373.4" }, { 2*1000 + 16,	"49410.2" },
                    { 2*1000 + 17,	"49447.0" }, { 2*1000 + 18,	"49301.2" }, { 2*1000 + 19,	"49504.3" }, { 2*1000 + 20,	"49485.2" },
                    { 2*1000 + 21,	"49548.7" }, { 2*1000 + 22,	"49578.2" }, { 2*1000 + 23,	"49616.7" }, { 2*1000 + 24,	"49655.2" },
                    { 2*1000 + 25,	"49690.1" }, { 2*1000 + 26,	"49856.8" }, { 3*1000 + 1,	"50023.4" }, { 3*1000 + 2,	"50126.4" },
                    { 3*1000 + 3,	"50156.2" }, { 3*1000 + 4,	"50252.3" }, { 3*1000 + 5,	"50074.3" }, { 3*1000 + 6,	"50203.1" },
                    { 3*1000 + 7,	"50063.2" }, { 3*1000 + 8,	"50187.9" }, { 3*1000 + 9,	"50312.6" }, { 3*1000 + 10,	"50348.1" },
                    { 3*1000 + 11,	"50384.2" }, { 3*1000 + 12,	"50425.1" }, { 3*1000 + 13,	"50442.7" }, { 3*1000 + 14,	"50460.3" },
                    { 3*1000 + 15,	"50518.6" }, { 3*1000 + 16,	"50537.2" }, { 3*1000 + 17,	"50614.2" }, { 3*1000 + 18,	"50693.2" },
                    { 3*1000 + 19,	"50712.8" }, { 3*1000 + 20,	"50732.4" }, { 3*1000 + 21,	"50784.3" }, { 3*1000 + 22,	"50836.2" },
                    { 3*1000 + 23,	"50874.3" }, { 3*1000 + 24,	"50912.4" }, { 3*1000 + 25,	"50953.4" }, { 3*1000 + 26,	"50984.3" },
                    { 4*1000 + 1,	"51003.7" }, { 4*1000 + 2,	"51008.0" }, { 4*1000 + 3,	"51045.2" }, { 4*1000 + 4,	"51082.4" },
                    { 4*1000 + 5,	"51186.2" }, { 4*1000 + 6,	"51215.2" }, { 4*1000 + 7,	"51244.3" }, { 4*1000 + 8,	"51268.4" },
                    { 4*1000 + 9,	"51425.4" }, { 4*1000 + 10,	"51367.2" }, { 4*1000 + 11,	"51386.4" }, { 4*1000 + 12,	"51449.2" },
                    { 4*1000 + 13,	"51471.3" }, { 4*1000 + 14,	"51462.0" }, { 4*1000 + 15,	"51501.4" }, { 4*1000 + 16,	"51652.3" },
                    { 4*1000 + 17,	"51658.2" }, { 4*1000 + 18,	"51686.7" }, { 4*1000 + 19,	"51715.2" }, { 4*1000 + 20,	"51762.4" },
                    { 4*1000 + 21,	"51781.2" }, { 4*1000 + 22,	"51813.4" }, { 4*1000 + 23,	"51852.0" }, { 4*1000 + 24,	"51890.7" },
                    { 4*1000 + 25,	"51929.3" }, { 4*1000 + 26,	"51978.2" }, { 5*1000 + 1,	"52081.2" }, { 5*1000 + 2,	"52099.6" },
                    { 5*1000 + 3,	"52118.0" }, { 5*1000 + 4,	"52136.4" }, { 5*1000 + 5,	"52140.0" }, { 5*1000 + 6,	"52143.6" },
                    { 5*1000 + 7,	"52188.7" }, { 5*1000 + 8,	"52184.1" }, { 5*1000 + 9,	"52179.4" }, { 5*1000 + 10,	"52244.3" },
                    { 5*1000 + 11,	"52309.2" }, { 5*1000 + 12,	"52374.0" }, { 5*1000 + 13,	"52438.9" }, { 5*1000 + 14,	"52542.3" },
                    { 5*1000 + 15,	"52619.2" }, { 5*1000 + 16,	"52619.2" }, { 5*1000 + 17,	"52602.8" }, { 5*1000 + 18,	"52586.3" },
                    { 5*1000 + 19,	"52601.5" }, { 5*1000 + 20,	"52616.7" }, { 5*1000 + 21,	"52631.8" }, { 5*1000 + 22,	"52647.0" },
                    { 5*1000 + 23,	"52754.1" }, { 5*1000 + 24,	"52861.3" }, { 5*1000 + 25,	"52908.3" }, { 5*1000 + 26,	"52955.2" },
                    { 6*1000 + 1,	"53002.2" }, { 6*1000 + 2,	"53049.2" }, { 6*1000 + 3,	"53102.7" }, { 6*1000 + 4,	"53156.2" },
                    { 6*1000 + 5,	"53209.7" }, { 6*1000 + 6,	"53263.2" }, { 6*1000 + 7,	"53167.9" }, { 6*1000 + 8,	"53292.7" },
                    { 6*1000 + 9,	"53345.4" }, { 6*1000 + 10,	"53398.2" }, { 6*1000 + 11,	"53450.9" }, { 6*1000 + 12,	"53503.7" },
                    { 6*1000 + 13,	"53556.4" }, { 6*1000 + 14,	"53501.8" }, { 6*1000 + 15,	"53447.2" }, { 6*1000 + 16,	"53524.6" },
                    { 6*1000 + 17,	"53602.0" }, { 6*1000 + 18,	"53679.4" }, { 6*1000 + 19,	"53716.3" }, { 6*1000 + 20,	"53753.2" },
                    { 6*1000 + 21,	"53849.2" }, { 6*1000 + 22,	"53896.0" }, { 6*1000 + 23,	"53919.7" }, { 6*1000 + 24,	"53943.4" },
                    { 6*1000 + 25,	"53967.0" }, { 6*1000 + 26,	"53990.7" }, { 7*1000 + 1,	"54014.4" }, { 7*1000 + 2,	"54058.6" },
                    { 7*1000 + 3,	"54090.4" }, { 7*1000 + 4,	"54129.4" }, { 7*1000 + 5,	"54168.9" }, { 7*1000 + 6,	"54208.3" },
                    { 7*1000 + 7,	"54238.3" }, { 7*1000 + 8,	"54274.7" }, { 7*1000 + 10,	"54306.1" }, { 7*1000 + 9,	"54337.5" },
                    { 7*1000 + 11,	"54395.1" }, { 7*1000 + 12,	"54452.6" }, { 7*1000 + 13,	"54485.4" }, { 7*1000 + 14,	"54518.2" },
                    { 7*1000 + 15,	"54553.4" }, { 7*1000 + 16,	"54584.3" }, { 7*1000 + 17,	"54622.4" }, { 7*1000 + 18,	"54663.4" },
                    { 7*1000 + 19,	"54704.5" }, { 7*1000 + 20,	"54732.3" }, { 7*1000 + 21,	"54775.4" }, { 7*1000 + 22,	"54827.7" },
                    { 7*1000 + 23,	"54868.6" }, { 7*1000 + 24,	"54890.7" }, { 7*1000 + 25,	"54973.4" }, { 7*1000 + 26,	"54973.4" }
                };
        }
    }
}