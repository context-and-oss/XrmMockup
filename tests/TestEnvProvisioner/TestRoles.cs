using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace XrmMockup.TestEnvProvisioner;

/// <summary>
/// Creates the (minimal) custom security roles the security/privilege tests need, names them as
/// part of the XrmMockup test suite, and grants the privileges each test actually exercises — so a
/// developer can stand up the environment without hooking up any roles by hand.
///
/// Four focused roles cover every requirement (the old set had seven; two were only referenced by
/// commented-out tests, and two more were merely "some role to assign to a user" and collapse into
/// the read-only role):
///   • Test User            — user-level CRUD on contact + the custom entities (ownership tests).
///   • Test No Contact Access — functional, but no contact access (denied-then-granted-via-team test).
///   • Test Read Only       — org read on the custom entity + user-level contact read (principal
///                            access + sharing), and the catch-all "assign any role" placeholder.
///   • Test Access Team     — full contact + account privileges (incl. Assign/Share); UnitTestBase
///                            clones it (and downgrades to Basic) to build the access-team roles,
///                            so the suite need not clone System Administrator.
///
/// Regeneration emits a SecurityRoles.&lt;sanitized name&gt; constant per role; the (migrated) tests
/// bind to those names.
/// </summary>
public static class TestRoles
{
    private static readonly PrivilegeType[] Crud =
    {
        PrivilegeType.Create, PrivilegeType.Read, PrivilegeType.Write,
        PrivilegeType.Delete, PrivilegeType.Append, PrivilegeType.AppendTo,
    };
    private static readonly PrivilegeType[] ReadOnly = { PrivilegeType.Read };
    private static readonly PrivilegeType[] CrudAssign =
    {
        PrivilegeType.Create, PrivilegeType.Read, PrivilegeType.Write, PrivilegeType.Delete,
        PrivilegeType.Append, PrivilegeType.AppendTo, PrivilegeType.Assign,
    };
    // Own/assignee access without Create (so a record can be owned/assigned but not created).
    private static readonly PrivilegeType[] OwnNoCreate =
    {
        PrivilegeType.Read, PrivilegeType.Write, PrivilegeType.Assign, PrivilegeType.Append, PrivilegeType.AppendTo,
    };
    private static readonly PrivilegeType[] ReadCreate = { PrivilegeType.Read, PrivilegeType.Create };
    private static readonly PrivilegeType[] FullAccess =
    {
        PrivilegeType.Create, PrivilegeType.Read, PrivilegeType.Write, PrivilegeType.Delete,
        PrivilegeType.Append, PrivilegeType.AppendTo, PrivilegeType.Assign, PrivilegeType.Share,
    };

    private sealed record Grant(string Entity, PrivilegeType[] Types, PrivilegeDepth Depth);
    private sealed record RoleDef(string Name, string Purpose, Grant[] Grants);

    // Baseline every test role needs to function: read reference data (self / BU / team / currency).
    // These reference-data read privileges (e.g. prvReadUser) reject Basic depth, so they are granted
    // at Global — harmless for a test user and not asserted against by any test.
    private static Grant[] Baseline() =>
    [
        new Grant("systemuser", ReadOnly, PrivilegeDepth.Global),
        new Grant("businessunit", ReadOnly, PrivilegeDepth.Global),
        new Grant("team", ReadOnly, PrivilegeDepth.Global),
        new Grant("transactioncurrency", ReadOnly, PrivilegeDepth.Global),
    ];

    private static readonly RoleDef[] Roles =
    [
        new RoleDef("XrmMockup Test User",
            "User-level CRUD (+Assign) on contact + custom entities; user-level own/assign on account " +
            "(but NO account create, so creating one is still denied); user-level CRUD on annotation. " +
            "Used by the ownership/assign/cascade security tests (can act on own records, not others').",
            [
                .. Baseline(),
                new Grant("contact", CrudAssign, PrivilegeDepth.Basic),
                new Grant(TestSchema.Names.ParentEntity, CrudAssign, PrivilegeDepth.Basic),
                new Grant(TestSchema.Names.ChildEntity, CrudAssign, PrivilegeDepth.Basic),
                new Grant("account", OwnNoCreate, PrivilegeDepth.Basic),   // can own/be-assigned accounts (cascade), not create
                new Grant("annotation", Crud, PrivilegeDepth.Basic),        // impersonation/notes test
            ]),

        new RoleDef("XrmMockup Test No Contact Access",
            "Functional on account + custom entities but has NO contact privileges; used to verify a " +
            "user/team without contact access is denied, then succeeds once granted contact access via a team.",
            [
                .. Baseline(),
                new Grant("account", Crud, PrivilegeDepth.Basic),
                new Grant(TestSchema.Names.ParentEntity, Crud, PrivilegeDepth.Basic),
                new Grant(TestSchema.Names.ChildEntity, Crud, PrivilegeDepth.Basic),
            ]),

        new RoleDef("XrmMockup Test Read Only",
            "Organization read on the custom entity (RetrievePrincipalAccess returns ReadAccess) plus " +
            "user-level read on contact (sharing test: cannot read others' until shared). Also the " +
            "catch-all role for tests that just need to assign some role to a user.",
            [
                .. Baseline(),
                new Grant(TestSchema.Names.ParentEntity, ReadOnly, PrivilegeDepth.Global),
                new Grant("contact", ReadOnly, PrivilegeDepth.Basic),
                new Grant("account", ReadCreate, PrivilegeDepth.Global), // RetrievePrincipalAccess on account expects Read|Create
            ]),

        new RoleDef("XrmMockup Test Access Team",
            "Source role that UnitTestBase clones (then downgrades contact/account to Basic) to build the " +
            "access-team test roles. Full privilege set (incl. Assign/Share) on contact + account at BU " +
            "level, plus the custom entities (so AccountPostPlugin's ctx_parent side-records succeed when " +
            "these users create accounts). The suite no longer clones System Administrator.",
            [
                .. Baseline(),
                new Grant("contact", FullAccess, PrivilegeDepth.Local),
                new Grant("account", FullAccess, PrivilegeDepth.Local),
                new Grant(TestSchema.Names.ParentEntity, FullAccess, PrivilegeDepth.Global),
                new Grant(TestSchema.Names.ChildEntity, FullAccess, PrivilegeDepth.Global),
            ]),
    ];

    public static void CreateRoles(MetadataBuilder b)
    {
        var rootBuId = GetRootBusinessUnitId(b.Service);

        foreach (var role in Roles)
        {
            var existing = b.Service.RetrieveMultiple(new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, role.Name),
                        new ConditionExpression("businessunitid", ConditionOperator.Equal, rootBuId),
                    }
                }
            });

            Guid roleId;
            if (existing.Entities.Count > 0)
            {
                // Re-apply grants on re-run (idempotent via GrantEntityPrivileges' try/catch); this
                // also repairs a role left grant-less by an earlier aborted run.
                Console.WriteLine($"  role '{role.Name}' already exists (re-applying grants)");
                roleId = existing.Entities[0].Id;
            }
            else
            {
                Console.WriteLine($"  create role '{role.Name}' ({role.Purpose})");
                if (b.WhatIf)
                {
                    foreach (var g in role.Grants)
                        Console.WriteLine($"      grant [{string.Join(",", g.Types)}] on '{g.Entity}' @ {g.Depth}");
                    continue;
                }
                roleId = b.Service.Create(new Entity("role")
                {
                    ["name"] = role.Name,
                    ["businessunitid"] = new EntityReference("businessunit", rootBuId),
                });
            }

            foreach (var g in role.Grants)
                b.GrantEntityPrivileges(roleId, g.Entity, g.Types, g.Depth);

            // Add the role to the target solution (solution component type 20 = Role).
            b.AddComponentToSolution(20, roleId);
        }
    }

    private static Guid GetRootBusinessUnitId(IOrganizationService service)
    {
        var bus = service.RetrieveMultiple(new QueryExpression("businessunit")
        {
            ColumnSet = new ColumnSet("businessunitid"),
            Criteria = new FilterExpression
            {
                Conditions = { new ConditionExpression("parentbusinessunitid", ConditionOperator.Null) }
            }
        });
        return bus.Entities[0].Id;
    }
}
