using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Graph;

/// <summary>
/// Who an entry point's code runs as: the caller (or the step's impersonating user), and the identity of
/// services that come from dependency injection. <see cref="PrimaryTable"/> is the table a plugin step is
/// registered on, which the execution context's <c>PrimaryEntityName</c> holds.
/// </summary>
internal sealed record EntryContext(RunsAs Caller, Identity Injected, string? PrimaryTable = null);

/// <summary>
/// A resolved identity. <see cref="Assumption"/> says why it was assumed, when it was.
/// </summary>
internal sealed record Identity(RunsAs RunsAs, string? Assumption = null) : IComparable<Identity>
{
    public int CompareTo(Identity? other)
    {
        if (other is null) return 1;
        var result = RunsAs.CompareTo(other.RunsAs);
        return result != 0 ? result : string.CompareOrdinal(Assumption, other.Assumption);
    }

    public override string ToString() => Assumption is null ? RunsAs.ToString() : $"{RunsAs}~{Assumption}";
}

internal sealed record RequirementHit(string Table, Privilege Privilege, RunsAs RunsAs, RequirementBasis Basis, RequirementSource Source);

internal sealed record UnresolvedHit(string Operation, IReadOnlyList<Privilege> Privileges, string Reason, RunsAs RunsAs, RequirementSource Source);

internal sealed record AssumptionHit(string Reason, RequirementSource Source);

/// <summary>
/// An operation that fires the plugin steps of <see cref="Message"/> on <see cref="Table"/>: <c>null</c> for no
/// table, <see cref="AnyTable"/> for a table that isn't known.
/// </summary>
internal sealed record TriggerHit(string Message, string? Table, RunsAs RunsAs, RequirementBasis Basis, RequirementSource Source)
{
    public const string AnyTable = "*";
}

/// <summary>
/// What an entry point or method needs, in the order found.
/// </summary>
internal sealed class EvaluationResult
{
    public List<RequirementHit> Requirements { get; } = [];
    public List<UnresolvedHit> Unresolved { get; } = [];
    public List<AssumptionHit> Assumptions { get; } = [];
    public List<TriggerHit> Triggers { get; } = [];

    /// <summary>
    /// Every node the evaluation visited.
    /// </summary>
    public HashSet<string> Reached { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Set when the evaluation stopped at <see cref="Evaluator.MaxVisits"/>, so the result may be incomplete.
    /// </summary>
    public bool Truncated { get; set; }
}

/// <summary>
/// Walks the call graph from an entry point, breadth first, so the first path to an operation is the shortest.
/// Each method is evaluated per call context: the services and tables bound to its parameters and the state of
/// the current object. A method called with the same context again is not walked again.
/// </summary>
internal sealed class Evaluator(CallGraph graph, EntryContext entry)
{
    public const int MaxVisits = 10000;

    // How deep services are followed through returns and constructors
    private const int MaxDepth = 16;

    private static readonly IReadOnlyDictionary<(string, int), Binding> NoBindings = new Dictionary<(string, int), Binding>();

    private readonly Dictionary<string, ObjectState> _defaultObjects = new(StringComparer.Ordinal);
    private readonly HashSet<string> _constructing = new(StringComparer.Ordinal);
    private readonly HashSet<string> _returning = new(StringComparer.Ordinal);

    /// <summary>
    /// Everything reachable from <paramref name="handlers"/>.
    /// </summary>
    public EvaluationResult Evaluate(IEnumerable<Handler> handlers)
    {
        var result = new EvaluationResult();
        var queue = new Queue<Frame>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        void Enqueue(Frame frame)
        {
            if (graph.Nodes.ContainsKey(frame.Node) && visited.Add(frame.Key)) queue.Enqueue(frame);
        }

        foreach (var handler in handlers)
        {
            foreach (var start in Start(handler)) Enqueue(start);
        }

        var visits = 0;
        while (queue.Count > 0)
        {
            if (++visits > MaxVisits)
            {
                result.Truncated = true;
                break;
            }

            var frame = queue.Dequeue();
            var node = graph.Nodes[frame.Node];
            result.Reached.Add(node.Id);

            foreach (var access in node.Accesses) Record(access, frame, result);

            foreach (var lambda in node.Lambdas)
            {
                if (graph.Nodes.TryGetValue(lambda, out var lambdaNode))
                {
                    Enqueue(new Frame(lambda, frame.Object, frame.Bindings, frame.Basis, [.. frame.Path, lambdaNode.Display]));
                }
            }

            foreach (var call in node.Calls)
            {
                foreach (var (target, state, basis) in Dispatch(call, frame, 0))
                {
                    if (!graph.Nodes.TryGetValue(target, out var callee)) continue;
                    var bindings = Bind(call, frame, callee);
                    var callBasis = basis > frame.Basis ? basis : frame.Basis;
                    Enqueue(new Frame(target, state, bindings, callBasis, [.. frame.Path, callee.Display]));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// The operations written directly in <paramref name="node"/>, outside any call context.
    /// </summary>
    public EvaluationResult EvaluateAlone(NodeSummary node, string display)
    {
        var result = new EvaluationResult();
        var state = node.Type is { } type ? DefaultObject(type, 0) : null;
        var frame = new Frame(node.Id, state, NoBindings, RequirementBasis.Direct, [display]);
        foreach (var access in node.Accesses) Record(access, frame, result);
        return result;
    }

    private IEnumerable<Frame> Start(Handler handler)
    {
        var path = new List<string>();
        if (handler.ServiceType is { } service)
        {
            var call = new CallFact(new SourceLocation("", 0, 0), handler.Target, true, ReceiverKind.Other, service, new Dictionary<int, ArgumentFact>());
            var frame = new Frame(handler.Target, null, NoBindings, RequirementBasis.Direct, path);
            foreach (var (target, state, basis) in Dispatch(call, frame, 0))
            {
                if (graph.Nodes.TryGetValue(target, out var node))
                {
                    yield return new Frame(target, state, NoBindings, basis, [node.Display]);
                }
            }
            yield break;
        }

        // A virtual method found on a base class, such as IPlugin.Execute implemented in a plugin base class and
        // overridden in the plugin, runs the override of the object's type
        var method = handler.ObjectType is { } type ? Implementation(type, handler.Target) : handler.Target;
        if (!graph.Nodes.TryGetValue(method, out var start)) yield break;
        var objectType = handler.ObjectType ?? start.Type;
        yield return new Frame(start.Id, objectType is null ? null : DefaultObject(objectType, 0), NoBindings, RequirementBasis.Direct, [start.Display]);
    }

    private void Record(AccessFact access, Frame frame, EvaluationResult result)
    {
        var source = new RequirementSource(access.Source, frame.Path);
        var identities = Resolve(access.Service, frame, 0);
        if (identities.Count == 0) identities = [Unknown("service is null")];

        var tables = BindTables(access.Tables, frame, 0);
        foreach (var identity in identities)
        {
            if (identity.Assumption is { } assumption) result.Assumptions.Add(new AssumptionHit(assumption, source));

            foreach (var table in tables.Tables)
            {
                foreach (var privilege in access.Privileges)
                {
                    result.Requirements.Add(new RequirementHit(table, privilege, identity.RunsAs, frame.Basis, source));
                }
            }

            // Every table a lookup can refer to, where the code doesn't say which
            foreach (var table in tables.LookupTargets.Except(tables.Tables, StringComparer.Ordinal))
            {
                foreach (var privilege in access.Privileges)
                {
                    result.Requirements.Add(new RequirementHit(table, privilege, identity.RunsAs, RequirementBasis.AllLookupTargets, source));
                }
            }

            foreach (var reason in tables.Reasons)
            {
                var privileges = access.Privileges.Distinct().OrderBy(p => p).ToList();
                result.Unresolved.Add(new UnresolvedHit(access.Operation, privileges, access.ReasonPrefix + reason, identity.RunsAs, source));
            }

            foreach (var fired in tables.Fired)
            {
                foreach (var table in fired.Value.Tables) result.Triggers.Add(new TriggerHit(fired.Key, table, identity.RunsAs, frame.Basis, source));
                foreach (var table in fired.Value.LookupTargets.Except(fired.Value.Tables, StringComparer.Ordinal))
                {
                    result.Triggers.Add(new TriggerHit(fired.Key, table, identity.RunsAs, RequirementBasis.AllLookupTargets, source));
                }
                if (fired.Value.Reasons.Count > 0) result.Triggers.Add(new TriggerHit(fired.Key, TriggerHit.AnyTable, identity.RunsAs, frame.Basis, source));
                if (fired.Value.Tables.Count == 0 && fired.Value.LookupTargets.Count == 0 && fired.Value.Reasons.Count == 0) result.Triggers.Add(new TriggerHit(fired.Key, null, identity.RunsAs, frame.Basis, source));
            }
        }
    }

    /// <summary>
    /// The methods a call can run, with the object each runs on.
    /// </summary>
    private IEnumerable<(string Target, ObjectState? Object, RequirementBasis Basis)> Dispatch(CallFact call, Frame frame, int depth)
    {
        var basis = frame.Basis;
        switch (call.Receiver)
        {
            case ReceiverKind.None:
                return [(call.Target, null, basis)];

            case ReceiverKind.This when frame.Object is { } current:
                return [(call.Virtual ? Implementation(current.Type, call.Target) : call.Target, current, basis)];

            case ReceiverKind.Created when (call.Creation ?? (IsConstructor(call.Target) ? call : null)) is { } creation:
            {
                var created = Construct(creation, frame, depth);
                return [(call.Virtual ? Implementation(created.Type, call.Target) : call.Target, created, basis)];
            }
        }

        // An object of unknown runtime type
        var receiverType = call.ReceiverType;
        if (!call.Virtual || receiverType is null)
        {
            return [(call.Target, receiverType is null ? null : DefaultObject(receiverType, depth), basis)];
        }

        if (graph.Registrations.TryGetValue(receiverType, out var registered))
        {
            return registered.Select(type => (Implementation(type, call.Target), (ObjectState?)DefaultObject(type, depth), basis)).ToList();
        }

        var candidates = graph.ConcreteTypes(receiverType)
            .Select(type => (Target: Implementation(type.Id, call.Target), Object: (ObjectState?)DefaultObject(type.Id, depth)))
            .ToList();
        if (candidates.Count == 0) return [(call.Target, DefaultObject(receiverType, depth), basis)];

        // A virtual method that nothing overrides runs as written
        var direct = candidates.All(c => c.Target == call.Target);
        return candidates.Select(c => (c.Target, c.Object, direct ? basis : RequirementBasis.AllImplementations)).ToList();
    }

    private string Implementation(string? type, string method) =>
        type is not null && graph.Types.TryGetValue(type, out var summary) && summary.Implementations.TryGetValue(method, out var implementation)
            ? implementation
            : method;

    private bool IsConstructor(string node) => graph.Nodes.TryGetValue(node, out var summary) && summary.IsConstructor;

    /// <summary>
    /// What a call passes to <paramref name="callee"/>. A lambda or local function also sees the parameters of the
    /// method around it.
    /// </summary>
    private IReadOnlyDictionary<(string, int), Binding> Bind(CallFact call, Frame caller, NodeSummary callee, int depth = 0)
    {
        var inherit = graph.Nodes.TryGetValue(caller.Node, out var callerNode) && callee.Member == callerNode.Member;
        if (call.Arguments.Count == 0)
        {
            return inherit ? caller.Bindings : NoBindings;
        }

        var bindings = inherit
            ? new Dictionary<(string, int), Binding>(caller.Bindings.ToDictionary(b => b.Key, b => b.Value))
            : new Dictionary<(string, int), Binding>();
        foreach (var argument in call.Arguments)
        {
            var services = argument.Value.Service is { } origins ? Resolve(origins, caller, 0) : null;
            var tables = argument.Value.Tables?.ToDictionary(t => t.Key, t => BindTables(t.Value, caller, depth));
            bindings[(callee.Id, argument.Key)] = new Binding(services, tables);
        }
        return bindings;
    }

    /// <summary>
    /// Fills in the parameter slots of <paramref name="tables"/> from the call context, and the values returned by
    /// calls from what each method that can run returns.
    /// </summary>
    private TableResolution BindTables(TableResolution tables, Frame frame, int depth)
    {
        if (tables.IsBound) return tables;

        var result = TableResolution.Empty;
        foreach (var table in tables.Tables) result.Add(TableResolution.Table(table));
        foreach (var reason in tables.Reasons) result.Add(TableResolution.Unresolved(reason));
        foreach (var target in tables.LookupTargets) result.Add(TableResolution.LookupTarget(target));
        foreach (var slot in tables.Slots)
        {
            if (slot.Method == TableSlot.PrimaryEntity) result.Add(entry.PrimaryTable is { } primary ? TableResolution.Table(primary) : TableResolution.Unresolved(slot.Reason));
            else if (frame.Bindings.TryGetValue((slot.Method, slot.Ordinal), out var binding) && binding.Tables?.TryGetValue(slot.Shape, out var bound) == true) result.Add(bound!);
            else if (slot.Reason.Length > 0) result.Add(TableResolution.Unresolved(slot.Reason));
        }
        foreach (var returned in tables.Returns) result.Add(Returned(returned, frame, depth + 1));
        foreach (var fired in tables.Fired) result.Add(TableResolution.Fires(fired.Key, BindTables(fired.Value, frame, depth)));
        return result;
    }

    private TableResolution Returned(ReturnedTables returned, Frame frame, int depth)
    {
        if (depth > MaxDepth) return returned.Reason.Length > 0 ? TableResolution.Unresolved($"{returned.Reason}, through too many calls") : TableResolution.Empty;

        var result = TableResolution.Empty;
        var cyclic = false;
        var found = false;
        foreach (var (target, state, _) in Dispatch(returned.Call, frame, depth))
        {
            if (!graph.Nodes.TryGetValue(target, out var callee) || !callee.ReturnTables.TryGetValue(returned.Shape, out var tables)) continue;
            found = true;

            // A method that returns its own result, directly or through others, adds nothing the other returns don't
            var key = target + "\n" + returned.Shape;
            if (!_returning.Add(key))
            {
                cyclic = true;
                continue;
            }
            try
            {
                var calleeFrame = new Frame(target, state, Bind(returned.Call, frame, callee, depth), frame.Basis, frame.Path, frame.Injected);
                result.Add(BindTables(tables, calleeFrame, depth));
            }
            finally
            {
                _returning.Remove(key);
            }
        }
        // Requests that need nothing resolve to nothing, so for them only a method that returns none is unknown
        if (cyclic || returned.Reason.Length == 0) return result;
        if (RequestShapes.IsRequest(returned.Shape)) return found ? result : TableResolution.Unresolved(returned.Reason);
        return result.OrUnresolved(returned.Reason);
    }

    private IReadOnlyList<Identity> Resolve(IEnumerable<ServiceOrigin> origins, Frame frame, int depth)
    {
        var result = new SortedSet<Identity>();
        foreach (var origin in origins) result.UnionWith(Resolve(origin, frame, depth));
        return result.ToList();
    }

    private IEnumerable<Identity> Resolve(ServiceOrigin origin, Frame frame, int depth)
    {
        if (depth > MaxDepth) return [Unknown("service is passed through too many calls")];

        switch (origin)
        {
            case FixedOrigin { RunsAs.Kind: RunsAsKind.Caller, Initiating: false }:
                return [new Identity(entry.Caller)];

            case FixedOrigin @fixed:
                return [new Identity(@fixed.RunsAs)];

            case ParameterOrigin parameter:
                if (frame.Bindings.TryGetValue((parameter.Method, parameter.Ordinal), out var binding) && binding.Services is { } services) return services;
                return frame.Injected ? [entry.Injected] : [Unknown($"service comes from parameter '{parameter.Name}'")];

            case FieldOrigin field:
                return Field(field, frame, depth) ?? [Unknown($"service comes from field '{ShortName(field.Field)}'")];

            case InjectedOrigin:
                return [entry.Injected];

            case ReturnedOrigin returned:
            {
                var result = new List<Identity>();
                foreach (var (target, state, _) in Dispatch(returned.Call, frame, depth + 1))
                {
                    if (!graph.Nodes.TryGetValue(target, out var callee)) continue;
                    var calleeFrame = new Frame(target, state, Bind(returned.Call, frame, callee), frame.Basis, frame.Path, frame.Injected);
                    foreach (var value in callee.Returns) result.AddRange(Resolve(value, calleeFrame, depth + 1));
                }
                return result.Count > 0 ? result : [Unknown($"service is returned by '{ShortName(returned.Call.Target)}'")];
            }

            case UnknownOrigin unknown:
                return [Unknown(unknown.Reason)];

            default:
                return [Unknown("service is computed at runtime")];
        }
    }

    private IReadOnlyList<Identity>? Field(FieldOrigin field, Frame frame, int depth)
    {
        if (field.OnThis && frame.Object is { } current)
        {
            if (current.Fields.TryGetValue(field.Field, out var value)) return value;
            if (current.Type is { } type && type != field.Type && DefaultObject(type, depth + 1).Fields.TryGetValue(field.Field, out var inherited)) return inherited;
        }
        return DefaultObject(field.Type, depth + 1).Fields.TryGetValue(field.Field, out var fallback) ? fallback : null;
    }

    /// <summary>
    /// The object a constructor call creates, with the services its constructors store.
    /// </summary>
    private ObjectState Construct(CallFact constructor, Frame caller, int depth)
    {
        var state = new ObjectState(constructor.ReceiverType);
        if (depth <= MaxDepth && graph.Nodes.TryGetValue(constructor.Target, out var node))
        {
            RunConstructor(node, Bind(constructor, caller, node), state, caller.Injected, depth + 1);
        }
        return state.Freeze();
    }

    /// <summary>
    /// An object made by the host or a dependency injection container: every constructor, with injected
    /// parameters. Memoized per type.
    /// </summary>
    private ObjectState DefaultObject(string type, int depth)
    {
        if (_defaultObjects.TryGetValue(type, out var known)) return known;
        if (depth > MaxDepth || !_constructing.Add(type)) return new ObjectState(type).Freeze();

        try
        {
            var state = new ObjectState(type);
            var constructors = graph.Types.TryGetValue(type, out var summary) ? summary.Constructors.ToList() : [];
            if (constructors.Count == 0)
            {
                Initialize(type, new Frame("", state, NoBindings, RequirementBasis.Direct, [], true), state, depth + 1);
            }

            foreach (var constructor in constructors)
            {
                if (!graph.Nodes.TryGetValue(constructor, out var node)) continue;
                var single = new ObjectState(type);
                RunConstructor(node, NoBindings, single, injected: true, depth + 1);
                state.Merge(single);
            }

            var result = state.Freeze();
            _defaultObjects[type] = result;
            return result;
        }
        finally
        {
            _constructing.Remove(type);
        }
    }

    private void RunConstructor(NodeSummary constructor, IReadOnlyDictionary<(string, int), Binding> bindings, ObjectState state, bool injected, int depth)
    {
        if (depth > MaxDepth) return;
        var frame = new Frame(constructor.Id, state, bindings, RequirementBasis.Direct, [], injected);

        if (constructor.Type is { } type) Initialize(type, frame, state, depth);

        // : base(…) and : this(…)
        foreach (var call in constructor.Calls.Where(c => c.Receiver == ReceiverKind.This && IsConstructor(c.Target)))
        {
            if (graph.Nodes.TryGetValue(call.Target, out var chained) && chained.Id != constructor.Id)
            {
                RunConstructor(chained, Bind(call, frame, chained), state, injected, depth + 1);
            }
        }

        foreach (var write in constructor.FieldWrites)
        {
            state.Fields[write.Field] = Resolve(write.Value, frame, depth + 1);
        }
    }

    private void Initialize(string type, Frame frame, ObjectState state, int depth)
    {
        if (!graph.Types.TryGetValue(type, out var summary)) return;
        foreach (var write in summary.Initializers)
        {
            state.Fields[write.Field] = Resolve(write.Value, frame, depth + 1);
        }
    }

    private Identity Unknown(string reason) => new(entry.Caller with { Assumed = true }, reason);

    private static string ShortName(string id)
    {
        var end = id.IndexOf('(');
        var name = end < 0 ? id : id.Substring(0, end);
        return name.Substring(name.LastIndexOf('.') + 1);
    }

    /// <summary>
    /// The services and tables passed to one parameter.
    /// </summary>
    private sealed record Binding(IReadOnlyList<Identity>? Services, IReadOnlyDictionary<TableShape, TableResolution>? Tables)
    {
        public override string ToString() =>
            string.Join(",", Services ?? []) + "|" + string.Join(",", (Tables ?? new Dictionary<TableShape, TableResolution>()).OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"));
    }

    /// <summary>
    /// The services stored in an object's fields.
    /// </summary>
    private sealed class ObjectState(string? type)
    {
        private string? _key;

        public string? Type { get; } = type;

        public Dictionary<string, IReadOnlyList<Identity>> Fields { get; } = new(StringComparer.Ordinal);

        public string Key => _key ?? throw new InvalidOperationException("Object state is not complete");

        public void Merge(ObjectState other)
        {
            foreach (var field in other.Fields)
            {
                Fields[field.Key] = Fields.TryGetValue(field.Key, out var existing)
                    ? existing.Concat(field.Value).Distinct().OrderBy(i => i).ToList()
                    : field.Value;
            }
        }

        public ObjectState Freeze()
        {
            _key = Type + "{" + string.Join(";", Fields.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => f.Key + "=" + string.Join(",", f.Value))) + "}";
            return this;
        }
    }

    /// <summary>
    /// A node in a call context. <see cref="Injected"/> is set while building an object for a container, where
    /// unbound constructor parameters are injected.
    /// </summary>
    private sealed class Frame(
        string node,
        ObjectState? state,
        IReadOnlyDictionary<(string, int), Binding> bindings,
        RequirementBasis basis,
        IReadOnlyList<string> path,
        bool injected = false)
    {
        public string Node { get; } = node;
        public ObjectState? Object { get; } = state;
        public IReadOnlyDictionary<(string, int), Binding> Bindings { get; } = bindings;
        public RequirementBasis Basis { get; } = basis;
        public IReadOnlyList<string> Path { get; } = path;
        public bool Injected { get; } = injected;

        public string Key =>
            Node + "\n" + Basis + "\n" + Object?.Key + "\n"
            + string.Join("\n", Bindings.OrderBy(b => b.Key.Item1, StringComparer.Ordinal).ThenBy(b => b.Key.Item2).Select(b => $"{b.Key.Item1}#{b.Key.Item2}:{b.Value}"));
    }
}
