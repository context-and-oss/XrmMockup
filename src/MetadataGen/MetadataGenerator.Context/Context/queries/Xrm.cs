using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace XrmMockup.MetadataGenerator.Tool.Context;

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

    public IQueryable<BusinessUnit> BusinessUnitSet
    {
        get { return CreateQuery<BusinessUnit>(); }
    }

    public IQueryable<Contact> ContactSet
    {
        get { return CreateQuery<Contact>(); }
    }

    public IQueryable<Solution> SolutionSet
    {
        get { return CreateQuery<Solution>(); }
    }

    public IQueryable<SolutionComponent> SolutionComponentSet
    {
        get { return CreateQuery<SolutionComponent>(); }
    }

    public IQueryable<StatusMap> StatusMapSet
    {
        get { return CreateQuery<StatusMap>(); }
    }

    public IQueryable<SystemUser> SystemUserSet
    {
        get { return CreateQuery<SystemUser>(); }
    }

    public IQueryable<Team> TeamSet
    {
        get { return CreateQuery<Team>(); }
    }

    public IQueryable<TransactionCurrency> TransactionCurrencySet
    {
        get { return CreateQuery<TransactionCurrency>(); }
    }

    public IQueryable<Workflow> WorkflowSet
    {
        get { return CreateQuery<Workflow>(); }
    }
}
