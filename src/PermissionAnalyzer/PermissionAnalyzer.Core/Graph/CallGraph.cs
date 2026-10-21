namespace XrmMockup.PermissionAnalyzer.Core.Graph;

/// <summary>
/// The summaries of every compilation in the analysis, merged by id.
/// </summary>
internal sealed class CallGraph
{
    public Dictionary<string, NodeSummary> Nodes { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, TypeSummary> Types { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Dependency injection registrations: implementation types by service type.
    /// </summary>
    public Dictionary<string, SortedSet<string>> Registrations { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds the summaries of another compilation. A node or type that both have, such as code compiled for two
    /// target frameworks, is merged.
    /// </summary>
    public void Merge(CallGraph other)
    {
        foreach (var node in other.Nodes.Values)
        {
            if (Nodes.TryGetValue(node.Id, out var existing)) existing.Merge(node);
            else Nodes.Add(node.Id, node);
        }

        foreach (var type in other.Types.Values)
        {
            if (!Types.ContainsKey(type.Id)) Types.Add(type.Id, type);
        }

        foreach (var registration in other.Registrations)
        {
            foreach (var implementation in registration.Value) Register(registration.Key, implementation);
        }
    }

    public void Register(string service, string implementation)
    {
        if (!Registrations.TryGetValue(service, out var implementations))
        {
            implementations = new SortedSet<string>(StringComparer.Ordinal);
            Registrations.Add(service, implementations);
        }
        implementations.Add(implementation);
    }

    /// <summary>
    /// Types in the solution that an object of <paramref name="type"/> can be at runtime, sorted.
    /// </summary>
    public IEnumerable<TypeSummary> ConcreteTypes(string type) =>
        Types.Values
            .Where(t => !t.IsAbstract && (t.Id == type || t.Ancestors.Contains(type, StringComparer.Ordinal)))
            .OrderBy(t => t.Id, StringComparer.Ordinal);
}
