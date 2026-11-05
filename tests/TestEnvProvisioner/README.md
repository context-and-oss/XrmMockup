# TestEnvProvisioner

Recreates the **custom test entities, relationships, option sets and security roles** that the
XrmMockup test suite depends on, into a target Dataverse environment. After running it you
regenerate metadata + context (`scripts/Regenerate-TestMetadata.ps1`) and the dependent tests can
be migrated / un-skipped.

It lives under `tests/` because it's part of the test suite, but it is **not** in `XrmMockup.slnx`
and is **not** a test project — it's an on-demand console tool run against a live org. It
authenticates exactly like the metadata generator (the `DataverseConnection` package, reading
`DataverseUrl` and `DataverseCredentialType`).

## Run

```bash
# Preview the plan without writing anything:
dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json --whatif

# Create the components:
dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json
```

`--config` points at an appsettings.json containing `DataverseUrl` (the same file the metadata
generator uses). Alternatively set the `DataverseUrl` environment variable and omit `--config`, or
pass `--dataverse-url <url>`. The legacy `DATAVERSE_URL` key still works. The tool is idempotent —
it skips components that already exist, so it is safe to re-run.

### Authentication

Authentication defaults to an interactive browser sign-in (DataverseConnection's `browser`
credential). Override it with `--credential-type`, or the `DataverseCredentialType` setting:

```bash
# No browser available (SSH, container):
dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json --credential-type devicecode

# Reuse an existing `az login` session:
dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json --credential-type azcli
```

Valid values are `browser` (default), `devicecode` and `azcli` — see the
[DataverseConnection configuration](https://github.com/context-and-oss/DataverseConnection#configuration)
docs. CLI overrides take precedence over the config file and environment variables.

### Solution membership

Everything created (entities, attributes, relationships, and the role records) is added to an
unmanaged **solution** so that metadata regeneration and solution export pick it up. The solution
unique name defaults to `XrmMockup`; override with `--solution <uniquename>`. The solution is
created under the `ctx` publisher (`ContextAnd`) if it doesn't exist.

> The solution's publisher must own the `ctx` prefix. If you point `--solution` at an existing
> solution whose publisher uses a different prefix, creating `ctx_*` components in it will be
> rejected by Dataverse — use a solution backed by the `ctx`/`ContextAnd` publisher.

After running, make sure the regeneration config (`tests/appsettings.json`) will include these:
add `ctx_parent` and `ctx_child` to its `XrmMockup.Metadata.Entities` (and `XrmContext.Entities`)
list, or switch that config to a solution-scoped pull.

## What it creates (scripted)

Everything uses the **`ctx`** customization prefix, and is kept to **two** entities (a parent/child
pair) — extra field *types* are added as columns on `ctx_parent` rather than as extra entities.
Display names omit the prefix.

- **Publisher**: prefix `ctx`.
- **`ctx_parent`** (user-owned) — the rich, do-everything test entity:
  - `ctx_Name` (primary), `ctx_Amount` (money, +`_base`), `ctx_WholeNumber` (int),
    `ctx_DateValue` (datetime), `ctx_Documenttypes` (**multiselect** Doc/PDF),
    `ctx_Postalcode` (string), `ctx_Score` (int), `ctx_Industrycode` (picklist).
  - Lookups to **account** (`ctx_AccountId`) and **contact** (`ctx_ContactId`) — the join targets
    that replace the old Lead parent lookups in the RetrieveMultiple query tests.
- **`ctx_child`** (user-owned): `ctx_Name`, `ctx_Allowance` (money); lookups to `ctx_parent`
  (`ctx_RollupParentId` rollup source, `ctx_ParentId` cascade/security parent).
- **Relationships**: the lookups above; the `ctx_parent_child` (ctx_parent ↔ ctx_child) and
  `ctx_account_contact` (account ↔ contact) **N:N** relationships.
- **Power Fx formula columns** on `ctx_parent` (`ctx_AmountCalc` = `Decimal(ctx_amount) * 20`,
  `ctx_WholeNumberCalc` = `ctx_wholenumber - 2`, `ctx_DateCalc` = `DateAdd(ctx_datevalue, 2)`,
  `ctx_TrimLeft` = `Mid(ctx_name, 3)`). XrmMockup evaluates PowerFx formula fields, so these are
  created automatically (with a non-fatal fallback message if the org rejects a definition).
- A classic **Calculated** field (`ctx_AmountCalcClassic`) and the **Rollup** fields
  (`ctx_TotalAllowance`/`Max`/`Min`/`Avg`). These are a different engine from Power Fx (XrmMockup
  supports both), and their definition is XAML. They're created automatically **once their XAML is
  supplied** — see *Capturing calculated/rollup XAML* below; until then they're skipped with a hint.
- **Security roles** (three, named as part of the suite, **with privilege grants applied** so no
  manual hook-up is needed — privilege ids are read from each entity's metadata by `PrivilegeType`,
  so it works for custom entities too):
  - **`XrmMockup Test User`** — user-level CRUD on contact + the custom entities (ownership tests).
  - **`XrmMockup Test No Contact Access`** — functional, but no contact privileges (denied-then-
    granted-via-team test).
  - **`XrmMockup Test Read Only`** — org read on `ctx_parent` (RetrievePrincipalAccess) + user-level
    contact read (sharing); also the catch-all "assign some role to a user" role.

## What it does NOT script (verify in the maker portal)

- **Enforced state transitions** on `ctx_parent` (Active → Inactive only). The non-enforced case is
  covered by `account`, so no separate entity is needed.

Everything else — entities, fields, relationships, **Power Fx formula columns**, and security roles
**with their privilege grants** — is created automatically. The classic **Calculated** and **Rollup**
fields are created automatically too, once you've supplied their XAML (below).

## Capturing calculated/rollup XAML

The classic Calculated field and the Rollup fields need a XAML `FormulaDefinition`, which is risky
to hand-author. The flow to get it scripted:

1. Run the provisioner once (creates `ctx_parent`/`ctx_child` and the columns the formulas reference).
2. In the maker portal, create one example calculated field and one rollup field on `ctx_parent`
   matching the test formulas.
3. Capture each definition:
   ```bash
   dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json \
       --dump-formula ctx_parent.<attributelogicalname>
   ```
   This prints the `SourceType` and the raw `FormulaDefinition` (the XAML).
4. Paste the XAML into the constants in `TestSchema.Definitions`.
5. Re-run the provisioner — the calculated/rollup columns are now created automatically.

(The SDK alternative is `RetrieveAttributeRequest` + reading `FormulaDefinition` off the typed
attribute metadata; the Web API alternative is
`GET .../attributes(LogicalName='<attr>')/Microsoft.Dynamics.CRM.<Type>AttributeMetadata?$select=SourceType,FormulaDefinition`.)
