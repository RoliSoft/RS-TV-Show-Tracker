namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using HtmlAgilityPack;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Extension method to <c>List&lt;T&gt;</c> to move an item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="old">The old index.</param>
        /// <param name="idx">The new index.</param>
        public static void Move<T>(this List<T> list, int old, int idx)
        {
            var tmp = list[idx];
            list[idx] = list[old];
            list[old] = tmp;
        }

        /// <summary>
        /// Moves the specified item up in the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="item">The item's index.</param>
        public static void MoveUp<T>(this List<T> list, int item)
        {
            Move(list, item, item - 1);
        }

        /// <summary>
        /// Moves the specified item down in the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="item">The item's index.</param>
        public static void MoveDown<T>(this List<T> list, int item)
        {
            Move(list, item, item + 1);
        }

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
        /// Extension method to DateTime to translate the next air date into a relative date.
        /// </summary>
        /// <param name="nextdate">The next air.</param>
        /// <param name="detailed">if set to <c>true</c> a more descriptive text will be returned.</param>
        /// <returns>Relative date.</returns>
        public static string NextAir(this DateTime nextdate, bool detailed = false)
        {
            if (DateTime.Now.Year == nextdate.Year)
            {
                if (DateTime.Now.ToShortDateString() == nextdate.ToShortDateString())
                {
                    return "Today at " + nextdate.ToString("h:mm tt");
                }

                if (DateTime.Now.AddDays(1).ToShortDateString() == nextdate.ToShortDateString())
                {
                    return "Tomorrow at " + nextdate.ToString("h:mm tt");
                }

                if (DateTime.Now.AddDays(-1).ToShortDateString() == nextdate.ToShortDateString())
                {
                    return "Yesterday at " + nextdate.ToString("h:mm tt");
                }

                if (CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return (detailed ? "This " : string.Empty) + nextdate.DayOfWeek + " at " + nextdate.ToString("h:mm tt");
                }

                if (CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now.AddDays(7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "Next " + nextdate.DayOfWeek + " at " + nextdate.ToString("h:mm tt");
                }

                if (CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now.AddDays(-7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "Last " + nextdate.DayOfWeek + " at " + nextdate.ToString("h:mm tt");
                }

                var weeks = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) - CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (Math.Abs(weeks) == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : detailed ? " until air" : String.Empty);
            }
            else
            {
                var weeks = (CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) + 52 * (nextdate.Year - DateTime.Now.Year)) - CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (weeks == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : detailed ? " until air" : String.Empty);
            }
        }

        /// <summary>
        /// Converts an Unix timestamp into a DateTime object.
        /// </summary>
        /// <param name="timestamp">The Unix timestamp.</param>
        /// <returns>Converted DateTime object.</returns>
        public static DateTime DateTimeFromUnix(double timestamp)
        {
            return UnixEpoch.AddSeconds(timestamp);
        }

        /// <summary>
        /// Converts a DateTime object into an Unix timestamp.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>Converted Unix timestamp.</returns>
        public static double DateTimeToUnix(DateTime date)
        {
            return Math.Floor((date - UnixEpoch).TotalSeconds);
        }

        /// <summary>
        /// Extension method to DateTime to convert the date to local time zone.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="source">The time zone of the specified date.</param>
        /// <returns></returns>
        public static DateTime ToLocalTimeZone(this DateTime date, string source = "Central Standard Time")
        {
            return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById(source), TimeZoneInfo.Local);
        }

        /// <summary>
        /// Extension method to TimeSpan to convert its value to a relative string.
        /// </summary>
        /// <param name="ts">The time span.</param>
        /// <returns>Relative time.</returns>
        public static string ToRelativeTime(this TimeSpan ts)
        {
            if (ts.Days >= 365)
            {
                return FormatNumber(ts.Days / 365, "year");
            }
            if (ts.Days >= 7)
            {
                return FormatNumber(ts.Days / 7, "week");
            }
            if (ts.Days >= 1)
            {
                return FormatNumber(ts.Days, "day");
            }
            if (ts.Hours >= 1)
            {
                return FormatNumber(ts.Hours, "hour");
            }
            return ts.Minutes >= 1
                   ? FormatNumber(ts.Minutes, "minute")
                   : FormatNumber(ts.Seconds, "second");
        }

        /// <summary>
        /// Extension method to TimeSpan to convert its value into a user friendly representation.
        /// </summary>
        /// <param name="ts">The time span.</param>
        /// <returns>User friendly total time.</returns>
        public static string ToTotalTime(this TimeSpan ts)
        {
            var time  = ts.TotalSeconds;
            var value = new Dictionary<string, double>
                {
                    { "year",   0 },
                    { "month",  0 },
                    { "week",   0 },
                    { "day",    0 },
                    { "hour",   0 },
                    { "minute", 0 },
                    { "second", 0 }
                };

            if (time >= 31556926)
            {
                value["year"] = Math.Floor(time / 31556926);
                time %= 31556926;
            }

            if (time >= 2592000)
            {
                value["month"] = Math.Floor(time / 2592000);
                time %= 2592000;
            }

            if (time >= 604800)
            {
                value["week"] = Math.Floor(time / 604800);
                time %= 604800;
            }

            if (time >= 86400)
            {
                value["day"] = Math.Floor(time / 86400);
                time %= 86400;
            }

            if (time >= 3600)
            {
                value["hour"] = Math.Floor(time / 3600);
                time %= 3600;
            }

            if (time >= 60)
            {
                value["minute"] = Math.Floor(time / 60);
                time %= 60;
            }

            value["second"] = Math.Floor(time);

            var ret = value.Where(unit => unit.Value != 0).Aggregate(String.Empty, (current, unit) => current + (FormatNumber((int)unit.Value, unit.Key) + ", "));
            return Regex.Replace(ret.TrimEnd(", ".ToCharArray()), "(.+), ", "$1 and ");
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
        /// Extension method to string to uppercase the first letter.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>String with uppercased first letter.</returns>
        public static string ToUppercaseFirst(this string s)
        {
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Extension method to XContainer to get the value of a tag or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="name">The name of the tag.</param>
        /// <returns>Value or null.</returns>
        public static string GetValue<T>(this T doc, string name) where T : XContainer
        {
            try
            {
                return doc.Descendants(name).First().Value.Trim();
            }
            catch
            {
                return null;
            }
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
        public static string GetUID()
        {
            var uid = Database.Setting("uid");

            if (string.IsNullOrWhiteSpace(uid))
            {
                uid = Guid.NewGuid().ToString();
                Database.Setting("uid", uid);
            }

            return uid;
        }

        /// <summary>
        /// Extension method to Type to get the derived classes of a class.
        /// </summary>
        /// <param name="baseClass">The base class.</param>
        /// <returns>List of derived classes.</returns>
        public static IEnumerable<Type> GetDerivedTypes(this Type baseClass)
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.IsSubclassOf(baseClass));
        }

        /// <summary>
        /// Extension method to ObservableCollection to add support for AddRange.
        /// </summary>
        /// <typeparam name="T">Type of the collection items.</typeparam>
        /// <param name="oc">The observable collection.</param>
        /// <param name="collection">The collection to insert.</param>
        public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                oc.Add(item);
            }
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
