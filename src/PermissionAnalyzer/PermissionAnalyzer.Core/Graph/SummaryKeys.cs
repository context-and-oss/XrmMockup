using System.Globalization;
using XrmMockup.PermissionAnalyzer.Core.Extraction;

namespace XrmMockup.PermissionAnalyzer.Core.Graph;

/// <summary>
/// Structural, ordinal keys for SDK-free facts. Collection record equality and their default ToString methods
/// use collection objects rather than their contents, so neither identifies facts across compilations.
/// </summary>
internal static class SummaryKeys
{
    public static string Call(CallFact call) => Parts(
        Parts(call.Source.File, Number(call.Source.Line), Number(call.Source.Column)), call.Target, call.Virtual.ToString(), call.Receiver.ToString(), call.ReceiverType,
        Parts(call.Arguments.OrderBy(a => a.Key).Select(a => Parts(Number(a.Key), Argument(a.Value)))),
        call.Creation is null ? null : Call(call.Creation),
        call.TypeArguments is null ? null : Parts(call.TypeArguments.OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => Parts(a.Key, Parts(a.Value.Select(Tables))))));

    public static string Access(AccessFact access) => Parts(access.Operation, access.ReasonPrefix,
        Parts(access.Privileges.Select(p => p.ToString())), Tables(access.Tables), Services(access.Service));

    public static string Services(IEnumerable<ServiceOrigin> services) =>
        Parts(services.Select(Service).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal));

    public static string Tables(TableResolution tables) => Parts(
        Parts(tables.Tables), Parts(tables.Reasons), Parts(tables.LookupTargets),
        Parts(tables.Slots.Select(s => Parts(s.Method, Number(s.Ordinal), s.Shape.ToString(), s.Reason))),
        Parts(tables.Returns.Select(r => Parts(Call(r.Call), r.Shape.ToString(), r.Reason))),
        Parts(tables.Fired.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => Parts(f.Key, Tables(f.Value)))),
        tables.Columns is null ? null : Tables(tables.Columns), tables.Bypass.ToString(),
        Parts(tables.Globals.Select(g => Parts(g.Privilege, g.Message))), Parts(tables.Assumptions));

    private static string Argument(ArgumentFact argument) => Parts(
        argument.Service is null ? null : Services(argument.Service),
        argument.Tables is null ? null : Parts(argument.Tables.OrderBy(t => t.Key).Select(t => Parts(t.Key.ToString(), Tables(t.Value)))));

    public static string Service(ServiceOrigin service) => service switch
    {
        FixedOrigin fixedOrigin => Parts(nameof(FixedOrigin), fixedOrigin.RunsAs.Kind.ToString(), fixedOrigin.RunsAs.UserId, fixedOrigin.RunsAs.Assumed.ToString(), fixedOrigin.Initiating.ToString()),
        ParameterOrigin parameter => Parts(nameof(ParameterOrigin), parameter.Method, Number(parameter.Ordinal), parameter.Name),
        FieldOrigin field => Parts(nameof(FieldOrigin), field.Type, field.Field, field.OnThis.ToString()),
        InjectedOrigin => nameof(InjectedOrigin),
        ReturnedOrigin returned => Parts(nameof(ReturnedOrigin), Call(returned.Call)),
        UnknownOrigin unknown => Parts(nameof(UnknownOrigin), unknown.Reason),
        _ => throw new ArgumentOutOfRangeException(nameof(service)),
    };

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    // Length prefixes prevent a delimiter in a path, reason or other value from making two keys identical.
    private static string Parts(params string?[] parts) => Parts((IEnumerable<string?>)parts);

    private static string Parts(IEnumerable<string?> parts) =>
        string.Concat(parts.Select(part => part is null ? "-1:" : Number(part.Length) + ":" + part));
}
