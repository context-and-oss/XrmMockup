using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup
{
    public interface ITracingServiceFactory
    {
        ITracingService GetService();
    }

    internal class TracingServiceFactory : ITracingServiceFactory
    {
        private readonly List<string> _messages = new List<string>();
        private readonly object _lock = new object();

        public ITracingService GetService() => new TracingService(Record);

        private void Record(string message)
        {
            lock (_lock)
            {
                _messages.Add(message);
            }
        }

        /// <summary>
        /// A snapshot of all trace messages emitted through services created by this factory.
        /// </summary>
        public IReadOnlyList<string> TraceLog
        {
            get
            {
                lock (_lock)
                {
                    return _messages.ToList();
                }
            }
        }

        /// <summary>
        /// Clears all collected trace messages.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }
    }
}
