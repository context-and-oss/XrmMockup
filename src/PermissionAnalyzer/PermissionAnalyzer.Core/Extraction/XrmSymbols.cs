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
        QueryExpression = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.QueryExpression");
        QueryByAttribute = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.QueryByAttribute");
        FetchExpression = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.FetchExpression");
        LinkEntity = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Query.LinkEntity");
        EntityLogicalNameAttribute = compilation.GetTypeByMetadataName("Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute");
    }

    /// <summary>
    /// <c>IOrganizationService</c> and, when referenced, the Dataverse client's async service interfaces.
    /// </summary>
    public IReadOnlyList<INamedTypeSymbol> ServiceInterfaces { get; }

    public INamedTypeSymbol? Entity { get; }
    public INamedTypeSymbol? EntityReference { get; }
    public INamedTypeSymbol? EntityReferenceCollection { get; }
    public INamedTypeSymbol? EntityCollection { get; }
    public INamedTypeSymbol? QueryExpression { get; }
    public INamedTypeSymbol? QueryByAttribute { get; }
    public INamedTypeSymbol? FetchExpression { get; }
    public INamedTypeSymbol? LinkEntity { get; }
    public INamedTypeSymbol? EntityLogicalNameAttribute { get; }

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
    /// The table of an early-bound type, read from <c>[EntityLogicalName]</c> on the type or a base type.
    /// </summary>
    public string? LogicalNameOf(ITypeSymbol? type)
    {
        if (EntityLogicalNameAttribute is null) return null;
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, EntityLogicalNameAttribute)
                    && attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is string name)
                {
                    return name;
                }
            }
        }
        return null;
    }

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
