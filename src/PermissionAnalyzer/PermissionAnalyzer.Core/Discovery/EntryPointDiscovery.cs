using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Discovery;

/// <summary>
/// Finds where code starts running: plugin steps and custom APIs registered in plugin constructors
/// (XrmPluginCore and DAXIF style), plain <c>IPlugin</c> classes, workflow activities and Azure Functions.
/// </summary>
internal sealed class EntryPointDiscovery(Compilation compilation, XrmSymbols symbols, SourcePaths paths)
{
    private static readonly HashSet<string> StepMethods = new(StringComparer.Ordinal) { "RegisterPluginStep", "RegisterStep" };
    private static readonly HashSet<string> ApiMethods = new(StringComparer.Ordinal) { "RegisterCustomAPI", "RegisterAPI" };

    private static readonly IReadOnlyDictionary<int, string> StageNames = new Dictionary<int, string>
    {
        [10] = "PreValidation",
        [20] = "PreOperation",
        [30] = "MainOperation",
        [40] = "PostOperation",
    };

    private static readonly string[] FunctionAttributes =
    [
        "Microsoft.Azure.Functions.Worker.FunctionAttribute",
        "Microsoft.Azure.WebJobs.FunctionNameAttribute",
    ];

    public List<EntryCandidate> Entries { get; } = [];

    public List<PluginClass> Plugins { get; } = [];

    public static string StageName(int stage) => StageNames.TryGetValue(stage, out var name) ? name : stage.ToString();

    public EntryPointDiscovery Discover(CancellationToken cancellationToken = default)
    {
        var codeActivity = compilation.GetTypeByMetadataName("System.Activities.CodeActivity");
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var syntax in tree.GetRoot(cancellationToken).DescendantNodes())
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (syntax)
                {
                    case ClassDeclarationSyntax declaration when model.GetDeclaredSymbol(declaration, cancellationToken) is INamedTypeSymbol { IsAbstract: false } type
                        && IsFirstDeclaration(type, declaration):
                        if (XrmSymbols.Implements(type, symbols.Plugin)) Plugin(model, type);
                        if (codeActivity is not null && symbols.Is(type, codeActivity)) Activity(type);
                        break;

                    case MethodDeclarationSyntax declaration when model.GetDeclaredSymbol(declaration, cancellationToken) is IMethodSymbol method:
                        Function(method);
                        break;
                }
            }
        }
        return this;
    }

    private void Plugin(SemanticModel model, INamedTypeSymbol type)
    {
        var typeId = Nodes.TypeId(type);
        var name = ReflectionName(type);
        var steps = new List<EntryCandidate>();

        foreach (var constructor in type.InstanceConstructors)
        {
            foreach (var reference in constructor.DeclaringSyntaxReferences)
            {
                var syntax = reference.GetSyntax();
                var constructorModel = syntax.SyntaxTree == model.SyntaxTree ? model : compilation.GetSemanticModel(syntax.SyntaxTree);
                if (constructorModel.GetOperation(syntax) is not { } body) continue;

                foreach (var invocation in body.DescendantsAndSelf().OfType<IInvocationOperation>())
                {
                    if (!XrmSymbols.Implements(invocation.TargetMethod.ContainingType, symbols.Plugin)) continue;
                    if (Registration(invocation, type, typeId, name) is { } step) steps.Add(step);
                }
            }
        }

        Entries.AddRange(steps);
        var execute = symbols.Plugin?.GetMembers("Execute").OfType<IMethodSymbol>().FirstOrDefault() is { } member
            && type.FindImplementationForInterfaceMember(member) is IMethodSymbol implementation
            && implementation.Locations.Any(l => l.IsInSource)
                ? Nodes.Id(implementation)
                : null;
        Plugins.Add(new PluginClass(name, compilation.AssemblyName, Declaration(type), typeId, execute, steps.Count > 0));
    }

    /// <summary>
    /// A <c>RegisterPluginStep</c>, <c>RegisterStep</c>, <c>RegisterCustomAPI</c> or <c>RegisterAPI</c> call in a
    /// plugin constructor.
    /// </summary>
    private EntryCandidate? Registration(IInvocationOperation invocation, INamedTypeSymbol type, string typeId, string name)
    {
        var method = invocation.TargetMethod;
        var isStep = StepMethods.Contains(method.Name);
        if (!isStep && !ApiMethods.Contains(method.Name)) return null;

        var arguments = invocation.Arguments.Where(a => a.Parameter is not null).OrderBy(a => a.Parameter!.Ordinal).ToList();
        var handler = arguments.LastOrDefault(a => a.Parameter!.Type.TypeKind == TypeKind.Delegate || a.Parameter.Ordinal >= (isStep ? 2 : 1));
        var service = method.TypeArguments.Length == 2 ? method.TypeArguments[1]
            : method.Name == "RegisterAPI" && method.TypeArguments.Length == 1 ? method.TypeArguments[0]
            : null;
        var handlers = Handlers(handler?.Value, typeId, service);
        var declaration = paths.Of(invocation.Syntax.GetLocation());

        if (!isStep)
        {
            var api = arguments.FirstOrDefault()?.Value.ConstantValue is { HasValue: true, Value: string apiName } ? apiName : "?";
            return new EntryCandidate($"custom-api:{api}", EntryPointKind.CustomApi, name, compilation.AssemblyName, new Trigger(Message: api), null, declaration, handlers);
        }

        var message = Name(arguments.ElementAtOrDefault(0)?.Value) ?? "?";
        var stage = Name(arguments.ElementAtOrDefault(1)?.Value) ?? "?";
        var table = method.TypeArguments.Length > 0 ? symbols.LogicalNameOf(method.TypeArguments[0]) : null;
        var impersonation = UserContext(invocation);

        return new EntryCandidate(
            StepId(name, message, table, stage),
            EntryPointKind.PluginStep,
            name,
            compilation.AssemblyName,
            new Trigger(message, table, stage),
            impersonation,
            declaration,
            handlers);
    }

    public static string StepId(string type, string message, string? table, string stage) =>
        $"plugin:{type}:{message}:{table ?? "any"}:{stage}";

    /// <summary>
    /// The code a registration runs: a lambda or method of the plugin, or a method of a service, by name or lambda.
    /// </summary>
    private static IReadOnlyList<Handler> Handlers(IOperation? value, string pluginType, ITypeSymbol? service)
    {
        var op = value;
        while (op is IConversionOperation or IParenthesizedOperation)
        {
            op = op is IConversionOperation conversion ? conversion.Operand : ((IParenthesizedOperation)op).Operand;
        }

        switch (op)
        {
            case IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda }:
                return [new Handler(Nodes.Id(lambda.Symbol), pluginType)];

            case IDelegateCreationOperation { Target: IMethodReferenceOperation reference }:
                return [new Handler(Nodes.Id(reference.Method), reference.Method.IsStatic ? null : pluginType)];

            case { ConstantValue: { HasValue: true, Value: string methodName } } when service is INamedTypeSymbol serviceType:
                // nameof(IAccountService.HandleUpdate): every overload, dispatched like a call on the service
                return serviceType.GetMembers(methodName)
                    .Concat(serviceType.AllInterfaces.SelectMany(i => i.GetMembers(methodName)))
                    .OfType<IMethodSymbol>()
                    .Select(m => new Handler(Nodes.Id(m), null, Nodes.TypeId(serviceType)))
                    .Distinct()
                    .OrderBy(h => h.Target, StringComparer.Ordinal)
                    .ToList();

            default:
                return [];
        }
    }

    /// <summary>
    /// <c>.SetUserContext(userId)</c> on the step configuration: the step runs as that user.
    /// </summary>
    private static string? UserContext(IInvocationOperation registration)
    {
        IOperation current = registration;
        while (current.Parent is { } parent)
        {
            if (parent is IConversionOperation or IArgumentOperation)
            {
                current = parent;
                continue;
            }
            if (parent is not IInvocationOperation call || !ReferenceEquals(Unwrap(call.Instance), registration) && !ReferenceEquals(call.Instance, current)) break;
            if (call.TargetMethod.Name == "SetUserContext" && call.Arguments.Length > 0)
            {
                return SummaryExtractor.GuidConstant(call.Arguments[0].Value);
            }
            current = call;
            registration = call;
        }
        return null;
    }

    private void Activity(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var execute = current.GetMembers("Execute").OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.IsOverride && m.Parameters.Length == 1 && m.Locations.Any(l => l.IsInSource));
            if (execute is null) continue;

            var name = ReflectionName(type);
            Entries.Add(new EntryCandidate(
                $"activity:{name}",
                EntryPointKind.CodeActivity,
                name,
                compilation.AssemblyName,
                null,
                null,
                Declaration(type),
                [new Handler(Nodes.Id(execute), Nodes.TypeId(type))]));
            return;
        }
    }

    private void Function(IMethodSymbol method)
    {
        var attribute = method.GetAttributes().FirstOrDefault(a => a.AttributeClass is { } c && FunctionAttributes.Contains(c.ToDisplayString()));
        if (attribute is null || attribute.ConstructorArguments.FirstOrDefault().Value is not string name) return;

        var binding = method.Parameters
            .SelectMany(p => p.GetAttributes())
            .Select(a => a.AttributeClass?.Name)
            .FirstOrDefault(n => n is not null && n.EndsWith("TriggerAttribute", StringComparison.Ordinal));
        if (binding is not null) binding = binding.Substring(0, binding.Length - "Attribute".Length);

        Entries.Add(new EntryCandidate(
            $"function:{name}",
            EntryPointKind.Function,
            ReflectionName(method.ContainingType),
            compilation.AssemblyName,
            new Trigger(Binding: binding),
            null,
            Declaration(method),
            [new Handler(Nodes.Id(method), method.IsStatic ? null : Nodes.TypeId(method.ContainingType))]));
    }

    /// <summary>
    /// The name of an enum value or a string constant, such as <c>EventOperation.Update</c> or <c>"Update"</c>.
    /// </summary>
    private static string? Name(IOperation? operation)
    {
        var op = Unwrap(operation);
        if (op is IFieldReferenceOperation { Field.ContainingType.TypeKind: TypeKind.Enum } field) return field.Field.Name;
        if (op?.ConstantValue is { HasValue: true, Value: string text }) return text;
        if (op?.ConstantValue is { HasValue: true, Value: int stage }) return StageName(stage);
        return null;
    }

    private static IOperation? Unwrap(IOperation? operation)
    {
        while (operation is IConversionOperation or IArgumentOperation or IParenthesizedOperation)
        {
            operation = operation switch
            {
                IConversionOperation conversion => conversion.Operand,
                IArgumentOperation argument => argument.Value,
                _ => ((IParenthesizedOperation)operation).Operand,
            };
        }
        return operation;
    }

    /// <summary>
    /// The name a type has at runtime, such as <c>Contoso.Plugins.Outer+Inner</c>, which plugin metadata uses.
    /// </summary>
    public static string ReflectionName(INamedTypeSymbol type)
    {
        var name = type.MetadataName;
        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            name = outer.MetadataName + "+" + name;
        }
        var ns = type.ContainingNamespace;
        return ns is null || ns.IsGlobalNamespace ? name : ns.ToDisplayString() + "." + name;
    }

    private SourceLocation? Declaration(ISymbol symbol) =>
        symbol.Locations.FirstOrDefault(l => l.IsInSource) is { } location ? paths.Of(location) : null;

    /// <summary>
    /// A partial class is looked at once, from its first declaration.
    /// </summary>
    private static bool IsFirstDeclaration(INamedTypeSymbol type, SyntaxNode declaration) =>
        type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() == declaration;
}
