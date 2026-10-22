using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Analysis;

/// <summary>
/// Limits an analysis to some entry points: <c>--plugin</c>, <c>--custom-api</c>, <c>--function</c> or
/// <c>--operation</c>.
/// Explicit methods (<c>--entry</c>) are added with <see cref="ManifestBuilder.AddEntryMethods"/> instead.
/// </summary>
public sealed class EntryPointSelector
{
    private readonly Func<EntryPoint, bool> _matches;
    private readonly string _text;

    private EntryPointSelector(string text, Func<EntryPoint, bool> matches)
    {
        _text = text;
        _matches = matches;
    }

    /// <summary>
    /// A plugin by simple or full type name, optionally narrowed to a message and stage:
    /// <c>AccountPostPlugin</c>, <c>AccountPostPlugin:Update</c> or <c>Contoso.AccountPostPlugin:Update:PostOperation</c>.
    /// </summary>
    public static EntryPointSelector Plugin(string selector)
    {
        var parts = selector.Split(':');
        var type = parts[0];
        var message = parts.Length > 1 ? parts[1] : null;
        var stage = parts.Length > 2 ? parts[2] : null;
        return new EntryPointSelector($"--plugin {selector}", e =>
            e.Kind is EntryPointKind.PluginStep or EntryPointKind.Plugin
            && TypeMatches(e.Type, type)
            && (message is null || Same(e.Trigger?.Message, message))
            && (stage is null || Same(e.Trigger?.Stage, stage)));
    }

    public static EntryPointSelector CustomApi(string name) =>
        new($"--custom-api {name}", e => e.Kind == EntryPointKind.CustomApi && Same(e.Trigger?.Message, name));

    public static EntryPointSelector Function(string name) =>
        new($"--function {name}", e => e.Kind == EntryPointKind.Function && Same(e.Id.Substring("function:".Length), name));

    /// <summary>
    /// Methods added with <see cref="ManifestBuilder.AddEntryMethods"/>, by the same names.
    /// </summary>
    public static EntryPointSelector Methods(IReadOnlyList<string> methods) =>
        new($"--entry {string.Join(" --entry ", methods)}", e =>
            e.Kind == EntryPointKind.Method
            && methods.Any(m => e.Id == "method:" + m || e.Id.StartsWith("method:" + m + "(", StringComparison.Ordinal)));

    /// <summary>
    /// Operations added with <see cref="ManifestBuilder.AddOperation"/>.
    /// </summary>
    public static EntryPointSelector Operation(string text) =>
        new($"--operation {text}", e => e.Kind == EntryPointKind.Operation);

    public bool Matches(EntryPoint entryPoint) => _matches(entryPoint);

    public override string ToString() => _text;

    private static bool TypeMatches(string fullName, string name)
    {
        if (Same(fullName, name)) return true;
        var simple = fullName.Substring(Math.Max(fullName.LastIndexOf('.'), fullName.LastIndexOf('+')) + 1);
        return Same(simple, name);
    }

    private static bool Same(string? x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
}
