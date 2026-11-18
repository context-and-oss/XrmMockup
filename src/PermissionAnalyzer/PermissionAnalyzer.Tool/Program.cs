using System.CommandLine;
using System.CommandLine.Invocation;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;

var options = new AnalysisOptions();
var rootCommand = new RootCommand("XrmMockup Permission Analyzer - Work out which Dataverse privileges your code needs, by static analysis, and check them against security roles");
options.AddTo(rootCommand);
rootCommand.SetAction((parseResult, cancellationToken) => AnalysisRunner.RunAsync(options.Read(parseResult), cancellationToken));

var parseResult = rootCommand.Parse(args);
// The errors alone, without the whole help after them, so they stand out in a build log
if (parseResult.Action is ParseErrorAction parseError) parseError.ShowHelp = false;
var exitCode = await parseResult.InvokeAsync();
if (parseResult.Errors.Count > 0) Console.Error.WriteLine("Run with --help to see the options.");

// Exit code 1 is reserved for "privileges missing", so invalid arguments exit like any other error
return parseResult.Errors.Count > 0 ? ExitCodes.Error : exitCode;
