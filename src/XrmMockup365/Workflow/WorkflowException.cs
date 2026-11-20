using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowExecuter {
    public class WorkflowException : Exception {

        public WorkflowException() {
        }

        public WorkflowException(string message)
        : base(message) {
        }

        public WorkflowException(string message, Exception inner)
        : base(message, inner) {
        }
    }
}
