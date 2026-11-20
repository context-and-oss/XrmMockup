using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// Restores the early-bound <c>SetState(service, state[, status])</c> helper that the older
    /// XrmContext generated but the regenerated context no longer emits, so the existing tests keep
    /// compiling. It simply issues a <see cref="SetStateRequest"/>; the generated state/status enums
    /// are converted to their integer values.
    /// </summary>
    public static class TestSetStateExtensions
    {
        public static void SetState<TState>(this Entity entity, IOrganizationService service, TState state)
            where TState : Enum
        {
            service.Execute(new SetStateRequest
            {
                EntityMoniker = entity.ToEntityReference(),
                State = new OptionSetValue(Convert.ToInt32(state)),
                Status = new OptionSetValue(-1), // -1 = default status for the target state
            });
        }

        public static void SetState<TState, TStatus>(this Entity entity, IOrganizationService service, TState state, TStatus status)
            where TState : Enum
            where TStatus : Enum
        {
            service.Execute(new SetStateRequest
            {
                EntityMoniker = entity.ToEntityReference(),
                State = new OptionSetValue(Convert.ToInt32(state)),
                Status = new OptionSetValue(Convert.ToInt32(status)),
            });
        }
    }
}
