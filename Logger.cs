namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Provides methods for logging.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets or sets a value indicating whether the logging of trace messages is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the logging of trace messages is enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsTraceEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the logging of debug messages is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the logging of debug messages is enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Trace(string message)
        {
            if (IsTraceEnabled)
            {
                Write(Level.Trace, message);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Trace(string message, params object[] args)
        {
            if (IsTraceEnabled)
            {
                Write(Level.Trace, string.Format(message, args));
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception.</param>
        public static void Trace(string message, Exception exception)
        {
            if (IsTraceEnabled)
            {
                Write(Level.Trace, message + Environment.NewLine + ParseException(exception));
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Trace level.
        /// </summary>
        /// <param name="getMessage">A method which returns the message to log when called.</param>
        public static void Trace(Func<string> getMessage)
        {
            if (IsTraceEnabled)
            {
                Write(Level.Trace, getMessage());
            }
        }


        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Debug(string message)
        {
            if (IsDebugEnabled)
            {
                Write(Level.Debug, message);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Debug(string message, params object[] args)
        {
            if (IsDebugEnabled)
            {
                Write(Level.Debug, string.Format(message, args));
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Debug level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception.</param>
        public static void Debug(string message, Exception exception)
        {
            if (IsDebugEnabled)
            {
                Write(Level.Debug, message + Environment.NewLine + ParseException(exception));
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="getMessage">A method which returns the message to log when called.</param>
        public static void Debug(Func<string> getMessage)
        {
            if (IsDebugEnabled)
            {
                Write(Level.Debug, getMessage());
            }
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message)
        {
            Write(Level.Info, message);
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Info(string message, params object[] args)
        {
            Write(Level.Info, string.Format(message, args));
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception.</param>
        public static void Info(string message, Exception exception)
        {
            Write(Level.Info, message + Environment.NewLine + ParseException(exception));
        }

        /// <summary>
        /// Writes the diagnostic message at Info level.
        /// </summary>
        /// <param name="getMessage">A method which returns the message to log when called.</param>
        public static void Info(Func<string> getMessage)
        {
            Write(Level.Info, getMessage());
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Warn(string message)
        {
            Write(Level.Warn, message);
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Warn(string message, params object[] args)
        {
            Write(Level.Warn, string.Format(message, args));
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception.</param>
        public static void Warn(string message, Exception exception)
        {
            Write(Level.Warn, message + Environment.NewLine + ParseException(exception));
        }

        /// <summary>
        /// Writes the diagnostic message at Warn level.
        /// </summary>
        /// <param name="getMessage">A method which returns the message to log when called.</param>
        public static void Warn(Func<string> getMessage)
        {
            Write(Level.Warn, getMessage());
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Error(string message)
        {
            Write(Level.Error, message);
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Error(string message, params object[] args)
        {
            Write(Level.Error, string.Format(message, args));
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception.</param>
        public static void Error(string message, Exception exception)
        {
            Write(Level.Error, message + Environment.NewLine + ParseException(exception));
        }

        /// <summary>
        /// Writes the diagnostic message at Error level.
        /// </summary>
        /// <param name="getMessage">A method which returns the message to log when called.</param>
        public static void Error(Func<string> getMessage)
        {
            Write(Level.Error, getMessage());
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Fatal(string message)
        {
            Write(Level.Fatal, message);
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Fatal(string message, params object[] args)
        {
            Write(Level.Fatal, string.Format(message, args));
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception.</param>
        public static void Fatal(string message, Exception exception)
        {
            Write(Level.Fatal, message + Environment.NewLine + ParseException(exception));
        }

        /// <summary>
        /// Writes the diagnostic message at Fatal level.
        /// </summary>
        /// <param name="getMessage">A method which returns the message to log when called.</param>
        public static void Fatal(Func<string> getMessage)
        {
            Write(Level.Fatal, getMessage());
        }

        /// <summary>
        /// Calls the specified method and logs it if it returns false or throws an exception.
        /// </summary>
        /// <param name="assertion">A method which will return a boolean indicating whether the assertion was successful.</param>
        public static void Assert(Func<bool> assertion)
        {
            bool result;

            try
            {
                result = assertion();
            }
            catch (Exception ex)
            {
                Write(Level.Warn, "Assertion thrown an unexpected exception:" + Environment.NewLine + ParseException(ex));
                return;
            }

            if (result)
            {
                Write(Level.Debug, "Assertion successful.");
            }
            else
            {
                Write(Level.Warn, "Assertion failed.");
            }
        }

        /// <summary>
        /// Creates a log entry when the specified argument is false.
        /// </summary>
        /// <param name="result">A boolean indicating whether the assertion was successful.</param>
        public static void Assert(bool result)
        {
            if (result)
            {
                Write(Level.Debug, "Assertion successful.");
            }
            else
            {
                Write(Level.Warn, "Assertion failed.");
            }
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
        /// Writes the specified messages to the log.
        /// </summary>
        /// <param name="level">The weight of the message.</param>
        /// <param name="message">The message to log.</param>
        private static void Write(Level level, string message)
        {
            var frm = new StackTrace(2, true).GetFrame(0);
            var log = string.Format("[{0:yyyy-MM-dd HH:mm:ss} / {1}] {2}:{3} / {4}.{5}(): {6}", DateTime.Now, level.ToString().ToUpper(), Path.GetFileName(frm.GetFileName()), frm.GetFileLineNumber(), frm.GetMethod().DeclaringType.FullName, frm.GetMethod().Name, message);
        }

        /// <summary>
        /// The weight of the message.
        /// </summary>
        private enum Level
        {
            Trace,
            Debug,
            Info,
            Warn,
            Error,
            Fatal
        }
    }
}
