using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// Reads Roslyn operations the same way for table resolution, method summaries and entry point discovery.
/// </summary>
internal static class Operations
{
    /// <summary>
    /// The value under conversions, arguments, parentheses and <c>out var</c> declarations.
    /// </summary>
    public static IOperation? Unwrap(IOperation? operation)
    {
        while (UnwrapOnce(operation) is { } inner) operation = inner;
        return operation;
    }

    public static IOperation? UnwrapOnce(IOperation? operation) => operation switch
    {
        IConversionOperation conversion => conversion.Operand,
        IArgumentOperation argument => argument.Value,
        IParenthesizedOperation parenthesized => parenthesized.Operand,
        IDeclarationExpressionOperation declaration => declaration.Expression,
        _ => null,
    };

    /// <summary>
    /// The body an operation is part of.
    /// </summary>
    public static IOperation Root(IOperation operation)
    {
        while (operation.Parent is not null) operation = operation.Parent;
        return operation;
    }

    public static bool IsLocal(IOperation? operation, ILocalSymbol local) =>
        Unwrap(operation) is ILocalReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Local, local);

    public static bool IsParameter(IOperation? operation, IParameterSymbol parameter) =>
        Unwrap(operation) is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);

    public static bool IsInSource(ISymbol? symbol) => symbol?.Locations.Any(l => l.IsInSource) == true;

    /// <summary>
    /// A call into the .NET libraries, such as LINQ, whose results come from its arguments.
    /// </summary>
    public static bool IsSystemCall(IInvocationOperation invocation) =>
        !IsInSource(invocation.TargetMethod)
        && invocation.TargetMethod.ContainingNamespace?.ToDisplayString() is { } name
        && (name == "System" || name.StartsWith("System.", StringComparison.Ordinal));

    /// <summary>
    /// A type of the .NET libraries or the SDK, under the <c>System</c> or <c>Microsoft</c> namespaces, whose
    /// virtual methods the solution doesn't implement.
    /// </summary>
    public static bool IsFrameworkType(INamedTypeSymbol? type)
    {
        var root = type?.ContainingNamespace;
        while (root is { ContainingNamespace.IsGlobalNamespace: false }) root = root.ContainingNamespace;
        return root?.Name is "System" or "Microsoft";
    }

    /// <summary>
    /// The value passed for the parameter at <paramref name="ordinal"/>, whatever order the arguments are written in.
    /// </summary>
    public static IOperation? Argument(IInvocationOperation invocation, int ordinal) =>
        invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == ordinal)?.Value;

    /// <summary>
    /// A user id written in code: <c>new Guid("…")</c> or <c>Guid.Parse("…")</c>.
    /// </summary>
    public static string? GuidConstant(IOperation operation)
    {
        var arguments = operation switch
        {
            IObjectCreationOperation creation when creation.Type?.ToDisplayString() == "System.Guid" => creation.Arguments,
            IInvocationOperation { TargetMethod.Name: "Parse" } invocation when invocation.TargetMethod.ContainingType.ToDisplayString() == "System.Guid" => invocation.Arguments,
            _ => default,
        };
        return !arguments.IsDefaultOrEmpty
            && arguments[0].Value.ConstantValue is { HasValue: true, Value: string text }
            && Guid.TryParse(text, out var id)
            ? id.ToString("D")
            : null;
    }

    /// <summary>
    /// Every value assigned to <paramref name="local"/> in <paramref name="root"/>. For a <c>foreach</c> variable
    /// that's the collection it iterates. <paramref name="failed"/> is told why an assignment can't be followed.
    /// </summary>
    public static List<LocalValue> LocalValues(ILocalSymbol local, IOperation root, Action<string>? failed)
    {
        var values = new List<LocalValue>();
        foreach (var node in root.DescendantsAndSelf())
        {
            switch (node)
            {
                case IVariableDeclaratorOperation declarator when SymbolEqualityComparer.Default.Equals(declarator.Symbol, local):
                    if (declarator.GetVariableInitializer() is { } initializer) values.Add(new LocalValue(initializer.Value, false));
                    break;

                case ISimpleAssignmentOperation assignment when IsLocal(assignment.Target, local):
                    values.Add(new LocalValue(assignment.Value, false));
                    break;

                case ICoalesceAssignmentOperation coalesce when IsLocal(coalesce.Target, local):
                    values.Add(new LocalValue(coalesce.Value, false));
                    break;

                case ICompoundAssignmentOperation compound when IsLocal(compound.Target, local):
                    failed?.Invoke($"local '{local.Name}' is changed by a compound assignment");
                    break;

                case IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument when IsLocal(argument.Value, local):
                    failed?.Invoke($"local '{local.Name}' is passed by ref or out");
                    break;

                case IForEachLoopOperation loop when loop.Locals.Contains(local, SymbolEqualityComparer.Default):
                    values.Add(new LocalValue(loop.Collection, true));
                    break;

                case IDeclarationPatternOperation pattern when SymbolEqualityComparer.Default.Equals(pattern.DeclaredSymbol, local):
                    failed?.Invoke($"local '{local.Name}' is declared by a pattern");
                    break;

                case IDeconstructionAssignmentOperation deconstruction when deconstruction.Target.DescendantsAndSelf().Any(t => IsLocal(t, local)):
                    failed?.Invoke($"local '{local.Name}' is assigned by deconstruction");
                    break;
            }
        }
        return values;
    }

    /// <summary>
    /// Why a <c>foreach</c> variable is unknown where its elements aren't followed.
    /// </summary>
    public static string LoopVariable(ILocalSymbol local) => $"local '{local.Name}' is a loop variable";

    public readonly record struct LocalValue(IOperation Operation, bool IsLoopCollection);
}
