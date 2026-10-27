namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A lookup column from the XrmMockup metadata, with the tables it can refer to. A polymorphic lookup, such as a
/// customer or regarding column, has several.
/// </summary>
/// <param name="Table">Logical name of the table the column is on.</param>
/// <param name="Column">Logical name of the column.</param>
/// <param name="Targets">Logical names of the tables the column can refer to.</param>
public sealed record LookupColumn(string Table, string Column, IReadOnlyList<string> Targets);
