using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Options;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// Turns the configured request rules into <see cref="RequestRule"/>s. Mistakes are errors that name the setting.
/// </summary>
internal static class RuleSetup
{
    public static IReadOnlyList<RequestRule> Create(PermissionsConfiguration configuration) =>
        configuration.Rules.Select(Rule).ToList();

    private static RequestRule Rule(RuleConfiguration rule, int index)
    {
        var setting = $"XrmMockup:Permissions:Rules:{index}";
        if (string.IsNullOrWhiteSpace(rule.Message)) throw new ToolException($"{setting} has no Message");

        var checks = new List<RuleCheck>();
        if (rule.Privileges.Length > 0)
        {
            if (string.IsNullOrWhiteSpace(rule.Target)) throw new ToolException($"{setting} has Privileges but no Target");
            checks.Add(Check(rule.Privileges, setting) with { Member = rule.Target.Trim() });
        }
        checks.AddRange(rule.Checks.Select((check, i) => Check(check, $"{setting}:Checks:{i}")));

        return new RequestRule(rule.Message.Trim())
        {
            Checks = checks,
            As = rule.As.Select(a => a.Trim()).ToList(),
            Targets = Blank(rule.Targets),
            Requests = Blank(rule.Requests),
            Enforced = rule.Enforced ?? true,
        };
    }

    private static RuleCheck Check(RuleCheckConfiguration check, string setting)
    {
        var member = Blank(check.Member);
        var table = Blank(check.Table);
        if ((member is null) == (table is null)) throw new ToolException($"{setting} needs either a Member or a Table");
        if (check.Privileges.Length == 0) throw new ToolException($"{setting} has no Privileges");

        return Check(check.Privileges, setting) with { Member = member, Table = table?.ToLowerInvariant(), When = Blank(check.When) };
    }

    private static RuleCheck Check(IEnumerable<string> privileges, string setting)
    {
        var parsed = new List<Privilege>();
        string? accessMask = null;
        foreach (var text in privileges.Select(p => p.Trim()))
        {
            if (text.StartsWith('$'))
            {
                if (accessMask is not null) throw new ToolException($"{setting} has more than one $ privilege");
                accessMask = text.Substring(1);
                if (accessMask.Length == 0) throw new ToolException($"{setting} has a $ privilege without a member");
            }
            else if (Enum.TryParse<Privilege>(text, ignoreCase: true, out var privilege) && Enum.IsDefined(privilege))
            {
                if (!parsed.Contains(privilege)) parsed.Add(privilege);
            }
            else
            {
                throw new ToolException(
                    $"{setting} has an unknown privilege '{text}'. Use one of {string.Join(", ", Enum.GetNames<Privilege>())}, " +
                    "or $Member for the rights a member grants");
            }
        }
        return new RuleCheck(parsed) { AccessMask = accessMask };
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
