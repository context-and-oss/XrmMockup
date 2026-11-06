namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A call site that needs a privilege, with the call path from the entry point to the method that makes it.
/// </summary>
public sealed record RequirementSource(SourceLocation Location, IReadOnlyList<string> Path) : IComparable<RequirementSource>
{
    public int CompareTo(RequirementSource? other)
    {
        if (other is null) return 1;
        var result = Location.CompareTo(other.Location);
        return result != 0 ? result : string.CompareOrdinal(string.Join("\n", Path), string.Join("\n", other.Path));
    }

    public bool Equals(RequirementSource? other) =>
        other is not null && Location.Equals(other.Location) && Path.SequenceEqual(other.Path, StringComparer.Ordinal);

    public override int GetHashCode() => Location.GetHashCode();
}
