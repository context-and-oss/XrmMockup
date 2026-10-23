using System.Runtime.Serialization;
using DG.Tools.XrmMockup;
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
            using (var stream = File.Create(Path.Combine(directory, PluginStepLoader.MetadataFile)))
            {
                new DataContractSerializer(typeof(MetadataSkeleton)).WriteObject(stream, skeleton);
            }

            var steps = PluginStepLoader.Load(directory);

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
    public void MetadataWithoutStepsGivesNone()
    {
        Assert.Empty(PluginStepLoader.Load(RoleLoaderTestsDirectory()));
        Assert.Empty(PluginStepLoader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }

    private static string RoleLoaderTestsDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "tests", "XrmMockup365Test", "Metadata"));
}
