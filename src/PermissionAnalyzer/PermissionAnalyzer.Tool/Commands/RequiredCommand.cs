using System.CommandLine;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// <c>required</c>: the privileges the code needs, per entry point. With <c>--role</c>, also whether the roles
/// grant them, without changing the exit code.
/// </summary>
internal static class RequiredCommand
{
    public const string Name = "required";

    public static Command Create()
    {
        var options = new AnalysisOptions();
        var command = new Command(Name, "List the table privileges the code needs, per entry point");
        options.AddTo(command);

        command.SetAction((parseResult, cancellationToken) => AnalysisRunner.RunAsync(
            Name,
            options.Read(parseResult),
            requireRoles: false,
            (_, _) => ExitCodes.Success,
            cancellationToken));

        return command;
    }
}
