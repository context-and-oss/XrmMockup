using DG.Tools;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace DG.Tools.XrmMockup.Metadata {
    class Program {

        public static ArgumentParser ParsedArgs;

        static List<string> listOfAssemblies = new List<string>();

        private static Assembly ResolveXrmAssemblies(object sender, ResolveEventArgs args) {
            if (listOfAssemblies.Contains(args.Name)) {
                return null;
            }
            try {
                listOfAssemblies.Add(args.Name);
                return AssemblyGetter.GetAssemblyFromName(args.Name);
            }
            finally {
                listOfAssemblies.Remove(args.Name);
            }
        }

        static void Main(string[] args)
        {
            ParsedArgs = new ArgumentParser(Arguments.ArgList, args);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveXrmAssemblies;
            GenerateMetadata();
        }

        static void GenerateMetadata() {
            var auth = new AuthHelper(
                ParsedArgs[Arguments.Url],
                ParsedArgs[Arguments.Username],
                ParsedArgs[Arguments.Password],
                ParsedArgs[Arguments.AuthProvider],
                ParsedArgs[Arguments.Domain],
                ParsedArgs[Arguments.Method],
                ParsedArgs[Arguments.ClientId],
                ParsedArgs[Arguments.ReturnUrl],
                ParsedArgs[Arguments.ClientSecret],
                ParsedArgs[Arguments.ConnectionString]
            );

            Console.WriteLine("*** WARNING: MetadataGenerator365 is DEPRECATED and will no longer be maintained or updated. Please use XrmMockup.MetadataGenerator tool instead. ***");
            Console.WriteLine("*** Follow the installation instructions at https://www.nuget.org/packages/XrmMockup.MetadataGenerator/ ***");

            Console.WriteLine("Generation of metadata files started");
            var generator = new DataHelper(auth.Authenticate(), ParsedArgs[Arguments.Entities], ParsedArgs[Arguments.Solutions], ParsedArgs.GetAsType<bool>(Arguments.fetchFromAssemblies));
            var outputLocation = ParsedArgs[Arguments.OutDir] ?? Directory.GetCurrentDirectory();

            var skeleton = generator.GetMetadata();

            var serializer = new DataContractSerializer(typeof(MetadataSkeleton));
            var workflowSerializer = new DataContractSerializer(typeof(Entity));
            var securitySerializer = new DataContractSerializer(typeof(SecurityRole));

            var workflowsLocation = Path.Combine(outputLocation, "Workflows");
            var securityLocation = Path.Combine(outputLocation, "SecurityRoles");

            Console.WriteLine("Deleting old files");

            Directory.CreateDirectory(workflowsLocation);
            foreach (var file in Directory.EnumerateFiles(workflowsLocation, "*.xml")) {
                File.Delete(Path.Combine(workflowsLocation, file));
            }

            Directory.CreateDirectory(securityLocation);
            foreach (var file in Directory.EnumerateFiles(securityLocation, "*.xml")) {
                File.Delete(Path.Combine(securityLocation, file));
            }

            Console.WriteLine("Writing files");

            Directory.CreateDirectory(outputLocation);
            using (var stream = new FileStream(outputLocation + "/Metadata.xml", FileMode.Create)) {
                serializer.WriteObject(stream, skeleton);
            }

            foreach (var workflow in generator.GetWorkflows()) {
                var safeName = ToSafeName(workflow.GetAttributeValue<string>("name"));
                using (var stream = new FileStream($"{workflowsLocation}/{safeName}.xml", FileMode.Create)) {
                    workflowSerializer.WriteObject(stream, workflow);
                }
            }

            var mitigateDuplicateSecurityRoles = ParsedArgs[Arguments.MitigateDuplicateSecurityRoles] != null;
            var securityRoles = generator.GetSecurityRoles(skeleton.RootBusinessUnit.Id, mitigateDuplicateSecurityRoles);

            foreach (var securityRole in securityRoles) {
                var safeName = ToSafeName(securityRole.Value.Name);
                using (var stream = new FileStream($"{securityLocation}/{safeName}.xml", FileMode.Create)) {
                    securitySerializer.WriteObject(stream, securityRole.Value);
                }
            }

            // Write to TypeDeclarations file
            var typedefFile = Path.Combine(outputLocation, "TypeDeclarations.cs");

            using (var file = new StreamWriter(typedefFile, false)) {
                file.WriteLine("using System;");
                file.WriteLine("");
                file.WriteLine("namespace DG.Tools.XrmMockup {");
                file.WriteLine("\tpublic struct SecurityRoles {");
                foreach (var securityRole in securityRoles.OrderBy(x => x.Value.Name)) {
                    file.WriteLine($"\t\tpublic static Guid {ToSafeName(securityRole.Value.Name)} = new Guid(\"{securityRole.Key}\");");
                }
                file.WriteLine("\t}");
                file.WriteLine("}");
            }

            Console.WriteLine("XrmMockup Metadata was updated successfully");
        }

        private static bool StartsWithNumber(string str) {
            return str.Length > 0 && str[0] >= '0' && str[0] <= '9';
        }

        private static string ToSafeName(string str) {
            var compressed = Regex.Replace(str, @"[^\w]", "");
            if (StartsWithNumber(compressed)) {
                return $"_{compressed}";
            }
            if (String.IsNullOrWhiteSpace(compressed)) {
                return "_EmptyString";
            }
            return compressed;
        }
    }
}
