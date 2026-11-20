using Microsoft.Xrm.Sdk.Metadata;

namespace XrmMockup.TestEnvProvisioner;

/// <summary>
/// Definitions of the custom test schema the XrmMockup suite needs, kept to the minimum number of
/// entities: one rich entity (ctx_parent) carrying every field type the tests exercise, and one
/// child entity (ctx_child) for rollups / N:N / cascade-security. Extra field *types* are added as
/// columns on ctx_parent rather than as extra entities. Everything uses the "ctx" prefix.
///
/// Every schema name is declared once as a constant in <see cref="Names"/> and referenced from
/// there, so a name used in more than one place (entity names inside relationships, lookup
/// attribute names, etc.) cannot drift via a typo.
///
/// Naming: the two entities sit in a parent/child relationship (ctx_child rolls up to, cascades
/// from, and is N:N-linked to ctx_parent). Display names deliberately omit the prefix. Schema names
/// match the C# property names the (migrated) tests will use — XrmContext derives the generated
/// property from the schema name — so the tests bind unchanged.
/// </summary>
public static class TestSchema
{
    /// <summary>Single source of truth for every custom schema name created by the provisioner.</summary>
    public static class Names
    {
        // Standard entities referenced in relationships.
        public const string Account = "account";
        public const string Contact = "contact";

        // Custom entities.
        public const string ParentEntity = "ctx_parent";
        public const string ChildEntity = "ctx_child";

        // Shared primary-name attribute (both custom entities use the same logical name).
        public const string Name = "ctx_Name";

        // ctx_parent fields.
        public const string Amount = "ctx_Amount";
        public const string WholeNumber = "ctx_WholeNumber";
        public const string DateValue = "ctx_DateValue";
        public const string DocumentTypes = "ctx_Documenttypes";
        public const string PostalCode = "ctx_Postalcode";
        public const string Score = "ctx_Score";
        public const string IndustryCode = "ctx_Industrycode";
        public const string AccountLookup = "ctx_AccountId";
        public const string ContactLookup = "ctx_ContactId";

        // ctx_parent Power Fx formula columns (auto-created; reference the columns above).
        public const string AmountCalc = "ctx_AmountCalc";
        public const string WholeNumberCalc = "ctx_WholeNumberCalc";
        public const string DateCalc = "ctx_DateCalc";
        public const string TrimLeft = "ctx_TrimLeft";

        // ctx_parent classic CALCULATED field (different engine from Power Fx — XrmMockup supports both).
        public const string AmountCalcClassic = "ctx_AmountCalcClassic";

        // ctx_parent ROLLUP fields (aggregate ctx_child.ctx_Allowance via the ctx_RollupParentId lookup).
        public const string TotalAllowance = "ctx_TotalAllowance";
        public const string MaxAllowance = "ctx_MaxAllowance";
        public const string MinAllowance = "ctx_MinAllowance";
        public const string AvgAllowance = "ctx_AvgAllowance";

        // ctx_child fields.
        public const string Allowance = "ctx_Allowance";
        public const string RollupParentLookup = "ctx_RollupParentId";
        public const string ParentLookup = "ctx_ParentId";

        // Relationships (1:N).
        public const string RelAccountParent = "ctx_account_ctx_parent";
        public const string RelContactParent = "ctx_contact_ctx_parent";
        public const string RelParentChildRollup = "ctx_parent_ctx_child_rollup";
        public const string RelParentChildCascade = "ctx_parent_ctx_child_cascade";

        // Relationships (N:N) and their intersect entities.
        public const string NnParentChild = "ctx_parent_child";
        public const string NnParentChildIntersect = "ctx_parent_ctx_child";
        public const string NnAccountContact = "ctx_account_contact";
        public const string NnAccountContactIntersect = "ctx_account_contact";

        // Alternate key on ctx_parent over ctx_Name (drives the generated Retrieve_<key> helper).
        public const string NameKey = "ctx_NameKey";
    }

    /// <summary>
    /// Alternate keys. Each one makes XrmContext emit a strongly-typed Retrieve_&lt;key&gt; helper after
    /// regeneration, so the alternate-key retrieve test can use it. Call after <see cref="CreateEntities"/>.
    /// </summary>
    public static void CreateAlternateKeys(MetadataBuilder b)
    {
        b.CreateAlternateKey(Names.ParentEntity, Names.NameKey, "Name key", Names.Name.ToLowerInvariant());
    }

    public static void CreateEntities(MetadataBuilder b)
    {
        // ---- ctx_parent : the one rich, user-owned entity covering money/currency, calculated &
        //      rollup fields, multiselect, lookups to account+contact, and assorted scalar types for
        //      the query-operator tests. It also carries enforced state transitions (see manual steps).
        b.CreateEntity(Names.ParentEntity, "Parent", "Parents", OwnershipTypes.UserOwned,
            b.String(Names.Name, "Name", 200, AttributeRequiredLevel.None));

        b.CreateAttribute(Names.ParentEntity, b.Money(Names.Amount, "Amount"));               // -> ctx_amount (+ _base auto) : currency base-calc
        b.SetMoneyPrecisionToCurrency(Names.ParentEntity, Names.Amount);                      // update existing column to currency precision
        b.CreateAttribute(Names.ParentEntity, b.WholeNumber(Names.WholeNumber, "Whole number")); // source for whole-number rollups/formulas
        b.CreateAttribute(Names.ParentEntity, b.DateTime(Names.DateValue, "Date value"));     // datetime field + formula source
        b.CreateAttribute(Names.ParentEntity, b.MultiSelect(Names.DocumentTypes, "Document types",
            (1, "Doc"), (2, "PDF")));                                                         // multiselect field

        // Scalar fields for the query-operator tests (string / int / optionset Equal, etc.).
        b.CreateAttribute(Names.ParentEntity, b.String(Names.PostalCode, "Postal code", 40));
        b.CreateAttribute(Names.ParentEntity, b.WholeNumber(Names.Score, "Score"));
        b.CreateAttribute(Names.ParentEntity, b.Picklist(Names.IndustryCode, "Industry",
            (1, "Accounting"), (2, "Agriculture"), (3, "Consulting")));

        // ---- ctx_child : children of ctx_parent (rollup sources + cascade/security parent lookup + N:N partner).
        b.CreateEntity(Names.ChildEntity, "Child", "Children", OwnershipTypes.UserOwned,
            b.String(Names.Name, "Name", 200, AttributeRequiredLevel.None));
        b.CreateAttribute(Names.ChildEntity, b.Money(Names.Allowance, "Allowance"));
        b.SetMoneyPrecisionToCurrency(Names.ChildEntity, Names.Allowance);
    }

    public static void CreateRelationships(MetadataBuilder b)
    {
        // ctx_parent -> account / contact lookups: the join targets that replace the old Lead
        // parentaccountid / parentcontactid in the RetrieveMultiple query tests.
        b.CreateLookup(Names.RelAccountParent, Names.Account, Names.ParentEntity, Names.AccountLookup, "Account");
        b.CreateLookup(Names.RelContactParent, Names.Contact, Names.ParentEntity, Names.ContactLookup, "Contact");

        // ctx_child -> ctx_parent : the rollup-source lookup (ctx_RollupParentId) and the
        // cascade/security parent lookup (ctx_ParentId).
        b.CreateLookup(Names.RelParentChildRollup, Names.ParentEntity, Names.ChildEntity, Names.RollupParentLookup, "Rollup parent");
        b.CreateLookup(Names.RelParentChildCascade, Names.ParentEntity, Names.ChildEntity, Names.ParentLookup, "Parent");
        // Reparenting a child to a parent owned by another user should reassign the child (cascade).
        b.SetCascade(Names.RelParentChildCascade, Microsoft.Xrm.Sdk.Metadata.CascadeType.Cascade, Microsoft.Xrm.Sdk.Metadata.CascadeType.Cascade);

        // ctx_parent <-> ctx_child N:N (associate/disassociate + the sync plugin).
        b.CreateManyToMany(Names.NnParentChild, Names.NnParentChildIntersect, Names.ParentEntity, Names.ChildEntity);

        // account <-> contact N:N (TestContext.TestContextIntersectEntity + association tests).
        b.CreateManyToMany(Names.NnAccountContact, Names.NnAccountContactIntersect, Names.Account, Names.Contact);
    }

    /// <summary>
    /// Classic-calculated and rollup XAML FormulaDefinitions. These are intricate to author by hand,
    /// so they start empty (the matching columns are then skipped). To fill them: create one example
    /// of each in the maker portal on ctx_parent, capture its definition with
    ///   dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json --dump-formula ctx_parent.&lt;attr&gt;
    /// and paste the XAML here. The next run then creates the column automatically.
    /// </summary>
    public static class Definitions
    {
        // Classic Calculated (Currency) = ctx_amount * 20. Captured from a manually-created field via
        // --dump-formula ctx_parent.ctx_amountcalcclassic. References logical names ctx_amount /
        // ctx_amountcalcclassic on ctx_parent, which match this schema.

        // classic Calculated (Currency) = ctx_Amount * 20
        public const string AmountCalcClassicXaml = """<?xml version="1.0" encoding="utf-16"?><Activity x:Class="XrmWorkflow00000000000000000000000000000000" xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities" xmlns:mcwc="clr-namespace:Microsoft.Crm.Workflow.ClientActivities;assembly=Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxs="clr-namespace:Microsoft.Xrm.Sdk;assembly=Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxswa="clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:s="clr-namespace:System;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:sco="clr-namespace:System.Collections.ObjectModel;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:srs="clr-namespace:System.Runtime.Serialization;assembly=System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:this="clr-namespace:" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><x:Members><x:Property Name="InputEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /><x:Property Name="CreatedEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /></x:Members><this:XrmWorkflow00000000000000000000000000000000.InputEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.InputEntities><this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><mva:VisualBasic.Settings>Assembly references and imported namespaces for internal implementation</mva:VisualBasic.Settings><mxswa:Workflow><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.ConditionSequence, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="ConditionStep1"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:Boolean" x:Key="Wait">False</InArgument></mxswa:ActivityReference.Arguments><mxswa:ActivityReference.Properties><sco:Collection x:TypeArguments="Variable" x:Key="Variables"><Variable x:TypeArguments="x:Boolean" Default="False" Name="True" /><Variable x:TypeArguments="x:Object" Name="ConditionBranchStep2_1" /></sco:Collection><sco:Collection x:TypeArguments="Activity" x:Key="Activities"><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">CreateCrmType</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.Boolean, "true" }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="x:Boolean" /></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[ConditionBranchStep2_1]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.ConditionBranch, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="ConditionBranchStep2"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:Boolean" x:Key="Condition">[True]</InArgument></mxswa:ActivityReference.Arguments><mxswa:ActivityReference.Properties><mxswa:ActivityReference x:Key="Then" AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.Composite, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="ConditionBranchStep2"><mxswa:ActivityReference.Properties><sco:Collection x:TypeArguments="Variable" x:Key="Variables" /><sco:Collection x:TypeArguments="Activity" x:Key="Activities"><Sequence DisplayName="SetAttributeValueStep4"><Sequence.Variables><Variable x:TypeArguments="x:Object" Name="SetAttributeValueStep4_1" /><Variable x:TypeArguments="x:Object" Name="SetAttributeValueStep4_2" /><Variable x:TypeArguments="x:Object" Name="SetAttributeValueStep4_3" /><Variable x:TypeArguments="x:Object" Name="SetAttributeValueStep4_4" /><Variable x:TypeArguments="x:Object" Name="SetAttributeValueStep4_5" /></Sequence.Variables><Assign x:TypeArguments="mxs:Entity" To="[CreatedEntities(&quot;primaryEntity#Temp&quot;)]" Value="[New Entity(&quot;ctx_parent&quot;)]" /><Assign x:TypeArguments="s:Guid" To="[CreatedEntities(&quot;primaryEntity#Temp&quot;).Id]" Value="[InputEntities(&quot;primaryEntity&quot;).Id]" /><mxswa:GetEntityProperty Attribute="ctx_amount" Entity="[InputEntities(&quot;primaryEntity&quot;)]" EntityName="ctx_parent" Value="[SetAttributeValueStep4_3]"><mxswa:GetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="mxs:Money" /></InArgument></mxswa:GetEntityProperty.TargetType></mxswa:GetEntityProperty><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">SelectFirstNonNull</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { SetAttributeValueStep4_3 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="mxs:Money" /></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[SetAttributeValueStep4_2]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">CreateCrmType</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.Integer, "20" }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="mxs:Money" /></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[SetAttributeValueStep4_5]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">SelectFirstNonNull</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { SetAttributeValueStep4_5 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="mxs:Money" /></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[SetAttributeValueStep4_4]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">Multiply</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { SetAttributeValueStep4_2, SetAttributeValueStep4_4 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="mxs:Money" /></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[SetAttributeValueStep4_1]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference><mxswa:SetEntityProperty Attribute="ctx_amountcalcclassic" Entity="[CreatedEntities(&quot;primaryEntity#Temp&quot;)]" EntityName="ctx_parent" Value="[SetAttributeValueStep4_1]"><mxswa:SetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type"><mxswa:ReferenceLiteral x:TypeArguments="s:Type" Value="mxs:Money" /></InArgument></mxswa:SetEntityProperty.TargetType></mxswa:SetEntityProperty><mcwc:SetAttributeValue DisplayName="SetAttributeValueStep4" Entity="[CreatedEntities(&quot;primaryEntity#Temp&quot;)]" EntityName="ctx_parent" /><Assign x:TypeArguments="mxs:Entity" To="[InputEntities(&quot;primaryEntity&quot;)]" Value="[CreatedEntities(&quot;primaryEntity#Temp&quot;)]" /></Sequence></sco:Collection></mxswa:ActivityReference.Properties></mxswa:ActivityReference><x:Null x:Key="Else" /><x:Null x:Key="Description" /></mxswa:ActivityReference.Properties></mxswa:ActivityReference></sco:Collection><x:Boolean x:Key="ContainsElseBranch">False</x:Boolean></mxswa:ActivityReference.Properties></mxswa:ActivityReference></mxswa:Workflow></Activity>""";
        // Rollup (Currency, SUM) over ctx_child.ctx_Allowance
        public const string TotalAllowanceXaml = """<?xml version="1.0" encoding="utf-16"?><Activity x:Class="XrmWorkflow00000000000000000000000000000000" xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities" xmlns:mcwc="clr-namespace:Microsoft.Crm.Workflow.ClientActivities;assembly=Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxs="clr-namespace:Microsoft.Xrm.Sdk;assembly=Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxswa="clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:s="clr-namespace:System;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:srs="clr-namespace:System.Runtime.Serialization;assembly=System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:this="clr-namespace:" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><x:Members><x:Property Name="InputEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /><x:Property Name="CreatedEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /></x:Members><this:XrmWorkflow00000000000000000000000000000000.InputEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.InputEntities><this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><mva:VisualBasic.Settings>Assembly references and imported namespaces for internal implementation</mva:VisualBasic.Settings><mxswa:Workflow><Sequence DisplayName="RollupRuleStep1"><Sequence DisplayName="Source"><Sequence.Variables><Variable x:TypeArguments="x:String" Default="[Nothing]" Name="HierarchicalRelationshipName" /></Sequence.Variables></Sequence><Sequence DisplayName="Target"><mcwc:SetAttributeValue DisplayName="ctx_child.ctx_parentid.ctx_parent_ctx_child_cascade" Entity="[CreatedEntities(&quot;relatedlinked_ctx_parent_ctx_child_cascade#ctx_parentid#ctx_child#Temp&quot;)]" EntityName="ctx_child" /></Sequence><Sequence DisplayName="Aggregate"><Sequence.Variables><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_1" /><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_2" /></Sequence.Variables><mxswa:GetEntityProperty Attribute="ctx_allowance" Entity="[InputEntities(&quot;primaryEntity&quot;)]" EntityName="ctx_child" Value="[RollupRuleStep1_2]"><mxswa:GetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument></mxswa:GetEntityProperty.TargetType></mxswa:GetEntityProperty><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">Sum</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { RollupRuleStep1_2 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[RollupRuleStep1_1]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference></Sequence></Sequence></mxswa:Workflow></Activity>""";
        // Rollup (Currency, MAX)
        public const string MaxAllowanceXaml = """<?xml version="1.0" encoding="utf-16"?><Activity x:Class="XrmWorkflow00000000000000000000000000000000" xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities" xmlns:mcwc="clr-namespace:Microsoft.Crm.Workflow.ClientActivities;assembly=Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxs="clr-namespace:Microsoft.Xrm.Sdk;assembly=Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxswa="clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:s="clr-namespace:System;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:srs="clr-namespace:System.Runtime.Serialization;assembly=System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:this="clr-namespace:" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><x:Members><x:Property Name="InputEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /><x:Property Name="CreatedEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /></x:Members><this:XrmWorkflow00000000000000000000000000000000.InputEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.InputEntities><this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><mva:VisualBasic.Settings>Assembly references and imported namespaces for internal implementation</mva:VisualBasic.Settings><mxswa:Workflow><Sequence DisplayName="RollupRuleStep1"><Sequence DisplayName="Source"><Sequence.Variables><Variable x:TypeArguments="x:String" Default="[Nothing]" Name="HierarchicalRelationshipName" /></Sequence.Variables></Sequence><Sequence DisplayName="Target"><mcwc:SetAttributeValue DisplayName="ctx_child.ctx_parentid.ctx_parent_ctx_child_cascade" Entity="[CreatedEntities(&quot;relatedlinked_ctx_parent_ctx_child_cascade#ctx_parentid#ctx_child#Temp&quot;)]" EntityName="ctx_child" /></Sequence><Sequence DisplayName="Aggregate"><Sequence.Variables><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_1" /><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_2" /></Sequence.Variables><mxswa:GetEntityProperty Attribute="ctx_allowance" Entity="[InputEntities(&quot;primaryEntity&quot;)]" EntityName="ctx_child" Value="[RollupRuleStep1_2]"><mxswa:GetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument></mxswa:GetEntityProperty.TargetType></mxswa:GetEntityProperty><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">Maximum</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { RollupRuleStep1_2 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[RollupRuleStep1_1]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference></Sequence></Sequence></mxswa:Workflow></Activity>""";
        // Rollup (Currency, MIN)
        public const string MinAllowanceXaml = """<?xml version="1.0" encoding="utf-16"?><Activity x:Class="XrmWorkflow00000000000000000000000000000000" xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities" xmlns:mcwc="clr-namespace:Microsoft.Crm.Workflow.ClientActivities;assembly=Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxs="clr-namespace:Microsoft.Xrm.Sdk;assembly=Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxswa="clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:s="clr-namespace:System;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:srs="clr-namespace:System.Runtime.Serialization;assembly=System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:this="clr-namespace:" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><x:Members><x:Property Name="InputEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /><x:Property Name="CreatedEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /></x:Members><this:XrmWorkflow00000000000000000000000000000000.InputEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.InputEntities><this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><mva:VisualBasic.Settings>Assembly references and imported namespaces for internal implementation</mva:VisualBasic.Settings><mxswa:Workflow><Sequence DisplayName="RollupRuleStep1"><Sequence DisplayName="Source"><Sequence.Variables><Variable x:TypeArguments="x:String" Default="[Nothing]" Name="HierarchicalRelationshipName" /></Sequence.Variables></Sequence><Sequence DisplayName="Target"><mcwc:SetAttributeValue DisplayName="ctx_child.ctx_parentid.ctx_parent_ctx_child_cascade" Entity="[CreatedEntities(&quot;relatedlinked_ctx_parent_ctx_child_cascade#ctx_parentid#ctx_child#Temp&quot;)]" EntityName="ctx_child" /></Sequence><Sequence DisplayName="Aggregate"><Sequence.Variables><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_1" /><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_2" /></Sequence.Variables><mxswa:GetEntityProperty Attribute="ctx_allowance" Entity="[InputEntities(&quot;primaryEntity&quot;)]" EntityName="ctx_child" Value="[RollupRuleStep1_2]"><mxswa:GetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument></mxswa:GetEntityProperty.TargetType></mxswa:GetEntityProperty><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">Minimum</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { RollupRuleStep1_2 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[RollupRuleStep1_1]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference></Sequence></Sequence></mxswa:Workflow></Activity>""";
        // Rollup (Currency, AVG)
        public const string AvgAllowanceXaml = """<?xml version="1.0" encoding="utf-16"?><Activity x:Class="XrmWorkflow00000000000000000000000000000000" xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities" xmlns:mcwc="clr-namespace:Microsoft.Crm.Workflow.ClientActivities;assembly=Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxs="clr-namespace:Microsoft.Xrm.Sdk;assembly=Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:mxswa="clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" xmlns:s="clr-namespace:System;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:srs="clr-namespace:System.Runtime.Serialization;assembly=System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" xmlns:this="clr-namespace:" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><x:Members><x:Property Name="InputEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /><x:Property Name="CreatedEntities" Type="InArgument(scg:IDictionary(x:String, mxs:Entity))" /></x:Members><this:XrmWorkflow00000000000000000000000000000000.InputEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.InputEntities><this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><InArgument x:TypeArguments="scg:IDictionary(x:String, mxs:Entity)" /></this:XrmWorkflow00000000000000000000000000000000.CreatedEntities><mva:VisualBasic.Settings>Assembly references and imported namespaces for internal implementation</mva:VisualBasic.Settings><mxswa:Workflow><Sequence DisplayName="RollupRuleStep1"><Sequence DisplayName="Source"><Sequence.Variables><Variable x:TypeArguments="x:String" Default="[Nothing]" Name="HierarchicalRelationshipName" /></Sequence.Variables></Sequence><Sequence DisplayName="Target"><mcwc:SetAttributeValue DisplayName="ctx_child.ctx_parentid.ctx_parent_ctx_child_cascade" Entity="[CreatedEntities(&quot;relatedlinked_ctx_parent_ctx_child_cascade#ctx_parentid#ctx_child#Temp&quot;)]" EntityName="ctx_child" /></Sequence><Sequence DisplayName="Aggregate"><Sequence.Variables><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_1" /><Variable x:TypeArguments="x:Object" Name="RollupRuleStep1_2" /></Sequence.Variables><mxswa:GetEntityProperty Attribute="ctx_allowance" Entity="[InputEntities(&quot;primaryEntity&quot;)]" EntityName="ctx_child" Value="[RollupRuleStep1_2]"><mxswa:GetEntityProperty.TargetType><InArgument x:TypeArguments="s:Type"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument></mxswa:GetEntityProperty.TargetType></mxswa:GetEntityProperty><mxswa:ActivityReference AssemblyQualifiedName="Microsoft.Crm.Workflow.Activities.EvaluateExpression, Microsoft.Crm.Workflow, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" DisplayName="EvaluateExpression"><mxswa:ActivityReference.Arguments><InArgument x:TypeArguments="x:String" x:Key="ExpressionOperator">Average</InArgument><InArgument x:TypeArguments="s:Object[]" x:Key="Parameters">[New Object() { RollupRuleStep1_2 }]</InArgument><InArgument x:TypeArguments="s:Type" x:Key="TargetType"><mxswa:ReferenceLiteral x:TypeArguments="s:Type"><x:Null /></mxswa:ReferenceLiteral></InArgument><OutArgument x:TypeArguments="x:Object" x:Key="Result">[RollupRuleStep1_1]</OutArgument></mxswa:ActivityReference.Arguments></mxswa:ActivityReference></Sequence></Sequence></mxswa:Workflow></Activity>""";
    }

    /// <summary>
    /// Computed columns on ctx_parent (call after <see cref="CreateEntities"/>, whose base columns
    /// they reference). Covers BOTH engines XrmMockup supports:
    ///   • Power Fx formula columns — created automatically (definition is just the expression);
    ///   • a classic Calculated field and the Rollup fields — created once their XAML is captured
    ///     into <see cref="Definitions"/> (see that doc); skipped with a hint until then.
    /// Power Fx references columns by logical name.
    /// </summary>
    public static void CreateComputedColumns(MetadataBuilder b)
    {
        var amount = Names.Amount.ToLowerInvariant();
        var wholeNumber = Names.WholeNumber.ToLowerInvariant();
        var date = Names.DateValue.ToLowerInvariant();
        var name = Names.Name.ToLowerInvariant();

        // Power Fx formula columns (auto). Power Fx can't reference a currency column directly, so the
        // money field is wrapped in Decimal() (per Dataverse's own guidance).
        b.CreateFormulaColumn(Names.ParentEntity, b.Decimal(Names.AmountCalc, "Amount calculated"), $"Decimal({amount}) * 20");
        b.CreateFormulaColumn(Names.ParentEntity, b.WholeNumber(Names.WholeNumberCalc, "Whole number calculated"), $"{wholeNumber} - 2");
        b.CreateFormulaColumn(Names.ParentEntity, b.DateTime(Names.DateCalc, "Date calculated"), $"DateAdd({date}, 2)");
        b.CreateFormulaColumn(Names.ParentEntity, b.String(Names.TrimLeft, "Trim left"), $"Mid({name}, 3)");

        // Classic Calculated field (distinct engine from Power Fx).
        b.CreateCalculatedColumn(Names.ParentEntity, b.Money(Names.AmountCalcClassic, "Amount calculated (classic)"), Definitions.AmountCalcClassicXaml);

        // Rollup fields over the children's allowance.
        b.CreateRollupColumn(Names.ParentEntity, b.Money(Names.TotalAllowance, "Total allowance"), Definitions.TotalAllowanceXaml);
        b.CreateRollupColumn(Names.ParentEntity, b.Money(Names.MaxAllowance, "Max allowance"), Definitions.MaxAllowanceXaml);
        b.CreateRollupColumn(Names.ParentEntity, b.Money(Names.MinAllowance, "Min allowance"), Definitions.MinAllowanceXaml);
        b.CreateRollupColumn(Names.ParentEntity, b.Money(Names.AvgAllowance, "Avg allowance"), Definitions.AvgAllowanceXaml);
    }

    /// <summary>
    /// Components intentionally NOT scripted (calculated/rollup/formula fields and state-transition
    /// rules) — safest to author/verify in the maker portal. Printed as actionable steps after a run.
    /// </summary>
    public static void PrintManualSteps()
    {
        Console.WriteLine();
        Console.WriteLine("================ MANUAL / VERIFY STEPS (not scripted) ================");
        Console.WriteLine($@"
Created automatically:
  - Power Fx formula columns on {Names.ParentEntity} ({Names.AmountCalc}, {Names.WholeNumberCalc},
    {Names.DateCalc}, {Names.TrimLeft}) — XrmMockup evaluates PowerFx formula fields;
  - security roles WITH their privilege grants (see TestRoles) — no manual role hook-up.

Classic CALCULATED + ROLLUP fields ({Names.AmountCalcClassic}, {Names.TotalAllowance}, {Names.MaxAllowance},
{Names.MinAllowance}, {Names.AvgAllowance}) need a XAML FormulaDefinition that is risky to author blindly.
They are created automatically too, but only once their XAML is supplied. To capture it: create one
example of each on {Names.ParentEntity} in the maker portal, then run:
    --dump-formula {Names.ParentEntity}.<attributelogicalname>
paste the printed XAML into TestSchema.Definitions, and re-run this tool.
(The classic calculated field is kept distinct from the PowerFx columns on purpose — XrmMockup has
separate engines for Calculated and Formula fields, so the tests should cover both.)

{Names.ParentEntity} enforced state transitions (Active -> Inactive only) are now applied automatically
(best-effort) by SetEnforcedStateTransitions; if a TestStates IsValidStateTransition test still fails,
verify the transition rules on {Names.ParentEntity} in the maker portal.
");
        Console.WriteLine("======================================================================");
    }
}
