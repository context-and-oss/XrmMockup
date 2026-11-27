using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Tool.Snapshots;
using XrmMockup.PermissionAnalyzer.Tool.Web;
using XrmMockup.PermissionAnalyzer.Workspace.Serialization;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class ColumnCatalogTests
{
    private static EntityMetadata Entity(string name, params AttributeMetadata[] attributes)
    {
        var entity = new EntityMetadata { LogicalName = name, DisplayName = new Label("Account", 1033) };
        typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.Attributes))!.SetValue(entity, attributes);
        return entity;
    }

    internal static void WriteMetadata(string directory, params EntityMetadata[] entities)
    {
        Directory.CreateDirectory(directory);
        using var stream = File.Create(Path.Combine(directory, MetadataFileLoader.MetadataFile));
        new DataContractSerializer(typeof(MetadataSkeleton)).WriteObject(stream, new MetadataSkeleton
        {
            EntityMetadata = new SortedDictionary<string, EntityMetadata>(entities.ToDictionary(e => e.LogicalName, StringComparer.Ordinal), StringComparer.Ordinal)
        });
    }

    private static AnalysisSnapshot Snapshot(IReadOnlyList<TableColumnCatalog>? columns = null) => AnalysisSnapshot.Create(
        new ManifestBuilder("/missing/source"), new AnalysisProvenance("Missing.csproj", [], [], "hash", []), [], [],
        "test", "missing/source", columns);

    [Fact]
    public void ReadsAllAttributeTypesAndDisplayNamesWithoutRetainingSdkMetadata()
    {
        var directory = Directory.CreateTempSubdirectory("column-metadata-");
        try
        {
            WriteMetadata(directory.FullName, Entity("account",
                new StringAttributeMetadata { LogicalName = "name", DisplayName = new Label("Account name", 1033) },
                new LookupAttributeMetadata { LogicalName = "ownerid", Targets = ["systemuser"] },
                new IntegerAttributeMetadata { LogicalName = "numberofemployees" },
                new StringAttributeMetadata { LogicalName = "name", DisplayName = new Label("Duplicate", 1033) }));
            var contents = MetadataFileLoader.Load(directory.FullName);
            var table = Assert.Single(contents.ColumnCatalog);
            Assert.Equal("account", table.Table);
            Assert.Equal("Account", table.DisplayName);
            Assert.Equal(["name", "numberofemployees", "ownerid"], table.Columns.Select(c => c.LogicalName));
            Assert.Equal("Account name", table.Columns[0].DisplayName);
            Assert.Null(table.Columns[1].DisplayName);
            Assert.Equal("ownerid", Assert.Single(contents.Lookups).Column);
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public void ExtraMetadataKeepsMainTablePrecedenceAndIncludesTablesWithNoRecordedColumns()
    {
        var directory = Directory.CreateTempSubdirectory("column-merge-");
        try
        {
            WriteMetadata(directory.FullName, Entity("account", new StringAttributeMetadata { LogicalName = "name" }));
            using (var stream = File.Create(Path.Combine(directory.FullName, "ExtraMetadata.xml")))
                new DataContractSerializer(typeof(MetadataSkeleton)).WriteObject(stream, new MetadataSkeleton {
                    EntityMetadata = new() {
                        ["account"] = Entity("account", new StringAttributeMetadata { LogicalName = "wrong" }),
                        ["contact"] = Entity("contact")
                    }
                });
            var columns = MetadataFileLoader.Load(directory.FullName).ColumnCatalog;
            Assert.Equal(["account", "contact"], columns.Select(c => c.Table));
            Assert.Equal("name", Assert.Single(columns[0].Columns).LogicalName);
            Assert.Empty(columns[1].Columns);
            Assert.Empty(MetadataFileLoader.Load(Path.Combine(directory.FullName, "missing")).ColumnCatalog);
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public void UsesDeterministicLocalizedLabelFallbackAndDoesNotInventCatalogForMissingAttributes()
    {
        var column = new StringAttributeMetadata { LogicalName = "name", DisplayName = new Label() };
        column.DisplayName.LocalizedLabels.Add(new LocalizedLabel("English", 1033));
        column.DisplayName.LocalizedLabels.Add(new LocalizedLabel("Danish", 1030));
        var tables = ColumnCatalog.Convert([new EntityMetadata { LogicalName = "missing" }, Entity("account", column)]);
        Assert.Equal("Danish", Assert.Single(Assert.Single(tables).Columns).DisplayName);
    }

    [Fact]
    public async Task ColumnCatalogRoundTripsDeterministicallyAndRejectsOtherSchemas()
    {
        var directory = Directory.CreateTempSubdirectory("column-snapshot-");
        try
        {
            TableColumnCatalog[] columns = [new("contact", null, []), new("account", "Accounts", [new("ownerid", null), new("name", "Name")])];
            var snapshot = Snapshot(columns);
            var a = Path.Combine(directory.FullName, "a.json"); var b = Path.Combine(directory.FullName, "b.json");
            await snapshot.SaveAsync(a, CancellationToken.None);
            await Snapshot(columns.Reverse().ToArray()).SaveAsync(b, CancellationToken.None);
            Assert.Equal(await File.ReadAllTextAsync(a), await File.ReadAllTextAsync(b));
            Assert.DoesNotContain('\r', await File.ReadAllTextAsync(a));
            var loaded = AnalysisSnapshot.Read(a);
            Assert.Equal(1, loaded.SchemaVersion);
            Assert.Equal(["account", "contact"], loaded.ColumnCatalog!.Select(t => t.Table));
            Assert.Equal("Name", loaded.ColumnCatalog![0].Columns[0].DisplayName);
            var unsupported = JsonNode.Parse(await File.ReadAllTextAsync(a))!;
            unsupported["schemaVersion"] = 999;
            await File.WriteAllTextAsync(b, unsupported.ToJsonString());
            Assert.Contains("Unsupported permission snapshot schema", Assert.Throws<ToolException>(() => AnalysisSnapshot.Read(b)).Message);
        }
        finally { directory.Delete(true); }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("duplicate-table")]
    [InlineData("duplicate-column")]
    [InlineData("empty-name")]
    public async Task RejectsMalformedColumnCatalogs(string kind)
    {
        var path = Path.GetTempFileName();
        try
        {
            await Snapshot([new("account", null, [new("name", null)])]).SaveAsync(path, CancellationToken.None);
            var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
            if (kind == "missing") node.AsObject().Remove("columnCatalog");
            else if (kind == "null") node["columnCatalog"] = null;
            else if (kind == "duplicate-table") node["columnCatalog"]!.AsArray().Add(node["columnCatalog"]![0]!.DeepClone());
            else if (kind == "duplicate-column") node["columnCatalog"]![0]!["columns"]!.AsArray().Add(node["columnCatalog"]![0]!["columns"]![0]!.DeepClone());
            else node["columnCatalog"]![0]!["columns"]![0]!["logicalName"] = "";
            await File.WriteAllTextAsync(path, node.ToJsonString());
            Assert.Throws<ToolException>(() => AnalysisSnapshot.Read(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task SnapshotAndExternalMetadataSupplyExplorerColumnsWithoutReplacingFrozenAnalysisInputs()
    {
        var directory = Directory.CreateTempSubdirectory("column-replay-");
        try
        {
            var source = new ManifestBuilder("/missing/source", lookups: [new LookupColumn("account", "oldlookup", ["contact"])]);
            var artifact = AnalysisSnapshot.Create(source, new AnalysisProvenance("Missing.csproj", [], [], "hash", []), [], [], "test", "missing",
                [new("account", "Old account", [new("oldlookup", null)]), new("contact", null, [new("fullname", null)])]);
            var path = Path.Combine(directory.FullName, "snapshot.json"); await artifact.SaveAsync(path, CancellationToken.None);
            var loaded = AnalysisSnapshot.Read(path);
            WriteMetadata(directory.FullName, Entity("account", new StringAttributeMetadata { LogicalName = "newcolumn", DisplayName = new Label("New column", 1033) }), Entity("newtable"));
            var columns = AnalysisRunner.LoadExplorerColumns(loaded, directory.FullName);
            Assert.Equal(["account", "contact", "newtable"], columns.Select(t => t.Table));
            Assert.Equal("newcolumn", Assert.Single(columns[0].Columns).LogicalName);
            Assert.Equal("oldlookup", Assert.Single(loaded.Graph.Lookups).Column);
            Assert.Empty(Assert.Single(ColumnCatalog.Merge(loaded.ColumnCatalog,
                [new("account", null, [])]), t => t.Table == "account").Columns);
            Assert.Equal("fullname", Assert.Single(columns[1].Columns).LogicalName);
            var model = new SecurityModel("/missing/source", loaded.Graph, loaded.Diagnostics, loaded.RoleDefinitions(), new PermissionsConfiguration(), null, loaded.Inputs, columnCatalog: columns);
            var catalog = JsonSerializer.SerializeToElement(model.Catalog(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.Equal("newcolumn", catalog.GetProperty("columnCatalog")[0].GetProperty("columns")[0].GetProperty("logicalName").GetString());
            Assert.Contains(catalog.GetProperty("tables").EnumerateArray(), t => t.GetString() == "newtable");
            var baselineModel = new SecurityModel("/missing/source", loaded.Graph, loaded.Diagnostics, loaded.RoleDefinitions(),
                new PermissionsConfiguration(), null, loaded.Inputs, columnCatalog: loaded.ColumnCatalog);
            var query = new SecurityQuery("operation", "account", "Update", ["oldlookup"], NoRoles: true);
            Assert.Equal(baselineModel.Query(query).GetRawText(), model.Query(query).GetRawText());
            // A snapshot captured without entity metadata can be supplemented without loading source.
            Assert.Equal(columns.Count - 1, AnalysisRunner.LoadExplorerColumns(loaded with { ColumnCatalog = [] }, directory.FullName).Count);
        }
        finally { directory.Delete(true); }
    }
}
