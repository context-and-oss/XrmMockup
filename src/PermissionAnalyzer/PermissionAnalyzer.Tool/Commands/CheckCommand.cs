using System.CommandLine;
using Microsoft.Extensions.Logging;
using XrmMockup.PermissionAnalyzer.Core.Coverage;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// <c>check</c>: whether security roles grant everything the code needs. Exits with
/// <see cref="ExitCodes.MissingPrivileges"/> when they don't.
/// </summary>
internal static class CheckCommand
{
    public const string Name = "check";

    public static Command Create()
    {
        var options = new AnalysisOptions();
        var strictOption = new Option<bool>(CliOptions.Strict.Primary) { Description = CliOptions.Strict.Description };

        var command = new Command(Name, "Check that security roles grant the table privileges the code needs");
        options.AddTo(command);
        command.Options.Add(strictOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var strict = parseResult.GetValue(strictOption);
            return AnalysisRunner.RunAsync(
                Name,
                options.Read(parseResult),
                requireRoles: true,
                (coverage, logger) => ExitCode(coverage!, strict, logger),
                cancellationToken);
        });

        return command;
    }

    private static int ExitCode(CoverageReport coverage, bool strict, ILogger logger)
    {
        var summary = coverage.Summary;
        if (summary.Passed(strict))
        {
            if (summary.Incomplete)
            {
                logger.LogWarning(
                    "The roles grant every privilege found, but the result is incomplete: {Unresolved} unresolved operations, " +
                    "{NotChecked} requirements without roles and {Unverifiable} impersonation checks weren't checked. " +
                    "See 'Not checked' in the report, or pass {Strict} to fail on them",
                    summary.Unresolved, summary.NotChecked, summary.Unverifiable, CliOptions.Strict.Primary);
            }
            return ExitCodes.Success;
        }

        if (summary.Missing > 0)
        {
            var privileges = coverage.Missing.Select(m => $"{m.Table} {m.Privilege}").Distinct().ToList();
            logger.LogError("The roles don't grant {Count} privileges: {Privileges}. See 'Missing privileges' in the report", privileges.Count, string.Join(", ", privileges));
        }
        else
        {
            logger.LogError(
                "{Strict}: {Unresolved} unresolved, {NotChecked} not checked, {Unverifiable} unverifiable",
                CliOptions.Strict.Primary, summary.Unresolved, summary.NotChecked, summary.Unverifiable);
        }
        return ExitCodes.MissingPrivileges;
    }
}
