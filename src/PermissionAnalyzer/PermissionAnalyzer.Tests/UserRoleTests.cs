using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Dataverse;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Workspace.Serialization;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class UserRoleTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid RootId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid TeamId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset ReadAt = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly RoleDefinition Direct = new("Reader", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>
    {
        ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.User },
    }, new Dictionary<string, PrivilegeDepth>(), RootId);
    private static readonly RoleDefinition TeamRole = new("Writer", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>
    {
        ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Write] = PrivilegeDepth.Organization },
    }, new Dictionary<string, PrivilegeDepth>(), TeamId);

    private static Entity Role(Guid id, Guid? root = null, bool team = false)
    {
        var role = new Entity("role", id) { ["name"] = "Live role name" };
        if (root is { } rootId) role["parentrootroleid"] = new EntityReference("role", rootId);
        if (team)
        {
            role["team.name"] = new AliasedValue("team", "name", "Entra group");
            role["team.teamid"] = new AliasedValue("team", "teamid", UserId);
        }
        return role;
    }

    private static EntityCollection Page(params Entity[] entities) => new(entities.ToList());
    private static RoleSet Resolve(FakeService service, params RoleDefinition[] roles) =>
        new UserRoleResolver(service).Resolve("jane@contoso.com", roles, "https://org.crm.dynamics.com/", "--user", ReadAt);

    [Fact]
    public void CombinesDirectAndTeamAssignmentsByRootIdAndRecordsProvenance()
    {
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) :
            Page(q.LinkEntities[0].LinkToEntityName == "systemuserroles" ? Role(Guid.NewGuid(), RootId) : Role(TeamId, team: true)));
        var roles = Resolve(service, Direct, TeamRole);

        Assert.Equal(new[] { "Reader", "Writer" }, roles.Names);
        Assert.Equal(PrivilegeDepth.User, roles.Depth("account", Privilege.Read));
        Assert.Equal(PrivilegeDepth.Organization, roles.Depth("account", Privilege.Write));
        Assert.Equal("--user: jane@contoso.com", roles.Origin);
        var source = Assert.IsType<LiveRoleSource>(roles.LiveSource);
        Assert.Equal(UserId, source.UserId);
        Assert.Equal("jane@contoso.com", source.UserName);
        Assert.Equal("https://org.crm.dynamics.com/", source.EnvironmentUrl);
        Assert.Equal(ReadAt, source.ReadAt);
        Assert.Equal(new[]
        {
            new LiveRoleAssignment("Reader", RootId),
            new LiveRoleAssignment("Writer", TeamId, "Entra group", UserId),
        }, source.Assignments);
    }

    [Fact]
    public void UsesEmailOnlyWhenUpnNotFound()
    {
        var attributes = new List<string>();
        var service = new FakeService(q =>
        {
            if (q.EntityName != "systemuser") return q.LinkEntities[0].LinkToEntityName == "systemuserroles" ? Page(Role(RootId)) : Page();
            var attribute = Assert.Single(q.Criteria.Conditions).AttributeName;
            attributes.Add(attribute);
            Assert.Equal(2, q.TopCount);
            return attribute == "domainname" ? Page() : Page(new Entity("systemuser", UserId));
        });
        Resolve(service, Direct);
        Assert.Equal(new[] { "domainname", "internalemailaddress" }, attributes);
    }

    [Theory]
    [InlineData("domainname")]
    [InlineData("internalemailaddress")]
    public void AmbiguousUserFailsWithoutChoosingOne(string attribute)
    {
        var service = new FakeService(q => Assert.Single(q.Criteria.Conditions).AttributeName == attribute
            ? Page(new Entity("systemuser", UserId), new Entity("systemuser", Guid.NewGuid())) : Page());
        Assert.Contains("ambiguous", Assert.Throws<ToolException>(() => Resolve(service, Direct)).Message);
    }

    [Fact]
    public void UnknownUserFails() => Assert.Contains("not found", Assert.Throws<ToolException>(() => Resolve(new FakeService(_ => Page()), Direct)).Message);

    [Fact]
    public void NoAssignmentsFails()
    {
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) : Page());
        Assert.Contains("no direct or team", Assert.Throws<ToolException>(() => Resolve(service, Direct)).Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingRoleFailsEvenWhenAnotherRoleHasSameName(bool team)
    {
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) :
            (q.LinkEntities[0].LinkToEntityName == "teamroles") == team ? Page(Role(Guid.NewGuid(), team: team)) : Page());
        var error = Assert.Throws<ToolException>(() => Resolve(service, Direct));
        Assert.Contains("Regenerate the metadata", error.Message);
    }

    [Fact]
    public void DuplicateMetadataIdFailsRatherThanUnioningDifferentDefinitions()
    {
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) : Page(Role(RootId)));
        Assert.Contains("duplicated", Assert.Throws<ToolException>(() => Resolve(service, Direct, Direct)).Message);
    }

    [Fact]
    public void PagesAssignmentsAndKeepsAllSourcesForRepeatedRoles()
    {
        var pages = new List<int>();
        var service = new FakeService(q =>
        {
            if (q.EntityName == "systemuser") return Page(new Entity("systemuser", UserId));
            if (q.LinkEntities[0].LinkToEntityName == "teamroles") return Page(Role(RootId, team: true));
            pages.Add(q.PageInfo.PageNumber);
            if (q.PageInfo.PageNumber == 2)
            {
                Assert.Equal("cookie", q.PageInfo.PagingCookie);
                return Page(Role(TeamId));
            }
            return new EntityCollection(new List<Entity> { Role(RootId) }) { MoreRecords = true, PagingCookie = "cookie" };
        });
        var roles = Resolve(service, Direct, TeamRole);
        Assert.Equal(new[] { 1, 2 }, pages);
        var source = Assert.IsType<LiveRoleSource>(roles.LiveSource);
        Assert.Contains(new LiveRoleAssignment("Reader", RootId), source.Assignments);
        Assert.Contains(new LiveRoleAssignment("Reader", RootId, "Entra group", UserId), source.Assignments);
        Assert.Equal(2, roles.Names.Count);
    }

    [Fact]
    public void TeamQueryIncludesAllTeamTypesAndFiltersMembershipByUser()
    {
        var query = UserRoleResolver.RoleQuery(UserId, true);
        var team = Assert.Single(Assert.Single(query.LinkEntities).LinkEntities);
        Assert.Equal("team", team.LinkToEntityName);
        Assert.Empty(team.LinkCriteria.Conditions);
        var membership = Assert.Single(team.LinkEntities);
        Assert.Equal("teammembership", membership.LinkToEntityName);
        var condition = Assert.Single(membership.LinkCriteria.Conditions);
        Assert.Equal("systemuserid", condition.AttributeName);
        Assert.Equal(UserId, Assert.Single(condition.Values));
    }

    [Fact]
    public void CancellationStopsBeforeQuery()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => new UserRoleResolver(new FakeService(_ => throw new Exception("Must not query")))
            .Resolve("jane", [Direct], "https://org.crm.dynamics.com/", "--user", ReadAt, source.Token));
    }

    [Fact]
    public void MetadataConversionPreservesRootRoleId()
    {
        Assert.Equal(RootId, RoleLoader.Convert(new SecurityRole { Name = "Reader", RoleId = RootId }).RoleId);
        Assert.Null(RoleLoader.Convert(new SecurityRole { Name = "Legacy" }).RoleId);
    }

    [Fact]
    public void LiveRolesReplacePrincipalsAndKeepImpersonatedCallerSeparate()
    {
        var user = RoleSet.Combine([Direct], "--user");
        var caller = RoleSet.Combine([TeamRole], "--caller-user");
        var settings = new PermissionsConfiguration { Principals = new() { ["stale"] = new() { Roles = ["Unknown"] } } };
        var coverage = CoverageSetup.Create([Direct, TeamRole], [], [], settings, true, user, caller)!;
        Assert.Same(user, coverage.Roles);
        Assert.Same(caller, coverage.CallerRoles);
        Assert.Empty(coverage.Principals);
    }

    [Theory]
    [InlineData("--user jane --role Reader")]
    [InlineData("--caller-user jane --caller-role Reader")]
    [InlineData("--user jane --no-roles")]
    [InlineData("--caller-user jane --no-roles")]
    [InlineData("--dataverse-url https://org.crm.dynamics.com")]
    [InlineData("--credential-type azcli")]
    [InlineData("--user jane --credential-type invalid")]
    public void InvalidOptionsFail(string args)
    {
        var command = new RootCommand();
        new AnalysisOptions().AddTo(command);
        Assert.NotEmpty(command.Parse(args).Errors);
    }

    [Fact]
    public void ReadsLiveOptions()
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var parse = command.Parse("--user jane --caller-user john -u https://org.crm.dynamics.com -t azcli");
        Assert.Empty(parse.Errors);
        var request = options.Read(parse);
        Assert.Equal("jane", request.User);
        Assert.Equal("john", request.CallerUser);
        Assert.Equal("https://org.crm.dynamics.com", request.DataverseUrl);
        Assert.Equal("azcli", request.CredentialType);
    }

    [Fact]
    public async Task OfflineRunDoesNotValidateOrConnectToDataverse()
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var configuration = new LoadedConfiguration(".", new(), null,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DataverseUrl"] = "invalid" }).Build());
        var result = await DataverseRoleLookup.ResolveAsync(options.Read(command.Parse([])), configuration, null,
            NullLoggerFactory.Instance, CancellationToken.None);
        Assert.Null(result.User);
        Assert.Null(result.Caller);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("markdown")]
    [InlineData("sarif")]
    public void PassingReportsRetainLiveProvenanceEvenWhenFiltered(string format)
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service) => service.Retrieve("account", Guid.Empty, new ColumnSet());
            }
            """, ["account"]);
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) :
            q.LinkEntities[0].LinkToEntityName == "systemuserroles" ? Page(Role(RootId)) : Page());
        var coverage = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = Resolve(service, Direct) });
        Assert.Equal(0, coverage.Summary.Missing);
        var filter = new ReportFilter(["contact"], [], [], []);
        IManifestWriter writer = format switch
        {
            "text" => new TextManifestWriter(new ReportOptions(Detail.Minimal, filter)),
            "json" => new JsonManifestWriter(filter),
            "markdown" => new MarkdownManifestWriter(filter),
            _ => new SarifManifestWriter(filter),
        };
        using var output = new StringWriter();
        writer.Write(manifest, coverage, output);
        Assert.Contains("jane@contoso.com", output.ToString());
        Assert.Contains("Reader: direct", output.ToString());
        if (format is "json" or "sarif")
        {
            Assert.Contains("https://org.crm.dynamics.com/", output.ToString());
            Assert.Contains("2026-11-23T12:00:00", output.ToString());
            Assert.Contains(UserId.ToString("D"), output.ToString());
            Assert.Contains(RootId.ToString("D"), output.ToString());
        }
        else
        {
            Assert.DoesNotContain("https://org.crm.dynamics.com/", output.ToString());
            Assert.DoesNotContain("2026-11-23T12:00:00", output.ToString());
            Assert.DoesNotContain(UserId.ToString("D"), output.ToString());
        }
    }

    [Theory]
    [InlineData("Minimal", false)]
    [InlineData("Compact", false)]
    [InlineData("Normal", false)]
    [InlineData("Detailed", true)]
    [InlineData("Full", true)]
    public void TextShowsReadableAssignmentsAndOnlyAddsLookupDetailsAtHigherLevels(string level, bool detailed)
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service) => service.Delete("account", Guid.Empty);
            }
            """, ["account"]);
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) :
            Page(q.LinkEntities[0].LinkToEntityName == "systemuserroles" ? Role(RootId) : Role(TeamId, team: true)));
        var coverage = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = Resolve(service, Direct, TeamRole) });
        Assert.Equal(1, coverage.Summary.Missing);
        using var output = new StringWriter();
        new TextManifestWriter(new ReportOptions(Enum.Parse<Detail>(level))).Write(manifest, coverage, output);
        var text = output.ToString();
        Assert.StartsWith("Live security roles:\n  User: jane@contoso.com", text);
        Assert.Contains("Reader, Writer (--user: jane@contoso.com)", text);
        if (detailed)
        {
            Assert.Contains($"User: jane@contoso.com (ID: {UserId:D})", text);
            Assert.Contains($"Reader (root role ID: {RootId:D})\n      direct", text);
            Assert.Contains($"Writer (root role ID: {TeamId:D})\n      team 'Entra group' (ID: {UserId:D})", text);
            Assert.Contains("Roles read: 2026-11-23T12:00:00", text);
            // The lookup details appear once, not repeated in section labels or missing-privilege rows.
            Assert.Equal(2, text.Split("https://org.crm.dynamics.com/", StringSplitOptions.None).Length);
        }
        else
        {
            Assert.StartsWith("Live security roles:\n  User: jane@contoso.com\n    Reader: direct\n    Writer: team 'Entra group'\n\n", text);
            Assert.DoesNotContain(UserId.ToString("D"), text);
            Assert.DoesNotContain(RootId.ToString("D"), text);
            Assert.DoesNotContain(TeamId.ToString("D"), text);
            Assert.DoesNotContain("https://org.crm.dynamics.com/", text);
            Assert.DoesNotContain("Roles read:", text);
        }
    }

    [Fact]
    public void RepeatedRoleKeepsBothDirectAndTeamSourcesOnOneReadableLine()
    {
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) :
            Page(Role(RootId, team: q.LinkEntities[0].LinkToEntityName == "teamroles")));
        var roles = Resolve(service, Direct);
        Assert.Equal(new[] { "User: jane@contoso.com", "  Reader: direct; team 'Entra group'" },
            LiveRoleSources.Lines(roles, Detail.Minimal));
    }

    [Fact]
    public void CallerUserHasDistinctReadableLabel()
    {
        var service = new FakeService(q => q.EntityName == "systemuser" ? Page(new Entity("systemuser", UserId)) :
            q.LinkEntities[0].LinkToEntityName == "systemuserroles" ? Page(Role(RootId)) : Page());
        var roles = new UserRoleResolver(service).Resolve("john@contoso.com", [Direct],
            "https://org.crm.dynamics.com/", "--caller-user", ReadAt);
        Assert.Equal(new[] { "Caller user: john@contoso.com", "  Reader: direct" },
            LiveRoleSources.Lines(roles, Detail.Minimal));
        Assert.Equal("Reader (--caller-user: john@contoso.com)", roles.ToString());
    }

    [Fact]
    public void ConnectionOverridesUseMetadataToolKeysAndHighestPrecedence()
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DataverseUrl"] = "https://configured.crm.dynamics.com",
            ["DataverseCredentialType"] = "browser",
            ["DATAVERSE_URL"] = "https://legacy.crm.dynamics.com",
            ["DATAVERSE_CREDENTIAL_TYPE"] = "devicecode",
        }).Build();
        var loaded = new LoadedConfiguration(".", new(), null, settings);
        var offline = DataverseRoleLookup.CreateConnectionConfiguration(options.Read(command.Parse([])), loaded);
        Assert.Equal(settings["DataverseUrl"], offline["DataverseUrl"]);
        Assert.Equal(settings["DATAVERSE_URL"], offline["DATAVERSE_URL"]);
        Assert.Equal(settings["DATAVERSE_CREDENTIAL_TYPE"], offline["DATAVERSE_CREDENTIAL_TYPE"]);
        var live = DataverseRoleLookup.CreateConnectionConfiguration(options.Read(command.Parse(
            "--user jane --dataverse-url https://override.crm.dynamics.com --credential-type azcli")), loaded);
        Assert.Equal("https://override.crm.dynamics.com", live["DataverseUrl"]);
        Assert.Equal("azcli", live["DataverseCredentialType"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://org.crm.dynamics.com")]
    [InlineData("https://user:secret@org.crm.dynamics.com")]
    [InlineData("https://org.crm.dynamics.com?token=secret")]
    public async Task InvalidConnectionFailsBeforeAuthentication(string? url)
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var loaded = new LoadedConfiguration(".", new(), null,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DataverseUrl"] = url }).Build());
        var error = await Assert.ThrowsAsync<ToolException>(() => DataverseRoleLookup.ResolveAsync(
            options.Read(command.Parse("--user jane")), loaded, [Direct], NullLoggerFactory.Instance, CancellationToken.None));
        Assert.Contains("HTTPS Dataverse environment URL", error.Message);
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public async Task LiveLookupRequiresMetadataBeforeAuthentication()
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var error = await Assert.ThrowsAsync<ToolException>(() => DataverseRoleLookup.ResolveAsync(
            options.Read(command.Parse("--caller-user jane")), new LoadedConfiguration(".", new(), null), null,
            NullLoggerFactory.Instance, CancellationToken.None));
        Assert.Contains("downloaded security roles", error.Message);
    }

    private sealed class FakeService(Func<QueryExpression, EntityCollection> retrieve) : IOrganizationService
    {
        public EntityCollection RetrieveMultiple(QueryBase query) => retrieve(Assert.IsType<QueryExpression>(query));
        public Guid Create(Entity entity) => throw new NotSupportedException();
        public void Update(Entity entity) => throw new NotSupportedException();
        public void Delete(string entityName, Guid id) => throw new NotSupportedException();
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotSupportedException();
        public OrganizationResponse Execute(OrganizationRequest request) => throw new NotSupportedException();
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
    }
}
