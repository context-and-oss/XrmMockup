using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections;

namespace DG.Tools.XrmMockup {

    internal static class MockupExecutionContext {
        internal static readonly string Key = "MockupServiceSettings";
        internal static MockupServiceSettings GetSettings(OrganizationRequest request) {
            if (!request.Parameters.ContainsKey(Key) || request.Parameters[Key] == null) {
                return new MockupServiceSettings();
            }
            return request.Parameters[Key] as MockupServiceSettings;
        }

        internal static void SetSettings(OrganizationRequest request, MockupServiceSettings settings) {
           request.Parameters[Key] = settings;
        }
    }
}
