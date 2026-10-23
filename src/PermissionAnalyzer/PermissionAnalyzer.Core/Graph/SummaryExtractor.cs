using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Graph;

/// <summary>
/// Summarizes every method body of a compilation: its service calls, the methods it calls with what it passes
/// them, and where its organization services come from.
/// </summary>
internal sealed class SummaryExtractor(Compilation compilation, XrmSymbols symbols, SourcePaths paths, RequestRules rules, LookupColumns lookups)
{
    // How far constants are followed through fields and locals, such as a user id in a static field.
    private const int MaxConstantDepth = 8;

    private const string NoEntityTable = "entity has no table name";

    private static readonly HashSet<string> RegistrationMethods = new(StringComparer.Ordinal)
    {
        "AddScoped", "AddTransient", "AddSingleton",
        "TryAddScoped", "TryAddTransient", "TryAddSingleton",
        "AddKeyedScoped", "AddKeyedTransient", "AddKeyedSingleton",
    };

    private readonly CallGraph _graph = new();
    private readonly HashSet<ILocalSymbol> _visiting = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<IMethodSymbol, TableResolver> _resolvers = new(SymbolEqualityComparer.Default);

    public CallGraph Extract(CancellationToken cancellationToken = default)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var syntax in tree.GetRoot(cancellationToken).DescendantNodesAndSelf())
            {
                cancellationToken.ThrowIfCancellationRequested();
                Visit(model, syntax);
            }
        }
        return _graph;
    }

    private void Visit(SemanticModel model, SyntaxNode syntax)
    {
        switch (syntax)
        {
            case CompilationUnitSyntax unit when unit.Members.OfType<GlobalStatementSyntax>().Any():
                Body(model.GetDeclaredSymbol(unit), model.GetOperation(unit));
                break;

            case BaseMethodDeclarationSyntax method when method.Body is not null || method.ExpressionBody is not null:
                Body(model.GetDeclaredSymbol(method) as IMethodSymbol, model.GetOperation(method));
                break;

            case AccessorDeclarationSyntax accessor when accessor.Body is not null || accessor.ExpressionBody is not null:
                Body(model.GetDeclaredSymbol(accessor) as IMethodSymbol, model.GetOperation(accessor));
                break;

            case PropertyDeclarationSyntax { ExpressionBody: { } body } property:
                Body((model.GetDeclaredSymbol(property) as IPropertySymbol)?.GetMethod, model.GetOperation(body));
                break;

            case IndexerDeclarationSyntax { ExpressionBody: { } body } indexer:
                Body((model.GetDeclaredSymbol(indexer) as IPropertySymbol)?.GetMethod, model.GetOperation(body));
                break;

            case BaseTypeDeclarationSyntax declaration when model.GetDeclaredSymbol(declaration) is INamedTypeSymbol type:
                AddType(type);
                if (declaration is TypeDeclarationSyntax { ParameterList: not null } primary) PrimaryConstructor(model, primary, type);
                break;

            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax } declarator } initializer:
                Initializer(model.GetDeclaredSymbol(declarator), model.GetOperation(initializer));
                break;

            case EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property } initializer:
                Initializer(model.GetDeclaredSymbol(property), model.GetOperation(initializer));
                break;
        }
    }

    private void Body(IMethodSymbol? symbol, IOperation? body)
    {
        if (symbol is null || body is null) return;

        // A context's query property, such as a generated AccountSet, is read where it is used
        if (symbol is { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol getterOf } && symbols.QueryElement(getterOf) is not null) return;

        Node(symbol);

        foreach (var operation in body.DescendantsAndSelf())
        {
            switch (operation)
            {
                case IAnonymousFunctionOperation lambda:
                    Node(Owner(lambda.Parent, symbol)).Lambdas.Add(Nodes.Id(lambda.Symbol));
                    Node(lambda.Symbol);
                    break;

                case ILocalFunctionOperation local:
                    Node(local.Symbol);
                    break;

                case IInvocationOperation invocation:
                    Invocation(invocation, symbol);
                    break;

                case IObjectCreationOperation creation when Construction(creation, symbol) is { } call:
                    Node(Owner(creation, symbol)).Calls.Add(call);
                    break;

                case IMethodReferenceOperation reference
                    when Call(reference.Method, reference.Instance, reference.IsVirtual, [], reference.Syntax.GetLocation(), symbol) is { } call:
                    Node(Owner(reference, symbol)).Calls.Add(call);
                    break;

                case IPropertyReferenceOperation property when symbols.QueryElement(property.Property) is { } element:
                    Node(Owner(property, symbol)).Accesses.Add(Query(
                        new Site(property.Property.Name, paths.Of(NameLocation(property.Syntax)), ServiceOf(property.Instance, symbol)),
                        Resolver(symbol).TypeArgument(element),
                        property));
                    break;

                case IPropertyReferenceOperation property when Accessor(property, symbol) is { } call:
                    Node(Owner(property, symbol)).Calls.Add(call);
                    break;

                case IReturnOperation { ReturnedValue: { } value } when symbols.IsService(value.Type):
                    Node(Owner(operation, symbol)).Returns.Add(ServiceOf(value, symbol));
                    break;

                case IReturnOperation { ReturnedValue: { } value }:
                    ReturnedTables(Owner(operation, symbol), value, symbol);
                    break;

                case ISimpleAssignmentOperation assignment when FieldOf(assignment.Target) is { } field && symbols.IsService(assignment.Target.Type):
                    Node(Owner(operation, symbol)).FieldWrites.Add(new FieldWrite(field, ServiceOf(assignment.Value, symbol)));
                    break;
            }
        }
    }

    /// <summary>
    /// The tables a method returns, for every way a caller might read them, such as a query built by a helper.
    /// </summary>
    private void ReturnedTables(IMethodSymbol owner, IOperation value, IMethodSymbol root)
    {
        var resolver = Resolver(root);
        var shapes = resolver.ShapesOf(owner.ReturnType);
        if (shapes.Count == 0) return;

        var returns = Node(owner).ReturnTables;
        foreach (var shape in shapes)
        {
            var tables = resolver.Resolve(value, shape);
            returns[shape] = returns.TryGetValue(shape, out var known) ? known.Add(tables) : tables;
        }
    }

    /// <summary>
    /// The table resolver for code in <paramref name="root"/>, which follows calls to methods in the solution. It is
    /// shared, so a local passed to the call it is assigned from, as in <c>x = Prepare(x)</c>, is resolved once.
    /// </summary>
    private TableResolver Resolver(IMethodSymbol root)
    {
        if (!_resolvers.TryGetValue(root, out var resolver))
        {
            _resolvers[root] = resolver = new TableResolver(symbols, rules, invocation =>
                IsInSource(invocation.TargetMethod)
                    ? Call(invocation.TargetMethod, invocation.Instance, invocation.IsVirtual, invocation.Arguments, NameLocation(invocation.Syntax), root)
                    : null, lookups, root);
        }
        return resolver;
    }

    private void Invocation(IInvocationOperation invocation, IMethodSymbol root)
    {
        var owner = Node(Owner(invocation, root));
        if (symbols.ServiceOperation(invocation.TargetMethod) is { } operation)
        {
            owner.Accesses.AddRange(Accesses(invocation, operation, root));
            return;
        }

        if (symbols.ContextOperation(invocation.TargetMethod) is { } contextOperation)
        {
            owner.Accesses.AddRange(ContextAccesses(invocation, contextOperation, root));
            return;
        }

        Registration(invocation);

        if (Call(invocation.TargetMethod, invocation.Instance, invocation.IsVirtual, invocation.Arguments, NameLocation(invocation.Syntax), root) is { } call)
        {
            owner.Calls.Add(call);
        }
    }

    private IEnumerable<AccessFact> Accesses(IInvocationOperation invocation, string operation, IMethodSymbol root)
    {
        var resolver = Resolver(root);
        var first = Argument(invocation, 0);
        var site = new Site(operation, paths.Of(NameLocation(invocation.Syntax)), ServiceOf(invocation.Instance, root));

        switch (operation)
        {
            case "Create":
                return Write(site, resolver, "Create", first, Privilege.Create);
            case "CreateAndReturn":
                return Write(site, resolver, "Create", first, Privilege.Create, Privilege.Read);
            case "Update":
                return Write(site, resolver, "Update", first, Privilege.Write);
            case "Retrieve":
                return [site.Access(Fires("Retrieve", resolver.Name(first)), Privilege.Read)];
            case "Delete":
                return [site.Access(Fires("Delete", resolver.Name(first)), Privilege.Delete)];
            case "RetrieveMultiple":
                return
                [
                    site.Access(
                        resolver.Query(first).OrUnresolved("query has no table name")
                            .Add(TableResolution.Fires("RetrieveMultiple", resolver.Query(first, includeLinks: false).OrUnresolved("query has no table name"))),
                        Privilege.Read),
                ];
            case "Associate":
            case "Disassociate":
                var related = invocation.Arguments.FirstOrDefault(a => symbols.Is(a.Parameter?.Type, symbols.EntityReferenceCollection));
                return
                [
                    site.Access(Fires(operation, resolver.Name(first)), Privilege.Read, Privilege.AppendTo),
                    site.Access(resolver.EntityReferences(related).OrUnresolved("no related records are added"), "related records: ", Privilege.Read, Privilege.Append),
                ];
            case "Execute":
                // One access per privilege the request can need, and one for what it can't be tied to a privilege
                return
                [
                    .. Enum.GetValues(typeof(Privilege)).Cast<Privilege>().Select(p => site.Access(resolver.Requests(first, RequestShapes.For(p)), p)),
                    site.Access(resolver.Requests(first, TableShape.RequestOther)),
                ];
            default:
                return [];
        }
    }

    /// <summary>
    /// The accesses of an <c>OrganizationServiceContext</c> method. Changes are sent by <c>SaveChanges</c>, but
    /// are reported where they are made, which names the record.
    /// </summary>
    private IEnumerable<AccessFact> ContextAccesses(IInvocationOperation invocation, string method, IMethodSymbol root)
    {
        var resolver = Resolver(root);
        var first = Argument(invocation, 0);
        var target = Argument(invocation, 2);
        var site = new Site(method, paths.Of(NameLocation(invocation.Syntax)), ServiceOf(invocation.Instance, root));

        switch (method)
        {
            case "CreateQuery":
                var tables = invocation.TargetMethod.IsGenericMethod ? resolver.TypeArgument(invocation.TargetMethod.TypeArguments[0]) : resolver.Name(first);
                return [Query(site, tables, invocation)];
            case "AddObject":
                return Write(site, resolver, "Create", first, Privilege.Create);
            case "UpdateObject":
                return Write(site, resolver, "Update", first, Privilege.Write);
            case "DeleteObject":
                return [site.Access(Fires("Delete", resolver.Entity(first).OrUnresolved(NoEntityTable)), Privilege.Delete)];
            case "AddLink":
            case "DeleteLink":
                return Link(site, method == "AddLink" ? "Associate" : "Disassociate", resolver, first, target);
            case "AddRelatedObject":
                return [.. Write(site, resolver, "Create", target, Privilege.Create), .. Link(site, "Associate", resolver, first, target)];
            case "LoadProperty":
                return [site.Access(TableResolution.Unresolved("related records are read through a relationship, whose table isn't known"), Privilege.Read)];
            case "Execute":
                return Accesses(invocation, "Execute", root);
            default:
                return [];
        }
    }

    /// <summary>
    /// Read on the tables a query reads. It fires the plugin steps of <c>RetrieveMultiple</c> unless it is joined to
    /// another query, whose table it is linked to.
    /// </summary>
    private static AccessFact Query(Site site, TableResolution tables, IOperation query) =>
        site.Access(IsJoined(query) ? tables : Fires("RetrieveMultiple", tables), Privilege.Read);

    /// <summary>
    /// Writing a record: the privileges on its table, firing <paramref name="message"/> with the columns it sets, and
    /// Append and AppendTo for its lookups.
    /// </summary>
    private static AccessFact[] Write(Site site, TableResolver resolver, string message, IOperation? entity, params Privilege[] privileges) =>
    [
        site.Access(Fires(message, resolver.Entity(entity).OrUnresolved(NoEntityTable), resolver.Columns(entity)), privileges),
        .. Lookups(site, resolver, entity),
    ];

    private static AccessFact[] Link(Site site, string message, TableResolver resolver, IOperation? source, IOperation? target) =>
    [
        site.Access(Fires(message, resolver.Entity(source)), Privilege.Read, Privilege.AppendTo),
        site.Access(resolver.Entity(target), "related records: ", Privilege.Read, Privilege.Append),
    ];

    /// <summary>
    /// Whether <paramref name="query"/> is the inner query of a LINQ join.
    /// </summary>
    private static bool IsJoined(IOperation query)
    {
        var parent = query.Parent;
        while (parent is IConversionOperation) parent = parent.Parent;
        return parent is IArgumentOperation { Parameter.Name: "inner", Parent: IInvocationOperation { TargetMethod.Name: "Join" or "GroupJoin" } };
    }

    /// <summary>
    /// The service a context is created with: its first argument that is a service.
    /// </summary>
    private IReadOnlyList<ServiceOrigin> ContextServiceOf(IEnumerable<IArgumentOperation> arguments, IMethodSymbol root) =>
        arguments.FirstOrDefault(a => symbols.IsService(a.Parameter?.Type)) is { } service
            ? ServiceOf(service.Value, root)
            : [new UnknownOrigin("context is created without a service")];

    /// <summary>
    /// <paramref name="tables"/>, on which the call also fires the plugin steps of <paramref name="message"/>,
    /// setting <paramref name="columns"/>.
    /// </summary>
    private static TableResolution Fires(string message, TableResolution tables, TableResolution? columns = null) =>
        TableResolution.Empty.Add(tables).Add(TableResolution.Fires(message, tables, columns));

    /// <summary>
    /// Append on an entity that gets a lookup, and AppendTo on the tables its lookups refer to.
    /// </summary>
    private static AccessFact[] Lookups(Site site, TableResolver resolver, IOperation? entity) =>
    [
        site.Access(resolver.Lookups(entity, TableShape.LookupHolder), Privilege.Append),
        site.Access(resolver.Lookups(entity, TableShape.LookupTarget), "lookup value: ", Privilege.AppendTo),
    ];

    /// <summary>
    /// A call to a method that is in the solution or may be implemented by it. Calls into the framework are left
    /// out, so the graph only holds calls that can lead to code in the solution.
    /// </summary>
    private CallFact? Call(IMethodSymbol target, IOperation? instance, bool isVirtual, IReadOnlyList<IArgumentOperation> arguments, Location location, IMethodSymbol root)
    {
        if (!IsInSource(target) && (!isVirtual || IsFramework(target.ContainingType))) return null;

        var created = Creation(instance);
        var unwrapped = Unwrap(instance);
        var receiver = target.IsStatic ? ReceiverKind.None
            : target.MethodKind == MethodKind.LocalFunction || IsThis(instance) ? ReceiverKind.This
            : created is not null ? ReceiverKind.Created
            : ReceiverKind.Other;

        var creation = receiver == ReceiverKind.Created ? Construction(created!, root) : null;
        var receiverType = receiver switch
        {
            ReceiverKind.Created => creation?.ReceiverType,
            ReceiverKind.Other when unwrapped?.Type is { } type => Nodes.TypeId(type),
            _ => Nodes.TypeId(target.ContainingType),
        };

        var argumentFacts = Arguments(target, arguments, isVirtual, root);
        if (receiver != ReceiverKind.None && target.MethodKind != MethodKind.Constructor && symbols.IsContext(target.ContainingType))
        {
            argumentFacts[ArgumentFact.ReceiverOrdinal] = new ArgumentFact(ServiceOf(instance, root), null);
        }

        return new CallFact(
            paths.Of(location),
            Nodes.Id(target),
            isVirtual,
            receiver,
            receiverType,
            argumentFacts,
            creation,
            TypeArguments(created?.Type ?? unwrapped?.Type, target, root));
    }

    /// <summary>
    /// The call to the constructor of a created object, so the services it stores can be followed.
    /// </summary>
    private CallFact? Construction(IObjectCreationOperation creation, IMethodSymbol root)
    {
        if (creation.Constructor is not { } constructor || !IsInSource(constructor)) return null;
        return new CallFact(
            paths.Of(creation.Syntax.GetLocation()),
            Nodes.Id(constructor),
            false,
            ReceiverKind.Created,
            Nodes.TypeId(constructor.ContainingType),
            Arguments(constructor, creation.Arguments, false, root),
            TypeArguments: TypeArguments(creation.Type, constructor, root));
    }

    /// <summary>
    /// The tables of the type arguments of each generic type <paramref name="receiver"/> is or derives from, and of
    /// the type that declares <paramref name="target"/>, so a method declared in one of them can read its type
    /// parameters.
    /// </summary>
    private IReadOnlyDictionary<string, IReadOnlyList<TableResolution>>? TypeArguments(ITypeSymbol? receiver, IMethodSymbol target, IMethodSymbol root)
    {
        Dictionary<string, IReadOnlyList<TableResolution>>? result = null;
        var resolver = Resolver(root);
        IEnumerable<ITypeSymbol> types = receiver is null ? [target.ContainingType] : [target.ContainingType, receiver, .. BaseTypes(receiver)];
        foreach (var type in types.OfType<INamedTypeSymbol>())
        {
            var arguments = Nodes.TypeArguments(type);
            if (arguments.Count == 0 || !IsInSource(type)) continue;

            result ??= new Dictionary<string, IReadOnlyList<TableResolution>>(StringComparer.Ordinal);
            result[Nodes.TypeId(type)] = arguments.Select(resolver.TypeArgument).ToList();
        }
        return result;
    }

    /// <summary>
    /// A property with a getter or setter body, which runs as a method.
    /// </summary>
    private CallFact? Accessor(IPropertyReferenceOperation property, IMethodSymbol root)
    {
        var isWrite = property.Parent is IAssignmentOperation assignment && assignment.Target == property;
        var accessor = isWrite ? property.Property.SetMethod : property.Property.GetMethod;
        if (accessor is null || IsAutoProperty(property.Property)) return null;

        var isVirtual = (property.Property.IsVirtual || property.Property.IsAbstract || property.Property.IsOverride
            || property.Property.ContainingType.TypeKind == TypeKind.Interface)
            && Unwrap(property.Instance) is not IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance, Syntax: BaseExpressionSyntax };
        return Call(accessor, property.Instance, isVirtual, property.Arguments, property.Syntax.GetLocation(), root);
    }

    /// <summary>
    /// What each argument passes on: its services, and its tables for every way the callee might read them. The
    /// type arguments of a generic method are passed as tables too, under <see cref="TableSlot.TypeParameterOrdinal"/>.
    /// </summary>
    private SortedDictionary<int, ArgumentFact> Arguments(IMethodSymbol target, IReadOnlyList<IArgumentOperation> arguments, bool isVirtual, IMethodSymbol root)
    {
        var result = new SortedDictionary<int, ArgumentFact>();
        var tablesMatter = isVirtual || IsInSource(target);
        var resolver = Resolver(root);
        foreach (var argument in arguments)
        {
            if (argument.Parameter is not { } parameter || argument.ArgumentKind == ArgumentKind.ParamArray) continue;

            var service = symbols.IsService(parameter.Type) ? ServiceOf(argument.Value, root) : null;

            Dictionary<TableShape, TableResolution>? tables = null;
            if (tablesMatter)
            {
                foreach (var shape in resolver.ShapesOf(parameter.Type))
                {
                    tables ??= new Dictionary<TableShape, TableResolution>();
                    tables[shape] = resolver.Resolve(argument.Value, shape);
                }
            }

            if (service is not null || tables is not null) result[parameter.Ordinal] = new ArgumentFact(service, tables);
        }

        if (tablesMatter && target.IsGenericMethod)
        {
            var typeArguments = (target.GetConstructedReducedFrom() ?? target).TypeArguments;
            for (var i = 0; i < typeArguments.Length; i++)
            {
                result[TableSlot.TypeParameterOrdinal(i)] = new ArgumentFact(null, new Dictionary<TableShape, TableResolution>
                {
                    [TableShape.Entity] = resolver.TypeArgument(typeArguments[i]),
                });
            }
        }
        return result;
    }

    /// <summary>
    /// Where the organization service in <paramref name="operation"/> comes from.
    /// </summary>
    private IReadOnlyList<ServiceOrigin> ServiceOf(IOperation? operation, IMethodSymbol root)
    {
        var op = Unwrap(operation);
        switch (op)
        {
            case null:
                return [new UnknownOrigin("service is missing")];

            case { ConstantValue: { HasValue: true, Value: null } }:
                return [];

            case IParameterReferenceOperation parameter:
                return [Parameter(parameter.Parameter, root)];

            case ILocalReferenceOperation local:
                return Local(local, root);

            case IFieldReferenceOperation field when IsInSource(field.Field):
                return [new FieldOrigin(Nodes.TypeId(field.Field.ContainingType), Nodes.Id(field.Field), !field.Field.IsStatic && IsThis(field.Instance))];

            case IFieldReferenceOperation field:
                return [new UnknownOrigin($"service comes from field '{field.Field.Name}'")];

            case IPropertyReferenceOperation property:
                return Property(property, root);

            case IInvocationOperation invocation:
                return ServiceFromInvocation(invocation, root);

            case IObjectCreationOperation creation when symbols.IsContext(creation.Type):
                return ContextServiceOf(creation.Arguments, root);

            // Code in a context runs as the service of the context it is called on
            case IInstanceReferenceOperation { Type: { } type } when symbols.IsContext(type):
                return [new ParameterOrigin(Nodes.Id(root), ArgumentFact.ReceiverOrdinal, "this")];

            case IObjectCreationOperation creation when !IsInSource(creation.Type):
                // ServiceClient, CrmServiceClient: connects as the application user
                return [new FixedOrigin(RunsAs.ApplicationUser)];

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return [.. ServiceOf(conditional.WhenTrue, root), .. ServiceOf(whenFalse, root)];

            case ICoalesceOperation coalesce:
                return [.. ServiceOf(coalesce.Value, root), .. ServiceOf(coalesce.WhenNull, root)];

            case ICoalesceAssignmentOperation coalesce:
                return [.. ServiceOf(coalesce.Target, root), .. ServiceOf(coalesce.Value, root)];

            case ISimpleAssignmentOperation assignment:
                return ServiceOf(assignment.Value, root);

            default:
                return [new UnknownOrigin("service is computed at runtime")];
        }
    }

    private ServiceOrigin Parameter(IParameterSymbol parameter, IMethodSymbol root)
    {
        // A primary constructor parameter used outside the constructor is a field of the object
        if (parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
            && IsPrimaryConstructor(constructor)
            && !SymbolEqualityComparer.Default.Equals(Nodes.Member(root), constructor))
        {
            var type = Nodes.TypeId(constructor.ContainingType);
            return new FieldOrigin(type, $"{type}.{parameter.Name}", true);
        }
        return new ParameterOrigin(Nodes.Id(parameter.ContainingSymbol), parameter.Ordinal, parameter.Name);
    }

    private IReadOnlyList<ServiceOrigin> Local(ILocalReferenceOperation reference, IMethodSymbol root)
    {
        var local = reference.Local;
        if (!_visiting.Add(local)) return [];
        try
        {
            var body = Root(reference);

            // App code acting for a user: client.CallerId = userId
            var impersonates = body.DescendantsAndSelf().Any(node =>
                node is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.Name: "CallerId" or "CallerAADObjectId" } property }
                && IsLocal(property.Instance, local));
            if (impersonates) return [new FixedOrigin(RunsAs.ImpersonatedCaller)];

            var result = new List<ServiceOrigin>();
            var values = LocalValues(local, body, out var failure);
            foreach (var value in values)
            {
                result.AddRange(ServiceOf(value, root));
            }
            if (failure is not null) result.Add(new UnknownOrigin(failure));
            return result;
        }
        finally
        {
            _visiting.Remove(local);
        }
    }

    private IReadOnlyList<ServiceOrigin> Property(IPropertyReferenceOperation property, IMethodSymbol root)
    {
        var symbol = property.Property;
        if (IsAutoProperty(symbol))
        {
            return [new FieldOrigin(Nodes.TypeId(symbol.ContainingType), Nodes.Id(symbol), !symbol.IsStatic && IsThis(property.Instance))];
        }

        if (IsInSource(symbol))
        {
            return Accessor(property, root) is { } call
                ? [new ReturnedOrigin(call)]
                : [new UnknownOrigin($"service comes from property '{symbol.Name}'")];
        }

        // The plugin context of XrmPluginCore and similar base classes
        return symbol.Name switch
        {
            "OrganizationService" => [new FixedOrigin(RunsAs.Caller)],
            "OrganizationAdminService" => [new FixedOrigin(RunsAs.System)],
            _ => [new UnknownOrigin($"service comes from property '{symbol.Name}'")],
        };
    }

    private IReadOnlyList<ServiceOrigin> ServiceFromInvocation(IInvocationOperation invocation, IMethodSymbol root)
    {
        var method = invocation.TargetMethod;
        if (method.Name == "CreateOrganizationService" && XrmSymbols.Implements(method.ContainingType, symbols.OrganizationServiceFactory))
        {
            return UserArgument(Argument(invocation, 0), root, 0);
        }

        if (method.Name is "GetService" or "GetRequiredService")
        {
            return [new InjectedOrigin()];
        }

        if (IsInSource(method) && Call(method, invocation.Instance, invocation.IsVirtual, invocation.Arguments, NameLocation(invocation.Syntax), root) is { } call)
        {
            return [new ReturnedOrigin(call)];
        }

        return [new UnknownOrigin($"service is returned by '{method.Name}'")];
    }

    /// <summary>
    /// Who a service from <c>CreateOrganizationService(userId)</c> runs as.
    /// </summary>
    private IReadOnlyList<ServiceOrigin> UserArgument(IOperation? argument, IMethodSymbol root, int depth)
    {
        var op = Unwrap(argument);
        switch (op)
        {
            case null:
            case { ConstantValue: { HasValue: true, Value: null } }:
            case IDefaultValueOperation:
                return [new FixedOrigin(RunsAs.System)];

            case IPropertyReferenceOperation { Property.Name: "UserId" or "InitiatingUserId" } property
                when XrmSymbols.Implements(property.Property.ContainingType, symbols.ExecutionContext):
                return [new FixedOrigin(RunsAs.Caller, property.Property.Name == "InitiatingUserId")];

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional when depth < MaxConstantDepth:
                return [.. UserArgument(conditional.WhenTrue, root, depth + 1), .. UserArgument(whenFalse, root, depth + 1)];

            case ILocalReferenceOperation local when depth < MaxConstantDepth && _visiting.Add(local.Local):
                try
                {
                    var values = LocalValues(local.Local, Root(local), out var failure);
                    var result = values.SelectMany(v => UserArgument(v, root, depth + 1)).ToList();
                    if (failure is not null) result.Add(new UnknownOrigin(failure));
                    return result;
                }
                finally
                {
                    _visiting.Remove(local.Local);
                }

            case IFieldReferenceOperation field when depth < MaxConstantDepth && FieldInitializer(field.Field) is { } initializer:
                return UserArgument(initializer, root, depth + 1);
        }

        if (GuidConstant(op) is { } userId) return [new FixedOrigin(RunsAs.User(userId))];

        return op is IParameterReferenceOperation parameter
            ? [new UnknownOrigin($"user id comes from parameter '{parameter.Parameter.Name}'")]
            : [new UnknownOrigin("user id is computed at runtime")];
    }

    /// <summary>
    /// A user id written in code: <c>new Guid("…")</c> or <c>Guid.Parse("…")</c>.
    /// </summary>
    internal static string? GuidConstant(IOperation operation)
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
    /// The value of a readonly or constant field set by its declaration, such as a user id.
    /// </summary>
    private IOperation? FieldInitializer(IFieldSymbol field)
    {
        if (!field.IsReadOnly && !field.IsConst) return null;
        foreach (var reference in field.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is VariableDeclaratorSyntax { Initializer: { } initializer }
                && compilation.ContainsSyntaxTree(initializer.SyntaxTree)
                && compilation.GetSemanticModel(initializer.SyntaxTree).GetOperation(initializer) is IFieldInitializerOperation operation)
            {
                return operation.Value;
            }
        }
        return null;
    }

    private void Initializer(ISymbol? member, IOperation? initializer)
    {
        if (member?.ContainingType is not { } type || initializer is not ISymbolInitializerOperation { Value: { } value }) return;
        if (!symbols.IsService(value.Type) && !symbols.IsService((member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type)) return;

        AddType(type);
        var constructor = type.InstanceConstructors.FirstOrDefault(IsPrimaryConstructor) ?? type.InstanceConstructors.FirstOrDefault();
        if (constructor is null) return;
        _graph.Types[Nodes.TypeId(type)].Initializers.Add(new FieldWrite(Nodes.Id(member), ServiceOf(value, constructor)));
    }

    /// <summary>
    /// A primary constructor stores its service parameters, which the type's members read as fields.
    /// </summary>
    private void PrimaryConstructor(SemanticModel model, TypeDeclarationSyntax declaration, INamedTypeSymbol type)
    {
        var constructor = type.InstanceConstructors.FirstOrDefault(c => c.DeclaringSyntaxReferences.Any(r => r.GetSyntax() == declaration));
        if (constructor is null) return;

        var node = Node(constructor);
        var typeId = Nodes.TypeId(type);
        foreach (var parameter in constructor.Parameters.Where(p => symbols.IsService(p.Type)))
        {
            node.FieldWrites.Add(new FieldWrite($"{typeId}.{parameter.Name}", [new ParameterOrigin(node.Id, parameter.Ordinal, parameter.Name)]));
        }

        if (declaration.BaseList?.Types.FirstOrDefault() is PrimaryConstructorBaseTypeSyntax baseType
            && model.GetOperation(baseType) is { } operation)
        {
            foreach (var invocation in operation.DescendantsAndSelf().OfType<IInvocationOperation>())
            {
                Invocation(invocation, constructor);
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> BaseTypes(ITypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType) yield return current;
    }

    private void AddType(INamedTypeSymbol type)
    {
        var id = Nodes.TypeId(type);
        if (_graph.Types.ContainsKey(id) || type.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface)) return;

        var ancestors = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var current in BaseTypes(type).Where(t => t.SpecialType != SpecialType.System_Object)) ancestors.Add(Nodes.TypeId(current));
        foreach (var @interface in type.AllInterfaces) ancestors.Add(Nodes.TypeId(@interface));

        var summary = new TypeSummary(id, type.IsAbstract || type.IsStatic || type.TypeKind == TypeKind.Interface, type.TypeKind == TypeKind.Interface, ancestors.ToList());

        var typeParameters = Nodes.TypeParameters(type);
        if (typeParameters.Count > 0)
        {
            foreach (var ancestor in type.AllInterfaces.Concat(BaseTypes(type)))
            {
                var arguments = Nodes.TypeArguments(ancestor);
                if (arguments.Count == 0 || !IsInSource(ancestor)) continue;
                summary.AncestorTypeArguments[Nodes.TypeId(ancestor)] = arguments.Select(a => a is ITypeParameterSymbol p ? typeParameters.IndexOf(p) : -1).ToList();
            }
        }

        // Most derived first, so an override replaces the method it overrides
        foreach (var current in BaseTypes(type).Prepend(type))
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.IsStatic || method.IsAbstract || method.MethodKind == MethodKind.Constructor || !IsInSource(method)) continue;
                var implementation = Nodes.Id(method);
                for (var overridden = method; overridden is not null; overridden = overridden.OverriddenMethod)
                {
                    var key = Nodes.Id(overridden);
                    if (!summary.Implementations.ContainsKey(key)) summary.Implementations.Add(key, implementation);
                }
            }
        }

        if (type.TypeKind != TypeKind.Interface)
        {
            foreach (var @interface in type.AllInterfaces)
            {
                foreach (var member in @interface.GetMembers().OfType<IMethodSymbol>())
                {
                    if (type.FindImplementationForInterfaceMember(member) is IMethodSymbol implementation && IsInSource(implementation))
                    {
                        var key = Nodes.Id(member);
                        if (!summary.Implementations.ContainsKey(key)) summary.Implementations.Add(key, Nodes.Id(implementation));
                    }
                }
            }
        }

        foreach (var constructor in type.InstanceConstructors.Where(IsInSource))
        {
            summary.Constructors.Add(Nodes.Id(constructor));
        }

        _graph.Types.Add(id, summary);
    }

    /// <summary>
    /// Records <c>services.AddScoped&lt;IFoo, Foo&gt;()</c> and the other <c>IServiceCollection</c> registrations.
    /// </summary>
    private void Registration(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        if (!RegistrationMethods.Contains(method.Name)
            || method.ContainingNamespace?.ToDisplayString().StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal) != true)
        {
            return;
        }

        var typeArguments = method.TypeArguments;
        if (typeArguments.Length == 2)
        {
            Register(typeArguments[0], typeArguments[1]);
        }
        else if (typeArguments.Length == 1)
        {
            var implementations = invocation.Arguments.SelectMany(a => CreatedTypes(a.Value)).ToList();
            if (implementations.Count == 0) implementations.Add(typeArguments[0]);
            foreach (var implementation in implementations) Register(typeArguments[0], implementation);
        }
        else
        {
            var types = invocation.Arguments.Select(a => Unwrap(a.Value)).OfType<ITypeOfOperation>().Select(t => t.TypeOperand).ToList();
            if (types.Count > 0) Register(types[0], types.Count > 1 ? types[1] : types[0]);
        }
    }

    /// <summary>
    /// The types a registration creates: <c>new Foo(…)</c> returned by a factory lambda, or an instance.
    /// </summary>
    private static IEnumerable<ITypeSymbol> CreatedTypes(IOperation value)
    {
        var op = Unwrap(value);
        if (op is IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda })
        {
            return lambda.Body.DescendantsAndSelf()
                .OfType<IReturnOperation>()
                .Select(r => Unwrap(r.ReturnedValue))
                .OfType<IObjectCreationOperation>()
                .Select(c => c.Type)
                .OfType<ITypeSymbol>();
        }
        return op is IObjectCreationOperation { Type: { } type } ? [type] : [];
    }

    private void Register(ITypeSymbol service, ITypeSymbol implementation)
    {
        if (service is INamedTypeSymbol && implementation is INamedTypeSymbol)
        {
            _graph.Register(Nodes.TypeId(service), Nodes.TypeId(implementation));
        }
    }

    private NodeSummary Node(IMethodSymbol symbol)
    {
        var id = Nodes.Id(symbol);
        if (_graph.Nodes.TryGetValue(id, out var node)) return node;

        var member = Nodes.Member(symbol) ?? symbol;
        node = new NodeSummary(
            id,
            Nodes.Id(member),
            Nodes.Display(symbol),
            member.ContainingType is { } type ? Nodes.TypeId(type) : null,
            symbol.MethodKind == MethodKind.Constructor,
            Declaration(symbol),
            Declaration(member));
        _graph.Nodes.Add(id, node);
        return node;
    }

    private SourceLocation? Declaration(ISymbol symbol) =>
        symbol.Locations.FirstOrDefault(l => l.IsInSource) is { } location ? paths.Of(location) : null;

    /// <summary>
    /// The node an operation belongs to: the innermost lambda or local function around it, or the member.
    /// </summary>
    private static IMethodSymbol Owner(IOperation? operation, IMethodSymbol root)
    {
        for (var current = operation; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case IAnonymousFunctionOperation lambda: return lambda.Symbol;
                case ILocalFunctionOperation local: return local.Symbol;
            }
        }
        return root;
    }

    /// <summary>
    /// The field a service is stored in, when it is a field or auto-property of the current object.
    /// </summary>
    private static string? FieldOf(IOperation target) => target switch
    {
        IFieldReferenceOperation field when IsInSource(field.Field) && (field.Field.IsStatic || IsThis(field.Instance)) => Nodes.Id(field.Field),
        IPropertyReferenceOperation property when IsAutoProperty(property.Property) && (property.Property.IsStatic || IsThis(property.Instance)) => Nodes.Id(property.Property),
        _ => null,
    };

    /// <summary>
    /// Every value assigned to <paramref name="local"/> in its method. <paramref name="failure"/> says why some
    /// assignment can't be followed.
    /// </summary>
    private static List<IOperation> LocalValues(ILocalSymbol local, IOperation root, out string? failure)
    {
        failure = null;
        var values = new List<IOperation>();
        foreach (var node in root.DescendantsAndSelf())
        {
            switch (node)
            {
                case IVariableDeclaratorOperation declarator when SymbolEqualityComparer.Default.Equals(declarator.Symbol, local):
                    if (declarator.GetVariableInitializer() is { } initializer) values.Add(initializer.Value);
                    break;
                case ISimpleAssignmentOperation assignment when IsLocal(assignment.Target, local):
                    values.Add(assignment.Value);
                    break;
                case ICoalesceAssignmentOperation assignment when IsLocal(assignment.Target, local):
                    values.Add(assignment.Value);
                    break;
                case IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument when IsLocal(argument.Value, local):
                    failure = $"local '{local.Name}' is passed by ref or out";
                    break;
                case IForEachLoopOperation loop when loop.Locals.Contains(local, SymbolEqualityComparer.Default):
                    failure = $"local '{local.Name}' is a loop variable";
                    break;
                case IDeclarationPatternOperation pattern when SymbolEqualityComparer.Default.Equals(pattern.DeclaredSymbol, local):
                    failure = $"local '{local.Name}' is declared by a pattern";
                    break;
            }
        }
        return values;
    }

    /// <summary>
    /// The object creation a receiver holds: <c>new Foo().M()</c>, or a local assigned only that object.
    /// </summary>
    private static IObjectCreationOperation? Creation(IOperation? instance)
    {
        switch (Unwrap(instance))
        {
            case IObjectCreationOperation creation:
                return creation;
            case ILocalReferenceOperation local:
                var values = LocalValues(local.Local, Root(local), out var failure);
                return failure is null && values.Count == 1 ? Unwrap(values[0]) as IObjectCreationOperation : null;
            default:
                return null;
        }
    }

    private static bool IsThis(IOperation? instance) =>
        instance is null || Unwrap(instance) is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };

    private static bool IsLocal(IOperation? operation, ILocalSymbol local) =>
        Unwrap(operation) is ILocalReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Local, local);

    private static bool IsInSource(ISymbol? symbol) => symbol?.Locations.Any(l => l.IsInSource) == true;

    private static bool IsPrimaryConstructor(IMethodSymbol constructor) =>
        constructor.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax);

    /// <summary>
    /// A property without accessor bodies, which stores its value like a field.
    /// </summary>
    private static bool IsAutoProperty(IPropertySymbol property) =>
        IsInSource(property)
        && property.DeclaringSyntaxReferences.All(r => r.GetSyntax() is PropertyDeclarationSyntax
        {
            ExpressionBody: null,
            AccessorList: var accessors,
        } && (accessors is null || accessors.Accessors.All(a => a.Body is null && a.ExpressionBody is null)))
        && !property.IsAbstract;

    private static bool IsFramework(INamedTypeSymbol? type)
    {
        var root = type?.ContainingNamespace;
        while (root is { ContainingNamespace.IsGlobalNamespace: false }) root = root.ContainingNamespace;
        return root?.Name is "System" or "Microsoft";
    }

    private static IOperation? Argument(IInvocationOperation invocation, int ordinal) =>
        invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == ordinal);

    private static IOperation Root(IOperation operation)
    {
        while (operation.Parent is not null) operation = operation.Parent;
        return operation;
    }

    private static IOperation? Unwrap(IOperation? operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversion: operation = conversion.Operand; break;
                case IArgumentOperation argument: operation = argument.Value; break;
                case IParenthesizedOperation parenthesized: operation = parenthesized.Operand; break;
                case IDeclarationExpressionOperation declaration: operation = declaration.Expression; break;
                default: return operation;
            }
        }
    }

    private static Location NameLocation(SyntaxNode syntax) => syntax switch
    {
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } => access.Name.GetLocation(),
        InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax binding } => binding.Name.GetLocation(),
        MemberAccessExpressionSyntax access => access.Name.GetLocation(),
        MemberBindingExpressionSyntax binding => binding.Name.GetLocation(),
        _ => syntax.GetLocation(),
    };

    private sealed record Site(string Operation, SourceLocation Source, IReadOnlyList<ServiceOrigin> Service)
    {
        public AccessFact Access(TableResolution tables, params Privilege[] privileges) => Access(tables, "", privileges);

        public AccessFact Access(TableResolution tables, string reasonPrefix, params Privilege[] privileges) =>
            new(Operation, Source, privileges, tables, reasonPrefix, Service);
    }
}
