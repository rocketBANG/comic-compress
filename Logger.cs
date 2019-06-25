using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ComicCompressor
{
    public class Logger
    {
        public LogLevel LoggingLevel { get; set; } = LogLevel.Warning;
        public bool Debug = false;

        // Set to null for stdout
        public string LogFile { get; set; } = null;

        public void Log(object message, LogLevel logLevel)
        {
            Log(message.ToString(), logLevel);
        }

        public void Log(string message, LogLevel logLevel)
        {
            if (logLevel > LoggingLevel) return;

            if (LogFile == null)
            {
                Console.WriteLine(message);
                return;
            }

            WriteToFile(message);
        }

        public void LogError(string message)
        {
            Log(message, LogLevel.Error);
        }

        public void LogError(object message)
        {
            Log(message, LogLevel.Error);
        }

        public void LogDebug(string message, LogLevel logLevel)
        {
            if (!Debug) return;
            Log(message, logLevel);
        }

        public void LogDebug(object message, LogLevel logLevel)
        {
            LogDebug(message.ToString(), logLevel);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void WriteToFile(string message)
        {
            if (LogFile == null) return;

            if (!Path.IsPathRooted(LogFile) && !LogFile.StartsWith("."))
            {
                LogFile = Path.Join(".", LogFile);
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
                File.AppendAllText(Path.GetFullPath(LogFile), message + "\n");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: User does not have permissions for log file: " + LogFile);
                return;
            }
            catch (Exception)
            {
                Console.WriteLine("Error: could not write to log file: " + LogFile);
                return;
            }
        }

    }

    public enum LogLevel
    {
        Error = 2, Warning = 3, Info = 4, Verbose = 5
    }
}
