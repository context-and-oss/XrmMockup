using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// Finds organization service calls in a compilation and the table privileges each one needs.
/// Calls in lambdas and local functions count toward the method that contains them.
/// </summary>
internal sealed class OperationExtractor(XrmSymbols symbols, SourcePaths paths)
{
    private const string ExecuteNotAnalyzed = "Execute requests are not analyzed yet";

    private static readonly SymbolDisplayFormat TypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public IEnumerable<DataAccess> Extract(Compilation compilation, CancellationToken cancellationToken = default)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = compilation.GetSemanticModel(tree);

            foreach (var syntax in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetOperation(syntax, cancellationToken) is not IInvocationOperation invocation) continue;
                if (symbols.ServiceOperation(invocation.TargetMethod) is not { } operation) continue;
                if (ContainingMember(model.GetEnclosingSymbol(syntax.SpanStart, cancellationToken)) is not { } member) continue;

                var site = new Site(MethodId(member), Declaration(member), operation, paths.Of(NameLocation(syntax)));
                foreach (var access in Accesses(invocation, site)) yield return access;
            }
        }
    }

    private IEnumerable<DataAccess> Accesses(IInvocationOperation invocation, Site site)
    {
        var resolver = new TableResolver(symbols);
        var first = Argument(invocation, 0);

        switch (site.Operation)
        {
            case "Create":
                return site.Emit(resolver.Entity(first).OrUnresolved("entity is created without a table name"), Privilege.Create);
            case "CreateAndReturn":
                return site.Emit(resolver.Entity(first).OrUnresolved("entity is created without a table name"), Privilege.Create, Privilege.Read);
            case "Update":
                return site.Emit(resolver.Entity(first).OrUnresolved("entity is created without a table name"), Privilege.Write);
            case "Retrieve":
                return site.Emit(resolver.Name(first), Privilege.Read);
            case "Delete":
                return site.Emit(resolver.Name(first), Privilege.Delete);
            case "RetrieveMultiple":
                return site.Emit(resolver.Query(first).OrUnresolved("query has no table name"), Privilege.Read);
            case "Associate":
            case "Disassociate":
                var related = invocation.Arguments.FirstOrDefault(a => symbols.Is(a.Parameter?.Type, symbols.EntityReferenceCollection));
                return site.Emit(resolver.Name(first), Privilege.Read, Privilege.AppendTo)
                    .Concat(site.Emit(resolver.EntityReferences(related).OrUnresolved("no related records are added"), "related records: ", Privilege.Read, Privilege.Append));
            case "Execute":
                return [site.Unresolved(ExecuteNotAnalyzed)];
            default:
                return [];
        }
    }

    private static IArgumentOperation? Argument(IInvocationOperation invocation, int ordinal) =>
        invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == ordinal);

    /// <summary>
    /// The method a call belongs to. Lambdas and local functions are part of the method that declares them.
    /// </summary>
    private static ISymbol? ContainingMember(ISymbol? symbol)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
        {
            symbol = symbol.ContainingSymbol;
        }
        return symbol?.ContainingType is null ? null : symbol;
    }

    /// <summary>
    /// A stable id such as <c>Contoso.Plugins.AccountPlugin.Execute(IServiceProvider)</c>.
    /// </summary>
    internal static string MethodId(ISymbol member)
    {
        var type = member.ContainingType.ToDisplayString(TypeFormat);
        if (member is not IMethodSymbol method) return $"{type}.{member.Name}";

        var name = method.MethodKind switch
        {
            MethodKind.Constructor => ".ctor",
            MethodKind.StaticConstructor => ".cctor",
            _ => method.Name,
        };
        var parameters = method.Parameters.Select(p =>
            (p.RefKind == RefKind.None ? "" : p.RefKind.ToString().ToLowerInvariant() + " ")
            + p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        return $"{type}.{name}({string.Join(", ", parameters)})";
    }

    private SourceLocation? Declaration(ISymbol member) =>
        member.Locations.FirstOrDefault(l => l.IsInSource) is { } location ? paths.Of(location) : null;

    private static Location NameLocation(InvocationExpressionSyntax syntax) => syntax.Expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.GetLocation(),
        MemberBindingExpressionSyntax binding => binding.Name.GetLocation(),
        _ => syntax.GetLocation(),
    };

    private sealed record Site(string MethodId, SourceLocation? Declaration, string Operation, SourceLocation Source)
    {
        public IEnumerable<DataAccess> Emit(TableResolution resolution, params Privilege[] privileges) =>
            Emit(resolution, "", privileges);

        public IEnumerable<DataAccess> Emit(TableResolution resolution, string reasonPrefix, params Privilege[] privileges)
        {
            foreach (var table in resolution.Tables)
            {
                yield return new DataAccess(MethodId, Declaration, Operation, Source, table, privileges, null);
            }
            foreach (var reason in resolution.Reasons)
            {
                yield return new DataAccess(MethodId, Declaration, Operation, Source, null, privileges, reasonPrefix + reason);
            }
        }

        public DataAccess Unresolved(string reason) =>
            new(MethodId, Declaration, Operation, Source, null, [], reason);
    }
}
