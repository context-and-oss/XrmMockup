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
        public const string Description = "Solution (.sln or .slnx) or project (.csproj) to analyze. A project is analyzed with the projects it references. Default: XrmMockup:Permissions:Solution, else the single solution in the current directory";
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

    internal static class PluginSolution
    {
        public const string Primary = "--plugin-solution";
        public const string Description = "A solution or project with plugin code that the analyzed code fires but doesn't include. Used for plugin cascades only. Repeatable; adds to XrmMockup:Permissions:PluginSolutions";
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
        public const string Description = "Output format: text (default), json, markdown (for a pull request comment or job summary) or sarif (for code scanning)";
        public static readonly string[] AllowedValues = ["text", "json", "markdown", "sarif"];
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

    internal static class Verbosity
    {
        public const string Primary = "--verbosity";
        public const string Alias = "-v";
        public const string Description = "How much of the text report to show: minimal (default) shows only the privileges to change per role, compact adds the entry points that need them, normal shows what needs attention with its sources, detailed adds granted requirements and assumptions, full adds every source and call path. JSON is always complete";
        public static readonly string[] AllowedValues = ["minimal", "compact", "normal", "detailed", "full"];
    }

    internal static class Table
    {
        public const string Primary = "--table";
        public const string Description = "Only report requirements on these tables. Comma-separated or repeatable";
    }

    internal static class Privilege
    {
        public const string Primary = "--privilege";
        public const string Description = "Only report these privileges, such as Write or prvBulkDelete. Comma-separated or repeatable";
    }

    internal static class RunsAs
    {
        public const string Primary = "--runs-as";
        public const string Description = "Only report requirements of these identities: Caller, User, ApplicationUser, ImpersonatedCaller or System. Comma-separated or repeatable";
    }

    internal static class Basis
    {
        public const string Primary = "--basis";
        public const string Description = "Only report requirements with these bases: direct, execute-privilege, all-implementations, all-lookup-targets or filter-unknown. Comma-separated or repeatable";
    }

    internal static class Strict
    {
        public const string Primary = "--strict";
        public const string Description = "Also fail on unresolved operations, identities without roles and privileges that can't be checked";
    }
}
