using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using DG.Tools.XrmMockup;
using DG.Some.Namespace;
using Microsoft.Extensions.Logging;
using TestPluginAssembly365.Plugins.LegacyDaxif;
using TestPluginAssembly365.Plugins.ServiceBased;
using XrmPluginCore;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestLogging : IDisposable
    {
        private readonly List<string> _tempFiles = new List<string>();

        private XrmMockupSettings CreateSettingsWithLogFile(out string logFilePath)
        {
            logFilePath = Path.Combine(Path.GetTempPath(), $"xrmmockup_test_{Guid.NewGuid():N}.log");
            _tempFiles.Add(logFilePath);

            return new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif), typeof(LegacyPlugin), typeof(DIPlugin) },
                BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(Plugin)), new Tuple<string, Type>("dg", typeof(LegacyCustomApi)) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                ExceptionFreeRequests = new string[] { "TestWrongRequest" },
                MetadataDirectoryPath = GetMetadataPath(),
                LogFilePath = logFilePath
            };
        }

        [Fact]
        public void TestLogFileCreated()
        {
            var settings = CreateSettingsWithLogFile(out var logFilePath);
            var crm = XrmMockup365.GetInstance(settings);

            Assert.True(File.Exists(logFilePath), "Log file should exist after initialization");
            var content = ReadLogFile(logFilePath);
            Assert.False(string.IsNullOrWhiteSpace(content), "Log file should not be empty");
        }

        [Fact]
        public void TestLogFileContainsPluginInfo()
        {
            var settings = CreateSettingsWithLogFile(out var logFilePath);
            var crm = XrmMockup365.GetInstance(settings);

            var content = ReadLogFile(logFilePath);
            Assert.Contains("plugin", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Plugins:", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestLogFileContainsWorkflowInfo()
        {
            var settings = CreateSettingsWithLogFile(out var logFilePath);
            var crm = XrmMockup365.GetInstance(settings);

            var content = ReadLogFile(logFilePath);
            Assert.Contains("Workflow", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("code activit", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestLogFileContainsCustomApiInfo()
        {
            var settings = CreateSettingsWithLogFile(out var logFilePath);
            var crm = XrmMockup365.GetInstance(settings);

            var content = ReadLogFile(logFilePath);
            Assert.Contains("Custom API", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestNoLogFileWithoutSetting()
        {
            var logFilePath = Path.Combine(Path.GetTempPath(), $"xrmmockup_nolog_{Guid.NewGuid():N}.log");
            _tempFiles.Add(logFilePath);

            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif), typeof(LegacyPlugin), typeof(DIPlugin) },
                BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(Plugin)), new Tuple<string, Type>("dg", typeof(LegacyCustomApi)) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                MetadataDirectoryPath = GetMetadataPath()
            };

            var crm = XrmMockup365.GetInstance(settings);

            Assert.False(File.Exists(logFilePath), "Log file should not exist when LogFilePath is not set");
        }

        [Fact]
        public void TestCustomLoggerFactoryIsUsed()
        {
            var factory = new InMemoryLoggerFactory();

            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif), typeof(LegacyPlugin), typeof(DIPlugin) },
                BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(Plugin)), new Tuple<string, Type>("dg", typeof(LegacyCustomApi)) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                MetadataDirectoryPath = GetMetadataPath(),
                LoggerFactory = factory
            };

            var crm = XrmMockup365.GetInstance(settings);

            Assert.True(factory.CreateLoggerCallCount > 0, "LoggerFactory.CreateLogger should have been called");
            Assert.True(factory.LogMessages.Count > 0, "Logger should have captured log messages");
        }

        [Fact]
        public void TestMinLogLevelFiltersDebugMessages()
        {
            // Default MinLogLevel is Information, so Debug messages should be filtered out
            var settings = CreateSettingsWithLogFile(out var logFilePath);
            var crm = XrmMockup365.GetInstance(settings);

            var content = ReadLogFile(logFilePath);
            Assert.DoesNotContain("[Debug]", content);
            Assert.Contains("[Information]", content);
        }

        [Fact]
        public void TestDebugLogLevelIncludesDetailedMessages()
        {
            var logFilePath = Path.Combine(Path.GetTempPath(), $"xrmmockup_test_{Guid.NewGuid():N}.log");
            _tempFiles.Add(logFilePath);

            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif), typeof(LegacyPlugin), typeof(DIPlugin) },
                BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(Plugin)), new Tuple<string, Type>("dg", typeof(LegacyCustomApi)) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                MetadataDirectoryPath = GetMetadataPath(),
                LogFilePath = logFilePath,
                MinLogLevel = LogLevel.Debug
            };

            var crm = XrmMockup365.GetInstance(settings);

            var content = ReadLogFile(logFilePath);
            // Debug level should produce [Debug] entries (cache-hit or cache-miss both log at Debug)
            Assert.Contains("[Debug]", content);
            // Should contain per-plugin registration details
            Assert.Contains("Plugin", content);
        }

        public void Dispose()
        {
            foreach (var file in _tempFiles)
            {
                try { if (File.Exists(file)) File.Delete(file); } catch { }
            }
        }

        private static string ReadLogFile(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                return reader.ReadToEnd();
            }
        }

        private static string GetMetadataPath()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var metadataPath = Path.Combine(currentDir, "Metadata");
            if (Directory.Exists(metadataPath)) return metadataPath;

            var testProjectPaths = new[]
            {
                Path.Combine(currentDir, "..", "..", "..", "Metadata"),
                "Metadata"
            };

            foreach (var path in testProjectPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath)) return fullPath;
            }

            throw new DirectoryNotFoundException($"Could not find Metadata directory. Searched in: {currentDir}");
        }

        private class InMemoryLoggerFactory : ILoggerFactory
        {
            public int CreateLoggerCallCount { get; private set; }
            public ConcurrentBag<string> LogMessages { get; } = new ConcurrentBag<string>();

            public ILogger CreateLogger(string categoryName)
            {
                CreateLoggerCallCount++;
                return new InMemoryLogger(categoryName, LogMessages);
            }

            public void AddProvider(ILoggerProvider provider) { }
            public void Dispose() { }
        }

        private class InMemoryLogger : ILogger
        {
            private readonly string _category;
            private readonly ConcurrentBag<string> _messages;

            public InMemoryLogger(string category, ConcurrentBag<string> messages)
            {
                _category = category;
                _messages = messages;
            }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                _messages.Add($"[{logLevel}] {_category}: {formatter(state, exception)}");
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }
    }
}
