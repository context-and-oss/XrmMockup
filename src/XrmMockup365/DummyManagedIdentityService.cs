using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup
{
    internal sealed class DummyManagedIdentityService : IManagedIdentityService
    {
        public string AcquireToken(IEnumerable<string> scopes) => "invalidtokenonlyfortest";

        public string AcquireToken(Guid managedIdentityId, IEnumerable<string> scopes) => "invalidtokenonlyfortest";
    }
}
