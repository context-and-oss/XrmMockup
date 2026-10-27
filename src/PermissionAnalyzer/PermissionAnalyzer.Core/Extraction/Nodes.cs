using System.Runtime.CompilerServices;
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

    // Parameter types with their namespaces, for overloads that only differ in those
    private static readonly SymbolDisplayFormat QualifiedParameterFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays);

    private static readonly SymbolDisplayFormat ShortTypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    // Display strings are slow and the same members and types are named at every call to them
    private static readonly ConditionalWeakTable<ISymbol, string> MemberIds = new();
    private static readonly ConditionalWeakTable<ISymbol, string> TypeIds = new();

    /// <summary>
    /// A stable id such as <c>Contoso.Plugins.AccountPlugin.Execute(IServiceProvider)</c>, with the type parameters
    /// of a generic method (<c>Load&lt;T&gt;(IOrganizationService)</c>), and with namespaces on the parameter types
    /// when another overload would have the same id without them. A lambda adds its position to the id of the
    /// member that contains it: <c>Contoso.Plugins.AccountPlugin..ctor()=>12:9</c>.
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

    public static string TypeId(ITypeSymbol type) => TypeIds.GetValue(type.OriginalDefinition, definition => definition.ToDisplayString(TypeFormat));

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

    private static string MemberId(ISymbol member) => MemberIds.GetValue(member, NewMemberId);

    private static string NewMemberId(ISymbol member)
    {
        var type = member.ContainingType.ToDisplayString(TypeFormat);
        if (member is not IMethodSymbol method) return $"{type}.{member.Name}";

        var name = method.MethodKind switch
        {
            MethodKind.Constructor => ".ctor",
            MethodKind.StaticConstructor => ".cctor",
            _ => method.Name,
        };
        if (method.TypeParameters.Length > 0) name += $"<{string.Join(", ", method.TypeParameters.Select(t => t.Name))}>";

        var parameters = Parameters(method, SymbolDisplayFormat.MinimallyQualifiedFormat);
        var clashes = method.ContainingType.GetMembers(method.Name)
            .OfType<IMethodSymbol>()
            .Any(other => !SymbolEqualityComparer.Default.Equals(other, method)
                && other.TypeParameters.Length == method.TypeParameters.Length
                && Parameters(other, SymbolDisplayFormat.MinimallyQualifiedFormat) == parameters);
        if (clashes) parameters = Parameters(method, QualifiedParameterFormat);
        return $"{type}.{name}({parameters})";
    }

    private static string Parameters(IMethodSymbol method, SymbolDisplayFormat format) =>
        string.Join(", ", method.Parameters.Select(p =>
            (p.RefKind == RefKind.None ? "" : p.RefKind.ToString().ToLowerInvariant() + " ") + p.Type.ToDisplayString(format)));

    private static string Position(ISymbol symbol)
    {
        var span = symbol.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition ?? default;
        return $"{span.Line + 1}:{span.Character + 1}";
    }

    private static int Line(ISymbol symbol) => (symbol.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line ?? 0) + 1;
}
