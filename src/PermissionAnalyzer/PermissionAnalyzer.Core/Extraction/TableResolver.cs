using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// Works out which tables a value refers to: early-bound types, constants, FetchXML and assignments to locals
/// in the same method. Anything else becomes a reason in the resolution, never a guess.
/// </summary>
internal sealed class TableResolver(XrmSymbols symbols)
{
    // Stands in for runtime text inside a FetchXML template. Private use area, so real FetchXML never contains it.
    private const char Hole = '';

    // Concatenating locals with several assignments multiplies the variants; stop before that gets out of hand.
    private const int MaxTemplates = 64;

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
            _ => TableResolution.Unresolved(Describe(op, "table name")),
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
            if (op.Type is ITypeParameterSymbol typeParameter) return TableResolution.Unresolved($"table depends on type parameter '{typeParameter.Name}'");

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

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Entity(conditional.WhenTrue).Add(Entity(whenFalse));

            case ICoalesceOperation coalesce:
                return Entity(coalesce.Value).Add(Entity(coalesce.WhenNull));

            default:
                return TableResolution.Unresolved(Describe(op, "entity"));
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
                return TableResolution.Unresolved(Describe(op, "query"));
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
        foreach (var template in Templates(operation, topLevel: true, result))
        {
            result.Add(ParseFetch(template, includeLinks));
        }
        return result;
    }

    /// <summary>
    /// The table of the records in an <c>EntityCollection</c>, such as the result of <c>RetrieveMultiple</c>.
    /// </summary>
    private TableResolution Records(IOperation? operation)
    {
        var op = Unwrap(operation);
        return op switch
        {
            IInvocationOperation invocation when symbols.ServiceOperation(invocation.TargetMethod) == "RetrieveMultiple" =>
                Query(invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 0), includeLinks: false).OrUnresolved("query has no table name"),
            ILocalReferenceOperation local => FromLocal(local, Records),
            _ => TableResolution.Unresolved(Describe(op, "records")),
        };
    }

    /// <summary>
    /// The tables of the elements of a collection that a loop variable iterates.
    /// </summary>
    private TableResolution EntityElements(IOperation collection) => Unwrap(collection) switch
    {
        IArrayCreationOperation or ICollectionExpressionOperation => Elements(collection),
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

        return elements is null
            ? TableResolution.Unresolved(Describe(op, "related records"))
            : elements.Aggregate(TableResolution.Empty, (result, element) => result.Add(Entity(element)));
    }

    /// <summary>
    /// The possible texts of a FetchXML string. Runtime parts become <see cref="Hole"/>, so the structure around
    /// them can still be parsed. Values that are runtime as a whole are reported in <paramref name="failures"/>.
    /// </summary>
    private List<string> Templates(IOperation? operation, bool topLevel, TableResolution failures)
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
                        else templates.AddRange(Templates(value.Operation, topLevel, failures));
                    }
                    return templates.Distinct(StringComparer.Ordinal).ToList();
                }
                finally
                {
                    _visiting.Remove(local.Local);
                }
            }

            case IConditionalOperation { WhenFalse: { } whenFalse } conditional:
                return Templates(conditional.WhenTrue, topLevel, failures).Concat(Templates(whenFalse, topLevel, failures)).ToList();

            case ICoalesceOperation coalesce:
                return Templates(coalesce.Value, topLevel, failures).Concat(Templates(coalesce.WhenNull, topLevel, failures)).ToList();

            default:
                if (!topLevel) return [Hole.ToString()];
                failures.Add(TableResolution.Unresolved(Describe(op, "FetchXML")));
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
