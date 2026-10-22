using System.CommandLine;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;

var rootCommand = new RootCommand("XrmMockup Permission Analyzer - Work out which Dataverse privileges your code needs, by static analysis")
{
    RequiredCommand.Create(),
    CheckCommand.Create()
};

var parseResult = rootCommand.Parse(args);
var exitCode = await parseResult.InvokeAsync();

// Exit code 1 is reserved for "privileges missing", so invalid arguments exit like any other error
return parseResult.Errors.Count > 0 ? ExitCodes.Error : exitCode;
