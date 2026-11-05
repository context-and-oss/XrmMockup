namespace XrmMockup.MetadataGenerator.Tool;

/// <summary>
/// CLI option definitions for consistency.
/// </summary>
internal static class CliOptions
{
    internal static class Output
    {
        public const string Primary = "--output";
        public const string Alias = "-o";
        public const string Description = "Output directory for generated metadata files";
    }

    internal static class Solutions
    {
        public const string Primary = "--solutions";
        public const string Alias = "-s";
        public const string Description = "Comma-separated list of solution unique names";
    }

    internal static class Entities
    {
        public const string Primary = "--entities";
        public const string Alias = "-e";
        public const string Description = "Comma-separated list of additional entity logical names to include";
    }

    internal static class Config
    {
        public const string Primary = "--config";
        public const string Alias = "-c";
        public const string Description = "Path to appsettings.json configuration file";
    }

    internal static class PrettyPrint
    {
        public const string Primary = "--pretty-print";
        public const string Alias = "-p";
        public const string Description = "Format XML output for readability (increases file size)";
    }

    internal static class SecurityRoles
    {
        public const string Primary = "--security-roles";
        public const string Alias = "-r";
        public const string Description = "Comma-separated list of additional security role names to include";
    }

    internal static class AllSecurityRoles
    {
        public const string Primary = "--all-security-roles";
        public const string Alias = "-a";
        public const string Description = "Include all security roles regardless of solution or named role filtering";
    }

    /// <summary>
    /// Overrides DataverseConnection's <c>DataverseCredentialType</c> setting, which selects the
    /// Azure credential used to authenticate. DataverseConnection defaults to <c>browser</c>.
    /// </summary>
    internal static class CredentialType
    {
        public const string Primary = "--credential-type";
        public const string Alias = "-t";
        public const string ConfigurationKey = "DataverseCredentialType";
        public const string HelpName = "browser|devicecode|azcli";
        public const string Description =
            "Azure credential used to authenticate with Dataverse: browser (default), devicecode or azcli";

        /// <summary>
        /// The values accepted by DataverseConnection's configuration binder.
        /// </summary>
        public static readonly string[] AllowedValues = ["browser", "devicecode", "azcli"];
    }

    internal static class DataverseUrl
    {
        public const string Primary = "--dataverse-url";
        public const string Alias = "-u";
        public const string ConfigurationKey = "DataverseUrl";
        public const string Description = "Dataverse environment URL (e.g. https://your-org.crm4.dynamics.com)";
    }
}
