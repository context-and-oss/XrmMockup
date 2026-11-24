using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Analysis;

/// <summary>
/// Extracted, scenario-independent inputs. No compilations, evaluated results, coverage or ad hoc user
/// operations are retained. The owning CLI defines the versioned wire format for these internal facts.
/// </summary>
internal sealed record ExtractedAnalysis(
    IReadOnlyList<NodeSummary> Nodes,
    IReadOnlyList<TypeSummary> Types,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Registrations,
    IReadOnlyList<EntryCandidate> Entries,
    IReadOnlyList<PluginClass> Plugins,
    IReadOnlyList<string> CascadeOnlyEntries,
    IReadOnlyList<string> CascadeOnlyPlugins,
    IReadOnlyList<string> ReportedNodes,
    IReadOnlyList<string> EntryMethods,
    IReadOnlyList<RequestRule> BuiltInRules,
    IReadOnlyList<RequestRule> Rules,
    IReadOnlyList<LookupColumn> Lookups,
    IReadOnlyList<PluginStepRegistration> PluginSteps,
    IReadOnlyList<WorkflowRegistration> Workflows,
    IReadOnlyList<CustomApiDefinition> CustomApis,
    IReadOnlyList<PrivilegeDefinition>? Privileges);

public sealed partial class ManifestBuilder
{
    internal ExtractedAnalysis Extracted() => new(
        _graph.Nodes.Values.OrderBy(n => n.Id, StringComparer.Ordinal).ToList(),
        _graph.Types.Values.OrderBy(t => t.Id, StringComparer.Ordinal).ToList(),
        _graph.Registrations.OrderBy(p => p.Key, StringComparer.Ordinal)
            .ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToList(), StringComparer.Ordinal),
        _entries.Values.OrderBy(e => e.Id, StringComparer.Ordinal).ToList(),
        _plugins.Values.OrderBy(p => Key(p), StringComparer.Ordinal).ToList(),
        _cascadeOnlyEntries.OrderBy(s => s, StringComparer.Ordinal).ToList(),
        _cascadeOnlyPlugins.OrderBy(s => s, StringComparer.Ordinal).ToList(),
        _reportedNodes.OrderBy(s => s, StringComparer.Ordinal).ToList(),
        _entryMethods.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList(),
        _rules.BuiltInDefinitions, _rules.CustomDefinitions, _lookups.Columns,
        _steps.ToList(), _workflows.ToList(),
        _catalogCustomApis.Values.OrderBy(a => a.UniqueName, StringComparer.Ordinal).ToList(),
        _privilegeCatalog?.Values.OrderBy(p => p.Name, StringComparer.Ordinal).ToList());

    internal static ManifestBuilder Restore(string root, ExtractedAnalysis state) => new(root, state);

    private ManifestBuilder(string root, ExtractedAnalysis state) : this(root, state.Rules, state.Lookups)
    {
        _rules = new RequestRules(state.Rules, state.BuiltInRules);
        foreach (var node in state.Nodes) _graph.Nodes.Add(node.Id, node);
        foreach (var type in state.Types) _graph.Types.Add(type.Id, type);
        foreach (var pair in state.Registrations)
            foreach (var implementation in pair.Value) _graph.Register(pair.Key, implementation);
        foreach (var entry in state.Entries) _entries.Add(entry.Id, entry);
        foreach (var plugin in state.Plugins) _plugins.Add(Key(plugin), plugin);
        _cascadeOnlyEntries.UnionWith(state.CascadeOnlyEntries);
        _cascadeOnlyPlugins.UnionWith(state.CascadeOnlyPlugins);
        _reportedNodes.UnionWith(state.ReportedNodes);
        _entryMethods.AddRange(state.EntryMethods);
        _steps.AddRange(state.PluginSteps);
        _workflows.AddRange(state.Workflows);
        if (state.Privileges is not null) AddPrivilegeCatalog(state.Privileges, state.CustomApis);
        else foreach (var api in state.CustomApis) _catalogCustomApis.Add(api.UniqueName, api);
    }
}
