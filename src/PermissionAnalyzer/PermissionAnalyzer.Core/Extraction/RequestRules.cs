using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The rules for the requests passed to <c>Execute</c>: the built-in ones, which follow Dataverse, replaced or
/// extended by configured ones. A rule names its message as is or as its request type, such as <c>AssignRequest</c>;
/// a message is matched to a rule by its name first, ignoring case.
/// </summary>
internal sealed class RequestRules
{
    /// <summary>
    /// The rules for the SDK requests, as Dataverse checks them. <see cref="RequestRule.Enforced"/> is <c>false</c>
    /// where XrmMockup's handler doesn't make the checks.
    /// </summary>
    public static IReadOnlyList<RequestRule> BuiltIn { get; } =
    [
        Rule(Messages.Create, On(SdkNames.Target, Privilege.Create)),
        Rule(Messages.Update, On(SdkNames.Target, Privilege.Write)),
        Rule(Messages.Delete, On(SdkNames.Target, Privilege.Delete)),
        Rule(Messages.Retrieve, On(SdkNames.Target, Privilege.Read)),
        Rule(Messages.RetrieveMultiple, On(SdkNames.Query, Privilege.Read)),
        Rule(Messages.Associate, On(SdkNames.Target, Privilege.Read, Privilege.AppendTo), On("RelatedEntities", Privilege.Read, Privilege.Append)),
        Rule(Messages.Disassociate, On(SdkNames.Target, Privilege.Read, Privilege.AppendTo), On("RelatedEntities", Privilege.Read, Privilege.Append)),
        new(Messages.Upsert) { As = [Messages.Create, Messages.Update] },
        new("CreateMultiple") { Targets = "Targets", As = [Messages.Create] },
        new("UpdateMultiple") { Targets = "Targets", As = [Messages.Update] },
        new("UpsertMultiple") { Targets = "Targets", As = [Messages.Upsert] },
        new("DeleteMultiple") { Targets = "Targets", As = [Messages.Delete] },
        new("ExecuteMultiple") { Requests = "Requests" },
        new("ExecuteTransaction") { Requests = "Requests" },
        Rule("SetState", On("EntityMoniker", Privilege.Write)) with { Enforced = false },
        Rule("Assign", On(SdkNames.Target, Privilege.Assign, Privilege.Write)),
        Rule("GrantAccess", On(SdkNames.Target, Privilege.Share) with { AccessMask = "PrincipalAccess.AccessMask" }),
        Rule("ModifyAccess", On(SdkNames.Target, Privilege.Share) with { AccessMask = "PrincipalAccess.AccessMask" }),
        Rule("RevokeAccess", On(SdkNames.Target, Privilege.Share)),
        Rule("Merge", On(SdkNames.Target, Privilege.Read, Privilege.Write)) with { GlobalPrivileges = ["prvMerge"] },
        Rule(
            "QualifyLead",
            On("LeadId", Privilege.Write),
            OnTable("account", Privilege.Create) with { When = "CreateAccount" },
            OnTable("contact", Privilege.Create) with { When = "CreateContact" },
            OnTable("opportunity", Privilege.Create) with { When = "CreateOpportunity" }),
        Rule("SendEmail", OnTable("email", Privilege.Read, Privilege.Write)) with
        {
            Assumption = "SendEmail sends the email as the calling user; sending it as another user needs prvSendAsUser",
        },
        Rule("CloseIncident", On("IncidentResolution", Privilege.Create), OnTable("incident", Privilege.Write)),
        Rule("WinOpportunity", On("OpportunityClose", Privilege.Create), OnTable("opportunity", Privilege.Write)),
        Rule("LoseOpportunity", On("OpportunityClose", Privilege.Create), OnTable("opportunity", Privilege.Write)),
        Rule("WinQuote", On("QuoteClose", Privilege.Create), OnTable("quote", Privilege.Write)),
        Rule("CloseQuote", On("QuoteClose", Privilege.Create), OnTable("quote", Privilege.Write)),
        Rule("ReviseQuote", OnTable("quote", Privilege.Read, Privilege.Create)),
        Rule("InitializeFileBlocksUpload", On(SdkNames.Target, Privilege.Write)) with { Enforced = false },
        Rule("InitializeFileBlocksDownload", On(SdkNames.Target, Privilege.Read)) with { Enforced = false },

        // Block transfers continue one that was initialized, and checked, above
        new("UploadBlock"),
        new("CommitFileBlocksUpload"),
        new("DownloadBlock"),

        // Privileges not tied to a table
        Global("BulkDelete", "prvBulkDelete"),
        Global("ImportSolution", "prvImportCustomization"),
        Global("ImportSolutionAsync", "prvImportCustomization"),
        Global("ExportSolution", "prvExportCustomization"),
        Global("ExportSolutionAsync", "prvExportCustomization"),
        Global("PublishXml", "prvPublishCustomization"),
        Global("PublishAllXml", "prvPublishCustomization"),
        Global("ExecuteWorkflow", "prvWorkflowExecution"),
        Global("DeleteRecordChangeHistory", "prvDeleteRecordChangeHistory"),
        Global("DeleteAuditData", "prvDeleteAuditPartitions"),
        Global("RetrieveAuditDetails", "prvReadRecordAuditHistory", "prvReadAuditSummary"),
        Global("RetrieveAttributeChangeHistory", "prvReadRecordAuditHistory", "prvReadAuditSummary"),
        Global("RetrieveRecordChangeHistory", "prvReadRecordAuditHistory", "prvReadAuditSummary"),

        // Metadata and organization information, readable by every user
        new("RetrieveEntity"),
        new("RetrieveAttribute"),
        new("RetrieveAllEntities"),
        new("RetrieveAllOptionSets"),
        new("RetrieveOptionSet"),
        new("RetrieveRelationship"),
        new("RetrieveMetadataChanges"),
        new("WhoAmI"),
        new("RetrieveVersion"),
        new("FetchXmlToQueryExpression"),
        new("QueryExpressionToFetchXml"),
        new("RetrieveCurrentOrganization"),
        new("RetrieveExchangeRate"),
        new("IsValidStateTransition"),
        new("RetrievePrincipalAccess"),
    ];

    private const string RequestSuffix = "Request";

    private readonly Dictionary<string, RequestRule> _builtIn = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RequestRule> _configured = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ExpandedRule> _expanded = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="rules">Rules that replace the built-in rule for their message, or add one.</param>
    public RequestRules(IEnumerable<RequestRule>? rules = null)
    {
        foreach (var rule in BuiltIn) _builtIn[rule.Message] = rule;
        foreach (var rule in rules ?? []) _configured[rule.Message] = rule;
    }

    /// <summary>
    /// The message of a request type: <c>AssignRequest</c> is <c>Assign</c>. Only a type name has the suffix to
    /// remove; a message such as <c>contoso_ApprovalRequest</c> is called by its name.
    /// </summary>
    public static string MessageOfType(string typeName) =>
        typeName.Length > RequestSuffix.Length && typeName.EndsWith(RequestSuffix, StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - RequestSuffix.Length)
            : typeName;

    /// <summary>
    /// The message a person names in a rule's <see cref="RequestRule.As"/> or with <c>--operation</c>: the name, or
    /// when only the request type has a rule, its message, so <c>CreateRequest</c> is <c>Create</c>.
    /// </summary>
    public string MessageNamed(string name) =>
        RuleOf(name) is null && MessageOfType(name) is var message && message != name && RuleOf(message) is not null ? message : name;

    /// <summary>
    /// The checks of <paramref name="message"/> with those of its <see cref="RequestRule.As"/> messages, or
    /// <c>null</c> when there is no rule for it.
    /// </summary>
    public ExpandedRule? Find(string message) => Expand(message, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// The rule for <paramref name="message"/>, named as the message or its request type; a configured rule goes
    /// before the built-in one.
    /// </summary>
    private RequestRule? RuleOf(string message)
    {
        foreach (var rules in new[] { _configured, _builtIn })
        {
            if (rules.TryGetValue(message, out var rule) || rules.TryGetValue(message + RequestSuffix, out rule)) return rule;
        }
        return null;
    }

    private ExpandedRule? Expand(string message, HashSet<string> visiting)
    {
        if (_expanded.TryGetValue(message, out var known)) return known;
        if (RuleOf(message) is not { } rule) return null;

        // A rule that is, through others, its own as-rule adds nothing more
        if (!visiting.Add(message)) return new ExpandedRule([], [], [], [], [], []);

        var checks = new List<RuleCheck>(rule.Checks);
        var globals = new List<string>(rule.GlobalPrivileges);
        var assumptions = new List<string>();
        if (rule.Assumption is { } assumption) assumptions.Add(assumption);
        var requests = new List<string>();
        var missing = new List<string>();
        var messages = new List<string> { message };
        if (rule.Requests is { } nested) requests.Add(nested);
        foreach (var other in rule.As.Select(MessageNamed))
        {
            if (Expand(other, visiting) is not { } expanded)
            {
                missing.Add(other);
                messages.Add(other);
                continue;
            }
            messages.AddRange(expanded.Messages);
            checks.AddRange(rule.Targets is { } targets ? expanded.Checks.Select(c => c.Member == SdkNames.Target ? c with { Member = targets } : c) : expanded.Checks);
            requests.AddRange(expanded.Requests);
            missing.AddRange(expanded.Missing);
            globals.AddRange(expanded.GlobalPrivileges);
            assumptions.AddRange(expanded.Assumptions);
        }
        visiting.Remove(message);

        var result = new ExpandedRule(
            checks,
            requests.Distinct(StringComparer.Ordinal).ToList(),
            missing.Distinct(StringComparer.Ordinal).ToList(),
            messages.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            globals.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(g => g, StringComparer.Ordinal).ToList(),
            assumptions.Distinct(StringComparer.Ordinal).OrderBy(a => a, StringComparer.Ordinal).ToList());
        // A rule expanded inside a cycle misses the part of the cycle above it, so only complete ones are kept
        if (visiting.Count == 0) _expanded[message] = result;
        return result;
    }

    private static RequestRule Rule(string message, params RuleCheck[] checks) => new(message) { Checks = checks };

    private static RequestRule Global(string message, params string[] privileges) => new(message) { GlobalPrivileges = privileges };

    private static RuleCheck On(string member, params Privilege[] privileges) => new(privileges) { Member = member };

    private static RuleCheck OnTable(string table, params Privilege[] privileges) => new(privileges) { Table = table };
}

/// <summary>
/// A rule with the checks of its as-rules included.
/// </summary>
/// <param name="Checks">Every check the request makes.</param>
/// <param name="Requests">The members that hold requests run as part of this one.</param>
/// <param name="Missing">As-messages that have no rule.</param>
/// <param name="Messages">
/// The messages the request fires plugin steps for: its own and its as-messages, as Dataverse runs the steps of
/// <c>Create</c> and <c>Update</c> for <c>Upsert</c>.
/// </param>
/// <param name="GlobalPrivileges">The privileges the request needs by name, sorted.</param>
/// <param name="Assumptions">What the analysis assumes about the request, sorted.</param>
internal sealed record ExpandedRule(
    IReadOnlyList<RuleCheck> Checks,
    IReadOnlyList<string> Requests,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> GlobalPrivileges,
    IReadOnlyList<string> Assumptions);

/// <summary>
/// The reasons a request isn't checked, built and recognized in one place, so the coverage summary can count them.
/// </summary>
internal static class RequestReasons
{
    private const string NoRulePrefix = "no rule for message '";
    private const string CustomApiPrefix = "custom API '";

    public static string NoRule(string message) => NoRulePrefix + message + "'";

    /// <summary>
    /// A custom API that neither the code nor the privilege catalog has, so neither its code nor its execute
    /// privilege is known.
    /// </summary>
    public static string CustomApi(string message) => CustomApiPrefix + message + "' is called; its code and execute privilege are unknown";

    /// <summary>
    /// A custom API in the privilege catalog whose code isn't loaded: its execute privilege is checked, its code isn't.
    /// </summary>
    public static string CustomApiCode(string message) => CustomApiPrefix + message + "' is called; its code isn't analyzed";

    public static string ExecutePrivilegeComputed(string message) => CustomApiPrefix + message + "' needs an execute privilege set at runtime";

    public static string PrivilegeCatalogMissing(string message, string privilege) =>
        CustomApiPrefix + message + $"' needs execute privilege '{privilege}', which needs a privilege catalog (PrivilegeCatalog.xml) to look up";

    public static string PrivilegeNotInCatalog(string message, string privilege) =>
        CustomApiPrefix + message + $"' needs execute privilege '{privilege}', which isn't in the privilege catalog";

    public static string PrivilegeWithoutTablePrivilege(string message, string privilege) =>
        CustomApiPrefix + message + $"' needs execute privilege '{privilege}', whose access right isn't a table privilege";

    /// <summary>
    /// A privilege a request rule names that the privilege catalog doesn't have, such as one misspelled in a configured
    /// rule.
    /// </summary>
    public static string NamedPrivilegeNotInCatalog(string message, string privilege) =>
        $"the {message} request needs privilege '{privilege}', which isn't in the privilege catalog";

    public static string NamedPrivilegeWithoutTablePrivilege(string message, string privilege) =>
        $"the {message} request needs privilege '{privilege}', whose access right isn't a table privilege";

    /// <summary>
    /// The message of a <see cref="NoRule"/> reason, or <c>null</c> for any other reason.
    /// </summary>
    public static string? NoRuleMessage(string reason) =>
        reason.StartsWith(NoRulePrefix, StringComparison.Ordinal) && reason.EndsWith("'", StringComparison.Ordinal)
            ? reason.Substring(NoRulePrefix.Length, reason.Length - NoRulePrefix.Length - 1)
            : null;

    /// <summary>
    /// A reason about calling a custom API: its code or its execute privilege.
    /// </summary>
    public static bool IsCustomApi(string reason) => reason.StartsWith(CustomApiPrefix, StringComparison.Ordinal);
}
