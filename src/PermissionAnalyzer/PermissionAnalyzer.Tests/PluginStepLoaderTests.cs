using System.Runtime.Serialization;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class PluginStepLoaderTests
{
    [Fact]
    public void ReadsPluginStepsFromMetadataFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var userId = Guid.NewGuid();
            var skeleton = new MetadataSkeleton
            {
                Plugins =
                [
                    new MetaPlugin { AssemblyName = "Contoso.B", PluginTypeAssemblyName = "Contoso", MessageName = "Update", PrimaryEntity = "account", Stage = 40, ImpersonatingUserId = userId },
                    new MetaPlugin { AssemblyName = "Contoso.A", MessageName = "Create", PrimaryEntity = "none", Stage = 20, Mode = 1 },
                ],
            };
            using (var stream = File.Create(Path.Combine(directory, MetadataFileLoader.MetadataFile)))
            {
                new DataContractSerializer(typeof(MetadataSkeleton)).WriteObject(stream, skeleton);
            }

            var steps = MetadataFileLoader.Load(directory).PluginSteps;

            Assert.Equal(
                [
                    new PluginStepRegistration("Contoso.A", null, "Create", null, 20, null, Asynchronous: true),
                    new PluginStepRegistration("Contoso.B", "Contoso", "Update", "account", 40, userId.ToString()),
                ],
                steps);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ReadsLookupsAndStepsInOnePassAndStepsOnlyFromTheMainFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            Write(Path.Combine(directory, MetadataFileLoader.MetadataFile), Skeleton("contact", "parentcustomerid", "Contoso.Main"));
            Write(Path.Combine(directory, "Extra" + MetadataFileLoader.MetadataFile), Skeleton("ctx_order", "ctx_customerid", "Contoso.Extra"));

            var contents = MetadataFileLoader.Load(directory);

            Assert.Equal(["contact.parentcustomerid", "ctx_order.ctx_customerid"], contents.Lookups.Select(c => $"{c.Table}.{c.Column}"));
            Assert.Equal(["Contoso.Main"], contents.PluginSteps.Select(s => s.TypeName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static MetadataSkeleton Skeleton(string table, string lookup, string plugin)
    {
        var entity = new EntityMetadata { LogicalName = table };
        typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.Attributes))!.SetValue(entity, new AttributeMetadata[]
        {
            new LookupAttributeMetadata { LogicalName = lookup, Targets = ["account"] },
        });
        return new MetadataSkeleton
        {
            EntityMetadata = new() { [table] = entity },
            Plugins = [new MetaPlugin { AssemblyName = plugin, MessageName = "Create", PrimaryEntity = table, Stage = 20 }],
        };
    }

    private static void Write(string file, MetadataSkeleton skeleton)
    {
        using var stream = File.Create(file);
        new DataContractSerializer(typeof(MetadataSkeleton)).WriteObject(stream, skeleton);
    }

    [Fact]
    public void MetadataWithoutStepsGivesNone()
    {
        Assert.Empty(MetadataFileLoader.Load(RoleLoaderTestsDirectory()).PluginSteps);
        Assert.Empty(MetadataFileLoader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).PluginSteps);
    }

    private static string RoleLoaderTestsDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "tests", "XrmMockup365Test", "Metadata"));
}
