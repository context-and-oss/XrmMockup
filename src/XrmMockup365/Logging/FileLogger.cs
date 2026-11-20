using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace DG.Tools.XrmMockup.Logging
{
    internal class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly StreamWriter _writer;
        private readonly object _lock;
        private readonly LogLevel _minLogLevel;

        public FileLogger(string categoryName, StreamWriter writer, object writeLock, LogLevel minLogLevel)
        {
            _categoryName = categoryName;
            _writer = writer;
            _lock = writeLock;
            _minLogLevel = minLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
                return;

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName}: {message}";

            lock (_lock)
            {
                _writer.WriteLine(line);
                if (exception != null)
                {
                    _writer.WriteLine(exception.ToString());
                }
                _writer.Flush();
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
