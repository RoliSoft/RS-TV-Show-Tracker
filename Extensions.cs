namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// Provides extension methods for various third party classes.
    /// </summary>
    public static class Extensions
    {
        #region EventHandler<EventArgs<T1..T4>>
        /// <summary>
        /// Extension method to <c>EventHandler&lt;EventArgs&gt;</c> to fire an event.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        public static void Fire(this EventHandler<EventArgs> handler, object sender)
        {
            if (handler != null)
            {
                handler(sender, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Extension method to <c>EventHandler&lt;EventArgs&lt;T&gt;&gt;</c> to fire an event.
        /// </summary>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The data.</param>
        public static void Fire<T>(this EventHandler<EventArgs<T>> handler, object sender, T data)
        {
            if (handler != null)
            {
                handler(sender, new EventArgs<T>(data));
            }
        }

        /// <summary>
        /// Extension method to <c>EventHandler&lt;EventArgs&lt;T1, T2&gt;&gt;</c> to fire an event.
        /// </summary>
        /// <typeparam name="T1">The type of the first data.</typeparam>
        /// <typeparam name="T2">The type of the second data.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="first">The first data.</param>
        /// <param name="second">The second data.</param>
        public static void Fire<T1, T2>(this EventHandler<EventArgs<T1, T2>> handler, object sender, T1 first, T2 second)
        {
            if (handler != null)
            {
                handler(sender, new EventArgs<T1, T2>(first, second));
            }
        }

        /// <summary>
        /// Extension method to <c>EventHandler&lt;EventArgs&lt;T1, T2, T3&gt;&gt;</c> to fire an event.
        /// </summary>
        /// <typeparam name="T1">The type of the first data.</typeparam>
        /// <typeparam name="T2">The type of the second data.</typeparam>
        /// <typeparam name="T3">The type of the third data.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="first">The first data.</param>
        /// <param name="second">The second data.</param>
        /// <param name="third">The third data.</param>
        public static void Fire<T1, T2, T3>(this EventHandler<EventArgs<T1, T2, T3>> handler, object sender, T1 first, T2 second, T3 third)
        {
            if (handler != null)
            {
                handler(sender, new EventArgs<T1, T2, T3>(first, second, third));
            }
        }

        /// <summary>
        /// Extension method to <c>EventHandler&lt;EventArgs&lt;T1, T2, T3, T4&gt;&gt;</c> to fire an event.
        /// </summary>
        /// <typeparam name="T1">The type of the first data.</typeparam>
        /// <typeparam name="T2">The type of the second data.</typeparam>
        /// <typeparam name="T3">The type of the third data.</typeparam>
        /// <typeparam name="T4">The type of the fourth data.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="first">The first data.</param>
        /// <param name="second">The second data.</param>
        /// <param name="third">The third data.</param>
        /// <param name="fourth">The fourth data.</param>
        public static void Fire<T1, T2, T3, T4>(this EventHandler<EventArgs<T1, T2, T3, T4>> handler, object sender, T1 first, T2 second, T3 third, T4 fourth)
        {
            if (handler != null)
            {
                handler(sender, new EventArgs<T1, T2, T3, T4>(first, second, third, fourth));
            }
        }
        #endregion

        #region Enum
        /// <summary>
        /// Extension method to <c>Enum</c> to get an attribute to an item of the enumeration.
        /// </summary>
        /// <typeparam name="T">The attribute to get.</typeparam>
        /// <param name="obj">The enumeration item on which the attribute is present.</param>
        /// <returns>Attribute.</returns>
        public static T GetAttribute<T>(this Enum obj)
        {
            var attrs = obj.GetType().GetField(obj.ToString()).GetCustomAttributes(typeof(T), false);

            return attrs.Count() != 0
                   ? (T)attrs.First()
                   : default(T);
        }
        #endregion

        #region Object
        /// <summary>
        /// Extension method to <c>object</c> to get an attribute.
        /// </summary>
        /// <typeparam name="T">The attribute to get.</typeparam>
        /// <param name="obj">The object on which the attribute is present.</param>
        /// <returns>Attribute.</returns>
        public static T GetAttribute<T>(this object obj)
        {
            var attrs = obj.GetType().GetCustomAttributes(typeof(T), false);

            return attrs.Count() != 0
                   ? (T)attrs.First()
                   : default(T);
        }
        #endregion

        #region Type
        /// <summary>
        /// Extension method to Type to get the derived classes of a class.
        /// </summary>
        /// <param name="baseClass">The base class.</param>
        /// <returns>List of derived classes.</returns>
        public static IEnumerable<Type> GetDerivedTypes(this Type baseClass)
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.IsSubclassOf(baseClass));
        }
        #endregion

        #region List<T>
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
        #endregion

        #region ObservableCollection<T>
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
        #endregion

        #region DateTime
        /// <summary>
        /// Extension method to DateTime to translate the date into a relative date.
        /// </summary>
        /// <param name="nextdate">The next air.</param>
        /// <param name="detailed">if set to <c>true</c> a more descriptive text will be returned.</param>
        /// <returns>Relative date.</returns>
        public static string ToRelativeDate(this DateTime nextdate, bool detailed = false)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;

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

                if (cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return (detailed ? "This " : string.Empty) + nextdate.DayOfWeek + " at " + nextdate.ToString("h:mm tt");
                }

                if (cal.GetWeekOfYear(DateTime.Now.AddDays(7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "Next " + nextdate.DayOfWeek + " at " + nextdate.ToString("h:mm tt");
                }

                if (cal.GetWeekOfYear(DateTime.Now.AddDays(-7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "Last " + nextdate.DayOfWeek + " at " + nextdate.ToString("h:mm tt");
                }

                var weeks = cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (Math.Abs(weeks) == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : detailed ? " until air" : String.Empty);
            }
            else
            {
                var weeks = (cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) + 52 * (nextdate.Year - DateTime.Now.Year)) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (weeks == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : detailed ? " until air" : String.Empty);
            }
        }

        /// <summary>
        /// Extension method to DateTime to convert the date to local time zone.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="source">The time zone of the specified date.</param>
        /// <returns>DateTime in local timezone.</returns>
        public static DateTime ToLocalTimeZone(this DateTime date, string source = "Central Standard Time")
        {
            return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById(source), TimeZoneInfo.Local);
        }

        /// <summary>
        /// Extension method to DateTime to convert it into an Unix timestamp.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>Converted Unix timestamp.</returns>
        public static double ToUnixTimestamp(this DateTime date)
        {
            return Math.Floor((date - Utils.UnixEpoch).TotalSeconds);
        }
        #endregion

        #region TimeSpan
        /// <summary>
        /// Extension method to TimeSpan to convert its value to a relative string containing only one unit.
        /// </summary>
        /// <param name="ts">The time span.</param>
        /// <returns>Short relative time.</returns>
        public static string ToShortRelativeTime(this TimeSpan ts)
        {
            if (ts.Days >= 365)
            {
                return Utils.FormatNumber(ts.Days / 365, "year");
            }
            if (ts.Days >= 7)
            {
                return Utils.FormatNumber(ts.Days / 7, "week");
            }
            if (ts.Days >= 1)
            {
                return Utils.FormatNumber(ts.Days, "day");
            }
            if (ts.Hours >= 1)
            {
                return Utils.FormatNumber(ts.Hours, "hour");
            }
            return ts.Minutes >= 1
                   ? Utils.FormatNumber(ts.Minutes, "minute")
                   : Utils.FormatNumber(ts.Seconds, "second");
        }

        /// <summary>
        /// Extension method to TimeSpan to convert its value to a relative string containing all applicable units.
        /// </summary>
        /// <param name="ts">The time span.</param>
        /// <returns>Full relative time.</returns>
        public static string ToFullRelativeTime(this TimeSpan ts)
        {
            var time = ts.TotalSeconds;
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

            var ret = value
                      .Where(unit => unit.Value != 0)
                      .Aggregate(String.Empty, (current, unit) => current + (Utils.FormatNumber((int)unit.Value, unit.Key) + ", "));

            return Regex.Replace(ret.TrimEnd(", ".ToCharArray()), "(.+), ", "$1 and ");
        }
        #endregion

        #region Double
        /// <summary>
        /// Extension method to double to convert the Unix timestamp into a DateTime object.
        /// </summary>
        /// <param name="timestamp">The Unix timestamp.</param>
        /// <returns>Converted DateTime object.</returns>
        public static DateTime GetUnixTimestamp(this double timestamp)
        {
            return Utils.UnixEpoch.AddSeconds(timestamp);
        }
        #endregion

        #region String
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
        /// Extension method to string to convert it to an Int32.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>Int32 value of the string.</returns>
        public static int ToInteger(this string s)
        {
            return int.Parse(s);
        }

        /// <summary>
        /// Extension method to string to convert it to a double.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>Double value of the string.</returns>
        public static double ToDouble(this string s)
        {
            return double.Parse(s);
        }
        #endregion

        #region XContainer
        /// <summary>
        /// Extension method to XContainer to get the value of a tag or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="name">The name of the tag.</param>
        /// <returns>Value or null.</returns>
        public static string GetValue<T>(this T doc, string name) where T : XContainer
        {
            try   { return doc.Descendants(name).First().Value.Trim(); }
            catch { return null; }
        }
        #endregion
    }
}