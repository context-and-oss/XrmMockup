using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System.ServiceModel;

namespace XrmMockup.TestEnvProvisioner;

/// <summary>
/// Idempotent helpers for creating Dataverse metadata (entities, attributes, relationships) and
/// supporting records (publishers, roles). Every "Ensure*/Create*" method first checks for the
/// component and skips creation if it already exists, so the provisioner is safe to re-run.
/// </summary>
public sealed class MetadataBuilder(IOrganizationService service, bool whatIf)
{
    public IOrganizationService Service => service;
    public bool WhatIf => whatIf;

    /// <summary>
    /// When set, every entity/attribute/relationship created is added to this (unmanaged) solution,
    /// and roles can be added with <see cref="AddComponentToSolution"/>.
    /// </summary>
    public string? SolutionUniqueName { get; set; }

    public static Label L(string text) => new(text, 1033);

    private void Log(string msg) => Console.WriteLine((whatIf ? "[plan] " : "  ") + msg);

    // ---------------------------------------------------------------- Publishers / solutions

    /// <summary>Ensures the publisher exists and returns its id.</summary>
    public Guid EnsurePublisher(string uniqueName, string prefix, int optionValuePrefix)
    {
        var existing = service.RetrieveMultiple(new QueryExpression("publisher")
        {
            ColumnSet = new ColumnSet("publisherid"),
            Criteria = new FilterExpression { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName) } }
        });
        if (existing.Entities.Count > 0)
        {
            Log($"publisher '{uniqueName}' (prefix '{prefix}') already exists");
            return existing.Entities[0].Id;
        }
        Log($"create publisher '{uniqueName}' (prefix '{prefix}')");
        if (whatIf) return Guid.Empty;
        return service.Create(new Entity("publisher")
        {
            ["uniquename"] = uniqueName,
            ["friendlyname"] = uniqueName,
            ["customizationprefix"] = prefix,
            ["customizationoptionvalueprefix"] = optionValuePrefix
        });
    }

    /// <summary>
    /// Ensures an unmanaged solution exists for the given publisher and records it as the target
    /// solution (<see cref="SolutionUniqueName"/>) for all subsequently created components.
    /// If the solution already exists it is reused as-is (its publisher is not changed).
    /// </summary>
    public void EnsureSolution(string uniqueName, string friendlyName, Guid publisherId)
    {
        SolutionUniqueName = uniqueName;
        var existing = service.RetrieveMultiple(new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("solutionid"),
            Criteria = new FilterExpression { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName) } }
        });
        if (existing.Entities.Count > 0)
        {
            Log($"solution '{uniqueName}' already exists (components will be added to it)");
            return;
        }
        Log($"create unmanaged solution '{uniqueName}' (publisher {publisherId})");
        if (whatIf) return;
        service.Create(new Entity("solution")
        {
            ["uniquename"] = uniqueName,
            ["friendlyname"] = friendlyName,
            ["version"] = "1.0.0.0",
            ["publisherid"] = new EntityReference("publisher", publisherId),
        });
    }

    /// <summary>Adds an existing component (e.g. a security role) to the target solution.</summary>
    public void AddComponentToSolution(int componentType, Guid componentId)
    {
        if (string.IsNullOrEmpty(SolutionUniqueName)) return;
        Log($"add component (type {componentType}) {componentId} to solution '{SolutionUniqueName}'");
        if (whatIf) return;
        try
        {
            service.Execute(new AddSolutionComponentRequest
            {
                ComponentType = componentType,
                ComponentId = componentId,
                SolutionUniqueName = SolutionUniqueName,
                AddRequiredComponents = false,
            });
        }
        catch (FaultException ex)
        {
            // Most commonly: the component is already a member of the solution.
            Log($"  (skipped: {ex.Message})");
        }
    }

    // ---------------------------------------------------------------- Existence checks

    public bool EntityExists(string logicalName)
    {
        try
        {
            service.Execute(new RetrieveEntityRequest { LogicalName = logicalName, EntityFilters = EntityFilters.Entity });
            return true;
        }
        catch (FaultException) { return false; }
    }

    public bool AttributeExists(string entityLogicalName, string attributeLogicalName)
    {
        try
        {
            service.Execute(new RetrieveAttributeRequest { EntityLogicalName = entityLogicalName, LogicalName = attributeLogicalName });
            return true;
        }
        catch (FaultException) { return false; }
    }

    public bool RelationshipExists(string schemaName)
    {
        try
        {
            service.Execute(new RetrieveRelationshipRequest { Name = schemaName });
            return true;
        }
        catch (FaultException) { return false; }
    }

    // ---------------------------------------------------------------- Entities / attributes

    public void CreateEntity(string schemaName, string displayName, string displayCollectionName,
        OwnershipTypes ownership, StringAttributeMetadata primaryAttribute, bool hasActivities = false, bool hasNotes = false)
    {
        var logical = schemaName.ToLowerInvariant();
        if (EntityExists(logical))
        {
            Log($"entity '{logical}' already exists");
            return;
        }
        Log($"create entity '{schemaName}' (primary attr '{primaryAttribute.SchemaName}')");
        if (whatIf) return;
        service.Execute(new CreateEntityRequest
        {
            Entity = new EntityMetadata
            {
                SchemaName = schemaName,
                DisplayName = L(displayName),
                DisplayCollectionName = L(displayCollectionName),
                OwnershipType = ownership,
                IsActivity = false,
            },
            PrimaryAttribute = primaryAttribute,
            HasActivities = hasActivities,
            HasNotes = hasNotes,
            SolutionUniqueName = SolutionUniqueName,
        });
    }

    public void CreateAttribute(string entitySchemaName, AttributeMetadata attribute)
    {
        var entityLogical = entitySchemaName.ToLowerInvariant();
        var attrLogical = attribute.SchemaName!.ToLowerInvariant();
        if (AttributeExists(entityLogical, attrLogical))
        {
            Log($"attribute '{entityLogical}.{attrLogical}' already exists");
            return;
        }
        Log($"create attribute '{entityLogical}.{attribute.SchemaName}' ({attribute.GetType().Name})");
        if (whatIf) return;
        service.Execute(new CreateAttributeRequest { EntityName = entityLogical, Attribute = attribute, SolutionUniqueName = SolutionUniqueName });
    }

    // SourceType values for computed columns.
    public const int SourceCalculated = 1;  // classic calculated field   (FormulaDefinition = classic XAML)
    public const int SourceRollup = 2;       // rollup field               (FormulaDefinition = rollup XAML)
    public const int SourceFormula = 3;      // Power Fx formula column     (FormulaDefinition = Power Fx text)

    /// <summary>
    /// Creates a computed column (Power Fx formula, classic calculated, or rollup) by setting the
    /// attribute's SourceType and FormulaDefinition. XrmMockup evaluates all three on Retrieve/
    /// RetrieveMultiple. FormulaDefinition lives on the concrete typed attribute classes, not the
    /// AttributeMetadata base, so it is set via reflection. A blank definition is skipped with a hint
    /// (use --dump-formula to capture the XAML); an org rejection is logged non-fatally.
    /// </summary>
    public void CreateComputedColumn(string entitySchemaName, AttributeMetadata attribute, int sourceType, string formulaDefinition)
    {
        var kind = sourceType switch { SourceCalculated => "calculated", SourceRollup => "rollup", _ => "formula" };
        var entityLogical = entitySchemaName.ToLowerInvariant();
        var attrLogical = attribute.SchemaName!.ToLowerInvariant();
        if (AttributeExists(entityLogical, attrLogical))
        {
            Log($"{kind} column '{entityLogical}.{attrLogical}' already exists");
            return;
        }
        if (string.IsNullOrWhiteSpace(formulaDefinition))
        {
            Log($"  ! no definition supplied for {kind} column '{attribute.SchemaName}' — skipped. " +
                $"Capture one with --dump-formula <entity>.<attribute> and add it to TestSchema.");
            return;
        }

        attribute.SourceType = sourceType;
        var formulaProperty = attribute.GetType().GetProperty("FormulaDefinition");
        if (formulaProperty is null)
        {
            Log($"  ! attribute type {attribute.GetType().Name} has no FormulaDefinition; create '{attribute.SchemaName}' manually.");
            return;
        }
        formulaProperty.SetValue(attribute, formulaDefinition);

        Log($"create {kind} column '{entityLogical}.{attribute.SchemaName}'");
        if (whatIf) return;
        try
        {
            service.Execute(new CreateAttributeRequest { EntityName = entityLogical, Attribute = attribute, SolutionUniqueName = SolutionUniqueName });
        }
        catch (FaultException ex)
        {
            Log($"  ! could not auto-create {kind} column '{attribute.SchemaName}' ({ex.Message}); create it manually.");
        }
    }

    public void CreateFormulaColumn(string entity, AttributeMetadata attribute, string powerFx)
        => CreateComputedColumn(entity, attribute, SourceFormula, powerFx);

    public void CreateCalculatedColumn(string entity, AttributeMetadata attribute, string classicXaml)
        => CreateComputedColumn(entity, attribute, SourceCalculated, classicXaml);

    public void CreateRollupColumn(string entity, AttributeMetadata attribute, string rollupXaml)
        => CreateComputedColumn(entity, attribute, SourceRollup, rollupXaml);

    /// <summary>
    /// Prints the SourceType and FormulaDefinition (the classic calculated / rollup XAML, or the
    /// Power Fx text) of an existing attribute, so it can be captured and baked into TestSchema.
    /// </summary>
    public void DumpFormula(string entityLogicalName, string attributeLogicalName)
    {
        // Logical names are always lower-case in Dataverse; normalise so callers can pass any casing.
        entityLogicalName = entityLogicalName.ToLowerInvariant();
        attributeLogicalName = attributeLogicalName.ToLowerInvariant();

        AttributeMetadata attr;
        try
        {
            attr = ((RetrieveAttributeResponse)service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName,
            })).AttributeMetadata;
        }
        catch (FaultException ex)
        {
            Console.Error.WriteLine($"Could not read '{entityLogicalName}.{attributeLogicalName}': {ex.Message}");
            Console.Error.WriteLine("This tool does not create calculated/rollup columns until their XAML is supplied, " +
                "so create the example field in the maker portal first, then dump it.");
            return;
        }

        var sourceType = attr.SourceType;
        var formula = attr.GetType().GetProperty("FormulaDefinition")?.GetValue(attr) as string;
        Console.WriteLine($"Entity:        {entityLogicalName}");
        Console.WriteLine($"Attribute:     {attributeLogicalName} ({attr.GetType().Name})");
        Console.WriteLine($"SourceType:    {sourceType} (1=Calculated, 2=Rollup, 3=Formula/PowerFx)");
        Console.WriteLine("FormulaDefinition:");
        Console.WriteLine("-----------------------------------------------------------------------");
        Console.WriteLine(string.IsNullOrEmpty(formula) ? "(none)" : formula);
        Console.WriteLine("-----------------------------------------------------------------------");
    }

    // ---------------------------------------------------------------- Attribute factories

    public StringAttributeMetadata String(string schema, string display, int maxLength = 200,
        AttributeRequiredLevel required = AttributeRequiredLevel.None) => new()
        {
            SchemaName = schema,
            DisplayName = L(display),
            MaxLength = maxLength,
            FormatName = StringFormatName.Text,
            RequiredLevel = new AttributeRequiredLevelManagedProperty(required),
        };

    public IntegerAttributeMetadata WholeNumber(string schema, string display) => new()
    {
        SchemaName = schema,
        DisplayName = L(display),
        Format = IntegerFormat.None,
        MinValue = int.MinValue,
        MaxValue = int.MaxValue,
    };

    public DecimalAttributeMetadata Decimal(string schema, string display, int precision = 2) => new()
    {
        SchemaName = schema,
        DisplayName = L(display),
        Precision = precision,
        MinValue = -100000000000m,
        MaxValue = 100000000000m,
    };

    public MoneyAttributeMetadata Money(string schema, string display, int precision = 2) => new()
    {
        SchemaName = schema,
        DisplayName = L(display),
        PrecisionSource = 2, // 0 = fixed Precision below; 1 = org pricing precision; 2 = currency precision
        Precision = precision,
        MinValue = -1000000000d,
        MaxValue = 1000000000d,
    };

    public DateTimeAttributeMetadata DateTime(string schema, string display) => new()
    {
        SchemaName = schema,
        DisplayName = L(display),
        Format = DateTimeFormat.DateAndTime,
    };

    public PicklistAttributeMetadata Picklist(string schema, string display, params (int value, string label)[] options)
    {
        var set = new OptionSetMetadata { IsGlobal = false, OptionSetType = OptionSetType.Picklist };
        foreach (var (value, label) in options)
            set.Options.Add(new OptionMetadata(L(label), value));
        return new PicklistAttributeMetadata { SchemaName = schema, DisplayName = L(display), OptionSet = set };
    }

    public MultiSelectPicklistAttributeMetadata MultiSelect(string schema, string display, params (int value, string label)[] options)
    {
        var set = new OptionSetMetadata { IsGlobal = false, OptionSetType = OptionSetType.Picklist };
        foreach (var (value, label) in options)
            set.Options.Add(new OptionMetadata(L(label), value));
        return new MultiSelectPicklistAttributeMetadata { SchemaName = schema, DisplayName = L(display), OptionSet = set };
    }

    // ---------------------------------------------------------------- Relationships

    public void CreateLookup(string relationshipSchemaName, string referencedEntity, string referencingEntity,
        string lookupSchemaName, string lookupDisplayName)
    {
        if (RelationshipExists(relationshipSchemaName))
        {
            Log($"relationship '{relationshipSchemaName}' already exists");
            return;
        }
        Log($"create 1:N '{relationshipSchemaName}' ({referencedEntity} -> {referencingEntity}.{lookupSchemaName})");
        if (whatIf) return;
        service.Execute(new CreateOneToManyRequest
        {
            OneToManyRelationship = new OneToManyRelationshipMetadata
            {
                SchemaName = relationshipSchemaName,
                ReferencedEntity = referencedEntity,
                ReferencingEntity = referencingEntity,
            },
            Lookup = new LookupAttributeMetadata { SchemaName = lookupSchemaName, DisplayName = L(lookupDisplayName) },
            SolutionUniqueName = SolutionUniqueName,
        });
    }

    /// <summary>
    /// Updates an existing 1:N relationship's Assign/Reparent cascade behaviour (applies to relationships
    /// that already exist, which CreateLookup would otherwise skip). Used so reparenting a child to a
    /// parent owned by another user reassigns the child (TestParentChangeCascading).
    /// </summary>
    public void SetCascade(string relationshipSchemaName, CascadeType assign, CascadeType reparent)
    {
        Log($"set cascade on '{relationshipSchemaName}' (assign={assign}, reparent={reparent})");
        if (whatIf) return;
        try
        {
            var rel = (OneToManyRelationshipMetadata)((RetrieveRelationshipResponse)service.Execute(
                new RetrieveRelationshipRequest { Name = relationshipSchemaName })).RelationshipMetadata;
            rel.CascadeConfiguration.Assign = assign;
            rel.CascadeConfiguration.Reparent = reparent;
            service.Execute(new UpdateRelationshipRequest { Relationship = rel });
        }
        catch (FaultException ex)
        {
            Log($"  ! could not set cascade on '{relationshipSchemaName}' ({ex.Message})");
        }
    }

    public void CreateManyToMany(string relationshipSchemaName, string intersectName, string entity1, string entity2)
    {
        if (RelationshipExists(relationshipSchemaName))
        {
            Log($"relationship '{relationshipSchemaName}' already exists");
            return;
        }
        Log($"create N:N '{relationshipSchemaName}' ({entity1} <-> {entity2}, intersect '{intersectName}')");
        if (whatIf) return;
        service.Execute(new CreateManyToManyRequest
        {
            IntersectEntitySchemaName = intersectName,
            ManyToManyRelationship = new ManyToManyRelationshipMetadata
            {
                SchemaName = relationshipSchemaName,
                Entity1LogicalName = entity1,
                Entity2LogicalName = entity2,
            },
            SolutionUniqueName = SolutionUniqueName,
        });
    }

    /// <summary>
    /// Sets an existing money column's PrecisionSource to "currency precision" (2). CreateAttribute is
    /// idempotent and won't change an existing column, so this updates one in place.
    /// </summary>
    public void SetMoneyPrecisionToCurrency(string entitySchemaName, string attributeSchemaName)
    {
        var entityLogical = entitySchemaName.ToLowerInvariant();
        var attrLogical = attributeSchemaName.ToLowerInvariant();
        Log($"set '{entityLogical}.{attrLogical}' precision source to currency");
        if (whatIf) return;
        try
        {
            var attr = (MoneyAttributeMetadata)((RetrieveAttributeResponse)service.Execute(
                new RetrieveAttributeRequest { EntityLogicalName = entityLogical, LogicalName = attrLogical })).AttributeMetadata;
            if (attr.PrecisionSource == 2)
            {
                Log("  (already currency precision)");
                return;
            }
            attr.PrecisionSource = 2;
            service.Execute(new UpdateAttributeRequest { EntityName = entityLogical, Attribute = attr, MergeLabels = true });
        }
        catch (FaultException ex)
        {
            Log($"  ! could not update precision of '{entityLogical}.{attrLogical}' ({ex.Message})");
        }
    }

    // ---------------------------------------------------------------- Alternate keys

    /// <summary>
    /// Creates an alternate key on an entity over the given attribute(s). XrmContext generates a
    /// strongly-typed <c>Retrieve_&lt;keyname&gt;</c> helper per alternate key, so adding one here makes
    /// that helper appear after regeneration. Key creation triggers an async index job in Dataverse.
    /// </summary>
    public void CreateAlternateKey(string entitySchemaName, string keySchemaName, string keyDisplayName, params string[] keyAttributeLogicalNames)
    {
        var entityLogical = entitySchemaName.ToLowerInvariant();
        Log($"create alternate key '{keySchemaName}' on '{entityLogical}' ({string.Join(",", keyAttributeLogicalNames)})");
        if (whatIf) return;
        try
        {
            service.Execute(new CreateEntityKeyRequest
            {
                EntityName = entityLogical,
                EntityKey = new EntityKeyMetadata
                {
                    SchemaName = keySchemaName,
                    DisplayName = L(keyDisplayName),
                    KeyAttributes = keyAttributeLogicalNames,
                },
                SolutionUniqueName = SolutionUniqueName,
            });
        }
        catch (FaultException ex)
        {
            // Most commonly: the key already exists. Non-fatal.
            Log($"  ! could not create alternate key '{keySchemaName}' ({ex.Message})");
        }
    }

    // ---------------------------------------------------------------- State transitions

    /// <summary>
    /// Turns on enforced state transitions for an entity and sets the allowed status moves. Each
    /// tuple is (fromStatusValue, allowed toStatusValues); any move not listed (including same-state)
    /// becomes invalid. Best-effort + non-fatal: if the org rejects it, configure it in the portal.
    /// </summary>
    public void SetEnforcedStateTransitions(string entitySchemaName, params (int From, int[] To)[] transitions)
    {
        var entityLogical = entitySchemaName.ToLowerInvariant();
        Log($"enforce state transitions on '{entityLogical}' ({string.Join("; ", transitions.Select(t => $"{t.From}->[{string.Join(",", t.To)}]"))})");
        if (whatIf) return;
        try
        {
            // 1. Set per-status TransitionData on the statuscode options FIRST. Dataverse rejects
            //    enabling EnforceStateTransitions on an entity that has no transitions defined yet,
            //    so the transitions must exist before we flip the entity-level flag.
            var statusAttr = (StatusAttributeMetadata)((RetrieveAttributeResponse)service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogical,
                LogicalName = "statuscode",
            })).AttributeMetadata;

            var allowed = transitions.ToDictionary(t => t.From, t => t.To);
            foreach (StatusOptionMetadata option in statusAttr.OptionSet.Options.Cast<StatusOptionMetadata>())
            {
                var from = option.Value!.Value;
                var tos = allowed.TryGetValue(from, out var arr) ? arr : System.Array.Empty<int>();
                var xml = "<allowedtransitions>" +
                          string.Concat(tos.Select(to => $"<allowedtransition sourcestatusid=\"{from}\" tostatusid=\"{to}\" />")) +
                          "</allowedtransitions>";
                option.TransitionData = xml;
            }
            service.Execute(new UpdateAttributeRequest { EntityName = entityLogical, Attribute = statusAttr, MergeLabels = true });

            // 2. Now enable enforcement at the entity level.
            var em = ((RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
            {
                LogicalName = entityLogical,
                EntityFilters = EntityFilters.Entity,
            })).EntityMetadata;
            // EnforceStateTransitions has a non-public setter, so set it via reflection.
            typeof(EntityMetadata).GetProperty("EnforceStateTransitions")!.SetValue(em, true);
            service.Execute(new UpdateEntityRequest { Entity = em, MergeLabels = true });

            // 3. Publish so the change is committed (metadata regeneration reads published state),
            //    then read it back AS IF PUBLISHED so the log reflects the true committed value
            //    (a default retrieve returns the *published* definition and would show the stale
            //    pre-update value until PublishAll runs at the end of the program).
            service.Execute(new PublishXmlRequest
            {
                ParameterXml = $"<importexportxml><entities><entity>{entityLogical}</entity></entities></importexportxml>"
            });
            var verify = ((RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
            {
                LogicalName = entityLogical,
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true,
            })).EntityMetadata;
            Log($"  EnforceStateTransitions on '{entityLogical}' is now {verify.EnforceStateTransitions}");
        }
        catch (FaultException ex)
        {
            Log($"  ! could not set enforced state transitions on '{entityLogical}' ({ex.Message}); configure them in the maker portal.");
        }
    }

    // ---------------------------------------------------------------- Role privileges

    /// <summary>
    /// Grants a role the requested privilege types on an entity, at the given depth. Privilege ids
    /// are read from the entity's metadata (by <see cref="PrivilegeType"/>) rather than guessed by
    /// name, so this works identically for standard and custom entities.
    /// </summary>
    public void GrantEntityPrivileges(Guid roleId, string entityLogicalName, PrivilegeType[] types, PrivilegeDepth depth)
    {
        Log($"grant [{string.Join(",", types)}] on '{entityLogicalName}' @ {depth} to role {roleId}");
        if (whatIf) return;

        var meta = ((RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
        {
            LogicalName = entityLogicalName,
            EntityFilters = EntityFilters.Privileges,
        })).EntityMetadata;

        // Only add privileges the role doesn't already hold, so re-running against an existing role
        // reliably adds NEW grants (e.g. Assign) instead of failing the whole batch on duplicates.
        var existing = ((RetrieveRolePrivilegesRoleResponse)service.Execute(
            new RetrieveRolePrivilegesRoleRequest { RoleId = roleId })).RolePrivileges
            .Select(p => p.PrivilegeId).ToHashSet();

        var grants = meta.Privileges
            .Where(p => types.Contains(p.PrivilegeType) && !existing.Contains(p.PrivilegeId))
            .Select(p => new RolePrivilege { PrivilegeId = p.PrivilegeId, Depth = depth })
            .ToArray();

        if (grants.Length == 0)
        {
            Log($"  ({string.Join(",", types)} on '{entityLogicalName}' already granted)");
            return;
        }
        try
        {
            service.Execute(new AddPrivilegesRoleRequest { RoleId = roleId, Privileges = grants });
        }
        catch (FaultException ex)
        {
            // Non-fatal: e.g. a privilege that rejects the requested depth. Log and keep going so one
            // bad grant doesn't abort the whole provisioning run.
            Log($"  ! could not grant [{string.Join(",", types)}] on '{entityLogicalName}' @ {depth} ({ex.Message})");
        }
    }

    // ---------------------------------------------------------------- Publish

    public void PublishAll()
    {
        Log("publish all customizations");
        if (whatIf) return;
        service.Execute(new PublishAllXmlRequest());
    }
}
