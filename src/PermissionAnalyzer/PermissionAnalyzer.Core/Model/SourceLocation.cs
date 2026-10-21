namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A position in the analyzed source code. <see cref="File"/> is relative to the analysis root
/// and always uses forward slashes, so output is identical across machines.
/// </summary>
public sealed record SourceLocation(string File, int Line, int Column) : IComparable<SourceLocation>
{
    public int CompareTo(SourceLocation? other)
    {
        if (other is null) return 1;

        var result = string.CompareOrdinal(File, other.File);
        if (result != 0) return result;

        result = Line.CompareTo(other.Line);
        return result != 0 ? result : Column.CompareTo(other.Column);
    }

    public override string ToString() => $"{File}:{Line}";
}
