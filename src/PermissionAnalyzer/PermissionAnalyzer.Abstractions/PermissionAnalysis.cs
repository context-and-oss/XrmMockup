using System.Reflection;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer;

/// <summary>Reusable static source analysis. Checks do not execute code or prove record-level access.</summary>
public abstract class PermissionAnalysis
{
    /// <summary>
    /// Loads source once. Enable XrmMockupEnablePermissionAnalysis in a .NET 10 test project.
    /// The shared SDK libraries ship inside Core; no tool installation or extra NuGet package is needed.
    /// </summary>
    public static Task<PermissionAnalysis> LoadAsync(string projectOrSolutionPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectOrSolutionPath)) throw new ArgumentException("A project or solution path is required.", nameof(projectOrSolutionPath));
        var loader = Type.GetType("DG.Tools.XrmMockup.PermissionAnalysisBootstrap, XrmMockup365", throwOnError: false)
            ?? Type.GetType("XrmMockup.PermissionAnalyzer.Workspace.PermissionAnalysisLoader, XrmMockup.PermissionAnalyzer.Workspace", throwOnError: false)
            ?? throw new InvalidOperationException("Enable XrmMockupEnablePermissionAnalysis in a .NET 10 Core test project to load permission analysis. Workspace loading is not supported on .NET Framework.");
        try
        {
            return (Task<PermissionAnalysis>)loader.GetMethod("LoadAsync", new[] { typeof(string), typeof(CancellationToken) })!
                .Invoke(null, new object[] { projectOrSolutionPath, cancellationToken })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <summary>Builds a fresh manifest against the current mockup metadata, reusing the loaded source graph.</summary>
    public abstract Manifest Analyze(PermissionScope scope, PermissionMetadata metadata);
}

/// <summary>Metadata supplied by the mockup at check time; no role coverage is cached.</summary>
public sealed class PermissionMetadata
{
    public IReadOnlyList<PluginStepRegistration> PluginSteps { get; set; } = [];
    public IReadOnlyList<LookupColumn> Lookups { get; set; } = [];
    public IReadOnlyList<WorkflowRegistration>? Workflows { get; set; }
    public IReadOnlyList<string> RoleGovernedTables { get; set; } = [];
}

/// <summary>A typed plugin identity or a synthetic entity operation. Construct via PermissionSelector in Core.</summary>
public sealed class PermissionScope
{
    private PermissionScope(string? pluginType, string? assembly, string? message, string? stage, string? table, IReadOnlyList<string>? columns)
    {
        PluginType = pluginType; Assembly = assembly; Message = message; Stage = stage; Table = table; Columns = columns;
    }
    public string? PluginType { get; }
    public string? Assembly { get; }
    public string? Message { get; }
    public string? Stage { get; }
    public string? Table { get; }
    public IReadOnlyList<string>? Columns { get; }

    public static PermissionScope ForPlugin(string fullTypeName, string assemblyName, string? message = null, string? stage = null)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName) || string.IsNullOrWhiteSpace(assemblyName)) throw new ArgumentException("A full plugin type and assembly identity are required.");
        if (message is not null && string.IsNullOrWhiteSpace(message)) throw new ArgumentException("A message cannot be blank.", nameof(message));
        return new PermissionScope(fullTypeName, assemblyName, message?.Trim(), stage, null, null);
    }

    public static PermissionScope ForOperation(string message, string table, IReadOnlyList<string>? columns = null)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(table)) throw new ArgumentException("An operation message and entity logical name are required.");
        if (columns?.Any(string.IsNullOrWhiteSpace) == true) throw new ArgumentException("Attribute names cannot be blank.", nameof(columns));
        return new PermissionScope(null, null, message.Trim(), null, table.Trim().ToLowerInvariant(), columns is null ? null : Array.AsReadOnly(columns.Select(c => c.Trim().ToLowerInvariant()).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToArray()));
    }
    public override string ToString() => PluginType is null ? $"{Message} {Table}" : $"{PluginType}, {Assembly} ({Message ?? "all messages"}, {Stage ?? "all stages"})";
}
