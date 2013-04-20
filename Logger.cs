namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// An event handler for new log messages.
    /// </summary>
    /// <param name="log">The message.</param>
    public delegate void LogMessageHandler(Log.LogItem log);

    /// <summary>
    /// Provides support for primitive logging.
    /// </summary>
    public static class Log
    {
        #region level(msg)
        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Trace(string message, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Trace, message, file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Debug, message, file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Info, message, file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Warn(string message, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Warn, message, file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Error(string message, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Error, message, file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Fatal(string message, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Fatal, message, file, method, line);
        }
        #endregion

        #region level(bytes)
        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="bytes">The byte array to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Trace(byte[] bytes, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Trace, bytes.HexDump(), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="bytes">The byte array to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Debug(byte[] bytes, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Debug, bytes.HexDump(), file, method, line);
        }
        #endregion

        #region level(object)
        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="obj">The object to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Trace(object obj, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Trace, obj.ObjDump(), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="obj">The object to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Debug(object obj, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Debug, obj.ObjDump(), file, method, line);
        }
        #endregion

        #region level(msg, ex)
        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The associated exception.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Trace(string message, Exception exception, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Trace, message + Environment.NewLine + ParseException(exception), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The associated exception.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Debug(string message, Exception exception, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Debug, message + Environment.NewLine + ParseException(exception), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The associated exception.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Info(string message, Exception exception, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Info, message + Environment.NewLine + ParseException(exception), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The associated exception.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Warn(string message, Exception exception, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Warn, message + Environment.NewLine + ParseException(exception), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The associated exception.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Error(string message, Exception exception, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Error, message + Environment.NewLine + ParseException(exception), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The associated exception.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Fatal(string message, Exception exception, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Fatal, message + Environment.NewLine + ParseException(exception), file, method, line);
        }
        #endregion

        #region level(msg, args)
        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="message">The message format to log.</param>
        /// <param name="fmtargs">The arguments to use to format the message.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Trace(string message, object[] fmtargs, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Trace, string.Format(message, fmtargs), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="message">The message format to log.</param>
        /// <param name="fmtargs">The arguments to use to format the message.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Debug(string message, object[] fmtargs, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Debug, string.Format(message, fmtargs), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message format to log.</param>
        /// <param name="fmtargs">The arguments to use to format the message.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Info(string message, object[] fmtargs, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Info, string.Format(message, fmtargs), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="message">The message format to log.</param>
        /// <param name="fmtargs">The arguments to use to format the message.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Warn(string message, object[] fmtargs, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Warn, string.Format(message, fmtargs), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="message">The message format to log.</param>
        /// <param name="fmtargs">The arguments to use to format the message.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Error(string message, object[] fmtargs, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Error, string.Format(message, fmtargs), file, method, line);
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message format to log.</param>
        /// <param name="fmtargs">The arguments to use to format the message.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Fatal(string message, object[] fmtargs, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Write(Level.Fatal, string.Format(message, fmtargs), file, method, line);
        }
        #endregion

        #region level(msgfunc)
        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="messagefunc">The function to call to get the message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Trace(Func<string> messagefunc, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                Write(Level.Trace, messagefunc(), file, method, line);
            }
            catch (Exception ex)
            {
                Warn("Exception during the evaluation of the message callback function.", ex, file, method, line);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="messagefunc">The function to call to get the message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        //[Conditional("DEBUG")]
        public static void Debug(Func<string> messagefunc, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                Write(Level.Debug, messagefunc(), file, method, line);
            }
            catch (Exception ex)
            {
                Warn("Exception during the evaluation of the message callback function.", ex, file, method, line);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="messagefunc">The function to call to get the message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Info(Func<string> messagefunc, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                Write(Level.Info, messagefunc(), file, method, line);
            }
            catch (Exception ex)
            {
                Warn("Exception during the evaluation of the message callback function.", ex, file, method, line);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="messagefunc">The function to call to get the message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Warn(Func<string> messagefunc, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                Write(Level.Warn, messagefunc(), file, method, line);
            }
            catch (Exception ex)
            {
                Warn("Exception during the evaluation of the message callback function.", ex, file, method, line);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="messagefunc">The function to call to get the message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Error(Func<string> messagefunc, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                Write(Level.Error, messagefunc(), file, method, line);
            }
            catch (Exception ex)
            {
                Warn("Exception during the evaluation of the message callback function.", ex, file, method, line);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="messagefunc">The function to call to get the message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Fatal(Func<string> messagefunc, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                Write(Level.Fatal, messagefunc(), file, method, line);
            }
            catch (Exception ex)
            {
                Warn("Exception during the evaluation of the message callback function.", ex, file, method, line);
            }
        }
        #endregion

        #region assert(...)
        /// <summary>
        /// Calls the specified method and logs the result.
        /// </summary>
        /// <param name="assertion">A method which will return a boolean indicating whether the assertion was successful.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Assert(Func<bool> assertion, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            bool result;

            try
            {
                result = assertion();
            }
            catch (Exception ex)
            {
                Write(Level.Warn, "Assertion threw an unexpected exception:" + Environment.NewLine + ParseException(ex), file, method, line);
                return;
            }

            if (result)
            {
                Write(Level.Debug, "Assertion successful.", file, method, line);
            }
            else
            {
                Write(Level.Warn, "Assertion failed.", file, method, line);
            }
        }

        /// <summary>
        /// Logs the assertion result.
        /// </summary>
        /// <param name="result">A boolean indicating whether the assertion was successful.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        public static void Assert(bool result, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            if (result)
            {
                Write(Level.Debug, "Assertion successful.", file, method, line);
            }
            else
            {
                Write(Level.Warn, "Assertion failed.", file, method, line);
            }
        }
        #endregion

        /// <summary>
        /// Gets or sets the current logging level.
        /// </summary>
        /// <value>The current logging level.</value>
        public static Level LoggingLevel = Level.Debug;

        /// <summary>
        /// Occurs when a new message was added to the log.
        /// </summary>
        public static event LogMessageHandler NewMessage;

        /// <summary>
        /// Gets or sets the message container.
        /// </summary>
        /// <value>
        /// The message container.
        /// </value>
        public static ConcurrentBag<LogItem> Messages { get; set; }

        /// <summary>
        /// Initializes the <see cref="Log" /> class.
        /// </summary>
        static Log()
        {
            Messages = new ConcurrentBag<LogItem>();
        }

        /// <summary>
        /// Writes the specified diagnostic message to the log.
        /// </summary>
        /// <param name="level">The weight of the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="file">The file where this message originates from.</param>
        /// <param name="method">The method where this message originates from.</param>
        /// <param name="line">The line where this message originates from.</param>
        private static void Write(Level level, string message, string file, string method, int line)
        {
            if (level > LoggingLevel) return;

            new Thread(() =>
                {
                    var log = new LogItem(DateTime.Now, level, file, method, line, message);

                    Messages.Add(log);

                    if (NewMessage != null)
                    {
                        NewMessage(log);
                    }
                }).Start();
        }

        /// <summary>
        /// Dumps the contents of the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>
        /// The message and the stacktrace of the exceptions.
        /// </returns>
        private static string ParseException(Exception exception)
        {
            var sb = new StringBuilder();

        parseException:
            sb.AppendLine(exception.GetType() + ": " + exception.Message);
            sb.AppendLine(exception.StackTrace);

            if (exception.InnerException != null)
            {
                exception = exception.InnerException;
                goto parseException;
            }

            return sb.ToString();
        }

        /// <summary>
        /// The weight of the message.
        /// </summary>
        public enum Level : int
        {
            None  = 0,
            Fatal = 1,
            Error = 1 << 1,
            Warn  = 1 << 2,
            Info  = 1 << 3,
            Debug = 1 << 4,
            Trace = 1 << 5
        }

        /// <summary>
        /// Represents a log message.
        /// </summary>
        public class LogItem
        {
            /// <summary>
            /// Gets or sets the time when the logged message occurred.
            /// </summary>
            /// <value>
            /// The time when the logged message occurred.
            /// </value>
            public readonly DateTime Time;

            /// <summary>
            /// Gets or sets the weight of the logged message.
            /// </summary>
            /// <value>
            /// The weight of the logged message.
            /// </value>
            public readonly Level Level;

            /// <summary>
            /// Gets or sets the file where the logged message occurred.
            /// </summary>
            /// <value>
            /// The file where the logged message occurred.
            /// </value>
            public readonly string File;

            /// <summary>
            /// Gets or sets the method in which the logged message occurred.
            /// </summary>
            /// <value>
            /// The method in which the logged message occurred.
            /// </value>
            public readonly string Method;

            /// <summary>
            /// Gets or sets the line where the logged message occurred.
            /// </summary>
            /// <value>
            /// The line where the logged message occurred.
            /// </value>
            public readonly int Line;

            /// <summary>
            /// Gets or sets the logged message.
            /// </summary>
            /// <value>
            /// The logged message.
            /// </value>
            public readonly string Message;

            /// <summary>
            /// Initializes a new instance of the <see cref="LogItem" /> class.
            /// </summary>
            /// <param name="time">The time when the logged message occurred.</param>
            /// <param name="level">The weight of the logged message.</param>
            /// <param name="file">The file where the logged message occurred.</param>
            /// <param name="method">The method in which the logged message occurred.</param>
            /// <param name="line">The line where the logged message occurred.</param>
            /// <param name="message">The logged message.</param>
            public LogItem(DateTime time, Level level, string file, string method, int line, string message)
            {
                Time    = time;
                Level   = level;
                File    = file;
                Method  = method;
                Line    = line;
                Message = message;
            }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return string.Format("{0:HH:mm:ss.fff} {1} {2}/{3}():{4} - {5}{6}", Time, Level.ToString().ToUpper(), Path.GetFileName(File), Method, Line, Message, Environment.NewLine);
            }
        }
    }
}
