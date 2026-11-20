using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore.Extensions;
using Microsoft.Xrm.Sdk;

namespace TestPluginAssembly365.Plugins.ServiceBased
{
    public interface IAccountService
    {
        void HandleCreate();
        void HandleUpdate();
        void HandleUpdate(PluginRegistrations.AccountPreImagePlugin.AccountUpdatePostOperation.PreImage preImage);
        void HandleUpdate(PluginRegistrations.AccountPostImagePlugin.AccountUpdatePostOperation.PostImage postImage);
        void HandleUpdate(PluginRegistrations.AccountPrePostImagePlugin.AccountUpdatePostOperation.PreImage preImage, PluginRegistrations.AccountPrePostImagePlugin.AccountUpdatePostOperation.PostImage postImage);
    }

    public class AccountService : IAccountService
    {
        public AccountService(IPluginExecutionContext context, ITracingService tracingService, IOrganizationServiceFactory organizationServiceFactory, IManagedIdentityService managedIdentityService)
        {
            Service = organizationServiceFactory.CreateOrganizationService(context.UserId);
            Context = context;
            TracingService = tracingService;

            // Just to show that DI can inject this service as well
            _ = managedIdentityService;
        }

        private IOrganizationService Service { get; }
        private IPluginExecutionContext Context { get; }
        private ITracingService TracingService { get; }

        public void HandleCreate()
        {
            var account = Context.GetEntity<Account>(TracingService);
            if (!(account.AccountNumber?.StartsWith("DI-") ?? false))
            {
                return;
            }

            Service.Create(new Contact()
            {
                FirstName = "Injected",
                LastName = "Contact",
                ParentCustomerId = account.ToEntityReference()
            });
        }

        public void HandleUpdate(PluginRegistrations.AccountPostImagePlugin.AccountUpdatePostOperation.PostImage postImage)
        {
            Service.Create(new Task
            {
                RegardingObjectId = new EntityReference(Context.PrimaryEntityName, Context.PrimaryEntityId),
                Subject = $"10 {nameof(IAccountService)}.{nameof(HandleUpdate)}(PostImage postImage) - PostImage ParentAccount: {postImage.ParentAccountId?.Id.ToString() ?? "null"}",
            });
        }

        public void HandleUpdate(PluginRegistrations.AccountPreImagePlugin.AccountUpdatePostOperation.PreImage preImage)
        {
            Service.Create(new Task
            {
                RegardingObjectId = new EntityReference(Context.PrimaryEntityName, Context.PrimaryEntityId),
                Subject = $"20 {nameof(IAccountService)}.{nameof(HandleUpdate)}(PreImage preImage) - PreImage ParentAccount: {preImage.ParentAccountId?.Id.ToString() ?? "null"}",
            });
        }

        public void HandleUpdate(PluginRegistrations.AccountPrePostImagePlugin.AccountUpdatePostOperation.PreImage preImage, PluginRegistrations.AccountPrePostImagePlugin.AccountUpdatePostOperation.PostImage postImage)
        {
            Service.Create(new Task
            {
                RegardingObjectId = new EntityReference(Context.PrimaryEntityName, Context.PrimaryEntityId),
                Subject = $"30 {nameof(IAccountService)}.{nameof(HandleUpdate)}(PreImage preImage, PostImage postImage) - PreImage ParentAccountId: {preImage.ParentAccountId?.Id.ToString() ?? "null"}, PostImage ParentAccount: {postImage.ParentAccountId?.Id.ToString() ?? "null"}",
            });
        }

        public void HandleUpdate()
        {
            var account = Context.GetEntity<Account>(TracingService);

            Service.Create(new Task
            {
                RegardingObjectId = new EntityReference(Context.PrimaryEntityName, Context.PrimaryEntityId),
                Subject = $"40 {nameof(IAccountService)}.{nameof(HandleUpdate)}() - AccountNumber: {account?.AccountNumber ?? "null"}",
            });
        }
    }
}
