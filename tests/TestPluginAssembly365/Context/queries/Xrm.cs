using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
public class Xrm : OrganizationServiceContext
{
    public Xrm(IOrganizationService service)
        : base(service)
    {
    }

    public IQueryable<Account> AccountSet
    {
        get { return CreateQuery<Account>(); }
    }

    public IQueryable<ActivityParty> ActivityPartySet
    {
        get { return CreateQuery<ActivityParty>(); }
    }

    public IQueryable<ActivityPointer> ActivityPointerSet
    {
        get { return CreateQuery<ActivityPointer>(); }
    }

    public IQueryable<BusinessUnit> BusinessUnitSet
    {
        get { return CreateQuery<BusinessUnit>(); }
    }

    public IQueryable<Contact> ContactSet
    {
        get { return CreateQuery<Contact>(); }
    }

    public IQueryable<ctx_child> ctx_childSet
    {
        get { return CreateQuery<ctx_child>(); }
    }

    public IQueryable<ctx_parent> ctx_parentSet
    {
        get { return CreateQuery<ctx_parent>(); }
    }

    public IQueryable<Email> EmailSet
    {
        get { return CreateQuery<Email>(); }
    }

    public IQueryable<Fax> FaxSet
    {
        get { return CreateQuery<Fax>(); }
    }

    public IQueryable<FileAttachment> FileAttachmentSet
    {
        get { return CreateQuery<FileAttachment>(); }
    }

    public IQueryable<SystemUser> SystemUserSet
    {
        get { return CreateQuery<SystemUser>(); }
    }

    public IQueryable<Task> TaskSet
    {
        get { return CreateQuery<Task>(); }
    }

    public IQueryable<Team> TeamSet
    {
        get { return CreateQuery<Team>(); }
    }

    public IQueryable<TeamMembership> TeamMembershipSet
    {
        get { return CreateQuery<TeamMembership>(); }
    }

    public IQueryable<Template> TemplateSet
    {
        get { return CreateQuery<Template>(); }
    }

    public IQueryable<TransactionCurrency> TransactionCurrencySet
    {
        get { return CreateQuery<TransactionCurrency>(); }
    }
}
