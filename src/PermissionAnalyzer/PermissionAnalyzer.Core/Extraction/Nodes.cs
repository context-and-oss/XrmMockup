using Microsoft.CodeAnalysis;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// Ids and display names of call graph nodes: members, plus the lambdas and local functions inside them,
/// which are nodes of their own so a lambda can be a plugin step handler.
/// </summary>
internal static class Nodes
{
    private static readonly SymbolDisplayFormat TypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private static readonly SymbolDisplayFormat ShortTypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    /// <summary>
    /// A stable id such as <c>Contoso.Plugins.AccountPlugin.Execute(IServiceProvider)</c>. A lambda adds its
    /// position to the id of the member that contains it: <c>Contoso.Plugins.AccountPlugin..ctor()=>12:9</c>.
    /// </summary>
    public static string Id(ISymbol symbol)
    {
        symbol = Definition(symbol);
        if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambda)
        {
            return $"{Id(lambda.ContainingSymbol)}=>{Position(lambda)}";
        }
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } local)
        {
            return $"{Id(local.ContainingSymbol)}.{local.Name}@{Position(local)}";
        }
        return MemberId(symbol);
    }

    /// <summary>
    /// The member a node belongs to: the node itself, or the member that declares the lambda or local function.
    /// </summary>
    public static ISymbol? Member(ISymbol? symbol)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
        {
            symbol = symbol.ContainingSymbol;
        }
        return symbol?.ContainingType is null ? null : Definition(symbol);
    }

    /// <summary>
    /// A short name for call paths, such as <c>AccountPlugin.Execute</c>.
    /// </summary>
    public static string Display(ISymbol symbol)
    {
        symbol = Definition(symbol);
        if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambda)
        {
            return $"{Display(lambda.ContainingSymbol)} (lambda, line {Line(lambda)})";
        }
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } local)
        {
            return $"{Display(local.ContainingSymbol)}.{local.Name}";
        }

        var type = symbol.ContainingType?.ToDisplayString(ShortTypeFormat) ?? "";
        var name = symbol is IMethodSymbol method
            ? method.MethodKind switch
            {
                MethodKind.Constructor => "ctor",
                MethodKind.StaticConstructor => "cctor",
                MethodKind.PropertyGet or MethodKind.PropertySet => method.AssociatedSymbol?.Name ?? method.Name,
                _ => method.Name,
            }
            : symbol.Name;
        return $"{type}.{name}";
    }

    public static string TypeId(ITypeSymbol type) => type.OriginalDefinition.ToDisplayString(TypeFormat);

    /// <summary>
    /// The type parameters of <paramref name="type"/> as declared, after those of the types it is nested in.
    /// </summary>
    public static List<ITypeParameterSymbol> TypeParameters(INamedTypeSymbol type) =>
        TypeArguments(type.OriginalDefinition).Cast<ITypeParameterSymbol>().ToList();

    /// <summary>
    /// The type arguments of <paramref name="type"/>, in the order of <see cref="TypeParameters"/>.
    /// </summary>
    public static List<ITypeSymbol> TypeArguments(INamedTypeSymbol type)
    {
        var result = type.ContainingType is { } outer ? TypeArguments(outer) : [];
        result.AddRange(type.TypeArguments);
        return result;
    }

    /// <summary>
    /// The symbol as declared: generic methods and members of generic types are not constructed, an extension
    /// method is not reduced and a partial method is its definition.
    /// </summary>
    public static ISymbol Definition(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            method = method.OriginalDefinition;
            return method.PartialDefinitionPart ?? method;
        }
        return symbol.OriginalDefinition;
    }

    private static string MemberId(ISymbol member)
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

    private static string Position(ISymbol symbol)
    {
        var span = symbol.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition ?? default;
        return $"{span.Line + 1}:{span.Character + 1}";
    }

    private static int Line(ISymbol symbol) => (symbol.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line ?? 0) + 1;
}
