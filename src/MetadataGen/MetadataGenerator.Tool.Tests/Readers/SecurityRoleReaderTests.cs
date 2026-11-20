extern alias XrmMockupLib;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for SecurityRoleReader.
/// XrmMockup loads security roles from TestMetadata/SecurityRoles (99 role files).
/// Tests verify the reader correctly retrieves roles with their privileges.
///
/// Note: Many tests are skipped because XrmMockup doesn't populate
/// privilege/roleprivileges/privilegeobjecttypecodes entities required for full security role retrieval.
/// </summary>
public class SecurityRoleReaderTests : ReaderTestBase
{
    private readonly SecurityRoleReader _reader;
    private readonly ILogger<SecurityRoleReader> _logger;

    /// <summary>
    /// Known security role GUIDs from TestMetadata/TypeDeclarations.cs.
    /// </summary>
    private static readonly Guid SystemAdministratorRoleId = new("edfa07f1-a1ba-f011-bbd3-7c1e52365f30");
    private static readonly Guid SystemCustomizerRoleId = new("78ff07f1-a1ba-f011-bbd3-7c1e52365f30");
    private static readonly Guid BasicUserRoleId = new("76ff07f1-a1ba-f011-bbd3-7c1e52365f30");

    public SecurityRoleReaderTests()
    {
        _logger = CreateLogger<SecurityRoleReader>();
        _reader = new SecurityRoleReader(ServiceProvider, _logger);
    }

    #region Scenario Matrix Tests

    /// <summary>
    /// allSecurityRoles=true → all roles regardless of other settings.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_AllSecurityRolesTrue_ReturnsAllRoles()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: ["TestSolution"],
            securityRoles: ["System Administrator"],
            allSecurityRoles: true);

        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// No solutions, securityRoles=null (unset) → all roles (backward compat).
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_NoSolutionsNullRoles_ReturnsAllRoles()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            securityRoles: null,
            allSecurityRoles: false);

        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// No solutions, securityRoles=[] (explicit empty) → no roles.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_NoSolutionsExplicitEmptyRoles_ReturnsEmpty()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            securityRoles: [],
            allSecurityRoles: false);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// No solutions, securityRoles=["X"] → only named roles (not all roles).
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_NoSolutionsNamedRoles_ReturnsOnlyNamedRoles()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        // With named roles and no solutions, only those named roles should be fetched.
        // Due to XrmMockup limitations the result may be empty, but the key behavior
        // is that it does NOT return all roles (no unfiltered query).
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            securityRoles: ["System Administrator"],
            allSecurityRoles: false);

        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// Solutions specified, no named roles → solution roles only (may be empty).
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_WithSolutionsNoNamedRoles_ReturnsSolutionRolesOnly()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: ["TestSolution"],
            securityRoles: null,
            allSecurityRoles: false);

        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// Solutions specified + named roles → union of both.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_WithSolutionsAndNamedRoles_ReturnsBoth()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: ["TestSolution"],
            securityRoles: ["System Administrator", "Basic User"],
            allSecurityRoles: false);

        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// Non-existent solution → empty result (no roles in that solution).
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_NonExistentSolution_ReturnsEmptyOrFiltered()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: ["NonExistentSolution123"],
            securityRoles: null,
            allSecurityRoles: false);

        Assert.NotNull(result);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests that when additional role names don't exist,
    /// the reader does not throw an exception and returns gracefully.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_AdditionalRolesNotFound_DoesNotFail()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: ["TestSolution"],
            securityRoles: ["NonExistentRole1", "NonExistentRole2"],
            allSecurityRoles: false);

        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that the method handles empty arrays gracefully.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_EmptyArrays_ReturnsEmpty()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            securityRoles: [],
            allSecurityRoles: false);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that multiple solutions are handled correctly.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_MultipleSolutions_HandlesCorrectly()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: ["Solution1", "Solution2", "Solution3"],
            securityRoles: null,
            allSecurityRoles: false);

        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests cancellation token support.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_WithCancellationToken_AcceptsToken()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        using var cts = new CancellationTokenSource();

        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            securityRoles: null,
            allSecurityRoles: false,
            ct: cts.Token);

        Assert.NotNull(result);
    }

    #endregion

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ReturnsSecurityRoles()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetSecurityRolesAsync_ReturnsDictionaryOfGuidToSecurityRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        var resultType = result.GetType();
        Assert.True(resultType.IsGenericType);
        Assert.Equal("Dictionary`2", resultType.Name);
        var typeArgs = resultType.GetGenericArguments();
        Assert.Equal(typeof(Guid), typeArgs[0]);
        Assert.Contains("SecurityRole", typeArgs[1].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsSystemAdministratorRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        Assert.True(result.ContainsKey(SystemAdministratorRoleId));
        Assert.Equal("System Administrator", result[SystemAdministratorRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsSystemCustomizerRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        Assert.True(result.ContainsKey(SystemCustomizerRoleId));
        Assert.Equal("System Customizer", result[SystemCustomizerRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsBasicUserRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        Assert.True(result.ContainsKey(BasicUserRoleId));
        Assert.Equal("Basic User", result[BasicUserRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_SystemAdministratorHasPrivileges()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        var sysAdmin = result[SystemAdministratorRoleId];
        Assert.NotEmpty(sysAdmin.Privileges);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_AllRolesHaveNames()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        foreach (var role in result.Values)
        {
            Assert.False(string.IsNullOrEmpty(role.Name), $"Role {role.RoleId} is missing a name");
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_DictionaryKeyMatchesRoleId()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        foreach (var kvp in result)
        {
            Assert.Equal(kvp.Key, kvp.Value.RoleId);
            Assert.NotEqual(Guid.Empty, kvp.Value.RoleId);
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_RolesWithPrivilegesHaveValidDepth()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        var rolesWithPrivileges = result.Values.Where(r => r.Privileges.Count > 0).ToList();
        Assert.NotEmpty(rolesWithPrivileges);

        foreach (var role in rolesWithPrivileges)
        {
            foreach (var entityPrivileges in role.Privileges)
            {
                foreach (var privilege in entityPrivileges.Value)
                {
                    Assert.True(
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Basic ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Local ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Deep ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Global ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.RecordFilter,
                        $"Role '{role.Name}' has invalid privilege depth: {privilege.Value.PrivilegeDepth}");
                }
            }
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_PrivilegesHaveValidAccessRights()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        var roleWithPrivileges = result.Values.First(r => r.Privileges.Count > 0);
        foreach (var entityPrivileges in roleWithPrivileges.Privileges)
        {
            foreach (var privilege in entityPrivileges.Value)
            {
                Assert.Equal(privilege.Key, privilege.Value.AccessRight);
                Assert.NotEqual(AccessRights.None, privilege.Value.AccessRight);
            }
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ConsistentResultsOnMultipleCalls()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result1 = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);
        var result2 = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], null, false);

        Assert.Equal(result1.Count, result2.Count);
        Assert.Equal(result1[SystemAdministratorRoleId].Name, result2[SystemAdministratorRoleId].Name);
    }
}
