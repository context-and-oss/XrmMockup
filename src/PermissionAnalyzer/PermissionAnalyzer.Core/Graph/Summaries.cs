using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Graph;

// The call graph is built from these summaries. They hold ids and source locations only, no Roslyn objects,
// so summaries of several compilations (projects, target frameworks) merge into one graph by id.

/// <summary>
/// Where an organization service comes from. Origins that depend on the caller (parameters, fields) are
/// resolved per call path.
/// </summary>
internal abstract record ServiceOrigin;

/// <summary>
/// A known identity. <see cref="RunsAs.Caller"/> means the caller of the entry point, which a plugin step's
/// impersonating user replaces.
/// <see cref="Initiating"/> marks the user who started the operation (<c>InitiatingUserId</c>), whom impersonation
/// doesn't replace.
/// </summary>
internal sealed record FixedOrigin(RunsAs RunsAs, bool Initiating = false) : ServiceOrigin;

internal sealed record ParameterOrigin(string Method, int Ordinal, string Name) : ServiceOrigin;

/// <summary>
/// A field or auto-property of <see cref="Type"/>. <see cref="OnThis"/> is set when it is read from the current
/// object, so the object's constructor arguments decide its value.
/// </summary>
internal sealed record FieldOrigin(string Type, string Field, bool OnThis) : ServiceOrigin;

/// <summary>
/// Provided by dependency injection or a service provider. Who it runs as depends on the host.
/// </summary>
internal sealed record InjectedOrigin(string Description) : ServiceOrigin;

/// <summary>
/// Returned by a method in the solution.
/// </summary>
internal sealed record ReturnedOrigin(CallFact Call) : ServiceOrigin;

internal sealed record UnknownOrigin(string Reason) : ServiceOrigin;

/// <summary>
/// One service call: the tables it touches and the service it is made on.
/// </summary>
internal sealed record AccessFact(
    string Operation,
    SourceLocation Source,
    IReadOnlyList<Privilege> Privileges,
    TableResolution Tables,
    string ReasonPrefix,
    IReadOnlyList<ServiceOrigin> Service);

internal enum ReceiverKind
{
    /// <summary>
    /// A static method.
    /// </summary>
    None,

    /// <summary>
    /// The current object, including <c>base.M()</c>, constructor initializers and local functions.
    /// </summary>
    This,

    /// <summary>
    /// An object created at the call site, or in a local that holds only that object.
    /// </summary>
    Created,

    /// <summary>
    /// Any other object, of unknown concrete type.
    /// </summary>
    Other,
}

/// <summary>
/// A call to a method that may be in the solution. <see cref="Virtual"/> is set when the method that runs depends
/// on the receiver's runtime type. <see cref="Creation"/> is the constructor call that made the receiver, for
/// <see cref="ReceiverKind.Created"/>; a constructor call is its own creation and leaves it <c>null</c>.
/// </summary>
internal sealed record CallFact(
    SourceLocation Source,
    string Target,
    bool Virtual,
    ReceiverKind Receiver,
    string? ReceiverType,
    IReadOnlyDictionary<int, ArgumentFact> Arguments,
    CallFact? Creation = null);

/// <summary>
/// What an argument contributes to the callee: its service origins and its tables per shape.
/// </summary>
internal sealed record ArgumentFact(IReadOnlyList<ServiceOrigin>? Service, IReadOnlyDictionary<TableShape, TableResolution>? Tables);

internal sealed record FieldWrite(string Field, IReadOnlyList<ServiceOrigin> Value);

/// <summary>
/// A call graph node: a member, lambda or local function, with the facts found in its body.
/// </summary>
internal sealed class NodeSummary(string id, string member, string display, string? type, bool isConstructor, SourceLocation? declaration, SourceLocation? memberDeclaration)
{
    public string Id { get; } = id;

    public bool IsConstructor { get; } = isConstructor;

    /// <summary>
    /// The id of the member that contains this node; the node's own id unless it's a lambda or local function.
    /// </summary>
    public string Member { get; } = member;

    public string Display { get; } = display;

    /// <summary>
    /// Id of the containing type.
    /// </summary>
    public string? Type { get; } = type;

    public SourceLocation? Declaration { get; } = declaration;

    public SourceLocation? MemberDeclaration { get; } = memberDeclaration;

    public List<AccessFact> Accesses { get; } = [];

    public List<CallFact> Calls { get; } = [];

    /// <summary>
    /// Lambdas declared directly in this node. They count as reachable from it.
    /// </summary>
    public SortedSet<string> Lambdas { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The services this method returns, when it returns one.
    /// </summary>
    public List<IReadOnlyList<ServiceOrigin>> Returns { get; } = [];

    /// <summary>
    /// Services stored in fields of the current object.
    /// </summary>
    public List<FieldWrite> FieldWrites { get; } = [];

    /// <summary>
    /// Adds the facts of the same node from another compilation, such as another target framework. Facts at the
    /// same source location are the same fact.
    /// </summary>
    public void Merge(NodeSummary other)
    {
        MergeBySource(Accesses, other.Accesses, a => (a.Source, a.Operation + "\n" + a.ReasonPrefix));
        MergeBySource(Calls, other.Calls, c => (c.Source, c.Target));
        Lambdas.UnionWith(other.Lambdas);
        if (Returns.Count == 0) Returns.AddRange(other.Returns);
        if (FieldWrites.Count == 0) FieldWrites.AddRange(other.FieldWrites);
    }

    private static void MergeBySource<T>(List<T> target, List<T> source, Func<T, (SourceLocation, string)> key)
    {
        var known = new HashSet<(SourceLocation, string)>(target.Select(key));
        target.AddRange(source.Where(f => known.Add(key(f))));
    }
}

/// <summary>
/// A type in the solution, for virtual dispatch and object construction.
/// </summary>
internal sealed class TypeSummary(string id, bool isAbstract, bool isInterface, IReadOnlyList<string> ancestors)
{
    public string Id { get; } = id;

    /// <summary>
    /// Abstract classes and interfaces, which are never the runtime type of an object.
    /// </summary>
    public bool IsAbstract { get; } = isAbstract;

    public bool IsInterface { get; } = isInterface;

    /// <summary>
    /// Base classes and every implemented interface.
    /// </summary>
    public IReadOnlyList<string> Ancestors { get; } = ancestors;

    /// <summary>
    /// For each method of the type or its ancestors, the method in the solution that runs for an object of this type.
    /// </summary>
    public Dictionary<string, string> Implementations { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Instance constructors in the solution.
    /// </summary>
    public SortedSet<string> Constructors { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Services stored by field and property initializers.
    /// </summary>
    public List<FieldWrite> Initializers { get; } = [];
}

/// <summary>
/// A dependency injection registration: <c>AddScoped&lt;IFoo, Foo&gt;()</c> maps <c>IFoo</c> to <c>Foo</c>.
/// </summary>
internal sealed record Registration(string Service, string Implementation);

/// <summary>
/// Where an entry point starts. <see cref="ObjectType"/> is the runtime type of the current object, when known;
/// otherwise the handler is dispatched like an interface call on <see cref="ServiceType"/>.
/// </summary>
internal sealed record Handler(string Target, string? ObjectType, string? ServiceType = null);

/// <summary>
/// An entry point found in code, before its requirements are worked out.
/// </summary>
internal sealed record EntryCandidate(
    string Id,
    EntryPointKind Kind,
    string Type,
    string? Assembly,
    Trigger? Trigger,
    string? ImpersonatingUserId,
    SourceLocation? Declaration,
    IReadOnlyList<Handler> Handlers);

/// <summary>
/// A class implementing <c>IPlugin</c>, for matching the steps in the plugin metadata.
/// </summary>
internal sealed record PluginClass(string Type, string? Assembly, SourceLocation? Declaration, string TypeId, string? Execute, bool HasCodeSteps);
