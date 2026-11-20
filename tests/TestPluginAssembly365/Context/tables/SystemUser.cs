using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Person with access to the Microsoft CRM system and who owns objects in the Microsoft CRM database.</para>
/// <para>Display Name: User</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("systemuser")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class SystemUser : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "systemuser";
    public const int EntityTypeCode = 8;

    public SystemUser() : base(EntityLogicalName) { }
    public SystemUser(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("fullname");

    [AttributeLogicalName("systemuserid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("systemuserid", value);
        }
    }

    /// <summary>
    /// <para>Type of user.</para>
    /// <para>Display Name: Access Mode</para>
    /// </summary>
    [AttributeLogicalName("accessmode")]
    [DisplayName("Access Mode")]
    public systemuser_accessmode? AccessMode
    {
        get => this.GetOptionSetValue<systemuser_accessmode>("accessmode");
        set => this.SetOptionSetValue("accessmode", value);
    }

    /// <summary>
    /// <para>Active Directory object GUID for the system user.</para>
    /// <para>Display Name: Active Directory Guid</para>
    /// </summary>
    [AttributeLogicalName("activedirectoryguid")]
    [DisplayName("Active Directory Guid")]
    public Guid? ActiveDirectoryGuid
    {
        get => GetAttributeValue<Guid?>("activedirectoryguid");
        set => SetAttributeValue("activedirectoryguid", value);
    }

    /// <summary>
    /// <para>Unique identifier for address 1.</para>
    /// <para>Display Name: Address 1: ID</para>
    /// </summary>
    [AttributeLogicalName("address1_addressid")]
    [DisplayName("Address 1: ID")]
    public Guid? Address1_AddressId
    {
        get => GetAttributeValue<Guid?>("address1_addressid");
        set => SetAttributeValue("address1_addressid", value);
    }

    /// <summary>
    /// <para>Type of address for address 1, such as billing, shipping, or primary address.</para>
    /// <para>Display Name: Address 1: Address Type</para>
    /// </summary>
    [AttributeLogicalName("address1_addresstypecode")]
    [DisplayName("Address 1: Address Type")]
    public systemuser_address1_addresstypecode? Address1_AddressTypeCode
    {
        get => this.GetOptionSetValue<systemuser_address1_addresstypecode>("address1_addresstypecode");
        set => this.SetOptionSetValue("address1_addresstypecode", value);
    }

    /// <summary>
    /// <para>City name for address 1.</para>
    /// <para>Display Name: City</para>
    /// </summary>
    [AttributeLogicalName("address1_city")]
    [DisplayName("City")]
    [MaxLength(128)]
    public string? Address1_City
    {
        get => GetAttributeValue<string?>("address1_city");
        set => SetAttributeValue("address1_city", value);
    }

    /// <summary>
    /// <para>Shows the complete primary address.</para>
    /// <para>Display Name: Address</para>
    /// </summary>
    [AttributeLogicalName("address1_composite")]
    [DisplayName("Address")]
    [MaxLength(1000)]
    public string? Address1_Composite
    {
        get => GetAttributeValue<string?>("address1_composite");
        set => SetAttributeValue("address1_composite", value);
    }

    /// <summary>
    /// <para>Country/region name in address 1.</para>
    /// <para>Display Name: Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address1_country")]
    [DisplayName("Country/Region")]
    [MaxLength(128)]
    public string? Address1_Country
    {
        get => GetAttributeValue<string?>("address1_country");
        set => SetAttributeValue("address1_country", value);
    }

    /// <summary>
    /// <para>County name for address 1.</para>
    /// <para>Display Name: Address 1: County</para>
    /// </summary>
    [AttributeLogicalName("address1_county")]
    [DisplayName("Address 1: County")]
    [MaxLength(128)]
    public string? Address1_County
    {
        get => GetAttributeValue<string?>("address1_county");
        set => SetAttributeValue("address1_county", value);
    }

    /// <summary>
    /// <para>Fax number for address 1.</para>
    /// <para>Display Name: Address 1: Fax</para>
    /// </summary>
    [AttributeLogicalName("address1_fax")]
    [DisplayName("Address 1: Fax")]
    [MaxLength(64)]
    public string? Address1_Fax
    {
        get => GetAttributeValue<string?>("address1_fax");
        set => SetAttributeValue("address1_fax", value);
    }

    /// <summary>
    /// <para>Latitude for address 1.</para>
    /// <para>Display Name: Address 1: Latitude</para>
    /// </summary>
    [AttributeLogicalName("address1_latitude")]
    [DisplayName("Address 1: Latitude")]
    public double? Address1_Latitude
    {
        get => GetAttributeValue<double?>("address1_latitude");
        set => SetAttributeValue("address1_latitude", value);
    }

    /// <summary>
    /// <para>First line for entering address 1 information.</para>
    /// <para>Display Name: Street 1</para>
    /// </summary>
    [AttributeLogicalName("address1_line1")]
    [DisplayName("Street 1")]
    [MaxLength(1024)]
    public string? Address1_Line1
    {
        get => GetAttributeValue<string?>("address1_line1");
        set => SetAttributeValue("address1_line1", value);
    }

    /// <summary>
    /// <para>Second line for entering address 1 information.</para>
    /// <para>Display Name: Street 2</para>
    /// </summary>
    [AttributeLogicalName("address1_line2")]
    [DisplayName("Street 2")]
    [MaxLength(1024)]
    public string? Address1_Line2
    {
        get => GetAttributeValue<string?>("address1_line2");
        set => SetAttributeValue("address1_line2", value);
    }

    /// <summary>
    /// <para>Third line for entering address 1 information.</para>
    /// <para>Display Name: Street 3</para>
    /// </summary>
    [AttributeLogicalName("address1_line3")]
    [DisplayName("Street 3")]
    [MaxLength(1024)]
    public string? Address1_Line3
    {
        get => GetAttributeValue<string?>("address1_line3");
        set => SetAttributeValue("address1_line3", value);
    }

    /// <summary>
    /// <para>Longitude for address 1.</para>
    /// <para>Display Name: Address 1: Longitude</para>
    /// </summary>
    [AttributeLogicalName("address1_longitude")]
    [DisplayName("Address 1: Longitude")]
    public double? Address1_Longitude
    {
        get => GetAttributeValue<double?>("address1_longitude");
        set => SetAttributeValue("address1_longitude", value);
    }

    /// <summary>
    /// <para>Name to enter for address 1.</para>
    /// <para>Display Name: Address 1: Name</para>
    /// </summary>
    [AttributeLogicalName("address1_name")]
    [DisplayName("Address 1: Name")]
    [MaxLength(100)]
    public string? Address1_Name
    {
        get => GetAttributeValue<string?>("address1_name");
        set => SetAttributeValue("address1_name", value);
    }

    /// <summary>
    /// <para>ZIP Code or postal code for address 1.</para>
    /// <para>Display Name: ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address1_postalcode")]
    [DisplayName("ZIP/Postal Code")]
    [MaxLength(40)]
    public string? Address1_PostalCode
    {
        get => GetAttributeValue<string?>("address1_postalcode");
        set => SetAttributeValue("address1_postalcode", value);
    }

    /// <summary>
    /// <para>Post office box number for address 1.</para>
    /// <para>Display Name: Address 1: Post Office Box</para>
    /// </summary>
    [AttributeLogicalName("address1_postofficebox")]
    [DisplayName("Address 1: Post Office Box")]
    [MaxLength(40)]
    public string? Address1_PostOfficeBox
    {
        get => GetAttributeValue<string?>("address1_postofficebox");
        set => SetAttributeValue("address1_postofficebox", value);
    }

    /// <summary>
    /// <para>Method of shipment for address 1.</para>
    /// <para>Display Name: Address 1: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("address1_shippingmethodcode")]
    [DisplayName("Address 1: Shipping Method")]
    public systemuser_address1_shippingmethodcode? Address1_ShippingMethodCode
    {
        get => this.GetOptionSetValue<systemuser_address1_shippingmethodcode>("address1_shippingmethodcode");
        set => this.SetOptionSetValue("address1_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>State or province for address 1.</para>
    /// <para>Display Name: State/Province</para>
    /// </summary>
    [AttributeLogicalName("address1_stateorprovince")]
    [DisplayName("State/Province")]
    [MaxLength(128)]
    public string? Address1_StateOrProvince
    {
        get => GetAttributeValue<string?>("address1_stateorprovince");
        set => SetAttributeValue("address1_stateorprovince", value);
    }

    /// <summary>
    /// <para>First telephone number associated with address 1.</para>
    /// <para>Display Name: Main Phone</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone1")]
    [DisplayName("Main Phone")]
    [MaxLength(64)]
    public string? Address1_Telephone1
    {
        get => GetAttributeValue<string?>("address1_telephone1");
        set => SetAttributeValue("address1_telephone1", value);
    }

    /// <summary>
    /// <para>Second telephone number associated with address 1.</para>
    /// <para>Display Name: Other Phone</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone2")]
    [DisplayName("Other Phone")]
    [MaxLength(50)]
    public string? Address1_Telephone2
    {
        get => GetAttributeValue<string?>("address1_telephone2");
        set => SetAttributeValue("address1_telephone2", value);
    }

    /// <summary>
    /// <para>Third telephone number associated with address 1.</para>
    /// <para>Display Name: Pager</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone3")]
    [DisplayName("Pager")]
    [MaxLength(50)]
    public string? Address1_Telephone3
    {
        get => GetAttributeValue<string?>("address1_telephone3");
        set => SetAttributeValue("address1_telephone3", value);
    }

    /// <summary>
    /// <para>United Parcel Service (UPS) zone for address 1.</para>
    /// <para>Display Name: Address 1: UPS Zone</para>
    /// </summary>
    [AttributeLogicalName("address1_upszone")]
    [DisplayName("Address 1: UPS Zone")]
    [MaxLength(4)]
    public string? Address1_UPSZone
    {
        get => GetAttributeValue<string?>("address1_upszone");
        set => SetAttributeValue("address1_upszone", value);
    }

    /// <summary>
    /// <para>UTC offset for address 1. This is the difference between local time and standard Coordinated Universal Time.</para>
    /// <para>Display Name: Address 1: UTC Offset</para>
    /// </summary>
    [AttributeLogicalName("address1_utcoffset")]
    [DisplayName("Address 1: UTC Offset")]
    [Range(-1500, 1500)]
    public int? Address1_UTCOffset
    {
        get => GetAttributeValue<int?>("address1_utcoffset");
        set => SetAttributeValue("address1_utcoffset", value);
    }

    /// <summary>
    /// <para>Unique identifier for address 2.</para>
    /// <para>Display Name: Address 2: ID</para>
    /// </summary>
    [AttributeLogicalName("address2_addressid")]
    [DisplayName("Address 2: ID")]
    public Guid? Address2_AddressId
    {
        get => GetAttributeValue<Guid?>("address2_addressid");
        set => SetAttributeValue("address2_addressid", value);
    }

    /// <summary>
    /// <para>Type of address for address 2, such as billing, shipping, or primary address.</para>
    /// <para>Display Name: Address 2: Address Type</para>
    /// </summary>
    [AttributeLogicalName("address2_addresstypecode")]
    [DisplayName("Address 2: Address Type")]
    public systemuser_address2_addresstypecode? Address2_AddressTypeCode
    {
        get => this.GetOptionSetValue<systemuser_address2_addresstypecode>("address2_addresstypecode");
        set => this.SetOptionSetValue("address2_addresstypecode", value);
    }

    /// <summary>
    /// <para>City name for address 2.</para>
    /// <para>Display Name: Other City</para>
    /// </summary>
    [AttributeLogicalName("address2_city")]
    [DisplayName("Other City")]
    [MaxLength(128)]
    public string? Address2_City
    {
        get => GetAttributeValue<string?>("address2_city");
        set => SetAttributeValue("address2_city", value);
    }

    /// <summary>
    /// <para>Shows the complete secondary address.</para>
    /// <para>Display Name: Other Address</para>
    /// </summary>
    [AttributeLogicalName("address2_composite")]
    [DisplayName("Other Address")]
    [MaxLength(1000)]
    public string? Address2_Composite
    {
        get => GetAttributeValue<string?>("address2_composite");
        set => SetAttributeValue("address2_composite", value);
    }

    /// <summary>
    /// <para>Country/region name in address 2.</para>
    /// <para>Display Name: Other Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address2_country")]
    [DisplayName("Other Country/Region")]
    [MaxLength(128)]
    public string? Address2_Country
    {
        get => GetAttributeValue<string?>("address2_country");
        set => SetAttributeValue("address2_country", value);
    }

    /// <summary>
    /// <para>County name for address 2.</para>
    /// <para>Display Name: Address 2: County</para>
    /// </summary>
    [AttributeLogicalName("address2_county")]
    [DisplayName("Address 2: County")]
    [MaxLength(128)]
    public string? Address2_County
    {
        get => GetAttributeValue<string?>("address2_county");
        set => SetAttributeValue("address2_county", value);
    }

    /// <summary>
    /// <para>Fax number for address 2.</para>
    /// <para>Display Name: Address 2: Fax</para>
    /// </summary>
    [AttributeLogicalName("address2_fax")]
    [DisplayName("Address 2: Fax")]
    [MaxLength(50)]
    public string? Address2_Fax
    {
        get => GetAttributeValue<string?>("address2_fax");
        set => SetAttributeValue("address2_fax", value);
    }

    /// <summary>
    /// <para>Latitude for address 2.</para>
    /// <para>Display Name: Address 2: Latitude</para>
    /// </summary>
    [AttributeLogicalName("address2_latitude")]
    [DisplayName("Address 2: Latitude")]
    public double? Address2_Latitude
    {
        get => GetAttributeValue<double?>("address2_latitude");
        set => SetAttributeValue("address2_latitude", value);
    }

    /// <summary>
    /// <para>First line for entering address 2 information.</para>
    /// <para>Display Name: Other Street 1</para>
    /// </summary>
    [AttributeLogicalName("address2_line1")]
    [DisplayName("Other Street 1")]
    [MaxLength(1024)]
    public string? Address2_Line1
    {
        get => GetAttributeValue<string?>("address2_line1");
        set => SetAttributeValue("address2_line1", value);
    }

    /// <summary>
    /// <para>Second line for entering address 2 information.</para>
    /// <para>Display Name: Other Street 2</para>
    /// </summary>
    [AttributeLogicalName("address2_line2")]
    [DisplayName("Other Street 2")]
    [MaxLength(1024)]
    public string? Address2_Line2
    {
        get => GetAttributeValue<string?>("address2_line2");
        set => SetAttributeValue("address2_line2", value);
    }

    /// <summary>
    /// <para>Third line for entering address 2 information.</para>
    /// <para>Display Name: Other Street 3</para>
    /// </summary>
    [AttributeLogicalName("address2_line3")]
    [DisplayName("Other Street 3")]
    [MaxLength(1024)]
    public string? Address2_Line3
    {
        get => GetAttributeValue<string?>("address2_line3");
        set => SetAttributeValue("address2_line3", value);
    }

    /// <summary>
    /// <para>Longitude for address 2.</para>
    /// <para>Display Name: Address 2: Longitude</para>
    /// </summary>
    [AttributeLogicalName("address2_longitude")]
    [DisplayName("Address 2: Longitude")]
    public double? Address2_Longitude
    {
        get => GetAttributeValue<double?>("address2_longitude");
        set => SetAttributeValue("address2_longitude", value);
    }

    /// <summary>
    /// <para>Name to enter for address 2.</para>
    /// <para>Display Name: Address 2: Name</para>
    /// </summary>
    [AttributeLogicalName("address2_name")]
    [DisplayName("Address 2: Name")]
    [MaxLength(100)]
    public string? Address2_Name
    {
        get => GetAttributeValue<string?>("address2_name");
        set => SetAttributeValue("address2_name", value);
    }

    /// <summary>
    /// <para>ZIP Code or postal code for address 2.</para>
    /// <para>Display Name: Other ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address2_postalcode")]
    [DisplayName("Other ZIP/Postal Code")]
    [MaxLength(40)]
    public string? Address2_PostalCode
    {
        get => GetAttributeValue<string?>("address2_postalcode");
        set => SetAttributeValue("address2_postalcode", value);
    }

    /// <summary>
    /// <para>Post office box number for address 2.</para>
    /// <para>Display Name: Address 2: Post Office Box</para>
    /// </summary>
    [AttributeLogicalName("address2_postofficebox")]
    [DisplayName("Address 2: Post Office Box")]
    [MaxLength(40)]
    public string? Address2_PostOfficeBox
    {
        get => GetAttributeValue<string?>("address2_postofficebox");
        set => SetAttributeValue("address2_postofficebox", value);
    }

    /// <summary>
    /// <para>Method of shipment for address 2.</para>
    /// <para>Display Name: Address 2: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("address2_shippingmethodcode")]
    [DisplayName("Address 2: Shipping Method")]
    public systemuser_address2_shippingmethodcode? Address2_ShippingMethodCode
    {
        get => this.GetOptionSetValue<systemuser_address2_shippingmethodcode>("address2_shippingmethodcode");
        set => this.SetOptionSetValue("address2_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>State or province for address 2.</para>
    /// <para>Display Name: Other State/Province</para>
    /// </summary>
    [AttributeLogicalName("address2_stateorprovince")]
    [DisplayName("Other State/Province")]
    [MaxLength(128)]
    public string? Address2_StateOrProvince
    {
        get => GetAttributeValue<string?>("address2_stateorprovince");
        set => SetAttributeValue("address2_stateorprovince", value);
    }

    /// <summary>
    /// <para>First telephone number associated with address 2.</para>
    /// <para>Display Name: Address 2: Telephone 1</para>
    /// </summary>
    [AttributeLogicalName("address2_telephone1")]
    [DisplayName("Address 2: Telephone 1")]
    [MaxLength(50)]
    public string? Address2_Telephone1
    {
        get => GetAttributeValue<string?>("address2_telephone1");
        set => SetAttributeValue("address2_telephone1", value);
    }

    /// <summary>
    /// <para>Second telephone number associated with address 2.</para>
    /// <para>Display Name: Address 2: Telephone 2</para>
    /// </summary>
    [AttributeLogicalName("address2_telephone2")]
    [DisplayName("Address 2: Telephone 2")]
    [MaxLength(50)]
    public string? Address2_Telephone2
    {
        get => GetAttributeValue<string?>("address2_telephone2");
        set => SetAttributeValue("address2_telephone2", value);
    }

    /// <summary>
    /// <para>Third telephone number associated with address 2.</para>
    /// <para>Display Name: Address 2: Telephone 3</para>
    /// </summary>
    [AttributeLogicalName("address2_telephone3")]
    [DisplayName("Address 2: Telephone 3")]
    [MaxLength(50)]
    public string? Address2_Telephone3
    {
        get => GetAttributeValue<string?>("address2_telephone3");
        set => SetAttributeValue("address2_telephone3", value);
    }

    /// <summary>
    /// <para>United Parcel Service (UPS) zone for address 2.</para>
    /// <para>Display Name: Address 2: UPS Zone</para>
    /// </summary>
    [AttributeLogicalName("address2_upszone")]
    [DisplayName("Address 2: UPS Zone")]
    [MaxLength(4)]
    public string? Address2_UPSZone
    {
        get => GetAttributeValue<string?>("address2_upszone");
        set => SetAttributeValue("address2_upszone", value);
    }

    /// <summary>
    /// <para>UTC offset for address 2. This is the difference between local time and standard Coordinated Universal Time.</para>
    /// <para>Display Name: Address 2: UTC Offset</para>
    /// </summary>
    [AttributeLogicalName("address2_utcoffset")]
    [DisplayName("Address 2: UTC Offset")]
    [Range(-1500, 1500)]
    public int? Address2_UTCOffset
    {
        get => GetAttributeValue<int?>("address2_utcoffset");
        set => SetAttributeValue("address2_utcoffset", value);
    }

    /// <summary>
    /// <para>The identifier for the application. This is used to access data in another application.</para>
    /// <para>Display Name: Application ID</para>
    /// </summary>
    [AttributeLogicalName("applicationid")]
    [DisplayName("Application ID")]
    public Guid? ApplicationId
    {
        get => GetAttributeValue<Guid?>("applicationid");
        set => SetAttributeValue("applicationid", value);
    }

    /// <summary>
    /// <para>The URI used as a unique logical identifier for the external app. This can be used to validate the application.</para>
    /// <para>Display Name: Application ID URI</para>
    /// </summary>
    [AttributeLogicalName("applicationiduri")]
    [DisplayName("Application ID URI")]
    [MaxLength(1024)]
    public string? ApplicationIdUri
    {
        get => GetAttributeValue<string?>("applicationiduri");
        set => SetAttributeValue("applicationiduri", value);
    }

    /// <summary>
    /// <para>This is the application directory object Id.</para>
    /// <para>Display Name: Azure AD Object ID</para>
    /// </summary>
    [AttributeLogicalName("azureactivedirectoryobjectid")]
    [DisplayName("Azure AD Object ID")]
    public Guid? AzureActiveDirectoryObjectId
    {
        get => GetAttributeValue<Guid?>("azureactivedirectoryobjectid");
        set => SetAttributeValue("azureactivedirectoryobjectid", value);
    }

    /// <summary>
    /// <para>Date and time when the user was set as soft deleted in Azure.</para>
    /// <para>Display Name: Azure Deleted On</para>
    /// </summary>
    [AttributeLogicalName("azuredeletedon")]
    [DisplayName("Azure Deleted On")]
    public DateTime? AzureDeletedOn
    {
        get => GetAttributeValue<DateTime?>("azuredeletedon");
        set => SetAttributeValue("azuredeletedon", value);
    }

    /// <summary>
    /// <para>Azure state of user</para>
    /// <para>Display Name: Azure State</para>
    /// </summary>
    [AttributeLogicalName("azurestate")]
    [DisplayName("Azure State")]
    public systemuser_azurestate? AzureState
    {
        get => this.GetOptionSetValue<systemuser_azurestate>("azurestate");
        set => this.SetOptionSetValue("azurestate", value);
    }

    /// <summary>
    /// <para>Unique identifier of the business unit with which the user is associated.</para>
    /// <para>Display Name: Business Unit</para>
    /// </summary>
    [AttributeLogicalName("businessunitid")]
    [DisplayName("Business Unit")]
    public EntityReference? BusinessUnitId
    {
        get => GetAttributeValue<EntityReference?>("businessunitid");
        set => SetAttributeValue("businessunitid", value);
    }

    /// <summary>
    /// <para>Fiscal calendar associated with the user.</para>
    /// <para>Display Name: Calendar</para>
    /// </summary>
    [AttributeLogicalName("calendarid")]
    [DisplayName("Calendar")]
    public EntityReference? CalendarId
    {
        get => GetAttributeValue<EntityReference?>("calendarid");
        set => SetAttributeValue("calendarid", value);
    }

    /// <summary>
    /// <para>License type of user. This is used only in the on-premises version of the product. Online licenses are managed through Microsoft 365 Office Portal</para>
    /// <para>Display Name: License Type</para>
    /// </summary>
    [AttributeLogicalName("caltype")]
    [DisplayName("License Type")]
    public systemuser_caltype? CALType
    {
        get => this.GetOptionSetValue<systemuser_caltype>("caltype");
        set => this.SetOptionSetValue("caltype", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the user.</para>
    /// <para>Display Name: Created By</para>
    /// </summary>
    [AttributeLogicalName("createdby")]
    [DisplayName("Created By")]
    public EntityReference? CreatedBy
    {
        get => GetAttributeValue<EntityReference?>("createdby");
        set => SetAttributeValue("createdby", value);
    }

    /// <summary>
    /// <para>Date and time when the user was created.</para>
    /// <para>Display Name: Created On</para>
    /// </summary>
    [AttributeLogicalName("createdon")]
    [DisplayName("Created On")]
    public DateTime? CreatedOn
    {
        get => GetAttributeValue<DateTime?>("createdon");
        set => SetAttributeValue("createdon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the delegate user who created the systemuser.</para>
    /// <para>Display Name: Created By (Delegate)</para>
    /// </summary>
    [AttributeLogicalName("createdonbehalfby")]
    [DisplayName("Created By (Delegate)")]
    public EntityReference? CreatedOnBehalfBy
    {
        get => GetAttributeValue<EntityReference?>("createdonbehalfby");
        set => SetAttributeValue("createdonbehalfby", value);
    }

    /// <summary>
    /// <para>Indicates if default outlook filters have been populated.</para>
    /// <para>Display Name: Default Filters Populated</para>
    /// </summary>
    [AttributeLogicalName("defaultfilterspopulated")]
    [DisplayName("Default Filters Populated")]
    public bool? DefaultFiltersPopulated
    {
        get => GetAttributeValue<bool?>("defaultfilterspopulated");
        set => SetAttributeValue("defaultfilterspopulated", value);
    }

    /// <summary>
    /// <para>Select the mailbox associated with this user.</para>
    /// <para>Display Name: Mailbox</para>
    /// </summary>
    [AttributeLogicalName("defaultmailbox")]
    [DisplayName("Mailbox")]
    public EntityReference? DefaultMailbox
    {
        get => GetAttributeValue<EntityReference?>("defaultmailbox");
        set => SetAttributeValue("defaultmailbox", value);
    }

    /// <summary>
    /// <para>Type a default folder name for the user's OneDrive For Business location.</para>
    /// <para>Display Name: Default OneDrive for Business Folder Name</para>
    /// </summary>
    [AttributeLogicalName("defaultodbfoldername")]
    [DisplayName("Default OneDrive for Business Folder Name")]
    [MaxLength(200)]
    public string? DefaultOdbFolderName
    {
        get => GetAttributeValue<string?>("defaultodbfoldername");
        set => SetAttributeValue("defaultodbfoldername", value);
    }

    /// <summary>
    /// <para>User delete state</para>
    /// <para>Display Name: Deleted State</para>
    /// </summary>
    [AttributeLogicalName("deletedstate")]
    [DisplayName("Deleted State")]
    public systemuser_deletestate? DeletedState
    {
        get => this.GetOptionSetValue<systemuser_deletestate>("deletedstate");
        set => this.SetOptionSetValue("deletedstate", value);
    }

    /// <summary>
    /// <para>Reason for disabling the user.</para>
    /// <para>Display Name: Disabled Reason</para>
    /// </summary>
    [AttributeLogicalName("disabledreason")]
    [DisplayName("Disabled Reason")]
    [MaxLength(500)]
    public string? DisabledReason
    {
        get => GetAttributeValue<string?>("disabledreason");
        set => SetAttributeValue("disabledreason", value);
    }

    /// <summary>
    /// <para>Whether to display the user in service views.</para>
    /// <para>Display Name: Display in Service Views</para>
    /// </summary>
    [AttributeLogicalName("displayinserviceviews")]
    [DisplayName("Display in Service Views")]
    public bool? DisplayInServiceViews
    {
        get => GetAttributeValue<bool?>("displayinserviceviews");
        set => SetAttributeValue("displayinserviceviews", value);
    }

    /// <summary>
    /// <para>Active Directory domain of which the user is a member.</para>
    /// <para>Display Name: User Name</para>
    /// </summary>
    [AttributeLogicalName("domainname")]
    [DisplayName("User Name")]
    [MaxLength(1024)]
    public string? DomainName
    {
        get => GetAttributeValue<string?>("domainname");
        set => SetAttributeValue("domainname", value);
    }

    /// <summary>
    /// <para>Shows the status of the primary email address.</para>
    /// <para>Display Name: Primary Email Status</para>
    /// </summary>
    [AttributeLogicalName("emailrouteraccessapproval")]
    [DisplayName("Primary Email Status")]
    public systemuser_emailrouteraccessapproval? EmailRouterAccessApproval
    {
        get => this.GetOptionSetValue<systemuser_emailrouteraccessapproval>("emailrouteraccessapproval");
        set => this.SetOptionSetValue("emailrouteraccessapproval", value);
    }

    /// <summary>
    /// <para>Employee identifier for the user.</para>
    /// <para>Display Name: Employee</para>
    /// </summary>
    [AttributeLogicalName("employeeid")]
    [DisplayName("Employee")]
    [MaxLength(100)]
    public string? EmployeeId
    {
        get => GetAttributeValue<string?>("employeeid");
        set => SetAttributeValue("employeeid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Entity Image Id</para>
    /// </summary>
    [AttributeLogicalName("entityimageid")]
    [DisplayName("Entity Image Id")]
    public Guid? EntityImageId
    {
        get => GetAttributeValue<Guid?>("entityimageid");
        set => SetAttributeValue("entityimageid", value);
    }

    /// <summary>
    /// <para>Exchange rate for the currency associated with the systemuser with respect to the base currency.</para>
    /// <para>Display Name: Exchange Rate</para>
    /// </summary>
    [AttributeLogicalName("exchangerate")]
    [DisplayName("Exchange Rate")]
    public decimal? ExchangeRate
    {
        get => GetAttributeValue<decimal?>("exchangerate");
        set => SetAttributeValue("exchangerate", value);
    }

    /// <summary>
    /// <para>First name of the user.</para>
    /// <para>Display Name: First Name</para>
    /// </summary>
    [AttributeLogicalName("firstname")]
    [DisplayName("First Name")]
    [MaxLength(256)]
    public string? FirstName
    {
        get => GetAttributeValue<string?>("firstname");
        set => SetAttributeValue("firstname", value);
    }

    /// <summary>
    /// <para>Full name of the user.</para>
    /// <para>Display Name: Full Name</para>
    /// </summary>
    [AttributeLogicalName("fullname")]
    [DisplayName("Full Name")]
    [MaxLength(200)]
    public string? FullName
    {
        get => GetAttributeValue<string?>("fullname");
        set => SetAttributeValue("fullname", value);
    }

    /// <summary>
    /// <para>Government identifier for the user.</para>
    /// <para>Display Name: Government</para>
    /// </summary>
    [AttributeLogicalName("governmentid")]
    [DisplayName("Government")]
    [MaxLength(100)]
    public string? GovernmentId
    {
        get => GetAttributeValue<string?>("governmentid");
        set => SetAttributeValue("governmentid", value);
    }

    /// <summary>
    /// <para>Home phone number for the user.</para>
    /// <para>Display Name: Home Phone</para>
    /// </summary>
    [AttributeLogicalName("homephone")]
    [DisplayName("Home Phone")]
    [MaxLength(50)]
    public string? HomePhone
    {
        get => GetAttributeValue<string?>("homephone");
        set => SetAttributeValue("homephone", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Unique user identity id</para>
    /// </summary>
    [AttributeLogicalName("identityid")]
    [DisplayName("Unique user identity id")]
    [Range(-2147483648, 2147483647)]
    public int? IdentityId
    {
        get => GetAttributeValue<int?>("identityid");
        set => SetAttributeValue("identityid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the data import or data migration that created this record.</para>
    /// <para>Display Name: Import Sequence Number</para>
    /// </summary>
    [AttributeLogicalName("importsequencenumber")]
    [DisplayName("Import Sequence Number")]
    [Range(-2147483648, 2147483647)]
    public int? ImportSequenceNumber
    {
        get => GetAttributeValue<int?>("importsequencenumber");
        set => SetAttributeValue("importsequencenumber", value);
    }

    /// <summary>
    /// <para>Incoming email delivery method for the user.</para>
    /// <para>Display Name: Incoming Email Delivery Method</para>
    /// </summary>
    [AttributeLogicalName("incomingemaildeliverymethod")]
    [DisplayName("Incoming Email Delivery Method")]
    public systemuser_incomingemaildeliverymethod? IncomingEmailDeliveryMethod
    {
        get => this.GetOptionSetValue<systemuser_incomingemaildeliverymethod>("incomingemaildeliverymethod");
        set => this.SetOptionSetValue("incomingemaildeliverymethod", value);
    }

    /// <summary>
    /// <para>Internal email address for the user.</para>
    /// <para>Display Name: Primary Email</para>
    /// </summary>
    [AttributeLogicalName("internalemailaddress")]
    [DisplayName("Primary Email")]
    [MaxLength(100)]
    public string? InternalEMailAddress
    {
        get => GetAttributeValue<string?>("internalemailaddress");
        set => SetAttributeValue("internalemailaddress", value);
    }

    /// <summary>
    /// <para>User invitation status.</para>
    /// <para>Display Name: Invitation Status</para>
    /// </summary>
    [AttributeLogicalName("invitestatuscode")]
    [DisplayName("Invitation Status")]
    public systemuser_invitestatuscode? InviteStatusCode
    {
        get => this.GetOptionSetValue<systemuser_invitestatuscode>("invitestatuscode");
        set => this.SetOptionSetValue("invitestatuscode", value);
    }

    /// <summary>
    /// <para>Information about whether the user is an AD user.</para>
    /// <para>Display Name: Is Active Directory User</para>
    /// </summary>
    [AttributeLogicalName("isactivedirectoryuser")]
    [DisplayName("Is Active Directory User")]
    public bool? IsActiveDirectoryUser
    {
        get => GetAttributeValue<bool?>("isactivedirectoryuser");
        set => SetAttributeValue("isactivedirectoryuser", value);
    }

    /// <summary>
    /// <para>Bypasses the selected user from IP firewall restriction</para>
    /// <para>Display Name: To bypass IP firewall restriction on the user</para>
    /// </summary>
    [AttributeLogicalName("isallowedbyipfirewall")]
    [DisplayName("To bypass IP firewall restriction on the user")]
    public bool? IsAllowedByIpFirewall
    {
        get => GetAttributeValue<bool?>("isallowedbyipfirewall");
        set => SetAttributeValue("isallowedbyipfirewall", value);
    }

    /// <summary>
    /// <para>Information about whether the user is enabled.</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("isdisabled")]
    [DisplayName("Status")]
    public bool? IsDisabled
    {
        get => GetAttributeValue<bool?>("isdisabled");
        set => SetAttributeValue("isdisabled", value);
    }

    /// <summary>
    /// <para>Shows the status of approval of the email address by O365 Admin.</para>
    /// <para>Display Name: Email Address O365 Admin Approval Status</para>
    /// </summary>
    [AttributeLogicalName("isemailaddressapprovedbyo365admin")]
    [DisplayName("Email Address O365 Admin Approval Status")]
    public bool? IsEmailAddressApprovedByO365Admin
    {
        get => GetAttributeValue<bool?>("isemailaddressapprovedbyo365admin");
        set => SetAttributeValue("isemailaddressapprovedbyo365admin", value);
    }

    /// <summary>
    /// <para>Check if user is an integration user.</para>
    /// <para>Display Name: Integration user mode</para>
    /// </summary>
    [AttributeLogicalName("isintegrationuser")]
    [DisplayName("Integration user mode")]
    public bool? IsIntegrationUser
    {
        get => GetAttributeValue<bool?>("isintegrationuser");
        set => SetAttributeValue("isintegrationuser", value);
    }

    /// <summary>
    /// <para>Information about whether the user is licensed.</para>
    /// <para>Display Name: User Licensed</para>
    /// </summary>
    [AttributeLogicalName("islicensed")]
    [DisplayName("User Licensed")]
    public bool? IsLicensed
    {
        get => GetAttributeValue<bool?>("islicensed");
        set => SetAttributeValue("islicensed", value);
    }

    /// <summary>
    /// <para>Information about whether the user is synced with the directory.</para>
    /// <para>Display Name: User Synced</para>
    /// </summary>
    [AttributeLogicalName("issyncwithdirectory")]
    [DisplayName("User Synced")]
    public bool? IsSyncWithDirectory
    {
        get => GetAttributeValue<bool?>("issyncwithdirectory");
        set => SetAttributeValue("issyncwithdirectory", value);
    }

    /// <summary>
    /// <para>Job title of the user.</para>
    /// <para>Display Name: Job Title</para>
    /// </summary>
    [AttributeLogicalName("jobtitle")]
    [DisplayName("Job Title")]
    [MaxLength(100)]
    public string? JobTitle
    {
        get => GetAttributeValue<string?>("jobtitle");
        set => SetAttributeValue("jobtitle", value);
    }

    /// <summary>
    /// <para>Last name of the user.</para>
    /// <para>Display Name: Last Name</para>
    /// </summary>
    [AttributeLogicalName("lastname")]
    [DisplayName("Last Name")]
    [MaxLength(256)]
    public string? LastName
    {
        get => GetAttributeValue<string?>("lastname");
        set => SetAttributeValue("lastname", value);
    }

    /// <summary>
    /// <para>Time stamp of the latest update for the user</para>
    /// <para>Display Name: Latest User Update Time</para>
    /// </summary>
    [AttributeLogicalName("latestupdatetime")]
    [DisplayName("Latest User Update Time")]
    public DateTime? LatestUpdateTime
    {
        get => GetAttributeValue<DateTime?>("latestupdatetime");
        set => SetAttributeValue("latestupdatetime", value);
    }

    /// <summary>
    /// <para>Middle name of the user.</para>
    /// <para>Display Name: Middle Name</para>
    /// </summary>
    [AttributeLogicalName("middlename")]
    [DisplayName("Middle Name")]
    [MaxLength(50)]
    public string? MiddleName
    {
        get => GetAttributeValue<string?>("middlename");
        set => SetAttributeValue("middlename", value);
    }

    /// <summary>
    /// <para>Mobile alert email address for the user.</para>
    /// <para>Display Name: Mobile Alert Email</para>
    /// </summary>
    [AttributeLogicalName("mobilealertemail")]
    [DisplayName("Mobile Alert Email")]
    [MaxLength(100)]
    public string? MobileAlertEMail
    {
        get => GetAttributeValue<string?>("mobilealertemail");
        set => SetAttributeValue("mobilealertemail", value);
    }

    /// <summary>
    /// <para>Items contained with a particular SystemUser.</para>
    /// <para>Display Name: Mobile Offline Profile</para>
    /// </summary>
    [AttributeLogicalName("mobileofflineprofileid")]
    [DisplayName("Mobile Offline Profile")]
    public EntityReference? MobileOfflineProfileId
    {
        get => GetAttributeValue<EntityReference?>("mobileofflineprofileid");
        set => SetAttributeValue("mobileofflineprofileid", value);
    }

    /// <summary>
    /// <para>Mobile phone number for the user.</para>
    /// <para>Display Name: Mobile Phone</para>
    /// </summary>
    [AttributeLogicalName("mobilephone")]
    [DisplayName("Mobile Phone")]
    [MaxLength(64)]
    public string? MobilePhone
    {
        get => GetAttributeValue<string?>("mobilephone");
        set => SetAttributeValue("mobilephone", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who last modified the user.</para>
    /// <para>Display Name: Modified By</para>
    /// </summary>
    [AttributeLogicalName("modifiedby")]
    [DisplayName("Modified By")]
    public EntityReference? ModifiedBy
    {
        get => GetAttributeValue<EntityReference?>("modifiedby");
        set => SetAttributeValue("modifiedby", value);
    }

    /// <summary>
    /// <para>Date and time when the user was last modified.</para>
    /// <para>Display Name: Modified On</para>
    /// </summary>
    [AttributeLogicalName("modifiedon")]
    [DisplayName("Modified On")]
    public DateTime? ModifiedOn
    {
        get => GetAttributeValue<DateTime?>("modifiedon");
        set => SetAttributeValue("modifiedon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the delegate user who last modified the systemuser.</para>
    /// <para>Display Name: Modified By (Delegate)</para>
    /// </summary>
    [AttributeLogicalName("modifiedonbehalfby")]
    [DisplayName("Modified By (Delegate)")]
    public EntityReference? ModifiedOnBehalfBy
    {
        get => GetAttributeValue<EntityReference?>("modifiedonbehalfby");
        set => SetAttributeValue("modifiedonbehalfby", value);
    }

    /// <summary>
    /// <para>Nickname of the user.</para>
    /// <para>Display Name: Nickname</para>
    /// </summary>
    [AttributeLogicalName("nickname")]
    [DisplayName("Nickname")]
    [MaxLength(50)]
    public string? NickName
    {
        get => GetAttributeValue<string?>("nickname");
        set => SetAttributeValue("nickname", value);
    }

    /// <summary>
    /// <para>Unique identifier of the organization associated with the user.</para>
    /// <para>Display Name: Organization </para>
    /// </summary>
    [AttributeLogicalName("organizationid")]
    [DisplayName("Organization ")]
    public Guid? OrganizationId
    {
        get => GetAttributeValue<Guid?>("organizationid");
        set => SetAttributeValue("organizationid", value);
    }

    /// <summary>
    /// <para>Outgoing email delivery method for the user.</para>
    /// <para>Display Name: Outgoing Email Delivery Method</para>
    /// </summary>
    [AttributeLogicalName("outgoingemaildeliverymethod")]
    [DisplayName("Outgoing Email Delivery Method")]
    public systemuser_outgoingemaildeliverymethod? OutgoingEmailDeliveryMethod
    {
        get => this.GetOptionSetValue<systemuser_outgoingemaildeliverymethod>("outgoingemaildeliverymethod");
        set => this.SetOptionSetValue("outgoingemaildeliverymethod", value);
    }

    /// <summary>
    /// <para>Date and time that the record was migrated.</para>
    /// <para>Display Name: Record Created On</para>
    /// </summary>
    [AttributeLogicalName("overriddencreatedon")]
    [DisplayName("Record Created On")]
    public DateTime? OverriddenCreatedOn
    {
        get => GetAttributeValue<DateTime?>("overriddencreatedon");
        set => SetAttributeValue("overriddencreatedon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the manager of the user.</para>
    /// <para>Display Name: Manager</para>
    /// </summary>
    [AttributeLogicalName("parentsystemuserid")]
    [DisplayName("Manager")]
    public EntityReference? ParentSystemUserId
    {
        get => GetAttributeValue<EntityReference?>("parentsystemuserid");
        set => SetAttributeValue("parentsystemuserid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Passport Hi</para>
    /// </summary>
    [AttributeLogicalName("passporthi")]
    [DisplayName("Passport Hi")]
    [Range(0, 1000000000)]
    public int? PassportHi
    {
        get => GetAttributeValue<int?>("passporthi");
        set => SetAttributeValue("passporthi", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Passport Lo</para>
    /// </summary>
    [AttributeLogicalName("passportlo")]
    [DisplayName("Passport Lo")]
    [Range(0, 1000000000)]
    public int? PassportLo
    {
        get => GetAttributeValue<int?>("passportlo");
        set => SetAttributeValue("passportlo", value);
    }

    /// <summary>
    /// <para>Personal email address of the user.</para>
    /// <para>Display Name: Email 2</para>
    /// </summary>
    [AttributeLogicalName("personalemailaddress")]
    [DisplayName("Email 2")]
    [MaxLength(100)]
    public string? PersonalEMailAddress
    {
        get => GetAttributeValue<string?>("personalemailaddress");
        set => SetAttributeValue("personalemailaddress", value);
    }

    /// <summary>
    /// <para>URL for the Website on which a photo of the user is located.</para>
    /// <para>Display Name: Photo URL</para>
    /// </summary>
    [AttributeLogicalName("photourl")]
    [DisplayName("Photo URL")]
    [MaxLength(200)]
    public string? PhotoUrl
    {
        get => GetAttributeValue<string?>("photourl");
        set => SetAttributeValue("photourl", value);
    }

    /// <summary>
    /// <para>User's position in hierarchical security model.</para>
    /// <para>Display Name: Position</para>
    /// </summary>
    [AttributeLogicalName("positionid")]
    [DisplayName("Position")]
    public EntityReference? PositionId
    {
        get => GetAttributeValue<EntityReference?>("positionid");
        set => SetAttributeValue("positionid", value);
    }

    /// <summary>
    /// <para>Preferred address for the user.</para>
    /// <para>Display Name: Preferred Address</para>
    /// </summary>
    [AttributeLogicalName("preferredaddresscode")]
    [DisplayName("Preferred Address")]
    public systemuser_preferredaddresscode? PreferredAddressCode
    {
        get => this.GetOptionSetValue<systemuser_preferredaddresscode>("preferredaddresscode");
        set => this.SetOptionSetValue("preferredaddresscode", value);
    }

    /// <summary>
    /// <para>Preferred email address for the user.</para>
    /// <para>Display Name: Preferred Email</para>
    /// </summary>
    [AttributeLogicalName("preferredemailcode")]
    [DisplayName("Preferred Email")]
    public systemuser_preferredemailcode? PreferredEmailCode
    {
        get => this.GetOptionSetValue<systemuser_preferredemailcode>("preferredemailcode");
        set => this.SetOptionSetValue("preferredemailcode", value);
    }

    /// <summary>
    /// <para>Preferred phone number for the user.</para>
    /// <para>Display Name: Preferred Phone</para>
    /// </summary>
    [AttributeLogicalName("preferredphonecode")]
    [DisplayName("Preferred Phone")]
    public systemuser_preferredphonecode? PreferredPhoneCode
    {
        get => this.GetOptionSetValue<systemuser_preferredphonecode>("preferredphonecode");
        set => this.SetOptionSetValue("preferredphonecode", value);
    }

    /// <summary>
    /// <para>Shows the ID of the process.</para>
    /// <para>Display Name: Process</para>
    /// </summary>
    [AttributeLogicalName("processid")]
    [DisplayName("Process")]
    public Guid? ProcessId
    {
        get => GetAttributeValue<Guid?>("processid");
        set => SetAttributeValue("processid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the default queue for the user.</para>
    /// <para>Display Name: Default Queue</para>
    /// </summary>
    [AttributeLogicalName("queueid")]
    [DisplayName("Default Queue")]
    public EntityReference? QueueId
    {
        get => GetAttributeValue<EntityReference?>("queueid");
        set => SetAttributeValue("queueid", value);
    }

    /// <summary>
    /// <para>Salutation for correspondence with the user.</para>
    /// <para>Display Name: Salutation</para>
    /// </summary>
    [AttributeLogicalName("salutation")]
    [DisplayName("Salutation")]
    [MaxLength(20)]
    public string? Salutation
    {
        get => GetAttributeValue<string?>("salutation");
        set => SetAttributeValue("salutation", value);
    }

    /// <summary>
    /// <para>Check if user is a setup user.</para>
    /// <para>Display Name: Restricted Access Mode</para>
    /// </summary>
    [AttributeLogicalName("setupuser")]
    [DisplayName("Restricted Access Mode")]
    public bool? SetupUser
    {
        get => GetAttributeValue<bool?>("setupuser");
        set => SetAttributeValue("setupuser", value);
    }

    /// <summary>
    /// <para>SharePoint Work Email Address</para>
    /// <para>Display Name: SharePoint Email Address</para>
    /// </summary>
    [AttributeLogicalName("sharepointemailaddress")]
    [DisplayName("SharePoint Email Address")]
    [MaxLength(1024)]
    public string? SharePointEmailAddress
    {
        get => GetAttributeValue<string?>("sharepointemailaddress");
        set => SetAttributeValue("sharepointemailaddress", value);
    }

    /// <summary>
    /// <para>Skill set of the user.</para>
    /// <para>Display Name: Skills</para>
    /// </summary>
    [AttributeLogicalName("skills")]
    [DisplayName("Skills")]
    [MaxLength(100)]
    public string? Skills
    {
        get => GetAttributeValue<string?>("skills");
        set => SetAttributeValue("skills", value);
    }

    /// <summary>
    /// <para>Shows the ID of the stage.</para>
    /// <para>Display Name: (Deprecated) Process Stage</para>
    /// </summary>
    [AttributeLogicalName("stageid")]
    [DisplayName("(Deprecated) Process Stage")]
    public Guid? StageId
    {
        get => GetAttributeValue<Guid?>("stageid");
        set => SetAttributeValue("stageid", value);
    }

    /// <summary>
    /// <para>The type of user</para>
    /// <para>Display Name: System Managed User Type</para>
    /// </summary>
    [AttributeLogicalName("systemmanagedusertype")]
    [DisplayName("System Managed User Type")]
    public systemuser_systemmanagedusertype? SystemManagedUserType
    {
        get => this.GetOptionSetValue<systemuser_systemmanagedusertype>("systemmanagedusertype");
        set => this.SetOptionSetValue("systemmanagedusertype", value);
    }

    /// <summary>
    /// <para>Display Name: User</para>
    /// </summary>
    [AttributeLogicalName("systemuserid")]
    [DisplayName("User")]
    public Guid? SystemUserId
    {
        get => GetAttributeValue<Guid?>("systemuserid");
        set => SetId("systemuserid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the territory to which the user is assigned.</para>
    /// <para>Display Name: Territory</para>
    /// </summary>
    [AttributeLogicalName("territoryid")]
    [DisplayName("Territory")]
    public EntityReference? TerritoryId
    {
        get => GetAttributeValue<EntityReference?>("territoryid");
        set => SetAttributeValue("territoryid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Time Zone Rule Version Number</para>
    /// </summary>
    [AttributeLogicalName("timezoneruleversionnumber")]
    [DisplayName("Time Zone Rule Version Number")]
    [Range(-1, 2147483647)]
    public int? TimeZoneRuleVersionNumber
    {
        get => GetAttributeValue<int?>("timezoneruleversionnumber");
        set => SetAttributeValue("timezoneruleversionnumber", value);
    }

    /// <summary>
    /// <para>Title of the user.</para>
    /// <para>Display Name: Title</para>
    /// </summary>
    [AttributeLogicalName("title")]
    [DisplayName("Title")]
    [MaxLength(128)]
    public string? Title
    {
        get => GetAttributeValue<string?>("title");
        set => SetAttributeValue("title", value);
    }

    /// <summary>
    /// <para>Unique identifier of the currency associated with the systemuser.</para>
    /// <para>Display Name: Currency</para>
    /// </summary>
    [AttributeLogicalName("transactioncurrencyid")]
    [DisplayName("Currency")]
    public EntityReference? TransactionCurrencyId
    {
        get => GetAttributeValue<EntityReference?>("transactioncurrencyid");
        set => SetAttributeValue("transactioncurrencyid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: (Deprecated) Traversed Path</para>
    /// </summary>
    [AttributeLogicalName("traversedpath")]
    [DisplayName("(Deprecated) Traversed Path")]
    [MaxLength(1250)]
    public string? TraversedPath
    {
        get => GetAttributeValue<string?>("traversedpath");
        set => SetAttributeValue("traversedpath", value);
    }

    /// <summary>
    /// <para>Shows the type of user license.</para>
    /// <para>Display Name: User License Type</para>
    /// </summary>
    [AttributeLogicalName("userlicensetype")]
    [DisplayName("User License Type")]
    [Range(-2147483648, 2147483647)]
    public int? UserLicenseType
    {
        get => GetAttributeValue<int?>("userlicensetype");
        set => SetAttributeValue("userlicensetype", value);
    }

    /// <summary>
    /// <para>User PUID User Identifiable Information</para>
    /// <para>Display Name: User PUID</para>
    /// </summary>
    [AttributeLogicalName("userpuid")]
    [DisplayName("User PUID")]
    [MaxLength(100)]
    public string? UserPuid
    {
        get => GetAttributeValue<string?>("userpuid");
        set => SetAttributeValue("userpuid", value);
    }

    /// <summary>
    /// <para>Time zone code that was in use when the record was created.</para>
    /// <para>Display Name: UTC Conversion Time Zone Code</para>
    /// </summary>
    [AttributeLogicalName("utcconversiontimezonecode")]
    [DisplayName("UTC Conversion Time Zone Code")]
    [Range(-1, 2147483647)]
    public int? UTCConversionTimeZoneCode
    {
        get => GetAttributeValue<int?>("utcconversiontimezonecode");
        set => SetAttributeValue("utcconversiontimezonecode", value);
    }

    /// <summary>
    /// <para>Version number of the user.</para>
    /// <para>Display Name: Version number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    /// <summary>
    /// <para>Windows Live ID</para>
    /// <para>Display Name: Windows Live ID</para>
    /// </summary>
    [AttributeLogicalName("windowsliveid")]
    [DisplayName("Windows Live ID")]
    [MaxLength(1024)]
    public string? WindowsLiveID
    {
        get => GetAttributeValue<string?>("windowsliveid");
        set => SetAttributeValue("windowsliveid", value);
    }

    /// <summary>
    /// <para>User's Yammer login email address</para>
    /// <para>Display Name: Yammer Email</para>
    /// </summary>
    [AttributeLogicalName("yammeremailaddress")]
    [DisplayName("Yammer Email")]
    [MaxLength(200)]
    public string? YammerEmailAddress
    {
        get => GetAttributeValue<string?>("yammeremailaddress");
        set => SetAttributeValue("yammeremailaddress", value);
    }

    /// <summary>
    /// <para>User's Yammer ID</para>
    /// <para>Display Name: Yammer User ID</para>
    /// </summary>
    [AttributeLogicalName("yammeruserid")]
    [DisplayName("Yammer User ID")]
    [MaxLength(128)]
    public string? YammerUserId
    {
        get => GetAttributeValue<string?>("yammeruserid");
        set => SetAttributeValue("yammeruserid", value);
    }

    /// <summary>
    /// <para>Pronunciation of the first name of the user, written in phonetic hiragana or katakana characters.</para>
    /// <para>Display Name: Yomi First Name</para>
    /// </summary>
    [AttributeLogicalName("yomifirstname")]
    [DisplayName("Yomi First Name")]
    [MaxLength(64)]
    public string? YomiFirstName
    {
        get => GetAttributeValue<string?>("yomifirstname");
        set => SetAttributeValue("yomifirstname", value);
    }

    /// <summary>
    /// <para>Pronunciation of the full name of the user, written in phonetic hiragana or katakana characters.</para>
    /// <para>Display Name: Yomi Full Name</para>
    /// </summary>
    [AttributeLogicalName("yomifullname")]
    [DisplayName("Yomi Full Name")]
    [MaxLength(200)]
    public string? YomiFullName
    {
        get => GetAttributeValue<string?>("yomifullname");
        set => SetAttributeValue("yomifullname", value);
    }

    /// <summary>
    /// <para>Pronunciation of the last name of the user, written in phonetic hiragana or katakana characters.</para>
    /// <para>Display Name: Yomi Last Name</para>
    /// </summary>
    [AttributeLogicalName("yomilastname")]
    [DisplayName("Yomi Last Name")]
    [MaxLength(64)]
    public string? YomiLastName
    {
        get => GetAttributeValue<string?>("yomilastname");
        set => SetAttributeValue("yomilastname", value);
    }

    /// <summary>
    /// <para>Pronunciation of the middle name of the user, written in phonetic hiragana or katakana characters.</para>
    /// <para>Display Name: Yomi Middle Name</para>
    /// </summary>
    [AttributeLogicalName("yomimiddlename")]
    [DisplayName("Yomi Middle Name")]
    [MaxLength(50)]
    public string? YomiMiddleName
    {
        get => GetAttributeValue<string?>("yomimiddlename");
        set => SetAttributeValue("yomimiddlename", value);
    }

    [AttributeLogicalName("businessunitid")]
    [RelationshipSchemaName("business_unit_system_users")]
    [RelationshipMetadata("ManyToOne", "businessunitid", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_system_users
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_system_users", null);
        set => SetRelatedEntity("business_unit_system_users", null, value);
    }

    [RelationshipSchemaName("contact_owning_user")]
    [RelationshipMetadata("OneToMany", "systemuserid", "contact", "owninguser", "Referenced")]
    public IEnumerable<Contact> contact_owning_user
    {
        get => GetRelatedEntities<Contact>("contact_owning_user", null);
        set => SetRelatedEntities("contact_owning_user", null, value);
    }

    [RelationshipSchemaName("email_acceptingentity_systemuser")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "acceptingentityid", "Referenced")]
    public IEnumerable<Email> email_acceptingentity_systemuser
    {
        get => GetRelatedEntities<Email>("email_acceptingentity_systemuser", null);
        set => SetRelatedEntities("email_acceptingentity_systemuser", null, value);
    }

    [RelationshipSchemaName("lk_accountbase_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "account", "createdby", "Referenced")]
    public IEnumerable<Account> lk_accountbase_createdby
    {
        get => GetRelatedEntities<Account>("lk_accountbase_createdby", null);
        set => SetRelatedEntities("lk_accountbase_createdby", null, value);
    }

    [RelationshipSchemaName("lk_accountbase_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "account", "createdonbehalfby", "Referenced")]
    public IEnumerable<Account> lk_accountbase_createdonbehalfby
    {
        get => GetRelatedEntities<Account>("lk_accountbase_createdonbehalfby", null);
        set => SetRelatedEntities("lk_accountbase_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_accountbase_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "account", "modifiedby", "Referenced")]
    public IEnumerable<Account> lk_accountbase_modifiedby
    {
        get => GetRelatedEntities<Account>("lk_accountbase_modifiedby", null);
        set => SetRelatedEntities("lk_accountbase_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_accountbase_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "account", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Account> lk_accountbase_modifiedonbehalfby
    {
        get => GetRelatedEntities<Account>("lk_accountbase_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_accountbase_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_activitypointer_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "activitypointer", "createdby", "Referenced")]
    public IEnumerable<ActivityPointer> lk_activitypointer_createdby
    {
        get => GetRelatedEntities<ActivityPointer>("lk_activitypointer_createdby", null);
        set => SetRelatedEntities("lk_activitypointer_createdby", null, value);
    }

    [RelationshipSchemaName("lk_activitypointer_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "activitypointer", "createdonbehalfby", "Referenced")]
    public IEnumerable<ActivityPointer> lk_activitypointer_createdonbehalfby
    {
        get => GetRelatedEntities<ActivityPointer>("lk_activitypointer_createdonbehalfby", null);
        set => SetRelatedEntities("lk_activitypointer_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_activitypointer_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "activitypointer", "modifiedby", "Referenced")]
    public IEnumerable<ActivityPointer> lk_activitypointer_modifiedby
    {
        get => GetRelatedEntities<ActivityPointer>("lk_activitypointer_modifiedby", null);
        set => SetRelatedEntities("lk_activitypointer_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_activitypointer_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "activitypointer", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<ActivityPointer> lk_activitypointer_modifiedonbehalfby
    {
        get => GetRelatedEntities<ActivityPointer>("lk_activitypointer_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_activitypointer_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_businessunit_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "businessunit", "createdonbehalfby", "Referenced")]
    public IEnumerable<BusinessUnit> lk_businessunit_createdonbehalfby
    {
        get => GetRelatedEntities<BusinessUnit>("lk_businessunit_createdonbehalfby", null);
        set => SetRelatedEntities("lk_businessunit_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_businessunit_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "businessunit", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<BusinessUnit> lk_businessunit_modifiedonbehalfby
    {
        get => GetRelatedEntities<BusinessUnit>("lk_businessunit_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_businessunit_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_businessunitbase_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "businessunit", "createdby", "Referenced")]
    public IEnumerable<BusinessUnit> lk_businessunitbase_createdby
    {
        get => GetRelatedEntities<BusinessUnit>("lk_businessunitbase_createdby", null);
        set => SetRelatedEntities("lk_businessunitbase_createdby", null, value);
    }

    [RelationshipSchemaName("lk_businessunitbase_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "businessunit", "modifiedby", "Referenced")]
    public IEnumerable<BusinessUnit> lk_businessunitbase_modifiedby
    {
        get => GetRelatedEntities<BusinessUnit>("lk_businessunitbase_modifiedby", null);
        set => SetRelatedEntities("lk_businessunitbase_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_contact_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "contact", "createdonbehalfby", "Referenced")]
    public IEnumerable<Contact> lk_contact_createdonbehalfby
    {
        get => GetRelatedEntities<Contact>("lk_contact_createdonbehalfby", null);
        set => SetRelatedEntities("lk_contact_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_contact_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "contact", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Contact> lk_contact_modifiedonbehalfby
    {
        get => GetRelatedEntities<Contact>("lk_contact_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_contact_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_contactbase_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "contact", "createdby", "Referenced")]
    public IEnumerable<Contact> lk_contactbase_createdby
    {
        get => GetRelatedEntities<Contact>("lk_contactbase_createdby", null);
        set => SetRelatedEntities("lk_contactbase_createdby", null, value);
    }

    [RelationshipSchemaName("lk_contactbase_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "contact", "modifiedby", "Referenced")]
    public IEnumerable<Contact> lk_contactbase_modifiedby
    {
        get => GetRelatedEntities<Contact>("lk_contactbase_modifiedby", null);
        set => SetRelatedEntities("lk_contactbase_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_child_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_child", "createdby", "Referenced")]
    public IEnumerable<ctx_child> lk_ctx_child_createdby
    {
        get => GetRelatedEntities<ctx_child>("lk_ctx_child_createdby", null);
        set => SetRelatedEntities("lk_ctx_child_createdby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_child_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_child", "createdonbehalfby", "Referenced")]
    public IEnumerable<ctx_child> lk_ctx_child_createdonbehalfby
    {
        get => GetRelatedEntities<ctx_child>("lk_ctx_child_createdonbehalfby", null);
        set => SetRelatedEntities("lk_ctx_child_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_child_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_child", "modifiedby", "Referenced")]
    public IEnumerable<ctx_child> lk_ctx_child_modifiedby
    {
        get => GetRelatedEntities<ctx_child>("lk_ctx_child_modifiedby", null);
        set => SetRelatedEntities("lk_ctx_child_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_child_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_child", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<ctx_child> lk_ctx_child_modifiedonbehalfby
    {
        get => GetRelatedEntities<ctx_child>("lk_ctx_child_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_ctx_child_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_parent_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_parent", "createdby", "Referenced")]
    public IEnumerable<ctx_parent> lk_ctx_parent_createdby
    {
        get => GetRelatedEntities<ctx_parent>("lk_ctx_parent_createdby", null);
        set => SetRelatedEntities("lk_ctx_parent_createdby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_parent_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_parent", "createdonbehalfby", "Referenced")]
    public IEnumerable<ctx_parent> lk_ctx_parent_createdonbehalfby
    {
        get => GetRelatedEntities<ctx_parent>("lk_ctx_parent_createdonbehalfby", null);
        set => SetRelatedEntities("lk_ctx_parent_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_parent_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_parent", "modifiedby", "Referenced")]
    public IEnumerable<ctx_parent> lk_ctx_parent_modifiedby
    {
        get => GetRelatedEntities<ctx_parent>("lk_ctx_parent_modifiedby", null);
        set => SetRelatedEntities("lk_ctx_parent_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_ctx_parent_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_parent", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<ctx_parent> lk_ctx_parent_modifiedonbehalfby
    {
        get => GetRelatedEntities<ctx_parent>("lk_ctx_parent_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_ctx_parent_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_email_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "createdby", "Referenced")]
    public IEnumerable<Email> lk_email_createdby
    {
        get => GetRelatedEntities<Email>("lk_email_createdby", null);
        set => SetRelatedEntities("lk_email_createdby", null, value);
    }

    [RelationshipSchemaName("lk_email_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "createdonbehalfby", "Referenced")]
    public IEnumerable<Email> lk_email_createdonbehalfby
    {
        get => GetRelatedEntities<Email>("lk_email_createdonbehalfby", null);
        set => SetRelatedEntities("lk_email_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_email_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "modifiedby", "Referenced")]
    public IEnumerable<Email> lk_email_modifiedby
    {
        get => GetRelatedEntities<Email>("lk_email_modifiedby", null);
        set => SetRelatedEntities("lk_email_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_email_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Email> lk_email_modifiedonbehalfby
    {
        get => GetRelatedEntities<Email>("lk_email_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_email_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_fax_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "fax", "createdby", "Referenced")]
    public IEnumerable<Fax> lk_fax_createdby
    {
        get => GetRelatedEntities<Fax>("lk_fax_createdby", null);
        set => SetRelatedEntities("lk_fax_createdby", null, value);
    }

    [RelationshipSchemaName("lk_fax_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "fax", "createdonbehalfby", "Referenced")]
    public IEnumerable<Fax> lk_fax_createdonbehalfby
    {
        get => GetRelatedEntities<Fax>("lk_fax_createdonbehalfby", null);
        set => SetRelatedEntities("lk_fax_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_fax_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "fax", "modifiedby", "Referenced")]
    public IEnumerable<Fax> lk_fax_modifiedby
    {
        get => GetRelatedEntities<Fax>("lk_fax_modifiedby", null);
        set => SetRelatedEntities("lk_fax_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_fax_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "fax", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Fax> lk_fax_modifiedonbehalfby
    {
        get => GetRelatedEntities<Fax>("lk_fax_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_fax_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_systemuser_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_systemuser_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_systemuser_createdonbehalfby", null);
        set => SetRelatedEntity("lk_systemuser_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_systemuser_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_systemuser_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_systemuser_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_systemuser_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_systemuserbase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_systemuserbase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_systemuserbase_createdby", null);
        set => SetRelatedEntity("lk_systemuserbase_createdby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_systemuserbase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_systemuserbase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_systemuserbase_modifiedby", null);
        set => SetRelatedEntity("lk_systemuserbase_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_task_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "task", "createdby", "Referenced")]
    public IEnumerable<Task> lk_task_createdby
    {
        get => GetRelatedEntities<Task>("lk_task_createdby", null);
        set => SetRelatedEntities("lk_task_createdby", null, value);
    }

    [RelationshipSchemaName("lk_task_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "task", "createdonbehalfby", "Referenced")]
    public IEnumerable<Task> lk_task_createdonbehalfby
    {
        get => GetRelatedEntities<Task>("lk_task_createdonbehalfby", null);
        set => SetRelatedEntities("lk_task_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_task_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "task", "modifiedby", "Referenced")]
    public IEnumerable<Task> lk_task_modifiedby
    {
        get => GetRelatedEntities<Task>("lk_task_modifiedby", null);
        set => SetRelatedEntities("lk_task_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_task_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "task", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Task> lk_task_modifiedonbehalfby
    {
        get => GetRelatedEntities<Task>("lk_task_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_task_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_team_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "team", "createdonbehalfby", "Referenced")]
    public IEnumerable<Team> lk_team_createdonbehalfby
    {
        get => GetRelatedEntities<Team>("lk_team_createdonbehalfby", null);
        set => SetRelatedEntities("lk_team_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_team_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "team", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Team> lk_team_modifiedonbehalfby
    {
        get => GetRelatedEntities<Team>("lk_team_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_team_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_teambase_administratorid")]
    [RelationshipMetadata("OneToMany", "systemuserid", "team", "administratorid", "Referenced")]
    public IEnumerable<Team> lk_teambase_administratorid
    {
        get => GetRelatedEntities<Team>("lk_teambase_administratorid", null);
        set => SetRelatedEntities("lk_teambase_administratorid", null, value);
    }

    [RelationshipSchemaName("lk_teambase_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "team", "createdby", "Referenced")]
    public IEnumerable<Team> lk_teambase_createdby
    {
        get => GetRelatedEntities<Team>("lk_teambase_createdby", null);
        set => SetRelatedEntities("lk_teambase_createdby", null, value);
    }

    [RelationshipSchemaName("lk_teambase_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "team", "modifiedby", "Referenced")]
    public IEnumerable<Team> lk_teambase_modifiedby
    {
        get => GetRelatedEntities<Team>("lk_teambase_modifiedby", null);
        set => SetRelatedEntities("lk_teambase_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_templatebase_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "template", "createdby", "Referenced")]
    public IEnumerable<Template> lk_templatebase_createdby
    {
        get => GetRelatedEntities<Template>("lk_templatebase_createdby", null);
        set => SetRelatedEntities("lk_templatebase_createdby", null, value);
    }

    [RelationshipSchemaName("lk_templatebase_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "template", "createdonbehalfby", "Referenced")]
    public IEnumerable<Template> lk_templatebase_createdonbehalfby
    {
        get => GetRelatedEntities<Template>("lk_templatebase_createdonbehalfby", null);
        set => SetRelatedEntities("lk_templatebase_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_templatebase_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "template", "modifiedby", "Referenced")]
    public IEnumerable<Template> lk_templatebase_modifiedby
    {
        get => GetRelatedEntities<Template>("lk_templatebase_modifiedby", null);
        set => SetRelatedEntities("lk_templatebase_modifiedby", null, value);
    }

    [RelationshipSchemaName("lk_templatebase_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "template", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<Template> lk_templatebase_modifiedonbehalfby
    {
        get => GetRelatedEntities<Template>("lk_templatebase_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_templatebase_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_transactioncurrency_createdonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "transactioncurrency", "createdonbehalfby", "Referenced")]
    public IEnumerable<TransactionCurrency> lk_transactioncurrency_createdonbehalfby
    {
        get => GetRelatedEntities<TransactionCurrency>("lk_transactioncurrency_createdonbehalfby", null);
        set => SetRelatedEntities("lk_transactioncurrency_createdonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_transactioncurrency_modifiedonbehalfby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "transactioncurrency", "modifiedonbehalfby", "Referenced")]
    public IEnumerable<TransactionCurrency> lk_transactioncurrency_modifiedonbehalfby
    {
        get => GetRelatedEntities<TransactionCurrency>("lk_transactioncurrency_modifiedonbehalfby", null);
        set => SetRelatedEntities("lk_transactioncurrency_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("lk_transactioncurrencybase_createdby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "transactioncurrency", "createdby", "Referenced")]
    public IEnumerable<TransactionCurrency> lk_transactioncurrencybase_createdby
    {
        get => GetRelatedEntities<TransactionCurrency>("lk_transactioncurrencybase_createdby", null);
        set => SetRelatedEntities("lk_transactioncurrencybase_createdby", null, value);
    }

    [RelationshipSchemaName("lk_transactioncurrencybase_modifiedby")]
    [RelationshipMetadata("OneToMany", "systemuserid", "transactioncurrency", "modifiedby", "Referenced")]
    public IEnumerable<TransactionCurrency> lk_transactioncurrencybase_modifiedby
    {
        get => GetRelatedEntities<TransactionCurrency>("lk_transactioncurrencybase_modifiedby", null);
        set => SetRelatedEntities("lk_transactioncurrencybase_modifiedby", null, value);
    }

    [RelationshipSchemaName("system_user_accounts")]
    [RelationshipMetadata("OneToMany", "systemuserid", "account", "preferredsystemuserid", "Referenced")]
    public IEnumerable<Account> system_user_accounts
    {
        get => GetRelatedEntities<Account>("system_user_accounts", null);
        set => SetRelatedEntities("system_user_accounts", null, value);
    }

    [RelationshipSchemaName("system_user_activity_parties")]
    [RelationshipMetadata("OneToMany", "systemuserid", "activityparty", "partyid", "Referenced")]
    public IEnumerable<ActivityParty> system_user_activity_parties
    {
        get => GetRelatedEntities<ActivityParty>("system_user_activity_parties", null);
        set => SetRelatedEntities("system_user_activity_parties", null, value);
    }

    [RelationshipSchemaName("system_user_contacts")]
    [RelationshipMetadata("OneToMany", "systemuserid", "contact", "preferredsystemuserid", "Referenced")]
    public IEnumerable<Contact> system_user_contacts
    {
        get => GetRelatedEntities<Contact>("system_user_contacts", null);
        set => SetRelatedEntities("system_user_contacts", null, value);
    }

    [RelationshipSchemaName("system_user_email_templates")]
    [RelationshipMetadata("OneToMany", "systemuserid", "template", "owninguser", "Referenced")]
    public IEnumerable<Template> system_user_email_templates
    {
        get => GetRelatedEntities<Template>("system_user_email_templates", null);
        set => SetRelatedEntities("system_user_email_templates", null, value);
    }

    [RelationshipSchemaName("SystemUser_Email_EmailSender")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "emailsender", "Referenced")]
    public IEnumerable<Email> SystemUser_Email_EmailSender
    {
        get => GetRelatedEntities<Email>("SystemUser_Email_EmailSender", null);
        set => SetRelatedEntities("SystemUser_Email_EmailSender", null, value);
    }

    [RelationshipSchemaName("teammembership_association")]
    [RelationshipMetadata("ManyToMany", "systemuserid", "team", "teamid", "Entity2")]
    public IEnumerable<Team> teammembership_association
    {
        get => GetRelatedEntities<Team>("teammembership_association", null);
        set => SetRelatedEntities("teammembership_association", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_SystemUser")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_SystemUser
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_SystemUser", null);
        set => SetRelatedEntity("TransactionCurrency_SystemUser", null, value);
    }

    [RelationshipSchemaName("user_accounts")]
    [RelationshipMetadata("OneToMany", "systemuserid", "account", "owninguser", "Referenced")]
    public IEnumerable<Account> user_accounts
    {
        get => GetRelatedEntities<Account>("user_accounts", null);
        set => SetRelatedEntities("user_accounts", null, value);
    }

    [RelationshipSchemaName("user_activity")]
    [RelationshipMetadata("OneToMany", "systemuserid", "activitypointer", "owninguser", "Referenced")]
    public IEnumerable<ActivityPointer> user_activity
    {
        get => GetRelatedEntities<ActivityPointer>("user_activity", null);
        set => SetRelatedEntities("user_activity", null, value);
    }

    [RelationshipSchemaName("user_ctx_child")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_child", "owninguser", "Referenced")]
    public IEnumerable<ctx_child> user_ctx_child
    {
        get => GetRelatedEntities<ctx_child>("user_ctx_child", null);
        set => SetRelatedEntities("user_ctx_child", null, value);
    }

    [RelationshipSchemaName("user_ctx_parent")]
    [RelationshipMetadata("OneToMany", "systemuserid", "ctx_parent", "owninguser", "Referenced")]
    public IEnumerable<ctx_parent> user_ctx_parent
    {
        get => GetRelatedEntities<ctx_parent>("user_ctx_parent", null);
        set => SetRelatedEntities("user_ctx_parent", null, value);
    }

    [RelationshipSchemaName("user_email")]
    [RelationshipMetadata("OneToMany", "systemuserid", "email", "owninguser", "Referenced")]
    public IEnumerable<Email> user_email
    {
        get => GetRelatedEntities<Email>("user_email", null);
        set => SetRelatedEntities("user_email", null, value);
    }

    [RelationshipSchemaName("user_fax")]
    [RelationshipMetadata("OneToMany", "systemuserid", "fax", "owninguser", "Referenced")]
    public IEnumerable<Fax> user_fax
    {
        get => GetRelatedEntities<Fax>("user_fax", null);
        set => SetRelatedEntities("user_fax", null, value);
    }

    [AttributeLogicalName("parentsystemuserid")]
    [RelationshipSchemaName("user_parent_user")]
    [RelationshipMetadata("ManyToOne", "parentsystemuserid", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_parent_user
    {
        get => GetRelatedEntity<SystemUser>("user_parent_user", null);
        set => SetRelatedEntity("user_parent_user", null, value);
    }

    [RelationshipSchemaName("user_task")]
    [RelationshipMetadata("OneToMany", "systemuserid", "task", "owninguser", "Referenced")]
    public IEnumerable<Task> user_task
    {
        get => GetRelatedEntities<Task>("user_task", null);
        set => SetRelatedEntities("user_task", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the SystemUser entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<SystemUser, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the SystemUser with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of SystemUser to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved SystemUser</returns>
    public static SystemUser Retrieve(IOrganizationService service, Guid id, params Expression<Func<SystemUser, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }

    /// <summary>
    /// Retrieves the SystemUser using the AAD ObjectId alternate key.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="AzureActiveDirectoryObjectId">AzureActiveDirectoryObjectId key value</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved SystemUser</returns>
    public static SystemUser Retrieve_aadobjectid(IOrganizationService service, Guid AzureActiveDirectoryObjectId, params Expression<Func<SystemUser, object>>[] columns)
    {
        var keyedEntityReference = new EntityReference(EntityLogicalName, new KeyAttributeCollection
        {
            ["azureactivedirectoryobjectid"] = AzureActiveDirectoryObjectId,
        });

        return service.Retrieve(keyedEntityReference, columns);
    }
}
