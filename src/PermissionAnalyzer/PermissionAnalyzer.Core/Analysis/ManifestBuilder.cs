using Microsoft.CodeAnalysis;
using XrmMockup.PermissionAnalyzer.Core.Discovery;
using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Analysis;

/// <summary>
/// Builds a <see cref="Manifest"/> from one or more compilations: finds the entry points, follows the calls
/// from each and collects the privileges the reached operations need. A project that targets several
/// frameworks is added once per target; its duplicate findings merge into one.
/// </summary>
/// <param name="rootDirectory">The directory source locations are reported relative to.</param>
/// <param name="rules">
/// Rules for the requests passed to <c>Execute</c>, which replace the built-in rule for their message or add one.
/// </param>
/// <param name="lookups">
/// The lookup columns in the metadata, which give the tables a lookup can refer to when the code doesn't say.
/// </param>
public sealed class ManifestBuilder(string rootDirectory, IEnumerable<RequestRule>? rules = null, IEnumerable<LookupColumn>? lookups = null)
{
    private const string InjectedAssumption = "service is injected";

    private readonly SourcePaths _paths = new(rootDirectory);
    private readonly RequestRules _rules = new(rules);
    private readonly LookupColumns _lookups = new(lookups);
    private readonly CallGraph _graph = new();
    private readonly Dictionary<string, EntryCandidate> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PluginClass> _plugins = new(StringComparer.Ordinal);
    private readonly List<PluginStepRegistration> _steps = [];
    private readonly List<string> _entryMethods = [];
    private readonly Dictionary<string, UserOperation> _operations = new(StringComparer.Ordinal);

    // What only plugin code from other solutions has, which runs in cascades but isn't reported on its own
    private readonly HashSet<string> _cascadeOnlyEntries = new(StringComparer.Ordinal);
    private readonly HashSet<string> _cascadeOnlyPlugins = new(StringComparer.Ordinal);
    private readonly HashSet<string> _reportedNodes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _customApis = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EntryCandidate> _customApiCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CustomApiDefinition> _catalogCustomApis = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, PrivilegeDefinition>? _privilegeCatalog;

    /// <summary>
    /// Adds the code in <paramref name="compilation"/>. Returns <c>false</c> when the compilation doesn't
    /// reference the Dataverse SDK, so it can't contain any operations.
    /// </summary>
    public bool Add(Compilation compilation, CancellationToken cancellationToken = default) => Add(compilation, AddedCode.Analyzed, cancellationToken);

    /// <summary>
    /// Adds the code of a library the analyzed code references, such as a data access library that several apps share.
    /// Its entry points are reported and what the analyzed code calls in it is analyzed, but its methods that no entry
    /// point reaches aren't reported as unreached: other apps may call them. Code that
    /// <see cref="Add(Compilation, CancellationToken)"/> adds as well is reported. Returns <c>false</c> when the
    /// compilation doesn't reference the Dataverse SDK.
    /// </summary>
    public bool AddReferencedCode(Compilation compilation, CancellationToken cancellationToken = default) => Add(compilation, AddedCode.Referenced, cancellationToken);

    /// <summary>
    /// Adds plugin code from outside the analyzed solution, such as the plugins of the environment an Azure Function
    /// app calls. Its plugin steps and custom APIs run when operations fire them, but its entry points and unreached
    /// methods aren't reported, unless a scope selects them. Code that <see cref="Add(Compilation, CancellationToken)"/>
    /// adds as well is reported. Returns <c>false</c> when the compilation doesn't reference the Dataverse SDK.
    /// </summary>
    public bool AddPluginCode(Compilation compilation, CancellationToken cancellationToken = default) => Add(compilation, AddedCode.PluginCode, cancellationToken);

    private enum AddedCode { Analyzed, Referenced, PluginCode }

    private bool Add(Compilation compilation, AddedCode added, CancellationToken cancellationToken)
    {
        var cascadesOnly = added == AddedCode.PluginCode;
        if (XrmSymbols.Create(compilation) is not { } symbols) return false;

        var summaries = new SummaryExtractor(compilation, symbols, _paths, _rules, _lookups).Extract(cancellationToken);
        if (added == AddedCode.Analyzed) _reportedNodes.UnionWith(summaries.Nodes.Keys);
        _graph.Merge(summaries);

        var discovery = new EntryPointDiscovery(compilation, symbols, _paths).Discover(cancellationToken);
        foreach (var entry in discovery.Entries)
        {
            if (!cascadesOnly) _cascadeOnlyEntries.Remove(entry.Id);
            else if (!_entries.ContainsKey(entry.Id)) _cascadeOnlyEntries.Add(entry.Id);
            AddEntry(entry);
        }
        foreach (var plugin in discovery.Plugins)
        {
            var key = plugin.Type + "\n" + plugin.Assembly;
            if (!cascadesOnly) _cascadeOnlyPlugins.Remove(key);
            else if (!_plugins.ContainsKey(key)) _cascadeOnlyPlugins.Add(key);

            if (_plugins.TryGetValue(key, out var existing)) _plugins[key] = existing with { HasCodeSteps = existing.HasCodeSteps || plugin.HasCodeSteps };
            else _plugins.Add(key, plugin);
        }
        return true;
    }

    /// <summary>
    /// Adds the plugin steps registered in the environment, from the XrmMockup metadata.
    /// </summary>
    public void AddPluginSteps(IEnumerable<PluginStepRegistration> steps) => _steps.AddRange(steps);

    /// <summary>
    /// Adds the privilege catalog from the metadata: the tables and table privilege of each privilege, by name, and
    /// the execute privileges of the custom APIs in the environment. Without it, an execute privilege set by name
    /// can't be looked up and is reported as unresolved.
    /// </summary>
    public void AddPrivilegeCatalog(IEnumerable<PrivilegeDefinition> privileges, IEnumerable<CustomApiDefinition> customApis)
    {
        _privilegeCatalog ??= new Dictionary<string, PrivilegeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var privilege in privileges) _privilegeCatalog[privilege.Name] = privilege;
        foreach (var api in customApis) _catalogCustomApis[api.UniqueName] = api;
    }

    /// <summary>
    /// Adds methods to analyze as entry points, such as <c>Contoso.Sync.Importer.Run</c>, which matches every
    /// overload, or a full id with parameter types.
    /// </summary>
    public void AddEntryMethods(IEnumerable<string> methods) => _entryMethods.AddRange(methods);

    /// <summary>
    /// Adds a user operation to analyze as an entry point, such as <c>Update</c> on <c>account</c> made in an app:
    /// the privileges its request needs as the caller, and those of the plugin steps it fires. <paramref name="columns"/>
    /// are the columns it sets, which decide the Update steps filtered on columns that it fires; without them those
    /// steps fire with the basis <see cref="RequirementBasis.FilterUnknown"/>.
    /// </summary>
    public void AddOperation(string message, string table, IEnumerable<string>? columns = null)
    {
        var set = columns?
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();
        var operation = new UserOperation(RequestRules.MessageOf(message.Trim()), table.Trim().ToLowerInvariant(), set is { Count: > 0 } ? set : null);
        var id = $"operation:{operation.Display}" + (operation.Columns is { } known ? $"({string.Join(",", known)})" : "");
        _operations[id] = operation;
    }

    /// <summary>
    /// Builds the manifest. Pass the tables that at least one security role mentions to fill
    /// <see cref="Manifest.NotRoleGoverned"/>; leave it <c>null</c> when no roles are known. With a
    /// <paramref name="scope"/>, only the matching entry points are analyzed and nothing is reported as unreached;
    /// without one, the entry points of plugin code added with <see cref="AddPluginCode"/> are left out.
    /// </summary>
    public Manifest Build(IEnumerable<string>? roleGovernedTables = null, EntryPointSelector? scope = null)
    {
        var candidates = new Dictionary<string, EntryCandidate>(_entries, StringComparer.Ordinal);
        var cascadeOnly = new HashSet<string>(_cascadeOnlyEntries, StringComparer.Ordinal);
        var codeless = AddMetadataSteps(candidates, cascadeOnly);
        AddMethodEntries(candidates);
        foreach (var operation in _operations)
        {
            candidates[operation.Key] = new EntryCandidate(operation.Key, EntryPointKind.Operation, "", null, new Trigger(operation.Value.Message, operation.Value.Table), null, null, []);
        }
        var cascades = new Cascades(_graph, CascadeSteps(candidates.Values, codeless), (step, caller) => Context(caller, step.Trigger?.Table));

        // Messages the solution implements or registers steps on, which no rule knows, are its custom APIs
        _customApis.Clear();
        _customApis.UnionWith(candidates.Values.Where(c => c.Kind != EntryPointKind.Operation).Select(c => c.Trigger?.Message).Concat(_steps.Select(s => s.Message)).OfType<string>());
        _customApiCode.Clear();
        foreach (var api in candidates.Values.Where(c => c.Kind == EntryPointKind.CustomApi).OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (api.Trigger?.Message is { } message && !_customApiCode.ContainsKey(message)) _customApiCode[message] = api;
        }

        var entryPoints = new List<EntryPoint>();
        var reached = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in candidates.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (scope is not null && !scope.Matches(Describe(candidate, [], [], []))) continue;

            var context = Context(candidate);
            if (scope is null && cascadeOnly.Contains(candidate.Id))
            {
                // Not reported, but what it reaches isn't unreached
                reached.UnionWith(new Evaluator(_graph, context).Evaluate(candidate.Handlers).Reached);
                continue;
            }

            var evaluator = new Evaluator(_graph, context);
            var evaluation = _operations.TryGetValue(candidate.Id, out var user)
                ? Unprefixed(evaluator.EvaluateAlone(user.Node(candidate.Id, _rules, _lookups), user.Display))
                : evaluator.Evaluate(candidate.Handlers);
            var cascade = cascades.Expand(evaluation, candidate.Id, context.Caller);
            reached.UnionWith(cascade.Result.Reached);
            entryPoints.Add(Describe(candidate, cascade));
        }

        var unreached = scope is null ? Unreached(reached) : [];
        var manifest = new Manifest(entryPoints, unreached);
        if (roleGovernedTables is null) return manifest;

        var governed = new HashSet<string>(roleGovernedTables.Select(t => t.ToLowerInvariant()), StringComparer.Ordinal);
        return new Manifest(entryPoints, unreached, manifest.Tables.Where(t => !governed.Contains(t)).ToList());
    }

    /// <summary>
    /// <paramref name="result"/> without call paths. A user operation's hits are at <see cref="UserOperation.Location"/>,
    /// so the path of a step it fires starts at the operation.
    /// </summary>
    private static EvaluationResult Unprefixed(EvaluationResult result)
    {
        var unprefixed = new EvaluationResult { Truncated = result.Truncated };
        unprefixed.Requirements.AddRange(result.Requirements.Select(h => h with { Source = h.Source with { Path = [] } }));
        unprefixed.Unresolved.AddRange(result.Unresolved.Select(h => h with { Source = h.Source with { Path = [] } }));
        unprefixed.Assumptions.AddRange(result.Assumptions.Select(h => h with { Source = h.Source with { Path = [] } }));
        unprefixed.Globals.AddRange(result.Globals.Select(h => h with { Source = h.Source with { Path = [] } }));
        unprefixed.Triggers.AddRange(result.Triggers.Select(h => h with { Source = h.Source with { Path = [] } }));
        return unprefixed;
    }

    private void AddEntry(EntryCandidate entry)
    {
        if (!_entries.TryGetValue(entry.Id, out var existing))
        {
            _entries.Add(entry.Id, entry);
            return;
        }

        // The same registration compiled for another target framework, or a step registered twice
        var handlers = existing.Handlers.Concat(entry.Handlers).Distinct().ToList();
        var declaration = existing.Declaration is null || entry.Declaration is not null && entry.Declaration.CompareTo(existing.Declaration) < 0
            ? entry.Declaration
            : existing.Declaration;
        _entries[entry.Id] = existing with
        {
            // Independent of the order compilations are added in
            Assembly = string.CompareOrdinal(entry.Assembly, existing.Assembly) < 0 ? entry.Assembly : existing.Assembly,
            Handlers = handlers.OrderBy(h => h.Target, StringComparer.Ordinal).ThenBy(h => h.ObjectType, StringComparer.Ordinal).ToList(),
            Declaration = declaration,
            ImpersonatingUserId = existing.ImpersonatingUserId is null || entry.ImpersonatingUserId is not null && string.CompareOrdinal(entry.ImpersonatingUserId, existing.ImpersonatingUserId) < 0
                ? entry.ImpersonatingUserId
                : existing.ImpersonatingUserId,
        };
    }

    /// <summary>
    /// Steps from the metadata. A step of a plugin that registers its steps in code adds the impersonating user
    /// to the matching code step, or runs <c>Execute</c> when no code step matches. A plain <c>IPlugin</c> gets
    /// one entry point per step, or one for the whole plugin when the metadata has no steps for it. Returns the
    /// steps of plugins whose code isn't loaded. Adds the entry points of plugins that only plugin code from other
    /// solutions has to <paramref name="cascadeOnly"/>.
    /// </summary>
    private List<CascadeStep> AddMetadataSteps(Dictionary<string, EntryCandidate> candidates, HashSet<string> cascadeOnly)
    {
        var codeless = new List<CascadeStep>();
        var classes = _plugins.Values.OrderBy(p => p.Type, StringComparer.Ordinal).ThenBy(p => p.Assembly, StringComparer.Ordinal).ToList();
        var stepsByClass = new Dictionary<PluginClass, List<PluginStepRegistration>>();
        foreach (var step in _steps)
        {
            var matches = classes.Where(c => c.Type == step.TypeName && step.AssemblyName is not null && c.Assembly == step.AssemblyName).ToList();
            if (matches.Count == 0) matches = classes.Where(c => c.Type == step.TypeName).ToList();
            if (matches.Count == 0 && !string.IsNullOrEmpty(step.Message))
            {
                var table = string.IsNullOrEmpty(step.Table) ? null : step.Table!.ToLowerInvariant();
                var id = EntryPointDiscovery.StepId(step.TypeName, step.Message, table, EntryPointDiscovery.StageName(step.Stage));
                codeless.Add(new CascadeStep(id, step.Message, table, step.ImpersonatingUserId?.ToLowerInvariant(), Filter(step), null, step.Asynchronous));
            }
            foreach (var match in matches)
            {
                if (!stepsByClass.TryGetValue(match, out var list)) stepsByClass.Add(match, list = []);
                list.Add(step);
            }
        }

        var bases = new HashSet<string>(
            classes.SelectMany(c => _graph.Types.TryGetValue(c.TypeId, out var type) ? type.Ancestors : []),
            StringComparer.Ordinal);

        foreach (var plugin in classes)
        {
            stepsByClass.TryGetValue(plugin, out var steps);
            var handlers = plugin.Execute is null ? [] : new[] { new Handler(plugin.Execute, plugin.TypeId) };

            if (steps is null || steps.Count == 0)
            {
                // A base class of other plugins, such as a DAXIF Plugin class, isn't registered itself
                if (!plugin.HasCodeSteps && !bases.Contains(plugin.TypeId))
                {
                    var id = $"plugin:{plugin.Type}";
                    if (!candidates.ContainsKey(id))
                    {
                        candidates.Add(id, new EntryCandidate(id, EntryPointKind.Plugin, plugin.Type, plugin.Assembly, null, null, plugin.Declaration, handlers));
                        if (IsCascadeOnly(plugin)) cascadeOnly.Add(id);
                    }
                }
                continue;
            }

            foreach (var step in steps)
            {
                var stage = EntryPointDiscovery.StageName(step.Stage);
                var table = string.IsNullOrEmpty(step.Table) ? null : step.Table!.ToLowerInvariant();
                var user = step.ImpersonatingUserId?.ToLowerInvariant();
                var filter = Filter(step);

                var code = plugin.HasCodeSteps
                    ? candidates.Values.FirstOrDefault(c =>
                        c.Kind == EntryPointKind.PluginStep
                        && c.Type == plugin.Type
                        && string.Equals(c.Trigger?.Message, step.Message, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(c.Trigger?.Table, table, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(c.Trigger?.Stage, stage, StringComparison.OrdinalIgnoreCase))
                    : null;

                if (code is not null)
                {
                    candidates[code.Id] = Registered(code, user, filter, step.Asynchronous);
                    continue;
                }

                var id = EntryPointDiscovery.StepId(plugin.Type, step.Message, table, stage);
                if (candidates.TryGetValue(id, out var existing))
                {
                    candidates[id] = Registered(existing, user, filter, step.Asynchronous);
                    continue;
                }
                candidates.Add(id, new EntryCandidate(id, EntryPointKind.PluginStep, plugin.Type, plugin.Assembly, new Trigger(step.Message, table, stage, FilteredAttributes: filter, Asynchronous: step.Asynchronous), user, plugin.Declaration, handlers));
                if (IsCascadeOnly(plugin)) cascadeOnly.Add(id);
            }
        }
        return codeless;
    }

    private bool IsCascadeOnly(PluginClass plugin) => _cascadeOnlyPlugins.Contains(plugin.Type + "\n" + plugin.Assembly);

    /// <summary>
    /// The columns an Update step from the metadata is filtered on, lowercased.
    /// </summary>
    private static IReadOnlyList<string>? Filter(PluginStepRegistration step) =>
        step.Message.Equals("Update", StringComparison.OrdinalIgnoreCase) ? step.FilteredAttributes : null;

    /// <summary>
    /// <paramref name="candidate"/> with the user, filtered attributes and mode of its registration in the metadata,
    /// where the code doesn't set them.
    /// </summary>
    private static EntryCandidate Registered(EntryCandidate candidate, string? user, IReadOnlyList<string>? filter, bool asynchronous)
    {
        if (user is not null && candidate.ImpersonatingUserId is null) candidate = candidate with { ImpersonatingUserId = user };
        if (filter is not null && candidate.Trigger is { FilteredAttributes: null } trigger) candidate = candidate with { Trigger = trigger with { FilteredAttributes = filter } };
        if (asynchronous && candidate.Trigger is { Asynchronous: false } synchronous) candidate = candidate with { Trigger = synchronous with { Asynchronous = true } };
        return candidate;
    }

    /// <summary>
    /// The plugin steps and custom APIs that operations fire, by message: every one, whatever the scope.
    /// </summary>
    private static IEnumerable<CascadeStep> CascadeSteps(IEnumerable<EntryCandidate> candidates, IEnumerable<CascadeStep> codeless) =>
        candidates
            .Where(c => c.Kind is EntryPointKind.PluginStep or EntryPointKind.CustomApi && c.Trigger?.Message is not null)
            .Select(c => new CascadeStep(c.Id, c.Trigger!.Message!, c.Trigger.Table, c.ImpersonatingUserId, c.Trigger.FilteredAttributes, c, c.Trigger.Asynchronous, c.Kind == EntryPointKind.CustomApi))
            .Concat(codeless.GroupBy(s => s.Id, StringComparer.Ordinal).Select(g => g.First()));

    private void AddMethodEntries(Dictionary<string, EntryCandidate> candidates)
    {
        foreach (var method in _entryMethods)
        {
            var matches = _graph.Nodes.Values
                .Where(n => n.Id == n.Member && (n.Id == method || n.Id.StartsWith(method + "(", StringComparison.Ordinal)))
                .OrderBy(n => n.Id, StringComparer.Ordinal);
            foreach (var node in matches)
            {
                var id = $"method:{node.Id}";
                if (candidates.ContainsKey(id)) continue;
                var type = node.Type is { } typeId ? typeId : "";
                candidates.Add(id, new EntryCandidate(id, EntryPointKind.Method, type, null, null, null, node.Declaration, [new Handler(node.Id, null)]));
            }
        }
    }

    /// <summary>
    /// Method entries named in <see cref="AddEntryMethods"/> that match no method in the solution.
    /// </summary>
    public IReadOnlyList<string> UnmatchedEntryMethods() =>
        _entryMethods
            .Where(m => !_graph.Nodes.Values.Any(n => n.Id == n.Member && (n.Id == m || n.Id.StartsWith(m + "(", StringComparison.Ordinal))))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Who the entry point's code runs as. Plugins and activities run as the calling user, or the step's
    /// impersonating user; functions and other code as an application user.
    /// </summary>
    private static EntryContext Context(EntryCandidate candidate) =>
        candidate.Kind switch
        {
            EntryPointKind.Function => Context(RunsAs.ApplicationUser),

            // Who calls an explicit method isn't known; app code usually runs as its application user
            EntryPointKind.Method => Context(RunsAs.ApplicationUser with { Assumed = true }),

            _ => Context(candidate.ImpersonatingUserId is { } user ? RunsAs.User(user) : RunsAs.Caller, candidate.Trigger?.Table),
        };

    /// <summary>
    /// Code that runs as <paramref name="caller"/>, with injected services assumed to run as it too, for a step on
    /// <paramref name="primaryTable"/>.
    /// </summary>
    private static EntryContext Context(RunsAs caller, string? primaryTable = null) =>
        new(caller, new Identity(caller with { Assumed = true }, InjectedAssumption), primaryTable);

    /// <summary>
    /// Methods with operations that no entry point reaches, each analyzed on its own. Referenced libraries and plugin code
    /// from other solutions are left out.
    /// </summary>
    private List<MethodManifest> Unreached(HashSet<string> reached)
    {
        var evaluator = new Evaluator(_graph, new EntryContext(RunsAs.Caller, new Identity(RunsAs.Caller with { Assumed = true }, InjectedAssumption)));
        return _graph.Nodes.Values
            .Where(n => n.Accesses.Count > 0 && !reached.Contains(n.Id) && _reportedNodes.Contains(n.Id))
            .GroupBy(n => n.Member, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var results = g.OrderBy(n => n.Id, StringComparer.Ordinal).Select(n => evaluator.EvaluateAlone(n, n.Display)).ToList();
                var member = _graph.Nodes.TryGetValue(g.Key, out var memberNode) ? memberNode : null;
                var declaration = member?.Declaration ?? g.Select(n => n.MemberDeclaration).Where(d => d is not null).Min();
                var (requirements, unresolved, assumptions, globalRequirements) = Aggregate(results, RunsAs.ApplicationUser with { Assumed = true });
                return new MethodManifest(g.Key, declaration, requirements, unresolved, assumptions, globalRequirements);
            })
            .ToList();
    }

    private EntryPoint Describe(EntryCandidate candidate, CascadeResult cascade)
    {
        var (requirements, unresolved, assumptions, globalRequirements) = Aggregate([cascade.Result], Context(candidate).Caller, candidate.Kind == EntryPointKind.CustomApi ? candidate : null);
        return Describe(candidate, requirements, unresolved, assumptions, cascade.Fired, globalRequirements);
    }

    /// <summary>
    /// A request without a rule is a custom API call when the solution implements the message or registers steps on
    /// it, or the message has a publisher prefix, as custom API and custom action names do.
    /// </summary>
    private string CustomApi(string reason) =>
        RequestReasons.NoRuleMessage(reason) is { } message && (_customApis.Contains(message) || message.IndexOf('_') > 0)
            ? RequestReasons.CustomApi(message)
            : reason;

    private static EntryPoint Describe(
        EntryCandidate candidate,
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<UnresolvedOperation> unresolved,
        IReadOnlyList<Assumption> assumptions,
        IReadOnlyList<string>? triggers = null,
        IReadOnlyList<GlobalRequirement>? globalRequirements = null) =>
        new(
            candidate.Id,
            candidate.Kind,
            candidate.Type,
            candidate.Assembly,
            candidate.Trigger,
            candidate.ImpersonatingUserId,
            candidate.Declaration,
            requirements,
            unresolved,
            assumptions,
            triggers,
            globalRequirements);

    /// <summary>
    /// Merges the hits of an evaluation: one requirement per table (or privilege not tied to a table), privilege and
    /// identity, with the strongest basis and one source per location (the shortest call path), and one unresolved
    /// operation per location, reason and identity, with the privileges of each. All sorted. A call to a custom API
    /// that the code or the privilege catalog has needs its execute privilege; operations that act for the user in
    /// <c>CallerId</c> need <see cref="GlobalRequirement.ActOnBehalfOfAnotherUser"/> on <paramref name="impersonator"/>,
    /// the identity that sets it. Pass the entry point of a custom API as <paramref name="customApi"/> to add its own
    /// execute privilege, which its caller needs.
    /// </summary>
    private (IReadOnlyList<Requirement>, IReadOnlyList<UnresolvedOperation>, IReadOnlyList<Assumption>, IReadOnlyList<GlobalRequirement>) Aggregate(
        IEnumerable<EvaluationResult> results,
        RunsAs impersonator,
        EntryCandidate? customApi = null)
    {
        var list = results.ToList();
        var truncated = list.Any(r => r.Truncated);

        var requirementHits = list.SelectMany(r => r.Requirements).ToList();
        var unresolvedHits = new List<UnresolvedHit>();
        var globalHits = new List<GlobalHit>();
        foreach (var hit in list.SelectMany(r => r.Unresolved))
        {
            if (RequestReasons.NoRuleMessage(hit.Reason) is { } message && IsKnownCustomApi(message))
            {
                ExecutePrivilegeHits(message, ExecutePrivilegeOf(message), hit.RunsAs, hit.Source, requirementHits, unresolvedHits, globalHits);

                // The code of a custom API runs as a cascade, so only a custom API without code stays unresolved
                if (!_customApiCode.ContainsKey(message)) unresolvedHits.Add(hit with { Reason = RequestReasons.CustomApiCode(message) });
                continue;
            }
            unresolvedHits.Add(hit with { Reason = CustomApi(hit.Reason) });
        }
        foreach (var hit in list.SelectMany(r => r.Globals))
        {
            NamedPrivilegeHits(hit.Operation, hit.Privilege, hit.RunsAs, hit.Basis, hit.Source, requirementHits, unresolvedHits, globalHits);
        }
        if (customApi is { Trigger.Message: { } apiMessage, Declaration: { } declaration })
        {
            ExecutePrivilegeHits(apiMessage, ExecutePrivilegeOf(apiMessage), RunsAs.Caller, new RequirementSource(declaration, []), requirementHits, unresolvedHits, globalHits);
        }
        globalHits.AddRange(requirementHits
            .Where(h => h.RunsAs.Kind == RunsAsKind.ImpersonatedCaller)
            .Select(h => new GlobalHit(GlobalRequirement.ActOnBehalfOfAnotherUser, impersonator, h.Basis, h.Source)));
        globalHits.AddRange(unresolvedHits
            .Where(h => h.RunsAs.Kind == RunsAsKind.ImpersonatedCaller)
            .Select(h => new GlobalHit(GlobalRequirement.ActOnBehalfOfAnotherUser, impersonator, RequirementBasis.Direct, h.Source)));

        var requirements = requirementHits
            .GroupBy(h => (h.Table, h.Privilege, h.RunsAs))
            .Select(g => new Requirement(
                g.Key.Table,
                g.Key.Privilege,
                g.Key.RunsAs,
                g.Min(h => h.Basis),
                FirstPerLocation(g.Select(h => h.Source))))
            .OrderBy(r => r.Table, StringComparer.Ordinal)
            .ThenBy(r => r.Privilege)
            .ThenBy(r => r.RunsAs)
            .ThenBy(r => r.Basis)
            .ToList();

        var globalRequirements = globalHits
            .GroupBy(h => (h.Privilege, h.RunsAs))
            .Select(g => new GlobalRequirement(g.Key.Privilege, g.Key.RunsAs, g.Min(h => h.Basis), FirstPerLocation(g.Select(h => h.Source))))
            .OrderBy(r => r.Privilege, StringComparer.Ordinal)
            .ThenBy(r => r.RunsAs)
            .ThenBy(r => r.Basis)
            .ToList();

        var unresolved = unresolvedHits
            .GroupBy(h => (h.Source.Location, h.Operation, h.Reason, h.RunsAs))
            .Select(g => g.First() with { Privileges = g.SelectMany(h => h.Privileges).Distinct().OrderBy(p => p).ToList() })
            .OrderBy(h => h.Source.Location)
            .ThenBy(h => h.Operation, StringComparer.Ordinal)
            .ThenBy(h => h.Reason, StringComparer.Ordinal)
            .ThenBy(h => string.Join(",", h.Privileges), StringComparer.Ordinal)
            .ThenBy(h => h.RunsAs)
            .Select(h => new UnresolvedOperation(h.Operation, h.Privileges, h.Reason, h.RunsAs, h.Source))
            .ToList();

        var assumptions = list
            .SelectMany(r => r.Assumptions)
            .GroupBy(h => (h.Source.Location, h.Reason))
            .Select(g => g.First())
            .OrderBy(h => h.Source.Location)
            .ThenBy(h => h.Reason, StringComparer.Ordinal)
            .Select(h => new Assumption(h.Reason, h.Source))
            .ToList();

        if (truncated)
        {
            var location = assumptions.FirstOrDefault()?.Source ?? requirements.SelectMany(r => r.Sources).FirstOrDefault();
            if (location is not null)
            {
                assumptions.Insert(0, new Assumption($"analysis stopped after {Evaluator.MaxVisits} calls; requirements may be incomplete", location));
            }
        }

        return (requirements, unresolved, assumptions, globalRequirements);
    }

    /// <summary>
    /// A privilege not tied to a table that an operation needs.
    /// </summary>
    private sealed record GlobalHit(string Privilege, RunsAs RunsAs, RequirementBasis Basis, RequirementSource Source);

    /// <summary>
    /// The code or the privilege catalog has the custom API, so what calling it needs is known.
    /// </summary>
    private bool IsKnownCustomApi(string message) => _customApiCode.ContainsKey(message) || _catalogCustomApis.ContainsKey(message);

    /// <summary>
    /// The execute privilege of a custom API: from its registration in code, else from the privilege catalog.
    /// <c>null</c> when it needs none.
    /// </summary>
    private ExecutePrivilege? ExecutePrivilegeOf(string message) =>
        (_customApiCode.TryGetValue(message, out var code) ? code.ExecutePrivilege : null)
        ?? (_catalogCustomApis.TryGetValue(message, out var api) && !string.IsNullOrEmpty(api.ExecutePrivilegeName) ? new ExecutePrivilege(api.ExecutePrivilegeName) : null);

    /// <summary>
    /// What calling the custom API <paramref name="message"/> needs for <paramref name="privilege"/>: table
    /// privileges, a privilege not tied to a table, or an unresolved operation when the privilege can't be looked up.
    /// A privilege name is only looked up in the privilege catalog, never derived from its spelling.
    /// </summary>
    private void ExecutePrivilegeHits(
        string message,
        ExecutePrivilege? privilege,
        RunsAs runsAs,
        RequirementSource source,
        List<RequirementHit> requirements,
        List<UnresolvedHit> unresolved,
        List<GlobalHit> globals)
    {
        if (privilege is null) return;
        if (privilege is { Table: { } table, Privilege: { } tablePrivilege })
        {
            requirements.Add(new RequirementHit(table, tablePrivilege, runsAs, RequirementBasis.ExecutePrivilege, source));
            return;
        }

        void Unresolved(string reason) => unresolved.Add(new UnresolvedHit(message, [], reason, runsAs, source));
        if (privilege.Name is not { } name) Unresolved(RequestReasons.ExecutePrivilegeComputed(message));
        else if (_privilegeCatalog is null) Unresolved(RequestReasons.PrivilegeCatalogMissing(message, name));
        else if (!_privilegeCatalog.TryGetValue(name, out var definition)) Unresolved(RequestReasons.PrivilegeNotInCatalog(message, name));
        else if (!CatalogHits(definition, runsAs, RequirementBasis.ExecutePrivilege, source, requirements, globals)) Unresolved(RequestReasons.PrivilegeWithoutTablePrivilege(message, name));
    }

    /// <summary>
    /// What <paramref name="operation"/> needs for <paramref name="privilege"/>, a privilege its request rule names.
    /// Without a privilege catalog it is taken as a privilege not tied to a table; with one, it is looked up there,
    /// as it may be tied to tables instead.
    /// </summary>
    private void NamedPrivilegeHits(
        string operation,
        string privilege,
        RunsAs runsAs,
        RequirementBasis basis,
        RequirementSource source,
        List<RequirementHit> requirements,
        List<UnresolvedHit> unresolved,
        List<GlobalHit> globals)
    {
        void Unresolved(string reason) => unresolved.Add(new UnresolvedHit(operation, [], reason, runsAs, source));
        if (_privilegeCatalog is null) globals.Add(new GlobalHit(privilege, runsAs, basis, source));
        else if (!_privilegeCatalog.TryGetValue(privilege, out var definition)) Unresolved(RequestReasons.NamedPrivilegeNotInCatalog(operation, privilege));
        else if (!CatalogHits(definition, runsAs, basis, source, requirements, globals)) Unresolved(RequestReasons.NamedPrivilegeWithoutTablePrivilege(operation, privilege));
    }

    /// <summary>
    /// Adds what <paramref name="definition"/> grants: a privilege not tied to a table, or its table privilege on each
    /// of its tables. <c>false</c> when it is tied to tables but its access right isn't a table privilege, so no role
    /// can be checked for it.
    /// </summary>
    private static bool CatalogHits(
        PrivilegeDefinition definition,
        RunsAs runsAs,
        RequirementBasis basis,
        RequirementSource source,
        List<RequirementHit> requirements,
        List<GlobalHit> globals)
    {
        if (definition.Tables.Count == 0)
        {
            globals.Add(new GlobalHit(definition.Name, runsAs, basis, source));
            return true;
        }
        if (definition.Privilege is not { } granted) return false;
        foreach (var table in definition.Tables)
        {
            requirements.Add(new RequirementHit(table.ToLowerInvariant(), granted, runsAs, basis, source));
        }
        return true;
    }

    /// <summary>
    /// One source per location, the first found; the evaluation is breadth-first, so it has the shortest path.
    /// </summary>
    private static IReadOnlyList<RequirementSource> FirstPerLocation(IEnumerable<RequirementSource> sources)
    {
        var seen = new HashSet<SourceLocation>();
        return sources.Where(s => seen.Add(s.Location)).OrderBy(s => s).ToList();
    }
}
