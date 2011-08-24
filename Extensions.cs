namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using HtmlAgilityPack;

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

        #region Attribute
        /// <summary>
        /// Extension method to Attribute to get all the classes on which it is present.
        /// </summary>
        /// <param name="attr">The attribute.</param>
        /// <returns>List of marked classes.</returns>
        public static IEnumerable<Type> GetWithAttribute(this Type attr)
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.GetCustomAttributes(attr, false).Count() != 0);
        }

        /// <summary>
        /// Extension method to Attribute to get all the classes on which it is present.
        /// </summary>
        /// <param name="attr">The attribute.</param>
        /// <param name="filter">The filter method.</param>
        /// <returns>List of marked classes.</returns>
        public static IEnumerable<Type> GetWithAttribute<T>(this Type attr, Func<T, bool> filter) where T : Attribute
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.GetCustomAttributes(attr, false).Count() != 0 && filter((T)type.GetCustomAttributes(attr, false).First()));
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
                return Math.Abs(weeks) + " week" + (Math.Abs(weeks) == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : detailed ? " until airs" : String.Empty);
            }
            else
            {
                var weeks = (cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) + 52 * (nextdate.Year - DateTime.Now.Year)) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (weeks == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : detailed ? " until airs" : String.Empty);
            }
        }

        /// <summary>
        /// Extension method to DateTime to translate the date into a short relative date.
        /// </summary>
        /// <param name="nextdate">The next air.</param>
        /// <returns>Short relative date.</returns>
        public static string ToShortRelativeDate(this DateTime nextdate)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;

            if (DateTime.Now.Year == nextdate.Year)
            {
                if (DateTime.Now.ToShortDateString() == nextdate.ToShortDateString())
                {
                    return "Today";
                }

                if (DateTime.Now.AddDays(1).ToShortDateString() == nextdate.ToShortDateString())
                {
                    return "Tomorrow";
                }

                if (DateTime.Now.AddDays(-1).ToShortDateString() == nextdate.ToShortDateString())
                {
                    return "Yesterday";
                }

                if (cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "This week";
                }

                if (cal.GetWeekOfYear(DateTime.Now.AddDays(7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "Next week";
                }

                if (cal.GetWeekOfYear(DateTime.Now.AddDays(-7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return "Last week";
                }

                var weeks = cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (Math.Abs(weeks) == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : " until airs");
            }
            else
            {
                var weeks = (cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) + 52 * (nextdate.Year - DateTime.Now.Year)) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) + " week" + (weeks == 1 ? String.Empty : "s") + (weeks < 0 ? " ago" : " until airs");
            }
        }

        /// <summary>
        /// Extension method to DateTime to translate the date into a number representing how far it is from now.
        /// </summary>
        /// <param name="nextdate">The next air.</param>
        /// <returns>Priority of the relative date.</returns>
        public static int ToRelativeDatePriority(this DateTime nextdate)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;

            if (DateTime.Now.Year == nextdate.Year)
            {
                if (DateTime.Now.ToShortDateString() == nextdate.ToShortDateString())
                {
                    return 0;
                }

                if (DateTime.Now.AddDays(1).ToShortDateString() == nextdate.ToShortDateString())
                {
                    return 1;
                }

                if (DateTime.Now.AddDays(-1).ToShortDateString() == nextdate.ToShortDateString())
                {
                    return -1;
                }

                if (cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return ((int)nextdate.DayOfWeek + 1) * 10;
                }

                if (cal.GetWeekOfYear(DateTime.Now.AddDays(7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return ((int)nextdate.DayOfWeek + 1) * 100;
                }

                if (cal.GetWeekOfYear(DateTime.Now.AddDays(-7), CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                {
                    return ((int)nextdate.DayOfWeek + 1) * -10;
                }

                var weeks = cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) * 1000;
            }
            else
            {
                var weeks = (cal.GetWeekOfYear(nextdate, CalendarWeekRule.FirstDay, DayOfWeek.Monday) + 52 * (nextdate.Year - DateTime.Now.Year)) - cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return Math.Abs(weeks) * 1000;
            }
        }

        /// <summary>
        /// Extension method to DateTime to convert the date to local time zone.
        /// If the "Convert Timezone" setting is false, the function won't convert.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="source">The time zone of the specified date.</param>
        /// <returns>DateTime in local timezone.</returns>
        public static DateTime ToLocalTimeZone(this DateTime date, string source)
        {
            if (!Settings.Get("Convert Timezone", true))
            {
                return date;
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                source = "Central Standard Time";
            }

            if (source.StartsWith("GMT"))
            {
                var offset = Regex.Match(source, @"(\-?\d{1,2})").Groups[1].Value.ToInteger();
                return TimeZoneInfo.ConvertTime(date.AddHours(offset * -1), TimeZoneInfo.Utc, TimeZoneInfo.Local);
            }
            else
            {
                return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById(source), TimeZoneInfo.Local);
            }
        }

        /// <summary>
        /// Extension method to DateTime to convert the date back to its original time zone.
        /// If the "Convert Timezone" setting is false, the function won't convert.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="source">The time zone of the specified date.</param>
        /// <returns>DateTime in original timezone.</returns>
        public static DateTime ToOriginalTimeZone(this DateTime date, string source)
        {
            if (!Settings.Get("Convert Timezone", true))
            {
                return date;
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                source = "Central Standard Time";
            }

            if (source.StartsWith("GMT"))
            {
                var offset = Regex.Match(source, @"(\-?\d{1,2})").Groups[1].Value.ToInteger();
                return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Local, TimeZoneInfo.Utc).AddHours(offset);
            }
            else
            {
                return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Local, TimeZoneInfo.FindSystemTimeZoneById(source));
            }
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
        /// Extension method to string to remove any excessive whitespace.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <param name="minify">if set to <c>true</c> the whitespace will be replaced to nothing instead of just 1 space.</param>
        /// <returns>String without excessive whitespace.</returns>
        public static string TrimAll(this string value, bool minify = true)
        {
            return Regex.Replace(value.Trim(), @"\s+", minify ? string.Empty : " ", RegexOptions.Singleline).Trim();
        }

        /// <summary>
        /// Extension method to string to uppercase the first letter.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>String with uppercased first letter.</returns>
        public static string ToUppercaseFirst(this string value)
        {
            return char.ToUpper(value[0]) + value.Substring(1);
        }

        /// <summary>
        /// Extension method to string to uppercase the first letter of each word.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>String with uppercased first letters.</returns>
        public static string ToUppercaseWords(this string value)
        {
            var array = value.ToCharArray();

            if (array.Length >= 1)
            {
                if (char.IsLower(array[0]))
                {
                    array[0] = char.ToUpper(array[0]);
                }
            }

            for (var i = 1; i < array.Length; i++)
            {
                if (array[i - 1] == ' ')
                {
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
            }

            return new string(array);
        }

        /// <summary>
        /// Extension method to string to alias the <c>string.Format()</c> method.
        /// </summary>
        /// <param name="format">The string to be formatted.</param>
        /// <param name="args">The arguments present in the string.</param>
        /// <returns>Formatted string.</returns>
        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        /// <summary>
        /// Extension method to string to cut it if it's longer then the specified length.
        /// </summary>
        /// <param name="value">The string to be cut.</param>
        /// <param name="len">The maximum length of the string.</param>
        /// <returns>Resized string.</returns>
        public static string CutIfLonger(this string value, int len)
        {
            return value.Length <= len
                   ? value
                   : value.Substring(0, len - 1) + "…";
        }

        /// <summary>
        /// Extension method to string to remove any accents from a string.
        /// </summary>
        /// <param name="value">The string to be transliterated.</param>
        /// <returns>ASCII string.</returns>
        public static string Transliterate(this string value)
        {
            return new string(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Extension method to string to determine whether the specified string is null, empty or whitespace.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if string is null, empty or whitespace; otherwise, <c>false</c>.</returns>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Extension method to string to convert it to an Int32.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>Int32 value of the string.</returns>
        public static int ToInteger(this string value)
        {
            return int.Parse(value);
        }

        /// <summary>
        /// Extension method to string to convert it to an Int64.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>Int64 value of the string.</returns>
        public static long ToLong(this string value)
        {
            return long.Parse(value);
        }

        /// <summary>
        /// Extension method to string to convert it to a double.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>Double value of the string.</returns>
        public static double ToDouble(this string value)
        {
            return double.Parse(value);
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

        /// <summary>
        /// Extension method to XContainer to get the value of an attribute or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="name">The name of the tag.</param>
        /// <param name="attribute">The name of the attribute.</param>
        /// <returns>Value or null.</returns>
        public static string GetAttributeValue<T>(this T doc, string name, string attribute) where T : XContainer
        {
            try   { return doc.Descendants(name).First().Attribute(attribute).Value.Trim(); }
            catch { return null; }
        }
        #endregion

        #region HtmlNode
        /// <summary>
        /// Extension method to HtmlNode to get the value of a tag or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="xpath">The xpath to the tag.</param>
        /// <returns>Value or null.</returns>
        public static string GetTextValue(this HtmlNode doc, string xpath)
        {
            try   { return doc.SelectSingleNode(xpath).InnerText; }
            catch { return null; }
        }

        /// <summary>
        /// Extension method to HtmlNode to get the value of a tag or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="xpath">The xpath to the tag.</param>
        /// <returns>Value or null.</returns>
        public static string GetHtmlValue(this HtmlNode doc, string xpath)
        {
            try   { return doc.SelectSingleNode(xpath).InnerHtml; }
            catch { return null; }
        }

        /// <summary>
        /// Extension method to HtmlNode to get the attribute of a tag or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="attribute">The name of the attribute.</param>
        /// <returns>Value or null.</returns>
        public static string GetAttributeValue(this HtmlNode doc, string attribute)
        {
            try   { return doc.GetAttributeValue(attribute, string.Empty); }
            catch { return null; }
        }

        /// <summary>
        /// Extension method to HtmlNode to get the attribute of a tag or null if it doesn't exist.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="xpath">The xpath to the tag.</param>
        /// <param name="attribute">The name of the attribute.</param>
        /// <returns>Value or null.</returns>
        public static string GetNodeAttributeValue(this HtmlNode doc, string xpath, string attribute)
        {
            try   { return doc.SelectSingleNode(xpath).GetAttributeValue(attribute, string.Empty); }
            catch { return null; }
        }
        #endregion
    }
}