using System.Text;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer;

/// <summary>Static privilege coverage, not execution of a scenario or proof of record access.</summary>
public sealed class PermissionCheckResult(Manifest manifest, CoverageReport coverage, RoleSet? primaryRoles = null)
{
    public Manifest Manifest { get; } = manifest;
    public CoverageReport Coverage { get; } = coverage;
    public bool Passed => Coverage.Summary.Passed(strict: true);

    /// <summary>Throws a framework-neutral exception for missing privileges or incomplete analysis.</summary>
    public void ThrowIfFailed()
    {
        if (Passed) return;
        var message = new StringBuilder("Static permission check failed (not a record-level access check).");
        var summary = Coverage.Summary;
        message.AppendLine().Append($"Summary: {summary.Missing} missing, {summary.Unresolved} unresolved, {summary.NotChecked} unchecked, {summary.Unverifiable} unverifiable, {summary.Truncated} truncated.");
        if (primaryRoles is not null) message.AppendLine().Append($"Primary roles: {primaryRoles}");
        foreach (var entry in Manifest.EntryPoints)
        {
            var section = Coverage.For(entry);
            message.AppendLine().Append($"Entry point: {entry.Id} at {entry.Declaration?.ToString() ?? "synthetic operation"}");
            foreach (var identity in section.Identities)
                message.AppendLine().Append($"  Identity: {identity.RunsAs}; roles: {identity.Roles?.ToString() ?? "unknown"}; {identity.Note}");
            foreach (var requirement in entry.Requirements)
            {
                var status = section.For(requirement).Status;
                if (status is CoverageStatus.Missing or CoverageStatus.NotChecked or CoverageStatus.Unverifiable)
                    message.AppendLine().Append($"  {status}: {requirement.Table} {requirement.Privilege}, {requirement.RunsAs}, at {string.Join(", ", requirement.Sources.Select(s => s.Location))}");
            }
            foreach (var requirement in entry.GlobalRequirements)
            {
                var status = section.For(requirement).Status;
                if (status is CoverageStatus.Missing or CoverageStatus.NotChecked or CoverageStatus.Unverifiable)
                    message.AppendLine().Append($"  {status}: {requirement.Privilege}, {requirement.RunsAs}, at {string.Join(", ", requirement.Sources.Select(s => s.Location))}");
            }
            foreach (var unresolved in entry.Unresolved.Where(u => u.RunsAs.Kind != RunsAsKind.System))
                message.AppendLine().Append($"  Unresolved: {unresolved.Operation}: {unresolved.Reason}, {unresolved.RunsAs}, at {unresolved.Source.Location}");
            if (entry.Truncated) message.AppendLine().Append("  Analysis truncated at the call limit.");
        }
        throw new PermissionCheckException(message.ToString(), this);
    }
}

public sealed class PermissionCheckException(string message, PermissionCheckResult result) : Exception(message)
{
    public PermissionCheckResult Result { get; } = result;
}
