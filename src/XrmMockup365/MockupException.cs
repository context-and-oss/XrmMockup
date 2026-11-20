using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DG.Tools.XrmMockup
{
    public class MockupException : Exception {
        public MockupException(string message) : base(message) { }
        public MockupException(string message, params object[] args) : base(String.Format(message, args)) { }
    }
}
