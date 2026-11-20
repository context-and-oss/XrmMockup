using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum componenttype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Entity", 1033)]
    [OptionSetMetadata("Objekt", 1030)]
    Entity = 1,

    [EnumMember]
    [OptionSetMetadata("Attribute", 1033)]
    [OptionSetMetadata("Attribut", 1030)]
    Attribute = 2,

    [EnumMember]
    [OptionSetMetadata("Relationship", 1033)]
    [OptionSetMetadata("Relation", 1030)]
    Relationship = 3,

    [EnumMember]
    [OptionSetMetadata("Attribute Picklist Value", 1033)]
    [OptionSetMetadata("Værdi på valgliste med attributter", 1030)]
    AttributePicklistValue = 4,

    [EnumMember]
    [OptionSetMetadata("Attribute Lookup Value", 1033)]
    [OptionSetMetadata("Opslagsværdi for attribut", 1030)]
    AttributeLookupValue = 5,

    [EnumMember]
    [OptionSetMetadata("View Attribute", 1033)]
    [OptionSetMetadata("Vis attribut", 1030)]
    ViewAttribute = 6,

    [EnumMember]
    [OptionSetMetadata("Localized Label", 1033)]
    [OptionSetMetadata("Lokaliseret etiket", 1030)]
    LocalizedLabel = 7,

    [EnumMember]
    [OptionSetMetadata("Relationship Extra Condition", 1033)]
    [OptionSetMetadata("Ekstra betingelse i relation", 1030)]
    RelationshipExtraCondition = 8,

    [EnumMember]
    [OptionSetMetadata("Option Set", 1033)]
    [OptionSetMetadata("Grupperet indstilling", 1030)]
    OptionSet = 9,

    [EnumMember]
    [OptionSetMetadata("Entity Relationship", 1033)]
    [OptionSetMetadata("Objektrelation", 1030)]
    EntityRelationship = 10,

    [EnumMember]
    [OptionSetMetadata("Entity Relationship Role", 1033)]
    [OptionSetMetadata("Rolle i objektrelation", 1030)]
    EntityRelationshipRole = 11,

    [EnumMember]
    [OptionSetMetadata("Entity Relationship Relationships", 1033)]
    [OptionSetMetadata("Relationer i objektrelation", 1030)]
    EntityRelationshipRelationships = 12,

    [EnumMember]
    [OptionSetMetadata("Managed Property", 1033)]
    [OptionSetMetadata("Administreret egenskab", 1030)]
    ManagedProperty = 13,

    [EnumMember]
    [OptionSetMetadata("Entity Key", 1033)]
    [OptionSetMetadata("Objektnøgle", 1030)]
    EntityKey = 14,

    [EnumMember]
    [OptionSetMetadata("Privilege", 1033)]
    [OptionSetMetadata("Rettighed", 1030)]
    Privilege = 16,

    [EnumMember]
    [OptionSetMetadata("PrivilegeObjectTypeCode", 1033)]
    [OptionSetMetadata("PrivilegeObjectTypeCode", 1030)]
    PrivilegeObjectTypeCode = 17,

    [EnumMember]
    [OptionSetMetadata("Index", 1033)]
    [OptionSetMetadata("Indeks", 1030)]
    Index = 18,

    [EnumMember]
    [OptionSetMetadata("Role", 1033)]
    [OptionSetMetadata("Rolle", 1030)]
    Role = 20,

    [EnumMember]
    [OptionSetMetadata("Role Privilege", 1033)]
    [OptionSetMetadata("Rollerettighed", 1030)]
    RolePrivilege = 21,

    [EnumMember]
    [OptionSetMetadata("Display String", 1033)]
    [OptionSetMetadata("Visningsstreng", 1030)]
    DisplayString = 22,

    [EnumMember]
    [OptionSetMetadata("Display String Map", 1033)]
    [OptionSetMetadata("Visningsstrengtilknytning", 1030)]
    DisplayStringMap = 23,

    [EnumMember]
    [OptionSetMetadata("Form", 1033)]
    [OptionSetMetadata("Formular", 1030)]
    Form = 24,

    [EnumMember]
    [OptionSetMetadata("Organization", 1033)]
    [OptionSetMetadata("Organisation", 1030)]
    Organization = 25,

    [EnumMember]
    [OptionSetMetadata("Saved Query", 1033)]
    [OptionSetMetadata("Gemt forespørgsel", 1030)]
    SavedQuery = 26,

    [EnumMember]
    [OptionSetMetadata("Workflow", 1033)]
    [OptionSetMetadata("Arbejdsproces", 1030)]
    Workflow = 29,

    [EnumMember]
    [OptionSetMetadata("Report", 1033)]
    [OptionSetMetadata("Rapport", 1030)]
    Report = 31,

    [EnumMember]
    [OptionSetMetadata("Report Entity", 1033)]
    [OptionSetMetadata("Rapportobjekt", 1030)]
    ReportEntity = 32,

    [EnumMember]
    [OptionSetMetadata("Report Category", 1033)]
    [OptionSetMetadata("Rapportkategori", 1030)]
    ReportCategory = 33,

    [EnumMember]
    [OptionSetMetadata("Report Visibility", 1033)]
    [OptionSetMetadata("Rapportsynlighed", 1030)]
    ReportVisibility = 34,

    [EnumMember]
    [OptionSetMetadata("Attachment", 1033)]
    [OptionSetMetadata("Vedhæftet fil", 1030)]
    Attachment = 35,

    [EnumMember]
    [OptionSetMetadata("Email Template", 1033)]
    [OptionSetMetadata("E-mail-skabelon", 1030)]
    EmailTemplate = 36,

    [EnumMember]
    [OptionSetMetadata("Contract Template", 1033)]
    [OptionSetMetadata("Kontraktskabelon", 1030)]
    ContractTemplate = 37,

    [EnumMember]
    [OptionSetMetadata("KB Article Template", 1033)]
    [OptionSetMetadata("Skabelon til KnowledgeBase-artikel", 1030)]
    KBArticleTemplate = 38,

    [EnumMember]
    [OptionSetMetadata("Mail Merge Template", 1033)]
    [OptionSetMetadata("Skabelon til brevfletning", 1030)]
    MailMergeTemplate = 39,

    [EnumMember]
    [OptionSetMetadata("Duplicate Rule", 1033)]
    [OptionSetMetadata("Duplikeret regel", 1030)]
    DuplicateRule = 44,

    [EnumMember]
    [OptionSetMetadata("Duplicate Rule Condition", 1033)]
    [OptionSetMetadata("Dubletregeltilstand", 1030)]
    DuplicateRuleCondition = 45,

    [EnumMember]
    [OptionSetMetadata("Entity Map", 1033)]
    [OptionSetMetadata("Objekttilknytning", 1030)]
    EntityMap = 46,

    [EnumMember]
    [OptionSetMetadata("Attribute Map", 1033)]
    [OptionSetMetadata("Attributtilknytning", 1030)]
    AttributeMap = 47,

    [EnumMember]
    [OptionSetMetadata("Ribbon Command", 1033)]
    [OptionSetMetadata("Kommando på bånd", 1030)]
    RibbonCommand = 48,

    [EnumMember]
    [OptionSetMetadata("Ribbon Context Group", 1033)]
    [OptionSetMetadata("Genvejsmenu til gruppe på bånd", 1030)]
    RibbonContextGroup = 49,

    [EnumMember]
    [OptionSetMetadata("Ribbon Customization", 1033)]
    [OptionSetMetadata("Tilpasning af båndet", 1030)]
    RibbonCustomization = 50,

    [EnumMember]
    [OptionSetMetadata("Ribbon Rule", 1033)]
    [OptionSetMetadata("Båndregel", 1030)]
    RibbonRule = 52,

    [EnumMember]
    [OptionSetMetadata("Ribbon Tab To Command Map", 1033)]
    [OptionSetMetadata("Tilknytning mellem fane på båndet og kommando", 1030)]
    RibbonTabToCommandMap = 53,

    [EnumMember]
    [OptionSetMetadata("Ribbon Diff", 1033)]
    [OptionSetMetadata("Difference på bånd", 1030)]
    RibbonDiff = 55,

    [EnumMember]
    [OptionSetMetadata("Saved Query Visualization", 1033)]
    [OptionSetMetadata("Visualisering af forespørgsel blev gemt", 1030)]
    SavedQueryVisualization = 59,

    [EnumMember]
    [OptionSetMetadata("System Form", 1033)]
    [OptionSetMetadata("Systemformular", 1030)]
    SystemForm = 60,

    [EnumMember]
    [OptionSetMetadata("Web Resource", 1033)]
    [OptionSetMetadata("Webressource", 1030)]
    WebResource = 61,

    [EnumMember]
    [OptionSetMetadata("Site Map", 1033)]
    [OptionSetMetadata("Oversigt over websted", 1030)]
    SiteMap = 62,

    [EnumMember]
    [OptionSetMetadata("Connection Role", 1033)]
    [OptionSetMetadata("Forbindelsesrolle", 1030)]
    ConnectionRole = 63,

    [EnumMember]
    [OptionSetMetadata("Complex Control", 1033)]
    [OptionSetMetadata("Komplekst kontrolelement", 1030)]
    ComplexControl = 64,

    [EnumMember]
    [OptionSetMetadata("Hierarchy Rule", 1033)]
    [OptionSetMetadata("Hierarkiregel", 1030)]
    HierarchyRule = 65,

    [EnumMember]
    [OptionSetMetadata("Custom Control", 1033)]
    [OptionSetMetadata("Brugerdefineret kontrolelement", 1030)]
    CustomControl = 66,

    [EnumMember]
    [OptionSetMetadata("Custom Control Default Config", 1033)]
    [OptionSetMetadata("Standardkonfiguration for brugerdefineret kontrolelement", 1030)]
    CustomControlDefaultConfig = 68,

    [EnumMember]
    [OptionSetMetadata("Field Security Profile", 1033)]
    [OptionSetMetadata("Profil for feltsikkerhed", 1030)]
    FieldSecurityProfile = 70,

    [EnumMember]
    [OptionSetMetadata("Field Permission", 1033)]
    [OptionSetMetadata("Feltrettighed", 1030)]
    FieldPermission = 71,

    [EnumMember]
    [OptionSetMetadata("Plugin Type", 1033)]
    [OptionSetMetadata("Type af plug-in", 1030)]
    PluginType = 90,

    [EnumMember]
    [OptionSetMetadata("Plugin Assembly", 1033)]
    [OptionSetMetadata("Plug-in-assembly", 1030)]
    PluginAssembly = 91,

    [EnumMember]
    [OptionSetMetadata("SDK Message Processing Step", 1033)]
    [OptionSetMetadata("Behandlingstrin for SDK-meddelelse", 1030)]
    SDKMessageProcessingStep = 92,

    [EnumMember]
    [OptionSetMetadata("SDK Message Processing Step Image", 1033)]
    [OptionSetMetadata("Behandlingstrinsbillede for SDK-meddelelse", 1030)]
    SDKMessageProcessingStepImage = 93,

    [EnumMember]
    [OptionSetMetadata("Service Endpoint", 1033)]
    [OptionSetMetadata("Slutpunkt for tjeneste", 1030)]
    ServiceEndpoint = 95,

    [EnumMember]
    [OptionSetMetadata("Routing Rule", 1033)]
    [OptionSetMetadata("Ruteregel", 1030)]
    RoutingRule = 150,

    [EnumMember]
    [OptionSetMetadata("Routing Rule Item", 1033)]
    [OptionSetMetadata("Ruteregelelement", 1030)]
    RoutingRuleItem = 151,

    [EnumMember]
    [OptionSetMetadata("SLA", 1033)]
    [OptionSetMetadata("SLA", 1030)]
    SLA = 152,

    [EnumMember]
    [OptionSetMetadata("SLA Item", 1033)]
    [OptionSetMetadata("SLA-element", 1030)]
    SLAItem = 153,

    [EnumMember]
    [OptionSetMetadata("Convert Rule", 1033)]
    [OptionSetMetadata("Konverteringsregel", 1030)]
    ConvertRule = 154,

    [EnumMember]
    [OptionSetMetadata("Convert Rule Item", 1033)]
    [OptionSetMetadata("Konverteringsregelelement", 1030)]
    ConvertRuleItem = 155,

    [EnumMember]
    [OptionSetMetadata("Mobile Offline Profile", 1033)]
    [OptionSetMetadata("Mobile Offline-profil", 1030)]
    MobileOfflineProfile = 161,

    [EnumMember]
    [OptionSetMetadata("Mobile Offline Profile Item", 1033)]
    [OptionSetMetadata("Mobile Offline-profilelement", 1030)]
    MobileOfflineProfileItem = 162,

    [EnumMember]
    [OptionSetMetadata("Similarity Rule", 1033)]
    [OptionSetMetadata("Lighedsregel", 1030)]
    SimilarityRule = 165,

    [EnumMember]
    [OptionSetMetadata("Data Source Mapping", 1033)]
    [OptionSetMetadata("Tilknytning af datakilde", 1030)]
    DataSourceMapping = 166,

    [EnumMember]
    [OptionSetMetadata("SDKMessage", 1033)]
    [OptionSetMetadata("SDKMessage", 1030)]
    SDKMessage = 201,

    [EnumMember]
    [OptionSetMetadata("SDKMessageFilter", 1033)]
    [OptionSetMetadata("SDKMessageFilter", 1030)]
    SDKMessageFilter = 202,

    [EnumMember]
    [OptionSetMetadata("SdkMessagePair", 1033)]
    [OptionSetMetadata("SdkMessagePair", 1030)]
    SdkMessagePair = 203,

    [EnumMember]
    [OptionSetMetadata("SdkMessageRequest", 1033)]
    [OptionSetMetadata("SdkMessageRequest", 1030)]
    SdkMessageRequest = 204,

    [EnumMember]
    [OptionSetMetadata("SdkMessageRequestField", 1033)]
    [OptionSetMetadata("SdkMessageRequestField", 1030)]
    SdkMessageRequestField = 205,

    [EnumMember]
    [OptionSetMetadata("SdkMessageResponse", 1033)]
    [OptionSetMetadata("SdkMessageResponse", 1030)]
    SdkMessageResponse = 206,

    [EnumMember]
    [OptionSetMetadata("SdkMessageResponseField", 1033)]
    [OptionSetMetadata("SdkMessageResponseField", 1030)]
    SdkMessageResponseField = 207,

    [EnumMember]
    [OptionSetMetadata("Import Map", 1033)]
    [OptionSetMetadata("Importer tilknytning", 1030)]
    ImportMap = 208,

    [EnumMember]
    [OptionSetMetadata("WebWizard", 1033)]
    [OptionSetMetadata("WebWizard", 1030)]
    WebWizard = 210,

    [EnumMember]
    [OptionSetMetadata("Canvas App", 1033)]
    [OptionSetMetadata("Lærred-app", 1030)]
    CanvasApp = 300,

    [EnumMember]
    [OptionSetMetadata("Connector", 1033)]
    [OptionSetMetadata("Connector", 1030)]
    Connector = 371,

    [EnumMember]
    [OptionSetMetadata("Connector", 1033)]
    [OptionSetMetadata("Connector", 1030)]
    Connector_1 = 372,

    [EnumMember]
    [OptionSetMetadata("Environment Variable Definition", 1033)]
    [OptionSetMetadata("Definition af miljøvariabel", 1030)]
    EnvironmentVariableDefinition = 380,

    [EnumMember]
    [OptionSetMetadata("Environment Variable Value", 1033)]
    [OptionSetMetadata("Værdi for miljøvariabel", 1030)]
    EnvironmentVariableValue = 381,

    [EnumMember]
    [OptionSetMetadata("AI Project Type", 1033)]
    [OptionSetMetadata("AI-projekttype", 1030)]
    AIProjectType = 400,

    [EnumMember]
    [OptionSetMetadata("AI Project", 1033)]
    [OptionSetMetadata("AI-projekt", 1030)]
    AIProject = 401,

    [EnumMember]
    [OptionSetMetadata("AI Configuration", 1033)]
    [OptionSetMetadata("AI-konfiguration", 1030)]
    AIConfiguration = 402,

    [EnumMember]
    [OptionSetMetadata("Entity Analytics Configuration", 1033)]
    [OptionSetMetadata("Konfiguration af objektanalyse", 1030)]
    EntityAnalyticsConfiguration = 430,

    [EnumMember]
    [OptionSetMetadata("Attribute Image Configuration", 1033)]
    [OptionSetMetadata("Konfiguration af attributbillede", 1030)]
    AttributeImageConfiguration = 431,

    [EnumMember]
    [OptionSetMetadata("Entity Image Configuration", 1033)]
    [OptionSetMetadata("Konfiguration af objektbillede", 1030)]
    EntityImageConfiguration = 432,
}
