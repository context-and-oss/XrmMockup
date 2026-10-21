using Microsoft.CodeAnalysis;
using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Analysis;

/// <summary>
/// Collects the data operations of one or more compilations into a <see cref="Manifest"/>. A project that
/// targets several frameworks is added once per target; its duplicate findings merge into one.
/// </summary>
public sealed class ManifestBuilder(string rootDirectory)
{
    private readonly SourcePaths _paths = new(rootDirectory);
    private readonly Dictionary<string, MethodAccumulator> _methods = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds the operations in <paramref name="compilation"/>. Returns <c>false</c> when the compilation doesn't
    /// reference the Dataverse SDK, so it can't contain any.
    /// </summary>
    public bool Add(Compilation compilation, CancellationToken cancellationToken = default)
    {
        if (XrmSymbols.Create(compilation) is not { } symbols) return false;

        foreach (var access in new OperationExtractor(symbols, _paths).Extract(compilation, cancellationToken))
        {
            if (!_methods.TryGetValue(access.MethodId, out var method))
            {
                method = new MethodAccumulator();
                _methods.Add(access.MethodId, method);
            }
            method.Add(access);
        }
        return true;
    }

    /// <summary>
    /// Builds the manifest. Pass the tables that at least one security role mentions to fill
    /// <see cref="Manifest.NotRoleGoverned"/>; leave it <c>null</c> when no roles are known.
    /// </summary>
    public Manifest Build(IEnumerable<string>? roleGovernedTables = null)
    {
        var methods = _methods
            .OrderBy(m => m.Key, StringComparer.Ordinal)
            .Select(m => m.Value.Build(m.Key))
            .ToList();

        var manifest = new Manifest(methods);
        if (roleGovernedTables is null) return manifest;

        var governed = new HashSet<string>(roleGovernedTables.Select(t => t.ToLowerInvariant()), StringComparer.Ordinal);
        return new Manifest(methods, manifest.Tables.Where(t => !governed.Contains(t)).ToList());
    }

    private sealed class MethodAccumulator
    {
        // Ordinal, so the order doesn't depend on the machine's culture
        private static readonly IComparer<(string Table, Privilege Privilege)> RequirementOrder = Comparer<(string Table, Privilege Privilege)>.Create((x, y) =>
        {
            var result = string.CompareOrdinal(x.Table, y.Table);
            return result != 0 ? result : x.Privilege.CompareTo(y.Privilege);
        });

        private readonly SortedDictionary<(string Table, Privilege Privilege), SortedSet<SourceLocation>> _requirements = new(RequirementOrder);
        private readonly Dictionary<(SourceLocation, string, string, string), UnresolvedOperation> _unresolved = new();
        private SourceLocation? _declaration;

        public void Add(DataAccess access)
        {
            // The same method compiled for two target frameworks has the same declaration; keep the first in
            // source order so the result doesn't depend on project load order.
            if (access.Declaration is { } declaration && (_declaration is null || declaration.CompareTo(_declaration) < 0))
            {
                _declaration = declaration;
            }

            if (access.Table is { } table)
            {
                foreach (var privilege in access.Privileges)
                {
                    if (!_requirements.TryGetValue((table, privilege), out var sources))
                    {
                        sources = new SortedSet<SourceLocation>();
                        _requirements.Add((table, privilege), sources);
                    }
                    sources.Add(access.Source);
                }
                return;
            }

            var privileges = access.Privileges.Distinct().OrderBy(p => p).ToList();
            var key = (access.Source, access.Operation, access.Reason ?? "", string.Join(",", privileges));
            if (!_unresolved.ContainsKey(key))
            {
                _unresolved.Add(key, new UnresolvedOperation(access.Operation, privileges, access.Reason ?? "", access.Source));
            }
        }

        public MethodManifest Build(string id) => new(
            id,
            _declaration,
            _requirements.Select(r => new Requirement(r.Key.Table, r.Key.Privilege, RequirementBasis.Direct, r.Value.ToList())).ToList(),
            _unresolved
                .OrderBy(u => u.Key.Item1)
                .ThenBy(u => u.Key.Item2, StringComparer.Ordinal)
                .ThenBy(u => u.Key.Item3, StringComparer.Ordinal)
                .ThenBy(u => u.Key.Item4, StringComparer.Ordinal)
                .Select(u => u.Value)
                .ToList());
    }
}
