using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// Works out which tables a value refers to: early-bound types, constants, FetchXML and assignments to locals
/// in the same method. Anything else becomes a reason in the resolution, never a guess.
/// </summary>
/// <remarks>
/// Parameters and type parameters of the method become slots, filled in per call site. <paramref name="calls"/>
/// turns a call to a method in the solution into the call graph's view of it, so the tables of its return value
/// are resolved per call context too; without it such a value is unresolved. Requests are read through
/// <paramref name="rules"/>.
/// </remarks>
internal sealed class TableResolver(XrmSymbols symbols, RequestRules? rules = null, Func<IInvocationOperation, CallFact?>? calls = null)
{
    // The access rights of AccessRights, as in PrincipalAccess.AccessMask
    private static readonly (int Bit, Privilege Privilege)[] AccessRights =
    [
        (1, Privilege.Read), (2, Privilege.Write), (4, Privilege.Append), (16, Privilege.AppendTo),
        (32, Privilege.Create), (65536, Privilege.Delete), (262144, Privilege.Share), (524288, Privilege.Assign),
    ];

    private readonly RequestRules _rules = rules ?? new RequestRules();

    // Stands in for runtime text inside a FetchXML template. Private use area, so real FetchXML never contains it.
    private const char Hole = '';

    // Concatenating locals with several assignments multiplies the variants; stop before that gets out of hand.
    private const int MaxTemplates = 64;

    private const int MaxNesting = 4;
    private const string NestedLinksReason = "tables linked through LinkEntities, LinkEntity.AddLink or new LinkEntity are not analyzed yet";

    private readonly HashSet<ILocalSymbol> _visiting = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// A table logical name, such as the <c>entityName</c> argument of <c>Retrieve</c>.
    /// </summary>
    public TableResolution Name(IOperation? operation)
    {
        var op = Unwrap(operation);
        if (op is null) return TableResolution.Unresolved("table name argument is missing");

        if (op.ConstantValue.HasValue)
        {
            return op.ConstantValue.Value is string { Length: > 0 } name
                ? TableResolution.Table(name)
                : TableResolution.Unresolved("table name is null or empty");
        }

        return op switch
        {
            ILocalReferenceOperation local => FromLocal(local, Name),
            IPropertyReferenceOperation { Property.Name: "LogicalName", Instance: { } instance } property
                when IsEntityOrReference(property.Property.ContainingType) => Entity(instance),
            IConditionalOperation { WhenFalse: { } whenFalse } conditional => Name(conditional.WhenTrue).Add(Name(whenFalse)),
            ICoalesceOperation coalesce => Name(coalesce.Value).Add(Name(coalesce.WhenNull)),
            _ => Unknown(op, "table name", TableShape.Name),
        };
    }

    /// <summary>
    /// The table of an <c>Entity</c> or <c>EntityReference</c> value.
    /// </summary>
    public TableResolution Entity(IOperation? operation)
    {
        var op = operation;
        while (true)
        {
            if (op is null) return TableResolution.Unresolved("entity argument is missing");
            if (symbols.LogicalNameOf(op.Type) is { } logicalName) return TableResolution.Table(logicalName);
            if (op.Type is ITypeParameterSymbol typeParameter) return TypeParameter(typeParameter);

            var inner = UnwrapOnce(op);
            if (inner is null) break;
            op = inner;
        }

        switch (op)
        {
            case IObjectCreationOperation creation:
                return AssignedMember(creation.Initializer, "LogicalName", Name)
                    ?? (FirstStringArgument(creation) is { } nameArgument ? Name(nameArgument.Value) : TableResolution.Empty);

            case ILocalReferenceOperation local:
                return FromLocal(local, Entity, EntityElements, (node, symbol) =>
                    node is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.Name: "LogicalName" } property } assignment
                    && IsLocal(property.Instance, symbol)
                        ? Name(assignment.Value)
                        : null);

            case IInvocationOperation { TargetMethod.Name: "ToEntityReference", Instance: { } instance }:
                return Entity(instance);

            case IPropertyReferenceOperation { Property.Name: "Entities", Instance: { } records } property
                when symbols.Is(property.Property.ContainingType, symbols.EntityCollection):
                return Records(records);

            case IParameterReferenceOperation parameter when LambdaSource(parameter) is { } source:
                return EntityElements(source);

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Entity(conditional.WhenTrue).Add(Entity(whenFalse));

            case ICoalesceOperation coalesce:
                return Entity(coalesce.Value).Add(Entity(coalesce.WhenNull));

            default:
                return Unknown(op, "entity", TableShape.Entity);
        }
    }

    /// <summary>
    /// The tables a <c>QueryBase</c> value reads: the main table plus, with <paramref name="includeLinks"/>,
    /// every linked table. The records a query returns belong to the main table only.
    /// </summary>
    public TableResolution Query(IOperation? operation, bool includeLinks = true)
    {
        var op = Unwrap(operation);
        switch (op)
        {
            case null:
                return TableResolution.Unresolved("query argument is missing");

            case IObjectCreationOperation creation when symbols.Is(creation.Type, symbols.FetchExpression):
                return AssignedMember(creation.Initializer, "Query", v => Fetch(v, includeLinks))
                    ?? (FirstStringArgument(creation) is { } fetchArgument ? Fetch(fetchArgument.Value, includeLinks) : TableResolution.Empty);

            case IObjectCreationOperation creation when IsTableQuery(creation.Type):
            {
                var result = AssignedMember(creation.Initializer, "EntityName", Name)
                    ?? (FirstStringArgument(creation) is { } nameArgument ? Name(nameArgument.Value) : TableResolution.Empty);
                return includeLinks && symbols.Is(creation.Type, symbols.QueryExpression) ? result.Add(NestedLinks(creation)) : result;
            }

            case ILocalReferenceOperation local:
            {
                var result = FromLocal(local, v => Query(v, includeLinks), null, (node, symbol) => node switch
                {
                    ISimpleAssignmentOperation { Target: IPropertyReferenceOperation property } assignment when IsLocal(property.Instance, symbol) =>
                        property.Property.Name switch
                        {
                            "EntityName" => Name(assignment.Value),
                            "Query" when symbols.Is(property.Property.ContainingType, symbols.FetchExpression) => Fetch(assignment.Value, includeLinks),
                            _ => null,
                        },
                    IInvocationOperation { TargetMethod.Name: "AddLink" } addLink
                        when includeLinks && IsLocal(addLink.Instance, symbol) && symbols.Is(addLink.TargetMethod.ContainingType, symbols.QueryExpression) =>
                        FirstStringArgument(addLink) is { } linkArgument ? Name(linkArgument.Value) : null,
                    _ => null,
                });
                return includeLinks && symbols.Is(local.Type, symbols.QueryExpression) ? result.Add(NestedLinks(local)) : result;
            }

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Query(conditional.WhenTrue, includeLinks).Add(Query(whenFalse, includeLinks));

            case ICoalesceOperation coalesce:
                return Query(coalesce.Value, includeLinks).Add(Query(coalesce.WhenNull, includeLinks));

            default:
                return Unknown(op, "query", includeLinks ? TableShape.Query : TableShape.QueryMainTable);
        }
    }

    /// <summary>
    /// The tables of the records in an <c>EntityReferenceCollection</c>, such as the related records of <c>Associate</c>.
    /// </summary>
    public TableResolution EntityReferences(IOperation? operation)
    {
        var op = Unwrap(operation);
        switch (op)
        {
            case null:
                return TableResolution.Unresolved("related records argument is missing");

            case IObjectCreationOperation creation when symbols.Is(creation.Type, symbols.EntityReferenceCollection):
            {
                var result = TableResolution.Empty;
                foreach (var argument in creation.Arguments) result.Add(Elements(argument.Value));
                foreach (var add in creation.Initializer?.Initializers.OfType<IInvocationOperation>() ?? [])
                {
                    foreach (var argument in add.Arguments) result.Add(Entity(argument.Value));
                }
                return result;
            }

            case ILocalReferenceOperation local:
                return FromLocal(local, EntityReferences, null, (node, symbol) => node switch
                {
                    IInvocationOperation { TargetMethod.Name: "Add" } add when IsLocal(add.Instance, symbol) =>
                        add.Arguments.Aggregate(TableResolution.Empty, (result, argument) => result.Add(Entity(argument.Value))),
                    IInvocationOperation { TargetMethod.Name: var name } call when IsLocal(call.Instance, symbol) && name is "AddRange" or "Insert" or "InsertRange" =>
                        TableResolution.Unresolved($"local '{symbol.Name}' gets records through {name}"),
                    _ => null,
                });

            default:
                return Elements(op);
        }
    }

    /// <summary>
    /// The tables named in FetchXML: the <c>entity</c> element plus, with <paramref name="includeLinks"/>,
    /// every <c>link-entity</c>.
    /// </summary>
    public TableResolution Fetch(IOperation? operation, bool includeLinks = true)
    {
        var result = TableResolution.Empty;
        foreach (var template in Templates(operation, topLevel: true, result, includeLinks))
        {
            result.Add(ParseFetch(template, includeLinks));
        }
        return result;
    }

    /// <summary>
    /// The tables of a value read as <paramref name="shape"/>, such as an argument bound to a parameter slot.
    /// </summary>
    public TableResolution Resolve(IOperation? operation, TableShape shape) => shape switch
    {
        TableShape.Name => Name(operation),
        TableShape.Entity => Entity(operation),
        TableShape.Query => Query(operation),
        TableShape.QueryMainTable => Query(operation, includeLinks: false),
        TableShape.Fetch => Fetch(operation),
        TableShape.FetchMainTable => Fetch(operation, includeLinks: false),
        TableShape.EntityReferences => EntityReferences(operation),
        TableShape.Records => Records(operation),
        _ => Requests(operation, shape),
    };

    /// <summary>
    /// The table of a type argument, such as <c>Account</c> in <c>repository.Retrieve&lt;Account&gt;()</c>.
    /// </summary>
    public TableResolution TypeArgument(ITypeSymbol type) =>
        symbols.LogicalNameOf(type) is { } logicalName ? TableResolution.Table(logicalName)
        : type is ITypeParameterSymbol typeParameter ? TypeParameter(typeParameter)
        : TableResolution.Unresolved($"type '{type.Name}' has no table");

    /// <summary>
    /// The shapes a parameter of <paramref name="type"/> can be read as; empty when it can't name a table.
    /// </summary>
    public IReadOnlyList<TableShape> ShapesOf(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return [TableShape.Name, TableShape.Fetch, TableShape.FetchMainTable];
        if (IsEntityOrReference(type) || type is ITypeParameterSymbol) return [TableShape.Entity];
        if (symbols.Is(type, symbols.QueryBase)) return [TableShape.Query, TableShape.QueryMainTable];
        if (symbols.Is(type, symbols.EntityReferenceCollection)) return [TableShape.EntityReferences];
        if (symbols.Is(type, symbols.EntityCollection)) return [TableShape.Records];
        if (IsRequestLike(type)) return RequestShapes.All;
        if (ElementType(type) is { } element && IsEntityLike(element)) return [TableShape.Records];
        return [];
    }

    /// <summary>
    /// The tables a request, or every request in a collection of them, needs the privilege of
    /// <paramref name="shape"/> on, by the request's rule. Read as <see cref="TableShape.RequestOther"/>, it gives
    /// what can't be tied to a privilege instead: requests without a rule, and requests that can't be followed.
    /// </summary>
    public TableResolution Requests(IOperation? operation, TableShape shape)
    {
        var op = Unwrap(operation);
        switch (op)
        {
            case null:
                return Other(shape, "request argument is missing");

            // Executing null throws, so such a branch, usually one the caller guards against, needs nothing
            case { ConstantValue: { HasValue: true, Value: null } }:
                return TableResolution.Empty;

            case IObjectCreationOperation creation when symbols.Is(creation.Type, symbols.OrganizationRequest):
                return Request(creation, null, shape);

            case IObjectCreationOperation creation:
            {
                // A collection of requests, such as OrganizationRequestCollection or List<OrganizationRequest>
                var result = TableResolution.Empty;
                foreach (var argument in creation.Arguments.Where(a => IsRequestLike(a.Value.Type))) result.Add(Requests(argument.Value, shape));
                foreach (var add in creation.Initializer?.Initializers.OfType<IInvocationOperation>() ?? [])
                {
                    foreach (var argument in add.Arguments) result.Add(Requests(argument.Value, shape));
                }
                return result;
            }

            case IArrayCreationOperation or ICollectionExpressionOperation:
                return CollectionElements(op).Aggregate(TableResolution.Empty, (result, element) => result.Add(Requests(element, shape)));

            case ILocalReferenceOperation local:
                return FromLocal(
                    local,
                    value => Unwrap(value) is IObjectCreationOperation creation && symbols.Is(creation.Type, symbols.OrganizationRequest)
                        ? Request(creation, local.Local, shape)
                        : Requests(value, shape),
                    collection => Requests(collection, shape),
                    (node, symbol) => node is IInvocationOperation { TargetMethod.Name: "Add" or "AddRange" or "Insert" or "InsertRange" } add && IsLocal(add.Instance, symbol)
                        ? add.Arguments.Where(a => IsRequestLike(Unwrap(a.Value)?.Type)).Aggregate(TableResolution.Empty, (result, a) => result.Add(Requests(a.Value, shape)))
                        : null);

            case IParameterReferenceOperation parameter when LambdaSource(parameter) is { } source:
                return Requests(source, shape);

            case IInvocationOperation invocation when IsFramework(invocation):
                return Passthrough(invocation, t => IsRequestLike(t), value => Requests(value, shape), value => Requests(value, shape)) ?? UnknownRequest(op, shape);

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Requests(conditional.WhenTrue, shape).Add(Requests(whenFalse, shape));

            case ICoalesceOperation coalesce:
                return Requests(coalesce.Value, shape).Add(Requests(coalesce.WhenNull, shape));

            default:
                return UnknownRequest(op, shape);
        }
    }

    /// <summary>
    /// A created request, read by the rule for its type, or for its <c>RequestName</c> when it is an
    /// <c>OrganizationRequest</c>. Its members are set in its initializer or, for <paramref name="owner"/>, through
    /// the local it is assigned to.
    /// </summary>
    private TableResolution Request(IObjectCreationOperation creation, ILocalSymbol? owner, TableShape shape)
    {
        List<IOperation> Members(string member) => MemberValues(creation, owner, member);

        if (!SymbolEqualityComparer.Default.Equals(creation.Type?.OriginalDefinition, symbols.OrganizationRequest))
        {
            return Apply(RequestRules.MessageOf(creation.Type!.Name), Members, shape);
        }

        var names = Members("RequestName");
        if (FirstStringArgument(creation) is { } nameArgument) names.Add(nameArgument.Value);
        if (names.Count == 0) return Other(shape, "request has no name");

        var result = TableResolution.Empty;
        foreach (var name in names)
        {
            result.Add(Unwrap(name)?.ConstantValue is { HasValue: true, Value: string { Length: > 0 } message }
                ? Apply(RequestRules.MessageOf(message), Members, shape)
                : Other(shape, "request name is computed at runtime"));
        }
        return result;
    }

    /// <summary>
    /// The tables <paramref name="message"/> needs the privilege of <paramref name="shape"/> on, with the values of
    /// the request's members from <paramref name="members"/>.
    /// </summary>
    private TableResolution Apply(string message, Func<string, List<IOperation>> members, TableShape shape)
    {
        if (_rules.Find(message) is not { } rule) return Other(shape, RequestReasons.NoRule(message));

        var result = TableResolution.Empty;
        foreach (var missing in rule.Missing) result.Add(Other(shape, RequestReasons.NoRule(missing)));
        foreach (var nested in rule.Requests)
        {
            foreach (var value in members(nested)) result.Add(Requests(value, shape));
        }

        var privilege = RequestShapes.PrivilegeOf(shape);
        foreach (var check in rule.Checks)
        {
            if (check.When is { } when && !members(when).Any(v => Unwrap(v)?.ConstantValue is not { HasValue: true, Value: false })) continue;

            var needed = privilege is { } p && check.Privileges.Contains(p);
            if (check.AccessMask is { } mask)
            {
                var rights = Rights(mask, members, out var reasons);
                if (shape == TableShape.RequestOther) result.Add(reasons);
                needed |= privilege is { } granted && rights.Contains(granted);
            }
            if (!needed) continue;

            if (check.Table is { } table)
            {
                result.Add(TableResolution.Table(table));
            }
            else if (check.Member is { } member)
            {
                var values = members(member);
                if (values.Count == 0) result.Add(TableResolution.Unresolved($"{member} of the {message} request isn't set where the request is created"));
                foreach (var value in values) result.Add(MemberTables(value));
            }
        }
        return result;
    }

    /// <summary>
    /// The tables of a request member's value, read by its type: an entity or reference, a table name, a query, or
    /// a collection of records.
    /// </summary>
    private TableResolution MemberTables(IOperation value)
    {
        var op = Unwrap(value);
        var shapes = op?.Type is { } type ? ShapesOf(type).Where(s => !RequestShapes.IsRequest(s)).ToList() : [];
        return shapes.Count > 0 ? Resolve(op, shapes[0]) : TableResolution.Unresolved(Describe(op, "request member"));
    }

    /// <summary>
    /// The privileges of the access rights in the member at <paramref name="path"/>, such as
    /// <c>PrincipalAccess.AccessMask</c>.
    /// </summary>
    private List<Privilege> Rights(string path, Func<string, List<IOperation>> members, out TableResolution reasons)
    {
        var segments = path.Split('.');
        var values = members(segments[0]);
        foreach (var segment in segments.Skip(1)) values = values.SelectMany(v => ObjectMemberValues(v, segment)).ToList();

        reasons = TableResolution.Empty;
        if (values.Count == 0) reasons.Add(TableResolution.Unresolved($"{path} isn't set where the request is created"));

        var privileges = new List<Privilege>();
        foreach (var value in values)
        {
            if (Unwrap(value)?.ConstantValue is { HasValue: true, Value: { } constant } && constant is int or uint or long)
            {
                var mask = Convert.ToInt64(constant);
                privileges.AddRange(AccessRights.Where(r => (mask & r.Bit) != 0).Select(r => r.Privilege));
            }
            else
            {
                reasons.Add(TableResolution.Unresolved($"{path} is computed at runtime"));
            }
        }
        return privileges;
    }

    /// <summary>
    /// The values a member of a created object is set to: by its constructor argument of the same name, in its
    /// initializer, and, for <paramref name="owner"/>, through the local that holds it. A <c>Parameters</c>
    /// entry or indexer with the member's name counts as the member.
    /// </summary>
    private List<IOperation> MemberValues(IObjectCreationOperation creation, ILocalSymbol? owner, string member)
    {
        var values = creation.Arguments
            .Where(a => string.Equals(a.Parameter?.Name, member, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Value)
            .ToList();
        if (creation.Initializer is { } initializer) values.AddRange(Inside(initializer).SelectMany(node => SetMember(node, IsImplicitReceiver, member)));
        if (owner is not null) values.AddRange(Root(creation).DescendantsAndSelf().SelectMany(node => SetMember(node, instance => IsLocal(instance, owner), member)));
        return values;
    }

    /// <summary>
    /// The values a member is set to on an object that is created here or held in a local.
    /// </summary>
    private List<IOperation> ObjectMemberValues(IOperation value, string member) => Unwrap(value) switch
    {
        IObjectCreationOperation creation => MemberValues(creation, null, member),
        ILocalReferenceOperation local => LocalValues(local.Local, Root(local), TableResolution.Empty)
            .Where(v => !v.IsLoopCollection)
            .SelectMany(v => Unwrap(v.Operation) is IObjectCreationOperation creation ? MemberValues(creation, null, member) : [])
            .Concat(Root(local).DescendantsAndSelf().SelectMany(node => SetMember(node, instance => IsLocal(instance, local.Local), member)))
            .ToList(),
        _ => [],
    };

    /// <summary>
    /// The value <paramref name="node"/> sets <paramref name="member"/> to on an object <paramref name="isObject"/>
    /// accepts: <c>o.Member = x</c>, <c>o["Member"] = x</c>, <c>o.Parameters["Member"] = x</c>,
    /// <c>o.Parameters.Add("Member", x)</c>, or what <c>o.Member.Add(x)</c> adds to a collection member.
    /// </summary>
    private static IEnumerable<IOperation> SetMember(IOperation node, Func<IOperation?, bool> isObject, string member)
    {
        bool IsOwnedBy(IOperation? instance) =>
            isObject(instance) || (Unwrap(instance) is IPropertyReferenceOperation { Property.Name: "Parameters" } parameters && isObject(parameters.Instance));

        switch (node)
        {
            case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.IsIndexer: false } property } assignment
                when property.Property.Name == member && isObject(property.Instance):
                return [assignment.Value];

            case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.IsIndexer: true } indexer } assignment
                when IsOwnedBy(indexer.Instance) && indexer.Arguments.Length == 1 && IsKey(indexer.Arguments[0].Value, member):
                return [assignment.Value];

            case IInvocationOperation { TargetMethod.Name: "Add", Arguments.Length: 2 } add
                when IsOwnedBy(add.Instance) && IsKey(add.Arguments[0].Value, member):
                return [add.Arguments[1].Value];

            case IInvocationOperation { TargetMethod.Name: "Add" or "AddRange" } add
                when Unwrap(add.Instance) is IPropertyReferenceOperation collection && collection.Property.Name == member && isObject(collection.Instance):
                return add.Arguments.Select(a => a.Value);

            case IMemberInitializerOperation { InitializedMember: IPropertyReferenceOperation property, Initializer: { } initializer }
                when property.Property.Name == member && isObject(property.Instance):
                return initializer.Initializers.OfType<IInvocationOperation>().SelectMany(add => add.Arguments.Select(a => a.Value));

            default:
                return [];
        }
    }

    /// <summary>
    /// The operations of an object initializer, without those of objects created inside it, whose members are
    /// their own.
    /// </summary>
    private static IEnumerable<IOperation> Inside(IOperation initializer)
    {
        var pending = new Stack<IOperation>([initializer]);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            yield return node;
            foreach (var child in node.ChildOperations.Reverse())
            {
                if (child is not (IObjectCreationOperation or IAnonymousObjectCreationOperation or IAnonymousFunctionOperation)) pending.Push(child);
            }
        }
    }

    private static bool IsKey(IOperation operation, string member) => operation.ConstantValue is { HasValue: true, Value: string key } && key == member;

    private static bool IsImplicitReceiver(IOperation? operation) => operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver };

    /// <summary>
    /// The table of the records in an <c>EntityCollection</c>, such as the result of <c>RetrieveMultiple</c>.
    /// </summary>
    /// <remarks>
    /// Any collection of entities is read the same way: a list or array of early-bound entities by its element
    /// type, and other collections by what is put in them.
    /// </remarks>
    private TableResolution Records(IOperation? operation)
    {
        var op = Unwrap(operation);
        if (symbols.Is(op?.Type, symbols.EntityReferenceCollection)) return EntityReferences(op);
        if (ElementType(op?.Type) is { } elementType && (symbols.LogicalNameOf(elementType) is not null || (elementType is ITypeParameterSymbol && IsEntityLike(elementType))))
        {
            return TypeArgument(elementType);
        }

        switch (op)
        {
            case null:
                return TableResolution.Unresolved("records argument is missing");

            case IInvocationOperation invocation when symbols.ServiceOperation(invocation.TargetMethod) == "RetrieveMultiple":
                return Query(invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 0), includeLinks: false).OrUnresolved("query has no table name");

            case IPropertyReferenceOperation { Property.Name: "Entities", Instance: { } records } property
                when symbols.Is(property.Property.ContainingType, symbols.EntityCollection):
                return Records(records);

            case IObjectCreationOperation creation:
            {
                var result = AssignedMember(creation.Initializer, "EntityName", Name) ?? TableResolution.Empty;
                foreach (var argument in creation.Arguments.Where(a => IsRecordsLike(a.Value.Type))) result.Add(Records(argument.Value));
                foreach (var add in creation.Initializer?.Initializers.OfType<IInvocationOperation>() ?? [])
                {
                    foreach (var argument in add.Arguments) result.Add(Entity(argument.Value));
                }
                if (creation.Initializer is { } initializer)
                {
                    foreach (var entities in Inside(initializer).SelectMany(node => SetMember(node, IsImplicitReceiver, "Entities"))) result.Add(EntityOrRecords(entities));
                }
                return result;
            }

            case IArrayCreationOperation or ICollectionExpressionOperation:
                return CollectionElements(op).Aggregate(TableResolution.Empty, (result, element) => result.Add(EntityOrRecords(element)));

            case ILocalReferenceOperation local:
                return FromLocal(local, Records, Records, (node, symbol) => node switch
                {
                    IInvocationOperation { TargetMethod.Name: "Add" or "AddRange" or "Insert" or "InsertRange" } add
                        when IsLocal(add.Instance, symbol) || (Unwrap(add.Instance) is IPropertyReferenceOperation { Property.Name: "Entities" } entities && IsLocal(entities.Instance, symbol)) =>
                        add.Arguments.Where(a => IsEntityLike(Unwrap(a.Value)?.Type) || IsRecordsLike(Unwrap(a.Value)?.Type))
                            .Aggregate(TableResolution.Empty, (result, a) => result.Add(EntityOrRecords(a.Value))),
                    ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.Name: "EntityName" } property } assignment when IsLocal(property.Instance, symbol) =>
                        Name(assignment.Value),
                    _ => null,
                });

            case IParameterReferenceOperation parameter when LambdaSource(parameter) is { } source:
                return Records(source);

            case IInvocationOperation invocation when IsFramework(invocation):
                return Passthrough(invocation, t => IsRecordsLike(t) || IsEntityLike(t), Records, EntityOrRecords) ?? Unknown(op, "records", TableShape.Records);

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Records(conditional.WhenTrue).Add(Records(whenFalse));

            case ICoalesceOperation coalesce:
                return Records(coalesce.Value).Add(Records(coalesce.WhenNull));

            default:
                return Unknown(op, "records", TableShape.Records);
        }
    }

    private TableResolution EntityOrRecords(IOperation value) => IsEntityLike(Unwrap(value)?.Type) ? Entity(value) : Records(value);

    /// <summary>
    /// What a framework call such as <c>ToList</c>, <c>Chunk</c> or <c>Select</c> passes on from the collections it
    /// is given: the receiver and arguments of a type <paramref name="accepts"/>, read with
    /// <paramref name="resolve"/>, and the values returned by lambdas, read with <paramref name="resolveReturned"/>.
    /// <c>null</c> when nothing is passed on.
    /// </summary>
    private TableResolution? Passthrough(
        IInvocationOperation invocation,
        Func<ITypeSymbol?, bool> accepts,
        Func<IOperation, TableResolution> resolve,
        Func<IOperation, TableResolution> resolveReturned)
    {
        TableResolution? result = null;
        var sources = invocation.Instance is { } instance ? invocation.Arguments.Select(a => a.Value).Prepend(instance) : invocation.Arguments.Select(a => a.Value);
        foreach (var source in sources)
        {
            if (Unwrap(source) is IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda } && accepts(lambda.Symbol.ReturnType))
            {
                foreach (var returned in Returned(lambda)) (result ??= TableResolution.Empty).Add(resolveReturned(returned));
            }
            else if (accepts(source.Type) || accepts(Unwrap(source)?.Type))
            {
                (result ??= TableResolution.Empty).Add(resolve(source));
            }
        }
        return result;
    }

    /// <summary>
    /// The values a lambda returns, without those of lambdas inside it.
    /// </summary>
    private static IEnumerable<IOperation> Returned(IAnonymousFunctionOperation lambda) =>
        Inside(lambda.Body).OfType<IReturnOperation>().Where(r => r.ReturnedValue is not null).Select(r => r.ReturnedValue!);

    /// <summary>
    /// The collection whose elements a lambda parameter receives, such as <c>accounts</c> for <c>a</c> in
    /// <c>accounts.Select(a => …)</c>; <c>null</c> when the lambda isn't passed to a framework call.
    /// </summary>
    private static IOperation? LambdaSource(IParameterReferenceOperation parameter)
    {
        if (parameter.Parameter is not { Ordinal: 0, ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambdaSymbol }) return null;

        var lambda = parameter.Parent;
        while (lambda is not null && !(lambda is IAnonymousFunctionOperation function && SymbolEqualityComparer.Default.Equals(function.Symbol, lambdaSymbol))) lambda = lambda.Parent;
        if (lambda?.Parent is not IDelegateCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation invocation } argument } || !IsFramework(invocation)) return null;

        return invocation.Instance ?? invocation.Arguments.FirstOrDefault(a => a != argument && a.Parameter?.Ordinal == 0)?.Value;
    }

    private static IEnumerable<IOperation> CollectionElements(IOperation operation) => operation switch
    {
        IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues,
        ICollectionExpressionOperation collection => collection.Elements.Select(e => e is ISpreadOperation spread ? spread.Operand : e),
        _ => [],
    };

    /// <summary>
    /// The tables of the elements of a collection that a loop variable iterates.
    /// </summary>
    private TableResolution EntityElements(IOperation collection) => Unwrap(collection) switch
    {
        IArrayCreationOperation or ICollectionExpressionOperation => Elements(collection),
        var op when IsRecordsLike(op?.Type) => Records(op),
        var op => Entity(op),
    };

    private TableResolution Elements(IOperation? operation)
    {
        var op = Unwrap(operation);
        var elements = op switch
        {
            IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues,
            ICollectionExpressionOperation collection => collection.Elements,
            _ => default(IEnumerable<IOperation>?),
        };

        if (elements is not null) return elements.Aggregate(TableResolution.Empty, (result, element) => result.Add(EntityOrRecords(element is ISpreadOperation spread ? spread.Operand : element)));
        return IsRecordsLike(op?.Type) && !symbols.Is(op?.Type, symbols.EntityReferenceCollection) ? Records(op) : Unknown(op, "related records", TableShape.EntityReferences);
    }

    /// <summary>
    /// The possible texts of a FetchXML string. Runtime parts become <see cref="Hole"/>, so the structure around
    /// them can still be parsed. Values that are runtime as a whole are reported in <paramref name="failures"/>.
    /// </summary>
    private List<string> Templates(IOperation? operation, bool topLevel, TableResolution failures, bool includeLinks = true)
    {
        var op = Unwrap(operation);
        if (op is null)
        {
            failures.Add(TableResolution.Unresolved("FetchXML argument is missing"));
            return [];
        }

        if (op.ConstantValue is { HasValue: true, Value: string constant }) return [constant];

        switch (op)
        {
            case IInterpolatedStringOperation interpolated:
            {
                var builder = new StringBuilder();
                foreach (var part in interpolated.Parts)
                {
                    builder.Append(part switch
                    {
                        IInterpolatedStringTextOperation text => text.Text.ConstantValue.Value as string,
                        IInterpolationOperation { Expression.ConstantValue: { HasValue: true, Value: var value } } => value?.ToString(),
                        _ => Hole.ToString(),
                    });
                }
                return [builder.ToString()];
            }

            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary when op.Type?.SpecialType == SpecialType.System_String:
            {
                var left = Templates(binary.LeftOperand, topLevel: false, failures);
                var right = Templates(binary.RightOperand, topLevel: false, failures);
                if (left.Count * right.Count > MaxTemplates)
                {
                    failures.Add(TableResolution.Unresolved("FetchXML is built in too many ways to analyze"));
                    return [];
                }
                return left.SelectMany(l => right.Select(r => l + r)).ToList();
            }

            case ILocalReferenceOperation local:
            {
                if (!_visiting.Add(local.Local)) return [];
                try
                {
                    var templates = new List<string>();
                    foreach (var value in LocalValues(local.Local, Root(local), failures))
                    {
                        if (value.IsLoopCollection) failures.Add(TableResolution.Unresolved($"local '{local.Local.Name}' is a loop variable"));
                        else templates.AddRange(Templates(value.Operation, topLevel, failures, includeLinks));
                    }
                    return templates.Distinct(StringComparer.Ordinal).ToList();
                }
                finally
                {
                    _visiting.Remove(local.Local);
                }
            }

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Templates(conditional.WhenTrue, topLevel, failures, includeLinks).Concat(Templates(whenFalse, topLevel, failures, includeLinks)).ToList();

            case ICoalesceOperation coalesce:
                return Templates(coalesce.Value, topLevel, failures, includeLinks).Concat(Templates(coalesce.WhenNull, topLevel, failures, includeLinks)).ToList();

            default:
                if (!topLevel) return [Hole.ToString()];
                failures.Add(Unknown(op, "FetchXML", includeLinks ? TableShape.Fetch : TableShape.FetchMainTable));
                return [];
        }
    }

    private static TableResolution ParseFetch(string template, bool includeLinks)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(template);
        }
        catch (XmlException)
        {
            return TableResolution.Unresolved("FetchXML could not be parsed");
        }

        var result = TableResolution.Empty;
        foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "entity" || (includeLinks && e.Name.LocalName == "link-entity")))
        {
            var name = (string?)element.Attribute("name");
            result.Add(
                string.IsNullOrEmpty(name) ? TableResolution.Unresolved("FetchXML entity has no name")
                : name!.IndexOf(Hole) >= 0 ? TableResolution.Unresolved("FetchXML table name is computed at runtime")
                : TableResolution.Table(name));
        }
        return result.OrUnresolved("FetchXML has no entity element");
    }

    /// <summary>
    /// Resolves a local from every value assigned to it in its method. The union is taken, because the analysis
    /// doesn't follow control flow. <paramref name="use"/> sees every operation in the method, to pick up
    /// changes made through the local, such as <c>entity.LogicalName = "account"</c>.
    /// </summary>
    private TableResolution FromLocal(
        ILocalReferenceOperation reference,
        Func<IOperation, TableResolution> resolve,
        Func<IOperation, TableResolution>? resolveElements = null,
        Func<IOperation, ILocalSymbol, TableResolution?>? use = null)
    {
        var local = reference.Local;
        if (!_visiting.Add(local)) return TableResolution.Empty;
        try
        {
            var root = Root(reference);
            var result = TableResolution.Empty;
            foreach (var value in LocalValues(local, root, result))
            {
                if (!value.IsLoopCollection)
                {
                    result.Add(resolve(value.Operation));
                }
                else
                {
                    result.Add(resolveElements is not null
                        ? resolveElements(value.Operation)
                        : TableResolution.Unresolved($"local '{local.Name}' is a loop variable"));
                }
            }

            if (use is not null)
            {
                foreach (var node in root.DescendantsAndSelf())
                {
                    if (use(node, local) is { } resolution) result.Add(resolution);
                }
            }
            return result;
        }
        finally
        {
            _visiting.Remove(local);
        }
    }

    /// <summary>
    /// Every value assigned to <paramref name="local"/>. For a <c>foreach</c> variable that's the collection it
    /// iterates. Assignments the analysis can't follow are added to <paramref name="failures"/>.
    /// </summary>
    private static List<LocalValue> LocalValues(ILocalSymbol local, IOperation root, TableResolution failures)
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

                case ICompoundAssignmentOperation compound when IsLocal(compound.Target, local):
                    failures.Add(TableResolution.Unresolved($"local '{local.Name}' is changed by a compound assignment"));
                    break;

                case IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument when IsLocal(argument.Value, local):
                    failures.Add(TableResolution.Unresolved($"local '{local.Name}' is passed by ref or out"));
                    break;

                case IForEachLoopOperation loop when loop.Locals.Contains(local, SymbolEqualityComparer.Default):
                    values.Add(new LocalValue(loop.Collection, true));
                    break;

                case IDeclarationPatternOperation pattern when SymbolEqualityComparer.Default.Equals(pattern.DeclaredSymbol, local):
                    failures.Add(TableResolution.Unresolved($"local '{local.Name}' is declared by a pattern"));
                    break;

                case IDeconstructionAssignmentOperation deconstruction when deconstruction.Target.DescendantsAndSelf().Any(t => IsLocal(t, local)):
                    failures.Add(TableResolution.Unresolved($"local '{local.Name}' is assigned by deconstruction"));
                    break;
            }
        }
        return values;
    }

    /// <summary>
    /// Links that can't be tied to a query local are not followed yet. Any of them in the method makes the
    /// query's linked tables unknown, so they are reported instead of missed.
    /// </summary>
    private TableResolution NestedLinks(IOperation query)
    {
        var hasNestedLinks = Root(query).DescendantsAndSelf().Any(node => node switch
        {
            IPropertyReferenceOperation { Property.Name: "LinkEntities" } => true,
            IInvocationOperation { TargetMethod.Name: "AddLink" } call => symbols.Is(call.TargetMethod.ContainingType, symbols.LinkEntity),
            IObjectCreationOperation creation => symbols.Is(creation.Type, symbols.LinkEntity),
            _ => false,
        });
        return hasNestedLinks ? TableResolution.Unresolved(NestedLinksReason) : TableResolution.Empty;
    }

    private TableResolution? AssignedMember(IObjectOrCollectionInitializerOperation? initializer, string member, Func<IOperation, TableResolution> resolve)
    {
        var assignments = initializer?.Initializers
            .OfType<ISimpleAssignmentOperation>()
            .Where(a => a.Target is IPropertyReferenceOperation property && property.Property.Name == member)
            .ToList();

        return assignments is { Count: > 0 }
            ? assignments.Aggregate(TableResolution.Empty, (result, assignment) => result.Add(resolve(assignment.Value)))
            : null;
    }

    private static IArgumentOperation? FirstStringArgument(IOperation call)
    {
        var arguments = call switch
        {
            IObjectCreationOperation creation => creation.Arguments,
            IInvocationOperation invocation => invocation.Arguments,
            _ => [],
        };
        return arguments.FirstOrDefault(a => a.Parameter is { Ordinal: 0, Type.SpecialType: SpecialType.System_String });
    }

    private bool IsEntityOrReference(ITypeSymbol? type) =>
        symbols.Is(type, symbols.Entity) || symbols.Is(type, symbols.EntityReference);

    /// <summary>
    /// An entity or reference, or a type parameter constrained to one.
    /// </summary>
    private bool IsEntityLike(ITypeSymbol? type) =>
        IsEntityOrReference(type) || (type is ITypeParameterSymbol typeParameter && typeParameter.ConstraintTypes.Any(IsEntityLike));

    /// <summary>
    /// A collection of entities or references: an <c>EntityCollection</c>, or a list or array of them.
    /// </summary>
    private bool IsRecordsLike(ITypeSymbol? type, int depth = 0) =>
        symbols.Is(type, symbols.EntityCollection) || symbols.Is(type, symbols.EntityReferenceCollection)
        || (Nested(type, depth) is { } element && (IsEntityLike(element) || IsRecordsLike(element, depth + 1)));

    /// <summary>
    /// A request, or a collection of requests, of collections of them, and so on.
    /// </summary>
    private bool IsRequestLike(ITypeSymbol? type, int depth = 0) =>
        symbols.Is(type, symbols.OrganizationRequest)
        || (Nested(type, depth) is { } element && IsRequestLike(element, depth + 1));

    /// <summary>
    /// The element type of a collection nested <paramref name="depth"/> levels deep. Types that are collections of
    /// themselves, such as <c>JToken</c>, stop at <see cref="MaxNesting"/>.
    /// </summary>
    private static ITypeSymbol? Nested(ITypeSymbol? type, int depth) =>
        depth < MaxNesting && type?.SpecialType != SpecialType.System_String ? ElementType(type) : null;

    private static ITypeSymbol? ElementType(ITypeSymbol? type) => type switch
    {
        null => null,
        IArrayTypeSymbol array => array.ElementType,
        INamedTypeSymbol named when named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T => named.TypeArguments[0],
        _ => type.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)?.TypeArguments[0],
    };

    /// <summary>
    /// A call into the .NET libraries, such as LINQ, whose results come from its arguments.
    /// </summary>
    private static bool IsFramework(IInvocationOperation invocation) =>
        !invocation.TargetMethod.Locations.Any(l => l.IsInSource)
        && invocation.TargetMethod.ContainingNamespace?.ToDisplayString() is { } name
        && (name == "System" || name.StartsWith("System.", StringComparison.Ordinal));

    private bool IsTableQuery(ITypeSymbol? type) =>
        symbols.Is(type, symbols.QueryExpression) || symbols.Is(type, symbols.QueryByAttribute);

    private static bool IsLocal(IOperation? operation, ILocalSymbol local) =>
        Unwrap(operation) is ILocalReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Local, local);

    private static IOperation Root(IOperation operation)
    {
        while (operation.Parent is not null) operation = operation.Parent;
        return operation;
    }

    private static IOperation? Unwrap(IOperation? operation)
    {
        while (UnwrapOnce(operation) is { } inner) operation = inner;
        return operation;
    }

    private static IOperation? UnwrapOnce(IOperation? operation) => operation switch
    {
        IConversionOperation conversion => conversion.Operand,
        IArgumentOperation argument => argument.Value,
        IParenthesizedOperation parenthesized => parenthesized.Operand,
        IDeclarationExpressionOperation declaration => declaration.Expression,
        _ => null,
    };

    private static string Indexer(IPropertyReferenceOperation indexer)
    {
        var target = Unwrap(indexer.Instance) switch
        {
            IPropertyReferenceOperation property => property.Property.Name,
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation parameter => parameter.Parameter.Name,
            IFieldReferenceOperation field => field.Field.Name,
            _ => "an indexer",
        };
        var arguments = indexer.Arguments.Select(a => a.Value.ConstantValue is { HasValue: true, Value: string key } ? $"\"{key}\"" : "…");
        return $"{target}[{string.Join(", ", arguments)}]";
    }

    private readonly record struct LocalValue(IOperation Operation, bool IsLoopCollection);

    /// <summary>
    /// A value the resolver can't follow here. A method parameter becomes a slot for the call graph to fill in, and
    /// a call to a method in the solution is resolved from what that method returns.
    /// </summary>
    /// <remarks>
    /// A <paramref name="silent"/> value reports nothing where it stays unknown: a request read for one privilege,
    /// whose reason is reported once, under <see cref="TableShape.RequestOther"/>.
    /// </remarks>
    private TableResolution Unknown(IOperation? operation, string what, TableShape shape, bool silent = false)
    {
        var reason = silent ? "" : Describe(operation, what);
        return operation switch
        {
            IParameterReferenceOperation { Parameter.ContainingSymbol: IMethodSymbol method } parameter =>
                TableResolution.Slot(new TableSlot(Nodes.Id(method), parameter.Parameter.Ordinal, shape, reason)),
            IInvocationOperation invocation when calls?.Invoke(invocation) is { } call =>
                TableResolution.Returned(new ReturnedTables(call, shape, reason)),
            _ => silent ? TableResolution.Empty : TableResolution.Unresolved(reason),
        };
    }

    private TableResolution UnknownRequest(IOperation? operation, TableShape shape) =>
        Unknown(operation, "request", shape, silent: shape != TableShape.RequestOther);

    /// <summary>
    /// A reason that is reported under <see cref="TableShape.RequestOther"/> only, as it applies to no privilege.
    /// </summary>
    private static TableResolution Other(TableShape shape, string reason) =>
        shape == TableShape.RequestOther ? TableResolution.Unresolved(reason) : TableResolution.Empty;

    /// <summary>
    /// A type parameter of a generic method becomes a slot for the type argument at each call site. One of a
    /// generic type is unresolved.
    /// </summary>
    private static TableResolution TypeParameter(ITypeParameterSymbol typeParameter)
    {
        var reason = $"table depends on type parameter '{typeParameter.Name}'";
        return typeParameter.DeclaringMethod is { } method
            ? TableResolution.Slot(new TableSlot(Nodes.Id(method), TableSlot.TypeParameterOrdinal(typeParameter.Ordinal), TableShape.Entity, reason))
            : TableResolution.Unresolved(reason);
    }

    private static string Describe(IOperation? operation, string what) => operation switch
    {
        IParameterReferenceOperation parameter => $"{what} comes from parameter '{parameter.Parameter.Name}'",
        IFieldReferenceOperation field => $"{what} comes from field '{field.Field.Name}'",
        IPropertyReferenceOperation { Property.IsIndexer: true } indexer => $"{what} comes from {Indexer(indexer)}",
        IPropertyReferenceOperation property => $"{what} comes from property '{property.Property.Name}'",
        ITypeParameterObjectCreationOperation creation => $"table depends on type parameter '{creation.Type?.Name}'",
        IInvocationOperation invocation => $"{what} is returned by '{invocation.TargetMethod.Name}'",
        { ConstantValue: { HasValue: true, Value: null } } => $"{what} is null",
        _ => $"{what} is computed at runtime",
    };
}
