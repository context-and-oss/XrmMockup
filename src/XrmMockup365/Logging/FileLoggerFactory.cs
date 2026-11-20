using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace DG.Tools.XrmMockup.Logging
{
    internal class FileLoggerFactory : ILoggerFactory, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly object _writeLock = new object();
        private readonly LogLevel _minLogLevel;
        private bool _disposed;

        public FileLoggerFactory(string filePath, LogLevel minLogLevel = LogLevel.Information)
        {
            _minLogLevel = minLogLevel;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(fileStream) { AutoFlush = true };
        }

        public ILogger CreateLogger(string categoryName)
        {
            // Shorten category name to just the class name
            var shortName = categoryName;
            var lastDot = categoryName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < categoryName.Length - 1)
            {
                shortName = categoryName.Substring(lastDot + 1);
            }

            return new FileLogger(shortName, _writer, _writeLock, _minLogLevel);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // Not needed for this simple implementation
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                lock (_writeLock)
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                }
            }
        }
    }
}
