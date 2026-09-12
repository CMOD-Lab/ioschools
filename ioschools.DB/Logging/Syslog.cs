using System;
using System.Diagnostics;

namespace clearpixels.Logging
{
    /// <summary>
    /// Error level enumeration for logging
    /// </summary>
    public enum ErrorLevel
    {
        DEBUG = 0,
        INFORMATION = 1,
        WARNING = 2,
        ERROR = 3,
        CRITICAL = 4
    }

    /// <summary>
    /// Stub implementation of the clearpixels Syslog utility.
    /// Writes log entries to the Debug output and Console.
    /// </summary>
    public static class Syslog
    {
        /// <summary>
        /// Writes an exception to the log.
        /// </summary>
        public static void Write(Exception ex)
        {
            if (ex == null) return;
            var message = $"[{ErrorLevel.ERROR}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Debug.WriteLine(message);
            Console.Error.WriteLine(message);
        }

        /// <summary>
        /// Writes a message at the specified error level.
        /// </summary>
        public static void Write(ErrorLevel level, string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            var logMessage = $"[{level}] {message}";
            Debug.WriteLine(logMessage);
            if (level >= ErrorLevel.WARNING)
            {
                Console.Error.WriteLine(logMessage);
            }
            else
            {
                Console.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Writes a message at the default (Information) level.
        /// </summary>
        public static void Write(string message)
        {
            Write(ErrorLevel.INFORMATION, message);
        }
    }
}
