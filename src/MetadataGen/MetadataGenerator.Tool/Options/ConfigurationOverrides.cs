namespace XrmMockup.MetadataGenerator.Tool.Options;

/// <summary>
/// Flat configuration keys supplied on the command line. These are added as the last (highest
/// precedence) configuration source, so they override appsettings.json and environment variables.
/// Used for settings that are consumed by DataverseConnection rather than by this tool, so the
/// library keeps ownership of key names, defaults and validation.
/// </summary>
public sealed record ConfigurationOverrides(IReadOnlyDictionary<string, string?> Values)
{
    /// <summary>
    /// No command-line overrides.
    /// </summary>
    public static ConfigurationOverrides Empty { get; } = new(new Dictionary<string, string?>());
}
