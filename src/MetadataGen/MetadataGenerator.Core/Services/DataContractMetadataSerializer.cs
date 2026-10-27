using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using DG.Tools.XrmMockup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using XrmMockup.MetadataGenerator.Core.Models;

namespace XrmMockup.MetadataGenerator.Core.Services;

/// <summary>
/// Serializes metadata using DataContractSerializer for compatibility with existing format.
/// </summary>
internal sealed partial class DataContractMetadataSerializer(
    IOptions<GeneratorOptions> options,
    ILogger<DataContractMetadataSerializer> logger) : IMetadataSerializer
{
    /// <summary>
    /// The file the privilege catalog is written to, next to Metadata.xml.
    /// </summary>
    public const string PrivilegeCatalogFile = "PrivilegeCatalog.xml";

    private readonly GeneratorOptions _options = options.Value;

    public async Task SerializeMetadataAsync(
        MetadataSkeleton skeleton,
        CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Serializing metadata to {Path}", _options.OutputDirectory);
        }

        await Task.Run(() =>
        {
            Directory.CreateDirectory(_options.OutputDirectory);

            var serializer = new DataContractSerializer(typeof(MetadataSkeleton));
            var metadataPath = Path.Combine(_options.OutputDirectory, "Metadata.xml");

            using var stream = new FileStream(metadataPath, FileMode.Create);
            using var writer = CreateXmlWriter(stream);
            serializer.WriteObject(writer, skeleton);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Metadata written to {Path}", metadataPath);
            }
        }, ct);
    }

    public async Task SerializeWorkflowsAsync(
        IEnumerable<Entity> workflows,
        CancellationToken ct = default)
    {
        var workflowsLocation = Path.Combine(_options.OutputDirectory, "Workflows");

        await Task.Run(() =>
        {
            Directory.CreateDirectory(workflowsLocation);

            // Delete old files
            foreach (var file in Directory.EnumerateFiles(workflowsLocation, "*.xml"))
            {
                File.Delete(file);
            }

            var serializer = new DataContractSerializer(typeof(Entity));
            var count = 0;

            foreach (var workflow in workflows)
            {
                var safeName = ToSafeName(workflow.GetAttributeValue<string>("name"));
                var filePath = Path.Combine(workflowsLocation, $"{safeName}.xml");

                using var stream = new FileStream(filePath, FileMode.Create);
                using var writer = CreateXmlWriter(stream);
                serializer.WriteObject(writer, workflow);
                count++;
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Written {Count} workflow files to {Path}", count, workflowsLocation);
            }
        }, ct);
    }

    public async Task SerializeSecurityRolesAsync(
        Dictionary<Guid, SecurityRole> securityRoles,
        CancellationToken ct = default)
    {
        var securityLocation = Path.Combine(_options.OutputDirectory, "SecurityRoles");

        await Task.Run(() =>
        {
            Directory.CreateDirectory(securityLocation);

            // Delete old files
            foreach (var file in Directory.EnumerateFiles(securityLocation, "*.xml"))
            {
                File.Delete(file);
            }

            var serializer = new DataContractSerializer(typeof(SecurityRole));

            foreach (var securityRole in securityRoles)
            {
                var safeName = ToSafeName(securityRole.Value.Name);
                var filePath = Path.Combine(securityLocation, $"{safeName}.xml");

                using var stream = new FileStream(filePath, FileMode.Create);
                using var writer = CreateXmlWriter(stream);
                serializer.WriteObject(writer, securityRole.Value);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Written {Count} security role files to {Path}", securityRoles.Count, securityLocation);
            }
        }, ct);
    }

    public async Task SerializePrivilegeCatalogAsync(
        PrivilegeCatalog catalog,
        CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(_options.OutputDirectory);

            var serializer = new DataContractSerializer(typeof(PrivilegeCatalog));
            var catalogPath = Path.Combine(_options.OutputDirectory, PrivilegeCatalogFile);

            using var stream = new FileStream(catalogPath, FileMode.Create);
            using var writer = CreateXmlWriter(stream);
            serializer.WriteObject(writer, catalog);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Written {Count} privileges to {Path}", catalog.Privileges.Count, catalogPath);
            }
        }, ct);
    }

    public async Task GenerateTypeDeclarationsAsync(
        Dictionary<Guid, SecurityRole> securityRoles,
        CancellationToken ct = default)
    {
        var typedefFile = Path.Combine(_options.OutputDirectory, "TypeDeclarations.cs");

        await Task.Run(() =>
        {
            using var file = new StreamWriter(typedefFile, false);
            file.WriteLine("using System;");
            file.WriteLine();
            file.WriteLine("namespace DG.Tools.XrmMockup {");
            file.WriteLine("\tpublic struct SecurityRoles {");

            foreach (var securityRole in securityRoles.OrderBy(x => x.Value.Name, StringComparer.Ordinal))
            {
                file.WriteLine($"\t\tpublic static Guid {ToSafeName(securityRole.Value.Name)} = new Guid(\"{securityRole.Key}\");");
            }

            file.WriteLine("\t}");
            file.WriteLine("}");

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Generated TypeDeclarations.cs with {Count} security roles", securityRoles.Count);
            }
        }, ct);
    }

    private static string ToSafeName(string str)
    {
        var compressed = NonWordCharRegex().Replace(str, "");

        if (StartsWithNumber(compressed))
        {
            return $"_{compressed}";
        }

        if (string.IsNullOrWhiteSpace(compressed))
        {
            return "_EmptyString";
        }

        return compressed;
    }

    private static bool StartsWithNumber(string str) =>
        str.Length > 0 && str[0] >= '0' && str[0] <= '9';

    private XmlWriter CreateXmlWriter(Stream stream)
    {
        var settings = new XmlWriterSettings
        {
            Indent = _options.PrettyPrint,
            IndentChars = "  "
        };
        return XmlWriter.Create(stream, settings);
    }

    [GeneratedRegex(@"[^\w]")]
    private static partial Regex NonWordCharRegex();
}
