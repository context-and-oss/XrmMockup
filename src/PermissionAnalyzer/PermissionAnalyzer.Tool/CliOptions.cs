namespace XrmMockup.PermissionAnalyzer.Tool;

/// <summary>
/// CLI option definitions for consistency.
/// </summary>
internal static class CliOptions
{
    internal static class Solution
    {
        public const string Primary = "--solution";
        public const string Alias = "-s";
        public const string Description = "Solution (.sln or .slnx) to analyze. Default: XrmMockup:Permissions:Solution, else the single solution in the current directory";
    }

    internal static class Project
    {
        public const string Primary = "--project";
        public const string Alias = "-p";
        public const string Description = "Project (.csproj) to analyze instead of a solution, with the projects it references";
    }

    internal static class Metadata
    {
        public const string Primary = "--metadata";
        public const string Alias = "-m";
        public const string Description = "XrmMockup metadata folder. Default: XrmMockup:Permissions:MetadataDirectory, else XrmMockup:Metadata:OutputDirectory";
    }

    internal static class Exclude
    {
        public const string Primary = "--exclude";
        public const string Alias = "-x";
        public const string Description = "Leave out projects matching this pattern, e.g. \"*.Tests\" or \"tests/*\". Repeatable; adds to XrmMockup:Permissions:ExcludeProjects";
    }

    internal static class Config
    {
        public const string Primary = "--config";
        public const string Alias = "-c";
        public const string Description = "Path to appsettings.json configuration file";
    }

    internal static class Format
    {
        public const string Primary = "--format";
        public const string Alias = "-f";
        public const string Description = "Output format: text (default) or json";
        public static readonly string[] AllowedValues = ["text", "json"];
    }

    internal static class Output
    {
        public const string Primary = "--output";
        public const string Alias = "-o";
        public const string Description = "Write the report to this file instead of standard output";
    }

    internal static class Plugin
    {
        public const string Primary = "--plugin";
        public const string Description = "Only analyze one plugin, or some of its steps: <type>[:<message>[:<stage>]], e.g. AccountPostPlugin:Update";
    }

    internal static class CustomApi
    {
        public const string Primary = "--custom-api";
        public const string Description = "Only analyze one custom API, by name";
    }

    internal static class Function
    {
        public const string Primary = "--function";
        public const string Description = "Only analyze one Azure Function, by function name";
    }

    internal static class Entry
    {
        public const string Primary = "--entry";
        public const string Description = "Only analyze this method, e.g. Contoso.Sync.Importer.Run, as an entry point. Repeatable";
    }

    internal static class Operation
    {
        public const string Primary = "--operation";
        public const string Description = "Only analyze a user operation on a table and the plugin steps it fires: \"<message> <table>\", e.g. \"Update account\"";
    }

    internal static class Attributes
    {
        public const string Primary = "--attributes";
        public const string Description = "The columns the --operation sets, which decide the Update steps filtered on columns it fires. Comma-separated or repeatable";
    }

    internal static class Role
    {
        public const string Primary = "--role";
        public const string Alias = "-r";
        public const string Description = "Security role of the caller and the application user, by name. Repeatable; roles combine. Replaces XrmMockup:Permissions:Principals";
    }

    internal static class CallerRole
    {
        public const string Primary = "--caller-role";
        public const string Description = "Security role of the user that app code acts for by setting CallerId, by name. Repeatable";
    }

    internal static class Strict
    {
        public const string Primary = "--strict";
        public const string Description = "Also fail on unresolved operations, identities without roles and privileges that can't be checked";
    }
}
