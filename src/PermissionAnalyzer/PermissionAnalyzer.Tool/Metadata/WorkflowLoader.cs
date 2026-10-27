using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Reads the workflows in <c>Metadata/Workflows</c>, written by the metadata tool, the same way XrmMockup does. Only
/// the workflows that run automatically are kept, since operations can only fire those.
/// </summary>
internal static class WorkflowLoader
{
    public const string WorkflowsFolder = "Workflows";

    // The category of a workflow, as opposed to an action (3) or a business rule
    private const int WorkflowCategory = 0;

    // The mode of a workflow: 0 is background, 1 real-time
    private const int RealTimeMode = 1;

    // Who a real-time workflow runs as: 0 is its owner, 1 the calling user
    private const int CallingUser = 1;

    /// <summary>
    /// Every workflow in the metadata folder that runs on a table, sorted by name. Empty when the folder has none.
    /// </summary>
    public static IReadOnlyList<WorkflowRegistration> Load(string metadataDirectory)
    {
        var directory = Path.Combine(metadataDirectory, WorkflowsFolder);
        if (!Directory.Exists(directory)) return [];

        var serializer = new DataContractSerializer(typeof(Entity));
        return Directory.GetFiles(directory, "*.xml")
            .Order(StringComparer.Ordinal)
            .Select(file =>
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                return serializer.ReadObject(stream) as Entity
                    ?? throw new ToolException($"Not a workflow: {file}");
            })
            .Select(Convert)
            .OfType<WorkflowRegistration>()
            .OrderBy(w => w.Name, StringComparer.Ordinal)
            .ThenBy(w => w.Table, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The workflow <paramref name="entity"/> describes; <c>null</c> when it isn't a workflow or no operation runs it.
    /// </summary>
    internal static WorkflowRegistration? Convert(Entity entity)
    {
        if (entity.LogicalName != "workflow" || entity.GetAttributeValue<OptionSetValue>("category")?.Value != WorkflowCategory) return null;

        var table = entity.GetAttributeValue<string>("primaryentity");
        if (string.IsNullOrEmpty(table) || table == "none") return null;

        var onCreate = entity.GetAttributeValue<bool?>("triggeroncreate") == true;
        var onDelete = entity.GetAttributeValue<bool?>("triggerondelete") == true;
        var columns = (entity.GetAttributeValue<string>("triggeronupdateattributelist") ?? "")
            .Split(',')
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();
        if (!onCreate && !onDelete && columns.Count == 0) return null;

        // A background workflow always runs as its owner
        var realTime = entity.GetAttributeValue<OptionSetValue>("mode")?.Value == RealTimeMode;
        var asCaller = realTime && entity.GetAttributeValue<OptionSetValue>("runas")?.Value == CallingUser;
        var owner = asCaller ? null : entity.GetAttributeValue<EntityReference>("ownerid")?.Id.ToString();

        return new WorkflowRegistration(
            entity.GetAttributeValue<string>("name") ?? entity.Id.ToString(),
            table,
            onCreate,
            onDelete,
            columns.Count > 0 ? columns : null,
            realTime,
            owner);
    }
}
