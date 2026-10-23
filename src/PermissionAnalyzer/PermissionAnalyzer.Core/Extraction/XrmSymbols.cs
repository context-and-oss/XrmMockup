using Microsoft.CodeAnalysis;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The Dataverse SDK types the extractor recognises, looked up once per compilation.
/// </summary>
internal sealed class XrmSymbols
{
    private XrmSymbols(Compilation compilation, INamedTypeSymbol organizationService)
    {
        var services = new[]
            {
                organizationService,
                compilation.GetTypeByMetadataName("Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync"),
                compilation.GetTypeByMetadataName("Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2"),
            }
            .Where(s => s is not null)
            .Cast<INamedTypeSymbol>()
            .ToList();

        // Async2 extends Async, so a method declared on any base interface counts as a service method.
        ServiceInterfaces = services.SelectMany(s => s.AllInterfaces.Prepend(s)).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToList();
        Entity = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Entity");
        EntityReference = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.EntityReference");
        EntityReferenceCollection = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.EntityReferenceCollection");
        EntityCollection = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.EntityCollection");
        QueryBase = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.QueryBase");
        QueryExpression = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.QueryExpression");
        QueryByAttribute = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.QueryByAttribute");
        FetchExpression = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.FetchExpression");
        LinkEntity = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.LinkEntity");
        OrganizationRequest = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.OrganizationRequest");
        EntityLogicalNameAttribute = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute");
        AttributeLogicalNameAttribute = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.AttributeLogicalNameAttribute");
        Plugin = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.IPlugin");
        OrganizationServiceFactory = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.IOrganizationServiceFactory");
        ExecutionContext = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.IExecutionContext");
        OrganizationServiceContext = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Client.OrganizationServiceContext");
        Queryable = compilation.GetTypeByMetadataName("System.Linq.IQueryable`1");
    }

    /// <summary>
    /// <c>IOrganizationService</c> and, when referenced, the Dataverse client's async service interfaces.
    /// </summary>
    public IReadOnlyList<INamedTypeSymbol> ServiceInterfaces { get; }

    public INamedTypeSymbol? Entity { get; }
    public INamedTypeSymbol? EntityReference { get; }
    public INamedTypeSymbol? EntityReferenceCollection { get; }
    public INamedTypeSymbol? EntityCollection { get; }
    public INamedTypeSymbol? QueryBase { get; }
    public INamedTypeSymbol? QueryExpression { get; }
    public INamedTypeSymbol? QueryByAttribute { get; }
    public INamedTypeSymbol? FetchExpression { get; }
    public INamedTypeSymbol? LinkEntity { get; }
    public INamedTypeSymbol? OrganizationRequest { get; }
    public INamedTypeSymbol? EntityLogicalNameAttribute { get; }
    public INamedTypeSymbol? AttributeLogicalNameAttribute { get; }
    public INamedTypeSymbol? Plugin { get; }
    public INamedTypeSymbol? OrganizationServiceFactory { get; }
    public INamedTypeSymbol? ExecutionContext { get; }
    public INamedTypeSymbol? OrganizationServiceContext { get; }
    public INamedTypeSymbol? Queryable { get; }

    /// <summary>
    /// Returns <c>null</c> when the compilation doesn't reference the Dataverse SDK, so it can't make service calls.
    /// </summary>
    public static XrmSymbols? Create(Compilation compilation)
    {
        var organizationService = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.IOrganizationService");
        return organizationService is null ? null : new XrmSymbols(compilation, organizationService);
    }

    public bool Is(ITypeSymbol? type, INamedTypeSymbol? expected)
    {
        if (expected is null) return false;
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, expected)) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether <paramref name="type"/> is, or implements, <paramref name="expected"/>, an interface.
    /// </summary>
    public static bool Implements(ITypeSymbol? type, INamedTypeSymbol? expected) =>
        type is not null && expected is not null
        && (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, expected)
            || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, expected)));

    /// <summary>
    /// Whether values of <paramref name="type"/> are organization services, so the identity they run as is tracked.
    /// </summary>
    public bool IsService(ITypeSymbol? type) =>
        type is not null && (ServiceInterfaces.Any(s => Implements(type, s)) || IsContext(type));

    /// <summary>
    /// Whether <paramref name="type"/> is an <c>OrganizationServiceContext</c>, such as a generated context. A context
    /// runs as the organization service it is created with, so it is tracked as a service.
    /// </summary>
    public bool IsContext(ITypeSymbol? type) => Is(type, OrganizationServiceContext);

    /// <summary>
    /// The <c>OrganizationServiceContext</c> method <paramref name="method"/> is, such as <c>CreateQuery</c> or
    /// <c>AddObject</c>, or <c>null</c> when it is another method.
    /// </summary>
    public string? ContextOperation(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary
        && OrganizationServiceContext is not null
        && SymbolEqualityComparer.Default.Equals(method.ContainingType?.OriginalDefinition, OrganizationServiceContext)
            ? method.Name
            : null;

    /// <summary>
    /// The record type of a query property of a context, such as <c>Account</c> for a generated
    /// <c>IQueryable&lt;Account&gt; AccountSet</c>, or <c>null</c> when <paramref name="property"/> is another property.
    /// </summary>
    public ITypeSymbol? QueryElement(IPropertySymbol property) =>
        !property.IsStatic
        && !property.IsIndexer
        && IsContext(property.ContainingType)
        && property.Type is INamedTypeSymbol { TypeArguments.Length: 1 } type
        && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, Queryable)
            ? type.TypeArguments[0]
            : null;

    /// <summary>
    /// The table of an early-bound type, read from <c>[EntityLogicalName]</c> on the type or a base type.
    /// </summary>
    public string? LogicalNameOf(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (NameArgument(current, EntityLogicalNameAttribute) is { } name) return name;
        }
        return null;
    }

    /// <summary>
    /// The column of an early-bound property, read from <c>[AttributeLogicalName]</c>.
    /// </summary>
    public string? AttributeNameOf(IPropertySymbol property) => NameArgument(property, AttributeLogicalNameAttribute);

    /// <summary>
    /// The name given to <paramref name="attributeType"/> on <paramref name="symbol"/>, its only constructor argument.
    /// </summary>
    private static string? NameArgument(ISymbol symbol, INamedTypeSymbol? attributeType) =>
        attributeType is null
            ? null
            : symbol.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType) && a.ConstructorArguments.Length == 1)
                .Select(a => a.ConstructorArguments[0].Value as string)
                .FirstOrDefault(name => name is not null);

    /// <summary>
    /// The service operation a method performs (<c>Create</c>, <c>RetrieveMultiple</c>, …), or <c>null</c> when
    /// the method is not an organization service method or an implementation of one.
    /// </summary>
    public string? ServiceOperation(IMethodSymbol method)
    {
        if (!IsServiceMethod(method)) return null;

        var name = method.Name;
        if (name.EndsWith("Async", StringComparison.Ordinal)) name = name.Substring(0, name.Length - "Async".Length);
        return name;
    }

    private bool IsServiceMethod(IMethodSymbol method)
    {
        var type = method.ContainingType;
        if (type is null) return false;

        if (ServiceInterfaces.Contains(type.OriginalDefinition, SymbolEqualityComparer.Default)) return true;

        // A class implementing the service, such as ServiceClient or a wrapper
        if (!type.AllInterfaces.Any(i => ServiceInterfaces.Contains(i.OriginalDefinition, SymbolEqualityComparer.Default))) return false;

        foreach (var serviceInterface in ServiceInterfaces)
        {
            foreach (var member in serviceInterface.GetMembers().OfType<IMethodSymbol>())
            {
                var implementation = type.FindImplementationForInterfaceMember(member);
                if (SymbolEqualityComparer.Default.Equals(implementation, method)
                    || SymbolEqualityComparer.Default.Equals(implementation, method.OriginalDefinition))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
