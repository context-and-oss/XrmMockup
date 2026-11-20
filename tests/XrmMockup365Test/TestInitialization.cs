using System;
using System.IO;
using System.Linq;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestInitialization
    {
        // Regression for #138: initialization threw a generic FaultException ("a record already
        // exists with that Id") when the Workflows folder contained multiple XML files referring to
        // the same workflow id — typically leftover files from older metadata generations whose
        // tooling did not clear the folder before writing renamed workflows back to disk. The fix
        // surfaces the duplicate as a MockupException at load time so the user gets actionable
        // guidance instead of a confusing database error.
        [Fact]
        public void InitializationThrowsMockupExceptionOnDuplicateWorkflowIds()
        {
            var sourceMetadata = ResolveSourceMetadataPath();
            var tempMetadata = Path.Combine(Path.GetTempPath(), "XrmMockupDupeWorkflowTest-" + Guid.NewGuid().ToString("N"));

            try
            {
                CopyDirectory(sourceMetadata, tempMetadata);

                var workflowsDir = Path.Combine(tempMetadata, "Workflows");
                var workflowFiles = Directory.GetFiles(workflowsDir, "*.xml");
                Assert.NotEmpty(workflowFiles);

                // Drop a copy of an existing workflow XML under a new filename — same workflow id,
                // different file. This is the shape produced by older metadata generators that did
                // not clear the Workflows folder before writing renamed workflows back to disk.
                var duplicatePath = Path.Combine(workflowsDir, "DuplicateOfFirstWorkflow.xml");
                File.Copy(workflowFiles[0], duplicatePath);

                var settings = new XrmMockupSettings
                {
                    MetadataDirectoryPath = tempMetadata,
                    IncludeAllWorkflows = false,
                    BasePluginTypes = Array.Empty<Type>(),
                    CodeActivityInstanceTypes = Array.Empty<Type>()
                };

                var ex = Assert.Throws<MockupException>(() => XrmMockup365.GetInstance(settings));

                // Message must surface the actual conflicting files and tell the user how to fix it.
                Assert.Contains("Duplicate workflow ids", ex.Message);
                Assert.Contains(Path.GetFileName(workflowFiles[0]), ex.Message);
                Assert.Contains("DuplicateOfFirstWorkflow.xml", ex.Message);
                Assert.Contains("re-generate metadata", ex.Message);
            }
            finally
            {
                if (Directory.Exists(tempMetadata))
                {
                    Directory.Delete(tempMetadata, recursive: true);
                }
            }
        }

        private static string ResolveSourceMetadataPath()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                Path.Combine(currentDir, "Metadata"),
                Path.Combine(currentDir, "..", "..", "..", "Metadata"),
            };

            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (Directory.Exists(full)) return full;
            }

            throw new DirectoryNotFoundException("Could not locate the test Metadata directory.");
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            }
            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }
}
