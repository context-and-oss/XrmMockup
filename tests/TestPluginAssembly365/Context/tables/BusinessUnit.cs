using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Business, division, or department in the Microsoft Dynamics 365 database.</para>
/// <para>Display Name: Business Unit</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("businessunit")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class BusinessUnit : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "businessunit";
    public const int EntityTypeCode = 10;

    public BusinessUnit() : base(EntityLogicalName) { }
    public BusinessUnit(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("name");

    [AttributeLogicalName("businessunitid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("businessunitid", value);
        }
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
    public businessunit_address1_addresstypecode? Address1_AddressTypeCode
    {
        get => this.GetOptionSetValue<businessunit_address1_addresstypecode>("address1_addresstypecode");
        set => this.SetOptionSetValue("address1_addresstypecode", value);
    }

    /// <summary>
    /// <para>City name for address 1.</para>
    /// <para>Display Name: Bill To City</para>
    /// </summary>
    [AttributeLogicalName("address1_city")]
    [DisplayName("Bill To City")]
    [MaxLength(80)]
    public string? Address1_City
    {
        get => GetAttributeValue<string?>("address1_city");
        set => SetAttributeValue("address1_city", value);
    }

    /// <summary>
    /// <para>Country/region name for address 1.</para>
    /// <para>Display Name: Bill To Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address1_country")]
    [DisplayName("Bill To Country/Region")]
    [MaxLength(80)]
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
    [MaxLength(50)]
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
    [MaxLength(50)]
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
    /// <para>Display Name: Bill To Street 1</para>
    /// </summary>
    [AttributeLogicalName("address1_line1")]
    [DisplayName("Bill To Street 1")]
    [MaxLength(250)]
    public string? Address1_Line1
    {
        get => GetAttributeValue<string?>("address1_line1");
        set => SetAttributeValue("address1_line1", value);
    }

    /// <summary>
    /// <para>Second line for entering address 1 information.</para>
    /// <para>Display Name: Bill To Street 2</para>
    /// </summary>
    [AttributeLogicalName("address1_line2")]
    [DisplayName("Bill To Street 2")]
    [MaxLength(250)]
    public string? Address1_Line2
    {
        get => GetAttributeValue<string?>("address1_line2");
        set => SetAttributeValue("address1_line2", value);
    }

    /// <summary>
    /// <para>Third line for entering address 1 information.</para>
    /// <para>Display Name: Bill To Street 3</para>
    /// </summary>
    [AttributeLogicalName("address1_line3")]
    [DisplayName("Bill To Street 3")]
    [MaxLength(250)]
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
    /// <para>Display Name: Bill To ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address1_postalcode")]
    [DisplayName("Bill To ZIP/Postal Code")]
    [MaxLength(20)]
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
    [MaxLength(20)]
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
    public businessunit_address1_shippingmethodcode? Address1_ShippingMethodCode
    {
        get => this.GetOptionSetValue<businessunit_address1_shippingmethodcode>("address1_shippingmethodcode");
        set => this.SetOptionSetValue("address1_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>State or province for address 1.</para>
    /// <para>Display Name: Bill To State/Province</para>
    /// </summary>
    [AttributeLogicalName("address1_stateorprovince")]
    [DisplayName("Bill To State/Province")]
    [MaxLength(50)]
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
    [MaxLength(50)]
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
    /// <para>Display Name: Address 1: Telephone 3</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone3")]
    [DisplayName("Address 1: Telephone 3")]
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
    public businessunit_address2_addresstypecode? Address2_AddressTypeCode
    {
        get => this.GetOptionSetValue<businessunit_address2_addresstypecode>("address2_addresstypecode");
        set => this.SetOptionSetValue("address2_addresstypecode", value);
    }

    /// <summary>
    /// <para>City name for address 2.</para>
    /// <para>Display Name: Ship To City</para>
    /// </summary>
    [AttributeLogicalName("address2_city")]
    [DisplayName("Ship To City")]
    [MaxLength(80)]
    public string? Address2_City
    {
        get => GetAttributeValue<string?>("address2_city");
        set => SetAttributeValue("address2_city", value);
    }

    /// <summary>
    /// <para>Country/region name for address 2.</para>
    /// <para>Display Name: Ship To Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address2_country")]
    [DisplayName("Ship To Country/Region")]
    [MaxLength(80)]
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
    [MaxLength(50)]
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
    /// <para>Display Name: Ship To Street 1</para>
    /// </summary>
    [AttributeLogicalName("address2_line1")]
    [DisplayName("Ship To Street 1")]
    [MaxLength(250)]
    public string? Address2_Line1
    {
        get => GetAttributeValue<string?>("address2_line1");
        set => SetAttributeValue("address2_line1", value);
    }

    /// <summary>
    /// <para>Second line for entering address 2 information.</para>
    /// <para>Display Name: Ship To Street 2</para>
    /// </summary>
    [AttributeLogicalName("address2_line2")]
    [DisplayName("Ship To Street 2")]
    [MaxLength(250)]
    public string? Address2_Line2
    {
        get => GetAttributeValue<string?>("address2_line2");
        set => SetAttributeValue("address2_line2", value);
    }

    /// <summary>
    /// <para>Third line for entering address 2 information.</para>
    /// <para>Display Name: Ship To Street 3</para>
    /// </summary>
    [AttributeLogicalName("address2_line3")]
    [DisplayName("Ship To Street 3")]
    [MaxLength(250)]
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
    /// <para>Display Name: Ship To ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address2_postalcode")]
    [DisplayName("Ship To ZIP/Postal Code")]
    [MaxLength(20)]
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
    [MaxLength(20)]
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
    public businessunit_address2_shippingmethodcode? Address2_ShippingMethodCode
    {
        get => this.GetOptionSetValue<businessunit_address2_shippingmethodcode>("address2_shippingmethodcode");
        set => this.SetOptionSetValue("address2_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>State or province for address 2.</para>
    /// <para>Display Name: Ship To State/Province</para>
    /// </summary>
    [AttributeLogicalName("address2_stateorprovince")]
    [DisplayName("Ship To State/Province")]
    [MaxLength(50)]
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
    /// <para>Display Name: Business Unit</para>
    /// </summary>
    [AttributeLogicalName("businessunitid")]
    [DisplayName("Business Unit")]
    public Guid? BusinessUnitId
    {
        get => GetAttributeValue<Guid?>("businessunitid");
        set => SetId("businessunitid", value);
    }

    /// <summary>
    /// <para>Fiscal calendar associated with the business unit.</para>
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
    /// <para>Name of the business unit cost center.</para>
    /// <para>Display Name: Cost Center</para>
    /// </summary>
    [AttributeLogicalName("costcenter")]
    [DisplayName("Cost Center")]
    [MaxLength(100)]
    public string? CostCenter
    {
        get => GetAttributeValue<string?>("costcenter");
        set => SetAttributeValue("costcenter", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the business unit.</para>
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
    /// <para>Date and time when the business unit was created.</para>
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
    /// <para>Unique identifier of the delegate user who created the businessunit.</para>
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
    /// <para>Credit limit for the business unit.</para>
    /// <para>Display Name: Credit Limit</para>
    /// </summary>
    [AttributeLogicalName("creditlimit")]
    [DisplayName("Credit Limit")]
    public double? CreditLimit
    {
        get => GetAttributeValue<double?>("creditlimit");
        set => SetAttributeValue("creditlimit", value);
    }

    /// <summary>
    /// <para>Description of the business unit.</para>
    /// <para>Display Name: Description</para>
    /// </summary>
    [AttributeLogicalName("description")]
    [DisplayName("Description")]
    [MaxLength(2000)]
    public string? Description
    {
        get => GetAttributeValue<string?>("description");
        set => SetAttributeValue("description", value);
    }

    /// <summary>
    /// <para>Reason for disabling the business unit.</para>
    /// <para>Display Name: Disable Reason</para>
    /// </summary>
    [AttributeLogicalName("disabledreason")]
    [DisplayName("Disable Reason")]
    [MaxLength(500)]
    public string? DisabledReason
    {
        get => GetAttributeValue<string?>("disabledreason");
        set => SetAttributeValue("disabledreason", value);
    }

    /// <summary>
    /// <para>Name of the division to which the business unit belongs.</para>
    /// <para>Display Name: Division</para>
    /// </summary>
    [AttributeLogicalName("divisionname")]
    [DisplayName("Division")]
    [MaxLength(100)]
    public string? DivisionName
    {
        get => GetAttributeValue<string?>("divisionname");
        set => SetAttributeValue("divisionname", value);
    }

    /// <summary>
    /// <para>Email address for the business unit.</para>
    /// <para>Display Name: Email</para>
    /// </summary>
    [AttributeLogicalName("emailaddress")]
    [DisplayName("Email")]
    [MaxLength(100)]
    public string? EMailAddress
    {
        get => GetAttributeValue<string?>("emailaddress");
        set => SetAttributeValue("emailaddress", value);
    }

    /// <summary>
    /// <para>Exchange rate for the currency associated with the businessunit with respect to the base currency.</para>
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
    /// <para>Alternative name under which the business unit can be filed.</para>
    /// <para>Display Name: File as Name</para>
    /// </summary>
    [AttributeLogicalName("fileasname")]
    [DisplayName("File as Name")]
    [MaxLength(100)]
    public string? FileAsName
    {
        get => GetAttributeValue<string?>("fileasname");
        set => SetAttributeValue("fileasname", value);
    }

    /// <summary>
    /// <para>FTP site URL for the business unit.</para>
    /// <para>Display Name: FTP Site</para>
    /// </summary>
    [AttributeLogicalName("ftpsiteurl")]
    [DisplayName("FTP Site")]
    [MaxLength(200)]
    public string? FtpSiteUrl
    {
        get => GetAttributeValue<string?>("ftpsiteurl");
        set => SetAttributeValue("ftpsiteurl", value);
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
    /// <para>Inheritance mask for the business unit.</para>
    /// <para>Display Name: Inheritance Mask</para>
    /// </summary>
    [AttributeLogicalName("inheritancemask")]
    [DisplayName("Inheritance Mask")]
    [Range(0, 1000000000)]
    public int? InheritanceMask
    {
        get => GetAttributeValue<int?>("inheritancemask");
        set => SetAttributeValue("inheritancemask", value);
    }

    /// <summary>
    /// <para>Information about whether the business unit is enabled or disabled.</para>
    /// <para>Display Name: Is Disabled</para>
    /// </summary>
    [AttributeLogicalName("isdisabled")]
    [DisplayName("Is Disabled")]
    public bool? IsDisabled
    {
        get => GetAttributeValue<bool?>("isdisabled");
        set => SetAttributeValue("isdisabled", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who last modified the business unit.</para>
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
    /// <para>Date and time when the business unit was last modified.</para>
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
    /// <para>Unique identifier of the delegate user who last modified the businessunit.</para>
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
    /// <para>Name of the business unit.</para>
    /// <para>Display Name: Name</para>
    /// </summary>
    [AttributeLogicalName("name")]
    [DisplayName("Name")]
    [MaxLength(160)]
    public string? Name
    {
        get => GetAttributeValue<string?>("name");
        set => SetAttributeValue("name", value);
    }

    /// <summary>
    /// <para>Unique identifier of the organization associated with the business unit.</para>
    /// <para>Display Name: Organization</para>
    /// </summary>
    [AttributeLogicalName("organizationid")]
    [DisplayName("Organization")]
    public EntityReference? OrganizationId
    {
        get => GetAttributeValue<EntityReference?>("organizationid");
        set => SetAttributeValue("organizationid", value);
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
    /// <para>Unique identifier for the parent business unit.</para>
    /// <para>Display Name: Parent Business</para>
    /// </summary>
    [AttributeLogicalName("parentbusinessunitid")]
    [DisplayName("Parent Business")]
    public EntityReference? ParentBusinessUnitId
    {
        get => GetAttributeValue<EntityReference?>("parentbusinessunitid");
        set => SetAttributeValue("parentbusinessunitid", value);
    }

    /// <summary>
    /// <para>Picture or diagram of the business unit.</para>
    /// <para>Display Name: Picture</para>
    /// </summary>
    [AttributeLogicalName("picture")]
    [DisplayName("Picture")]
    [MaxLength(1073741823)]
    public string? Picture
    {
        get => GetAttributeValue<string?>("picture");
        set => SetAttributeValue("picture", value);
    }

    /// <summary>
    /// <para>Stock exchange on which the business is listed.</para>
    /// <para>Display Name: Stock Exchange</para>
    /// </summary>
    [AttributeLogicalName("stockexchange")]
    [DisplayName("Stock Exchange")]
    [MaxLength(20)]
    public string? StockExchange
    {
        get => GetAttributeValue<string?>("stockexchange");
        set => SetAttributeValue("stockexchange", value);
    }

    /// <summary>
    /// <para>Stock exchange ticker symbol for the business unit.</para>
    /// <para>Display Name: Ticker Symbol</para>
    /// </summary>
    [AttributeLogicalName("tickersymbol")]
    [DisplayName("Ticker Symbol")]
    [MaxLength(10)]
    public string? TickerSymbol
    {
        get => GetAttributeValue<string?>("tickersymbol");
        set => SetAttributeValue("tickersymbol", value);
    }

    /// <summary>
    /// <para>Unique identifier of the currency associated with the businessunit.</para>
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
    /// <para>Display Name: usergroupid</para>
    /// </summary>
    [AttributeLogicalName("usergroupid")]
    [DisplayName("usergroupid")]
    public Guid? UserGroupId
    {
        get => GetAttributeValue<Guid?>("usergroupid");
        set => SetAttributeValue("usergroupid", value);
    }

    /// <summary>
    /// <para>UTC offset for the business unit. This is the difference between local time and standard Coordinated Universal Time.</para>
    /// <para>Display Name: UTC Offset</para>
    /// </summary>
    [AttributeLogicalName("utcoffset")]
    [DisplayName("UTC Offset")]
    [Range(-1500, 1500)]
    public int? UTCOffset
    {
        get => GetAttributeValue<int?>("utcoffset");
        set => SetAttributeValue("utcoffset", value);
    }

    /// <summary>
    /// <para>Version number of the business unit.</para>
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
    /// <para>Website URL for the business unit.</para>
    /// <para>Display Name: Website</para>
    /// </summary>
    [AttributeLogicalName("websiteurl")]
    [DisplayName("Website")]
    [MaxLength(200)]
    public string? WebSiteUrl
    {
        get => GetAttributeValue<string?>("websiteurl");
        set => SetAttributeValue("websiteurl", value);
    }

    /// <summary>
    /// <para>Information about whether workflow or sales process rules have been suspended.</para>
    /// <para>Display Name: Workflow Suspended</para>
    /// </summary>
    [AttributeLogicalName("workflowsuspended")]
    [DisplayName("Workflow Suspended")]
    public bool? WorkflowSuspended
    {
        get => GetAttributeValue<bool?>("workflowsuspended");
        set => SetAttributeValue("workflowsuspended", value);
    }

    [RelationshipSchemaName("business_unit_accounts")]
    [RelationshipMetadata("OneToMany", "businessunitid", "account", "owningbusinessunit", "Referenced")]
    public IEnumerable<Account> business_unit_accounts
    {
        get => GetRelatedEntities<Account>("business_unit_accounts", null);
        set => SetRelatedEntities("business_unit_accounts", null, value);
    }

    [RelationshipSchemaName("business_unit_activitypointer")]
    [RelationshipMetadata("OneToMany", "businessunitid", "activitypointer", "owningbusinessunit", "Referenced")]
    public IEnumerable<ActivityPointer> business_unit_activitypointer
    {
        get => GetRelatedEntities<ActivityPointer>("business_unit_activitypointer", null);
        set => SetRelatedEntities("business_unit_activitypointer", null, value);
    }

    [RelationshipSchemaName("business_unit_contacts")]
    [RelationshipMetadata("OneToMany", "businessunitid", "contact", "owningbusinessunit", "Referenced")]
    public IEnumerable<Contact> business_unit_contacts
    {
        get => GetRelatedEntities<Contact>("business_unit_contacts", null);
        set => SetRelatedEntities("business_unit_contacts", null, value);
    }

    [RelationshipSchemaName("business_unit_ctx_child")]
    [RelationshipMetadata("OneToMany", "businessunitid", "ctx_child", "owningbusinessunit", "Referenced")]
    public IEnumerable<ctx_child> business_unit_ctx_child
    {
        get => GetRelatedEntities<ctx_child>("business_unit_ctx_child", null);
        set => SetRelatedEntities("business_unit_ctx_child", null, value);
    }

    [RelationshipSchemaName("business_unit_ctx_parent")]
    [RelationshipMetadata("OneToMany", "businessunitid", "ctx_parent", "owningbusinessunit", "Referenced")]
    public IEnumerable<ctx_parent> business_unit_ctx_parent
    {
        get => GetRelatedEntities<ctx_parent>("business_unit_ctx_parent", null);
        set => SetRelatedEntities("business_unit_ctx_parent", null, value);
    }

    [RelationshipSchemaName("business_unit_email_activities")]
    [RelationshipMetadata("OneToMany", "businessunitid", "email", "owningbusinessunit", "Referenced")]
    public IEnumerable<Email> business_unit_email_activities
    {
        get => GetRelatedEntities<Email>("business_unit_email_activities", null);
        set => SetRelatedEntities("business_unit_email_activities", null, value);
    }

    [RelationshipSchemaName("business_unit_fax_activities")]
    [RelationshipMetadata("OneToMany", "businessunitid", "fax", "owningbusinessunit", "Referenced")]
    public IEnumerable<Fax> business_unit_fax_activities
    {
        get => GetRelatedEntities<Fax>("business_unit_fax_activities", null);
        set => SetRelatedEntities("business_unit_fax_activities", null, value);
    }

    [AttributeLogicalName("parentbusinessunitid")]
    [RelationshipSchemaName("business_unit_parent_business_unit")]
    [RelationshipMetadata("ManyToOne", "parentbusinessunitid", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_parent_business_unit
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_parent_business_unit", null);
        set => SetRelatedEntity("business_unit_parent_business_unit", null, value);
    }

    [RelationshipSchemaName("business_unit_system_users")]
    [RelationshipMetadata("OneToMany", "businessunitid", "systemuser", "businessunitid", "Referenced")]
    public IEnumerable<SystemUser> business_unit_system_users
    {
        get => GetRelatedEntities<SystemUser>("business_unit_system_users", null);
        set => SetRelatedEntities("business_unit_system_users", null, value);
    }

    [RelationshipSchemaName("business_unit_task_activities")]
    [RelationshipMetadata("OneToMany", "businessunitid", "task", "owningbusinessunit", "Referenced")]
    public IEnumerable<Task> business_unit_task_activities
    {
        get => GetRelatedEntities<Task>("business_unit_task_activities", null);
        set => SetRelatedEntities("business_unit_task_activities", null, value);
    }

    [RelationshipSchemaName("business_unit_teams")]
    [RelationshipMetadata("OneToMany", "businessunitid", "team", "businessunitid", "Referenced")]
    public IEnumerable<Team> business_unit_teams
    {
        get => GetRelatedEntities<Team>("business_unit_teams", null);
        set => SetRelatedEntities("business_unit_teams", null, value);
    }

    [RelationshipSchemaName("business_unit_templates")]
    [RelationshipMetadata("OneToMany", "businessunitid", "template", "owningbusinessunit", "Referenced")]
    public IEnumerable<Template> business_unit_templates
    {
        get => GetRelatedEntities<Template>("business_unit_templates", null);
        set => SetRelatedEntities("business_unit_templates", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_businessunit_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_businessunit_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_businessunit_createdonbehalfby", null);
        set => SetRelatedEntity("lk_businessunit_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_businessunit_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_businessunit_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_businessunit_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_businessunit_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_businessunitbase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_businessunitbase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_businessunitbase_createdby", null);
        set => SetRelatedEntity("lk_businessunitbase_createdby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_businessunitbase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_businessunitbase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_businessunitbase_modifiedby", null);
        set => SetRelatedEntity("lk_businessunitbase_modifiedby", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_BusinessUnit")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_BusinessUnit
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_BusinessUnit", null);
        set => SetRelatedEntity("TransactionCurrency_BusinessUnit", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the BusinessUnit entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<BusinessUnit, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the BusinessUnit with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of BusinessUnit to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved BusinessUnit</returns>
    public static BusinessUnit Retrieve(IOrganizationService service, Guid id, params Expression<Func<BusinessUnit, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
