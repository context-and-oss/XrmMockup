using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class WorkflowLoaderTests
{
    [Fact]
    public void ReadsTheWorkflowsThatRunOnATable()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(directory, WorkflowLoader.WorkflowsFolder));
        try
        {
            var ownerId = Guid.NewGuid();
            Write(directory, "a.xml", Workflow("Notify owner", "account", mode: 1, runAs: 1, ownerId, onCreate: true, updateColumns: "statecode, Name"));
            Write(directory, "b.xml", Workflow("Send welcome", "contact", mode: 0, runAs: 1, ownerId, onCreate: true));
            Write(directory, "c.xml", Workflow("On demand only", "account", mode: 1, runAs: 1, ownerId));
            var action = Workflow("ctx_DoThing", "none", mode: 1, runAs: 1, ownerId, onCreate: true);
            action["category"] = new OptionSetValue(3);
            Write(directory, "d.xml", action);

            Assert.Equal(
                [
                    new WorkflowRegistration("Notify owner", "account", true, false, ["name", "statecode"], true, null),
                    new WorkflowRegistration("Send welcome", "contact", true, false, null, false, ownerId.ToString()),
                ],
                WorkflowLoader.Load(directory),
                new WorkflowComparer());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MetadataWithoutWorkflowsGivesNone()
    {
        Assert.Empty(WorkflowLoader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }

    private static Entity Workflow(string name, string table, int mode, int runAs, Guid ownerId, bool onCreate = false, string? updateColumns = null) =>
        new("workflow", Guid.NewGuid())
        {
            ["name"] = name,
            ["category"] = new OptionSetValue(0),
            ["primaryentity"] = table,
            ["mode"] = new OptionSetValue(mode),
            ["runas"] = new OptionSetValue(runAs),
            ["ownerid"] = new EntityReference("systemuser", ownerId),
            ["triggeroncreate"] = onCreate,
            ["triggerondelete"] = false,
            ["triggeronupdateattributelist"] = updateColumns,
        };

    private static void Write(string directory, string file, Entity workflow)
    {
        using var stream = File.Create(Path.Combine(directory, WorkflowLoader.WorkflowsFolder, file));
        new DataContractSerializer(typeof(Entity)).WriteObject(stream, workflow);
    }

    /// <summary>
    /// Compares the update columns by value, which the record's own equality doesn't.
    /// </summary>
    private sealed class WorkflowComparer : IEqualityComparer<WorkflowRegistration>
    {
        public bool Equals(WorkflowRegistration? x, WorkflowRegistration? y) =>
            x is not null && y is not null
            && x with { UpdateColumns = null } == y with { UpdateColumns = null }
            && (x.UpdateColumns ?? []).SequenceEqual(y.UpdateColumns ?? []);

        public int GetHashCode(WorkflowRegistration obj) => obj.Name.GetHashCode();
    }
}
