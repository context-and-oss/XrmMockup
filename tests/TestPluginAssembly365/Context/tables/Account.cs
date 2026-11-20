using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Business that represents a customer or potential customer. The company that is billed in business transactions.</para>
/// <para>Display Name: Account</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("account")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Account : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "account";
    public const int EntityTypeCode = 1;

    public Account() : base(EntityLogicalName) { }
    public Account(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("name");

    [AttributeLogicalName("accountid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("accountid", value);
        }
    }

    /// <summary>
    /// <para>Select a category to indicate whether the customer account is standard or preferred.</para>
    /// <para>Display Name: Category</para>
    /// </summary>
    [AttributeLogicalName("accountcategorycode")]
    [DisplayName("Category")]
    public account_accountcategorycode? AccountCategoryCode
    {
        get => this.GetOptionSetValue<account_accountcategorycode>("accountcategorycode");
        set => this.SetOptionSetValue("accountcategorycode", value);
    }

    /// <summary>
    /// <para>Select a classification code to indicate the potential value of the customer account based on the projected return on investment, cooperation level, sales cycle length or other criteria.</para>
    /// <para>Display Name: Classification</para>
    /// </summary>
    [AttributeLogicalName("accountclassificationcode")]
    [DisplayName("Classification")]
    public account_accountclassificationcode? AccountClassificationCode
    {
        get => this.GetOptionSetValue<account_accountclassificationcode>("accountclassificationcode");
        set => this.SetOptionSetValue("accountclassificationcode", value);
    }

    /// <summary>
    /// <para>Display Name: Account</para>
    /// </summary>
    [AttributeLogicalName("accountid")]
    [DisplayName("Account")]
    public Guid? AccountId
    {
        get => GetAttributeValue<Guid?>("accountid");
        set => SetId("accountid", value);
    }

    /// <summary>
    /// <para>Type an ID number or code for the account to quickly search and identify the account in system views.</para>
    /// <para>Display Name: Account Number</para>
    /// </summary>
    [AttributeLogicalName("accountnumber")]
    [DisplayName("Account Number")]
    [MaxLength(20)]
    public string? AccountNumber
    {
        get => GetAttributeValue<string?>("accountnumber");
        set => SetAttributeValue("accountnumber", value);
    }

    /// <summary>
    /// <para>Select a rating to indicate the value of the customer account.</para>
    /// <para>Display Name: Account Rating</para>
    /// </summary>
    [AttributeLogicalName("accountratingcode")]
    [DisplayName("Account Rating")]
    public account_accountratingcode? AccountRatingCode
    {
        get => this.GetOptionSetValue<account_accountratingcode>("accountratingcode");
        set => this.SetOptionSetValue("accountratingcode", value);
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
    /// <para>Select the primary address type.</para>
    /// <para>Display Name: Address 1: Address Type</para>
    /// </summary>
    [AttributeLogicalName("address1_addresstypecode")]
    [DisplayName("Address 1: Address Type")]
    public account_address1_addresstypecode? Address1_AddressTypeCode
    {
        get => this.GetOptionSetValue<account_address1_addresstypecode>("address1_addresstypecode");
        set => this.SetOptionSetValue("address1_addresstypecode", value);
    }

    /// <summary>
    /// <para>Type the city for the primary address.</para>
    /// <para>Display Name: Address 1: City</para>
    /// </summary>
    [AttributeLogicalName("address1_city")]
    [DisplayName("Address 1: City")]
    [MaxLength(80)]
    public string? Address1_City
    {
        get => GetAttributeValue<string?>("address1_city");
        set => SetAttributeValue("address1_city", value);
    }

    /// <summary>
    /// <para>Shows the complete primary address.</para>
    /// <para>Display Name: Address 1</para>
    /// </summary>
    [AttributeLogicalName("address1_composite")]
    [DisplayName("Address 1")]
    [MaxLength(1000)]
    public string? Address1_Composite
    {
        get => GetAttributeValue<string?>("address1_composite");
        set => SetAttributeValue("address1_composite", value);
    }

    /// <summary>
    /// <para>Type the country or region for the primary address.</para>
    /// <para>Display Name: Address 1: Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address1_country")]
    [DisplayName("Address 1: Country/Region")]
    [MaxLength(80)]
    public string? Address1_Country
    {
        get => GetAttributeValue<string?>("address1_country");
        set => SetAttributeValue("address1_country", value);
    }

    /// <summary>
    /// <para>Type the county for the primary address.</para>
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
    /// <para>Type the fax number associated with the primary address.</para>
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
    /// <para>Select the freight terms for the primary address to make sure shipping orders are processed correctly.</para>
    /// <para>Display Name: Address 1: Freight Terms</para>
    /// </summary>
    [AttributeLogicalName("address1_freighttermscode")]
    [DisplayName("Address 1: Freight Terms")]
    public account_address1_freighttermscode? Address1_FreightTermsCode
    {
        get => this.GetOptionSetValue<account_address1_freighttermscode>("address1_freighttermscode");
        set => this.SetOptionSetValue("address1_freighttermscode", value);
    }

    /// <summary>
    /// <para>Type the latitude value for the primary address for use in mapping and other applications.</para>
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
    /// <para>Type the first line of the primary address.</para>
    /// <para>Display Name: Address 1: Street 1</para>
    /// </summary>
    [AttributeLogicalName("address1_line1")]
    [DisplayName("Address 1: Street 1")]
    [MaxLength(250)]
    public string? Address1_Line1
    {
        get => GetAttributeValue<string?>("address1_line1");
        set => SetAttributeValue("address1_line1", value);
    }

    /// <summary>
    /// <para>Type the second line of the primary address.</para>
    /// <para>Display Name: Address 1: Street 2</para>
    /// </summary>
    [AttributeLogicalName("address1_line2")]
    [DisplayName("Address 1: Street 2")]
    [MaxLength(250)]
    public string? Address1_Line2
    {
        get => GetAttributeValue<string?>("address1_line2");
        set => SetAttributeValue("address1_line2", value);
    }

    /// <summary>
    /// <para>Type the third line of the primary address.</para>
    /// <para>Display Name: Address 1: Street 3</para>
    /// </summary>
    [AttributeLogicalName("address1_line3")]
    [DisplayName("Address 1: Street 3")]
    [MaxLength(250)]
    public string? Address1_Line3
    {
        get => GetAttributeValue<string?>("address1_line3");
        set => SetAttributeValue("address1_line3", value);
    }

    /// <summary>
    /// <para>Type the longitude value for the primary address for use in mapping and other applications.</para>
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
    /// <para>Type a descriptive name for the primary address, such as Corporate Headquarters.</para>
    /// <para>Display Name: Address 1: Name</para>
    /// </summary>
    [AttributeLogicalName("address1_name")]
    [DisplayName("Address 1: Name")]
    [MaxLength(200)]
    public string? Address1_Name
    {
        get => GetAttributeValue<string?>("address1_name");
        set => SetAttributeValue("address1_name", value);
    }

    /// <summary>
    /// <para>Type the ZIP Code or postal code for the primary address.</para>
    /// <para>Display Name: Address 1: ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address1_postalcode")]
    [DisplayName("Address 1: ZIP/Postal Code")]
    [MaxLength(20)]
    public string? Address1_PostalCode
    {
        get => GetAttributeValue<string?>("address1_postalcode");
        set => SetAttributeValue("address1_postalcode", value);
    }

    /// <summary>
    /// <para>Type the post office box number of the primary address.</para>
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
    /// <para>Type the name of the main contact at the account's primary address.</para>
    /// <para>Display Name: Address 1: Primary Contact Name</para>
    /// </summary>
    [AttributeLogicalName("address1_primarycontactname")]
    [DisplayName("Address 1: Primary Contact Name")]
    [MaxLength(100)]
    public string? Address1_PrimaryContactName
    {
        get => GetAttributeValue<string?>("address1_primarycontactname");
        set => SetAttributeValue("address1_primarycontactname", value);
    }

    /// <summary>
    /// <para>Select a shipping method for deliveries sent to this address.</para>
    /// <para>Display Name: Address 1: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("address1_shippingmethodcode")]
    [DisplayName("Address 1: Shipping Method")]
    public account_address1_shippingmethodcode? Address1_ShippingMethodCode
    {
        get => this.GetOptionSetValue<account_address1_shippingmethodcode>("address1_shippingmethodcode");
        set => this.SetOptionSetValue("address1_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>Type the state or province of the primary address.</para>
    /// <para>Display Name: Address 1: State/Province</para>
    /// </summary>
    [AttributeLogicalName("address1_stateorprovince")]
    [DisplayName("Address 1: State/Province")]
    [MaxLength(50)]
    public string? Address1_StateOrProvince
    {
        get => GetAttributeValue<string?>("address1_stateorprovince");
        set => SetAttributeValue("address1_stateorprovince", value);
    }

    /// <summary>
    /// <para>Type the main phone number associated with the primary address.</para>
    /// <para>Display Name: Address Phone</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone1")]
    [DisplayName("Address Phone")]
    [MaxLength(50)]
    public string? Address1_Telephone1
    {
        get => GetAttributeValue<string?>("address1_telephone1");
        set => SetAttributeValue("address1_telephone1", value);
    }

    /// <summary>
    /// <para>Type a second phone number associated with the primary address.</para>
    /// <para>Display Name: Address 1: Telephone 2</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone2")]
    [DisplayName("Address 1: Telephone 2")]
    [MaxLength(50)]
    public string? Address1_Telephone2
    {
        get => GetAttributeValue<string?>("address1_telephone2");
        set => SetAttributeValue("address1_telephone2", value);
    }

    /// <summary>
    /// <para>Type a third phone number associated with the primary address.</para>
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
    /// <para>Type the UPS zone of the primary address to make sure shipping charges are calculated correctly and deliveries are made promptly, if shipped by UPS.</para>
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
    /// <para>Select the time zone, or UTC offset, for this address so that other people can reference it when they contact someone at this address.</para>
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
    /// <para>Select the secondary address type.</para>
    /// <para>Display Name: Address 2: Address Type</para>
    /// </summary>
    [AttributeLogicalName("address2_addresstypecode")]
    [DisplayName("Address 2: Address Type")]
    public account_address2_addresstypecode? Address2_AddressTypeCode
    {
        get => this.GetOptionSetValue<account_address2_addresstypecode>("address2_addresstypecode");
        set => this.SetOptionSetValue("address2_addresstypecode", value);
    }

    /// <summary>
    /// <para>Type the city for the secondary address.</para>
    /// <para>Display Name: Address 2: City</para>
    /// </summary>
    [AttributeLogicalName("address2_city")]
    [DisplayName("Address 2: City")]
    [MaxLength(80)]
    public string? Address2_City
    {
        get => GetAttributeValue<string?>("address2_city");
        set => SetAttributeValue("address2_city", value);
    }

    /// <summary>
    /// <para>Shows the complete secondary address.</para>
    /// <para>Display Name: Address 2</para>
    /// </summary>
    [AttributeLogicalName("address2_composite")]
    [DisplayName("Address 2")]
    [MaxLength(1000)]
    public string? Address2_Composite
    {
        get => GetAttributeValue<string?>("address2_composite");
        set => SetAttributeValue("address2_composite", value);
    }

    /// <summary>
    /// <para>Type the country or region for the secondary address.</para>
    /// <para>Display Name: Address 2: Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address2_country")]
    [DisplayName("Address 2: Country/Region")]
    [MaxLength(80)]
    public string? Address2_Country
    {
        get => GetAttributeValue<string?>("address2_country");
        set => SetAttributeValue("address2_country", value);
    }

    /// <summary>
    /// <para>Type the county for the secondary address.</para>
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
    /// <para>Type the fax number associated with the secondary address.</para>
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
    /// <para>Select the freight terms for the secondary address to make sure shipping orders are processed correctly.</para>
    /// <para>Display Name: Address 2: Freight Terms</para>
    /// </summary>
    [AttributeLogicalName("address2_freighttermscode")]
    [DisplayName("Address 2: Freight Terms")]
    public account_address2_freighttermscode? Address2_FreightTermsCode
    {
        get => this.GetOptionSetValue<account_address2_freighttermscode>("address2_freighttermscode");
        set => this.SetOptionSetValue("address2_freighttermscode", value);
    }

    /// <summary>
    /// <para>Type the latitude value for the secondary address for use in mapping and other applications.</para>
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
    /// <para>Type the first line of the secondary address.</para>
    /// <para>Display Name: Address 2: Street 1</para>
    /// </summary>
    [AttributeLogicalName("address2_line1")]
    [DisplayName("Address 2: Street 1")]
    [MaxLength(250)]
    public string? Address2_Line1
    {
        get => GetAttributeValue<string?>("address2_line1");
        set => SetAttributeValue("address2_line1", value);
    }

    /// <summary>
    /// <para>Type the second line of the secondary address.</para>
    /// <para>Display Name: Address 2: Street 2</para>
    /// </summary>
    [AttributeLogicalName("address2_line2")]
    [DisplayName("Address 2: Street 2")]
    [MaxLength(250)]
    public string? Address2_Line2
    {
        get => GetAttributeValue<string?>("address2_line2");
        set => SetAttributeValue("address2_line2", value);
    }

    /// <summary>
    /// <para>Type the third line of the secondary address.</para>
    /// <para>Display Name: Address 2: Street 3</para>
    /// </summary>
    [AttributeLogicalName("address2_line3")]
    [DisplayName("Address 2: Street 3")]
    [MaxLength(250)]
    public string? Address2_Line3
    {
        get => GetAttributeValue<string?>("address2_line3");
        set => SetAttributeValue("address2_line3", value);
    }

    /// <summary>
    /// <para>Type the longitude value for the secondary address for use in mapping and other applications.</para>
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
    /// <para>Type a descriptive name for the secondary address, such as Corporate Headquarters.</para>
    /// <para>Display Name: Address 2: Name</para>
    /// </summary>
    [AttributeLogicalName("address2_name")]
    [DisplayName("Address 2: Name")]
    [MaxLength(200)]
    public string? Address2_Name
    {
        get => GetAttributeValue<string?>("address2_name");
        set => SetAttributeValue("address2_name", value);
    }

    /// <summary>
    /// <para>Type the ZIP Code or postal code for the secondary address.</para>
    /// <para>Display Name: Address 2: ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address2_postalcode")]
    [DisplayName("Address 2: ZIP/Postal Code")]
    [MaxLength(20)]
    public string? Address2_PostalCode
    {
        get => GetAttributeValue<string?>("address2_postalcode");
        set => SetAttributeValue("address2_postalcode", value);
    }

    /// <summary>
    /// <para>Type the post office box number of the secondary address.</para>
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
    /// <para>Type the name of the main contact at the account's secondary address.</para>
    /// <para>Display Name: Address 2: Primary Contact Name</para>
    /// </summary>
    [AttributeLogicalName("address2_primarycontactname")]
    [DisplayName("Address 2: Primary Contact Name")]
    [MaxLength(100)]
    public string? Address2_PrimaryContactName
    {
        get => GetAttributeValue<string?>("address2_primarycontactname");
        set => SetAttributeValue("address2_primarycontactname", value);
    }

    /// <summary>
    /// <para>Select a shipping method for deliveries sent to this address.</para>
    /// <para>Display Name: Address 2: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("address2_shippingmethodcode")]
    [DisplayName("Address 2: Shipping Method")]
    public account_address2_shippingmethodcode? Address2_ShippingMethodCode
    {
        get => this.GetOptionSetValue<account_address2_shippingmethodcode>("address2_shippingmethodcode");
        set => this.SetOptionSetValue("address2_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>Type the state or province of the secondary address.</para>
    /// <para>Display Name: Address 2: State/Province</para>
    /// </summary>
    [AttributeLogicalName("address2_stateorprovince")]
    [DisplayName("Address 2: State/Province")]
    [MaxLength(50)]
    public string? Address2_StateOrProvince
    {
        get => GetAttributeValue<string?>("address2_stateorprovince");
        set => SetAttributeValue("address2_stateorprovince", value);
    }

    /// <summary>
    /// <para>Type the main phone number associated with the secondary address.</para>
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
    /// <para>Type a second phone number associated with the secondary address.</para>
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
    /// <para>Type a third phone number associated with the secondary address.</para>
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
    /// <para>Type the UPS zone of the secondary address to make sure shipping charges are calculated correctly and deliveries are made promptly, if shipped by UPS.</para>
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
    /// <para>Select the time zone, or UTC offset, for this address so that other people can reference it when they contact someone at this address.</para>
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
    /// <para>Display Name: Created By (IP Address)</para>
    /// </summary>
    [AttributeLogicalName("adx_createdbyipaddress")]
    [DisplayName("Created By (IP Address)")]
    [MaxLength(100)]
    public string? Adx_CreatedByIPAddress
    {
        get => GetAttributeValue<string?>("adx_createdbyipaddress");
        set => SetAttributeValue("adx_createdbyipaddress", value);
    }

    /// <summary>
    /// <para>Display Name: Created By (User Name)</para>
    /// </summary>
    [AttributeLogicalName("adx_createdbyusername")]
    [DisplayName("Created By (User Name)")]
    [MaxLength(100)]
    public string? Adx_CreatedByUsername
    {
        get => GetAttributeValue<string?>("adx_createdbyusername");
        set => SetAttributeValue("adx_createdbyusername", value);
    }

    /// <summary>
    /// <para>Display Name: Modified By (IP Address)</para>
    /// </summary>
    [AttributeLogicalName("adx_modifiedbyipaddress")]
    [DisplayName("Modified By (IP Address)")]
    [MaxLength(100)]
    public string? Adx_ModifiedByIPAddress
    {
        get => GetAttributeValue<string?>("adx_modifiedbyipaddress");
        set => SetAttributeValue("adx_modifiedbyipaddress", value);
    }

    /// <summary>
    /// <para>Display Name: Modified By (User Name)</para>
    /// </summary>
    [AttributeLogicalName("adx_modifiedbyusername")]
    [DisplayName("Modified By (User Name)")]
    [MaxLength(100)]
    public string? Adx_ModifiedByUsername
    {
        get => GetAttributeValue<string?>("adx_modifiedbyusername");
        set => SetAttributeValue("adx_modifiedbyusername", value);
    }

    /// <summary>
    /// <para>For system use only.</para>
    /// <para>Display Name: Aging 30</para>
    /// </summary>
    [AttributeLogicalName("aging30")]
    [DisplayName("Aging 30")]
    public decimal? Aging30
    {
        get => this.GetMoneyValue("aging30");
        set => this.SetMoneyValue("aging30", value);
    }

    /// <summary>
    /// <para>The base currency equivalent of the aging 30 field.</para>
    /// <para>Display Name: Aging 30 (Base)</para>
    /// </summary>
    [AttributeLogicalName("aging30_base")]
    [DisplayName("Aging 30 (Base)")]
    public decimal? Aging30_Base
    {
        get => this.GetMoneyValue("aging30_base");
        set => this.SetMoneyValue("aging30_base", value);
    }

    /// <summary>
    /// <para>For system use only.</para>
    /// <para>Display Name: Aging 60</para>
    /// </summary>
    [AttributeLogicalName("aging60")]
    [DisplayName("Aging 60")]
    public decimal? Aging60
    {
        get => this.GetMoneyValue("aging60");
        set => this.SetMoneyValue("aging60", value);
    }

    /// <summary>
    /// <para>The base currency equivalent of the aging 60 field.</para>
    /// <para>Display Name: Aging 60 (Base)</para>
    /// </summary>
    [AttributeLogicalName("aging60_base")]
    [DisplayName("Aging 60 (Base)")]
    public decimal? Aging60_Base
    {
        get => this.GetMoneyValue("aging60_base");
        set => this.SetMoneyValue("aging60_base", value);
    }

    /// <summary>
    /// <para>For system use only.</para>
    /// <para>Display Name: Aging 90</para>
    /// </summary>
    [AttributeLogicalName("aging90")]
    [DisplayName("Aging 90")]
    public decimal? Aging90
    {
        get => this.GetMoneyValue("aging90");
        set => this.SetMoneyValue("aging90", value);
    }

    /// <summary>
    /// <para>The base currency equivalent of the aging 90 field.</para>
    /// <para>Display Name: Aging 90 (Base)</para>
    /// </summary>
    [AttributeLogicalName("aging90_base")]
    [DisplayName("Aging 90 (Base)")]
    public decimal? Aging90_Base
    {
        get => this.GetMoneyValue("aging90_base");
        set => this.SetMoneyValue("aging90_base", value);
    }

    /// <summary>
    /// <para>Select the legal designation or other business type of the account for contracts or reporting purposes.</para>
    /// <para>Display Name: Business Type</para>
    /// </summary>
    [AttributeLogicalName("businesstypecode")]
    [DisplayName("Business Type")]
    public account_businesstypecode? BusinessTypeCode
    {
        get => this.GetOptionSetValue<account_businesstypecode>("businesstypecode");
        set => this.SetOptionSetValue("businesstypecode", value);
    }

    /// <summary>
    /// <para>Shows who created the record.</para>
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
    /// <para>Shows the external party who created the record.</para>
    /// <para>Display Name: Created By (External Party)</para>
    /// </summary>
    [AttributeLogicalName("createdbyexternalparty")]
    [DisplayName("Created By (External Party)")]
    public EntityReference? CreatedByExternalParty
    {
        get => GetAttributeValue<EntityReference?>("createdbyexternalparty");
        set => SetAttributeValue("createdbyexternalparty", value);
    }

    /// <summary>
    /// <para>Shows the date and time when the record was created. The date and time are displayed in the time zone selected in Microsoft Dynamics 365 options.</para>
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
    /// <para>Shows who created the record on behalf of another user.</para>
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
    /// <para>Type the credit limit of the account. This is a useful reference when you address invoice and accounting issues with the customer.</para>
    /// <para>Display Name: Credit Limit</para>
    /// </summary>
    [AttributeLogicalName("creditlimit")]
    [DisplayName("Credit Limit")]
    public decimal? CreditLimit
    {
        get => this.GetMoneyValue("creditlimit");
        set => this.SetMoneyValue("creditlimit", value);
    }

    /// <summary>
    /// <para>Shows the credit limit converted to the system's default base currency for reporting purposes.</para>
    /// <para>Display Name: Credit Limit (Base)</para>
    /// </summary>
    [AttributeLogicalName("creditlimit_base")]
    [DisplayName("Credit Limit (Base)")]
    public decimal? CreditLimit_Base
    {
        get => this.GetMoneyValue("creditlimit_base");
        set => this.SetMoneyValue("creditlimit_base", value);
    }

    /// <summary>
    /// <para>Select whether the credit for the account is on hold. This is a useful reference while addressing the invoice and accounting issues with the customer.</para>
    /// <para>Display Name: Credit Hold</para>
    /// </summary>
    [AttributeLogicalName("creditonhold")]
    [DisplayName("Credit Hold")]
    public bool? CreditOnHold
    {
        get => GetAttributeValue<bool?>("creditonhold");
        set => SetAttributeValue("creditonhold", value);
    }

    /// <summary>
    /// <para>Select the size category or range of the account for segmentation and reporting purposes.</para>
    /// <para>Display Name: Customer Size</para>
    /// </summary>
    [AttributeLogicalName("customersizecode")]
    [DisplayName("Customer Size")]
    public account_customersizecode? CustomerSizeCode
    {
        get => this.GetOptionSetValue<account_customersizecode>("customersizecode");
        set => this.SetOptionSetValue("customersizecode", value);
    }

    /// <summary>
    /// <para>Select the category that best describes the relationship between the account and your organization.</para>
    /// <para>Display Name: Relationship Type</para>
    /// </summary>
    [AttributeLogicalName("customertypecode")]
    [DisplayName("Relationship Type")]
    public account_customertypecode? CustomerTypeCode
    {
        get => this.GetOptionSetValue<account_customertypecode>("customertypecode");
        set => this.SetOptionSetValue("customertypecode", value);
    }

    /// <summary>
    /// <para>Type additional information to describe the account, such as an excerpt from the company's website.</para>
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
    /// <para>Select whether the account allows bulk email sent through campaigns. If Do Not Allow is selected, the account can be added to marketing lists, but is excluded from email.</para>
    /// <para>Display Name: Do not allow Bulk Emails</para>
    /// </summary>
    [AttributeLogicalName("donotbulkemail")]
    [DisplayName("Do not allow Bulk Emails")]
    public bool? DoNotBulkEMail
    {
        get => GetAttributeValue<bool?>("donotbulkemail");
        set => SetAttributeValue("donotbulkemail", value);
    }

    /// <summary>
    /// <para>Select whether the account allows bulk postal mail sent through marketing campaigns or quick campaigns. If Do Not Allow is selected, the account can be added to marketing lists, but will be excluded from the postal mail.</para>
    /// <para>Display Name: Do not allow Bulk Mails</para>
    /// </summary>
    [AttributeLogicalName("donotbulkpostalmail")]
    [DisplayName("Do not allow Bulk Mails")]
    public bool? DoNotBulkPostalMail
    {
        get => GetAttributeValue<bool?>("donotbulkpostalmail");
        set => SetAttributeValue("donotbulkpostalmail", value);
    }

    /// <summary>
    /// <para>Select whether the account allows direct email sent from Microsoft Dynamics 365.</para>
    /// <para>Display Name: Do not allow Emails</para>
    /// </summary>
    [AttributeLogicalName("donotemail")]
    [DisplayName("Do not allow Emails")]
    public bool? DoNotEMail
    {
        get => GetAttributeValue<bool?>("donotemail");
        set => SetAttributeValue("donotemail", value);
    }

    /// <summary>
    /// <para>Select whether the account allows faxes. If Do Not Allow is selected, the account will be excluded from fax activities distributed in marketing campaigns.</para>
    /// <para>Display Name: Do not allow Faxes</para>
    /// </summary>
    [AttributeLogicalName("donotfax")]
    [DisplayName("Do not allow Faxes")]
    public bool? DoNotFax
    {
        get => GetAttributeValue<bool?>("donotfax");
        set => SetAttributeValue("donotfax", value);
    }

    /// <summary>
    /// <para>Select whether the account allows phone calls. If Do Not Allow is selected, the account will be excluded from phone call activities distributed in marketing campaigns.</para>
    /// <para>Display Name: Do not allow Phone Calls</para>
    /// </summary>
    [AttributeLogicalName("donotphone")]
    [DisplayName("Do not allow Phone Calls")]
    public bool? DoNotPhone
    {
        get => GetAttributeValue<bool?>("donotphone");
        set => SetAttributeValue("donotphone", value);
    }

    /// <summary>
    /// <para>Select whether the account allows direct mail. If Do Not Allow is selected, the account will be excluded from letter activities distributed in marketing campaigns.</para>
    /// <para>Display Name: Do not allow Mails</para>
    /// </summary>
    [AttributeLogicalName("donotpostalmail")]
    [DisplayName("Do not allow Mails")]
    public bool? DoNotPostalMail
    {
        get => GetAttributeValue<bool?>("donotpostalmail");
        set => SetAttributeValue("donotpostalmail", value);
    }

    /// <summary>
    /// <para>Select whether the account accepts marketing materials, such as brochures or catalogs.</para>
    /// <para>Display Name: Send Marketing Materials</para>
    /// </summary>
    [AttributeLogicalName("donotsendmm")]
    [DisplayName("Send Marketing Materials")]
    public bool? DoNotSendMM
    {
        get => GetAttributeValue<bool?>("donotsendmm");
        set => SetAttributeValue("donotsendmm", value);
    }

    /// <summary>
    /// <para>Type the primary email address for the account.</para>
    /// <para>Display Name: Email</para>
    /// </summary>
    [AttributeLogicalName("emailaddress1")]
    [DisplayName("Email")]
    [MaxLength(100)]
    public string? EMailAddress1
    {
        get => GetAttributeValue<string?>("emailaddress1");
        set => SetAttributeValue("emailaddress1", value);
    }

    /// <summary>
    /// <para>Type the secondary email address for the account.</para>
    /// <para>Display Name: Email Address 2</para>
    /// </summary>
    [AttributeLogicalName("emailaddress2")]
    [DisplayName("Email Address 2")]
    [MaxLength(100)]
    public string? EMailAddress2
    {
        get => GetAttributeValue<string?>("emailaddress2");
        set => SetAttributeValue("emailaddress2", value);
    }

    /// <summary>
    /// <para>Type an alternate email address for the account.</para>
    /// <para>Display Name: Email Address 3</para>
    /// </summary>
    [AttributeLogicalName("emailaddress3")]
    [DisplayName("Email Address 3")]
    [MaxLength(100)]
    public string? EMailAddress3
    {
        get => GetAttributeValue<string?>("emailaddress3");
        set => SetAttributeValue("emailaddress3", value);
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
    /// <para>Shows the conversion rate of the record's currency. The exchange rate is used to convert all money fields in the record from the local currency to the system's default currency.</para>
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
    /// <para>Type the fax number for the account.</para>
    /// <para>Display Name: Fax</para>
    /// </summary>
    [AttributeLogicalName("fax")]
    [DisplayName("Fax")]
    [MaxLength(50)]
    public string? Fax
    {
        get => GetAttributeValue<string?>("fax");
        set => SetAttributeValue("fax", value);
    }

    /// <summary>
    /// <para>Information about whether to allow following email activity like opens, attachment views and link clicks for emails sent to the account.</para>
    /// <para>Display Name: Follow Email Activity</para>
    /// </summary>
    [AttributeLogicalName("followemail")]
    [DisplayName("Follow Email Activity")]
    public bool? FollowEmail
    {
        get => GetAttributeValue<bool?>("followemail");
        set => SetAttributeValue("followemail", value);
    }

    /// <summary>
    /// <para>Type the URL for the account's FTP site to enable users to access data and share documents.</para>
    /// <para>Display Name: FTP Site</para>
    /// </summary>
    [AttributeLogicalName("ftpsiteurl")]
    [DisplayName("FTP Site")]
    [MaxLength(200)]
    public string? FtpSiteURL
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
    /// <para>Select the account's primary industry for use in marketing segmentation and demographic analysis.</para>
    /// <para>Display Name: Industry</para>
    /// </summary>
    [AttributeLogicalName("industrycode")]
    [DisplayName("Industry")]
    public account_industrycode? IndustryCode
    {
        get => this.GetOptionSetValue<account_industrycode>("industrycode");
        set => this.SetOptionSetValue("industrycode", value);
    }

    /// <summary>
    /// <para>Display Name: isprivate</para>
    /// </summary>
    [AttributeLogicalName("isprivate")]
    [DisplayName("isprivate")]
    public bool? IsPrivate
    {
        get => GetAttributeValue<bool?>("isprivate");
        set => SetAttributeValue("isprivate", value);
    }

    /// <summary>
    /// <para>Contains the date and time stamp of the last on hold time.</para>
    /// <para>Display Name: Last On Hold Time</para>
    /// </summary>
    [AttributeLogicalName("lastonholdtime")]
    [DisplayName("Last On Hold Time")]
    public DateTime? LastOnHoldTime
    {
        get => GetAttributeValue<DateTime?>("lastonholdtime");
        set => SetAttributeValue("lastonholdtime", value);
    }

    /// <summary>
    /// <para>Shows the date when the account was last included in a marketing campaign or quick campaign.</para>
    /// <para>Display Name: Last Date Included in Campaign</para>
    /// </summary>
    [AttributeLogicalName("lastusedincampaign")]
    [DisplayName("Last Date Included in Campaign")]
    public DateTime? LastUsedInCampaign
    {
        get => GetAttributeValue<DateTime?>("lastusedincampaign");
        set => SetAttributeValue("lastusedincampaign", value);
    }

    /// <summary>
    /// <para>Type the market capitalization of the account to identify the company's equity, used as an indicator in financial performance analysis.</para>
    /// <para>Display Name: Market Capitalization</para>
    /// </summary>
    [AttributeLogicalName("marketcap")]
    [DisplayName("Market Capitalization")]
    public decimal? MarketCap
    {
        get => this.GetMoneyValue("marketcap");
        set => this.SetMoneyValue("marketcap", value);
    }

    /// <summary>
    /// <para>Shows the market capitalization converted to the system's default base currency.</para>
    /// <para>Display Name: Market Capitalization (Base)</para>
    /// </summary>
    [AttributeLogicalName("marketcap_base")]
    [DisplayName("Market Capitalization (Base)")]
    public decimal? MarketCap_Base
    {
        get => this.GetMoneyValue("marketcap_base");
        set => this.SetMoneyValue("marketcap_base", value);
    }

    /// <summary>
    /// <para>Whether is only for marketing</para>
    /// <para>Display Name: Marketing Only</para>
    /// </summary>
    [AttributeLogicalName("marketingonly")]
    [DisplayName("Marketing Only")]
    public bool? MarketingOnly
    {
        get => GetAttributeValue<bool?>("marketingonly");
        set => SetAttributeValue("marketingonly", value);
    }

    /// <summary>
    /// <para>Shows the master account that the account was merged with.</para>
    /// <para>Display Name: Master ID</para>
    /// </summary>
    [AttributeLogicalName("masterid")]
    [DisplayName("Master ID")]
    public EntityReference? MasterId
    {
        get => GetAttributeValue<EntityReference?>("masterid");
        set => SetAttributeValue("masterid", value);
    }

    /// <summary>
    /// <para>Shows whether the account has been merged with another account.</para>
    /// <para>Display Name: Merged</para>
    /// </summary>
    [AttributeLogicalName("merged")]
    [DisplayName("Merged")]
    public bool? Merged
    {
        get => GetAttributeValue<bool?>("merged");
        set => SetAttributeValue("merged", value);
    }

    /// <summary>
    /// <para>Shows who last updated the record.</para>
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
    /// <para>Shows the external party who modified the record.</para>
    /// <para>Display Name: Modified By (External Party)</para>
    /// </summary>
    [AttributeLogicalName("modifiedbyexternalparty")]
    [DisplayName("Modified By (External Party)")]
    public EntityReference? ModifiedByExternalParty
    {
        get => GetAttributeValue<EntityReference?>("modifiedbyexternalparty");
        set => SetAttributeValue("modifiedbyexternalparty", value);
    }

    /// <summary>
    /// <para>Shows the date and time when the record was last updated. The date and time are displayed in the time zone selected in Microsoft Dynamics 365 options.</para>
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
    /// <para>Shows who created the record on behalf of another user.</para>
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
    /// <para>Unique identifier for Account associated with Account.</para>
    /// <para>Display Name: Managing Partner</para>
    /// </summary>
    [AttributeLogicalName("msa_managingpartnerid")]
    [DisplayName("Managing Partner")]
    public EntityReference? msa_managingpartnerid
    {
        get => GetAttributeValue<EntityReference?>("msa_managingpartnerid");
        set => SetAttributeValue("msa_managingpartnerid", value);
    }

    /// <summary>
    /// <para>Type the company or business name.</para>
    /// <para>Display Name: Account Name</para>
    /// </summary>
    [AttributeLogicalName("name")]
    [DisplayName("Account Name")]
    [MaxLength(160)]
    public string? Name
    {
        get => GetAttributeValue<string?>("name");
        set => SetAttributeValue("name", value);
    }

    /// <summary>
    /// <para>Type the number of employees that work at the account for use in marketing segmentation and demographic analysis.</para>
    /// <para>Display Name: Number of Employees</para>
    /// </summary>
    [AttributeLogicalName("numberofemployees")]
    [DisplayName("Number of Employees")]
    [Range(0, 1000000000)]
    public int? NumberOfEmployees
    {
        get => GetAttributeValue<int?>("numberofemployees");
        set => SetAttributeValue("numberofemployees", value);
    }

    /// <summary>
    /// <para>Shows how long, in minutes, that the record was on hold.</para>
    /// <para>Display Name: On Hold Time (Minutes)</para>
    /// </summary>
    [AttributeLogicalName("onholdtime")]
    [DisplayName("On Hold Time (Minutes)")]
    [Range(-2147483648, 2147483647)]
    public int? OnHoldTime
    {
        get => GetAttributeValue<int?>("onholdtime");
        set => SetAttributeValue("onholdtime", value);
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
    /// <para>Enter the user or team who is assigned to manage the record. This field is updated every time the record is assigned to a different user.</para>
    /// <para>Display Name: Owner</para>
    /// </summary>
    [AttributeLogicalName("ownerid")]
    [DisplayName("Owner")]
    public EntityReference? OwnerId
    {
        get => GetAttributeValue<EntityReference?>("ownerid");
        set => SetAttributeValue("ownerid", value);
    }

    /// <summary>
    /// <para>Select the account's ownership structure, such as public or private.</para>
    /// <para>Display Name: Ownership</para>
    /// </summary>
    [AttributeLogicalName("ownershipcode")]
    [DisplayName("Ownership")]
    public account_ownershipcode? OwnershipCode
    {
        get => this.GetOptionSetValue<account_ownershipcode>("ownershipcode");
        set => this.SetOptionSetValue("ownershipcode", value);
    }

    /// <summary>
    /// <para>Shows the business unit that the record owner belongs to.</para>
    /// <para>Display Name: Owning Business Unit</para>
    /// </summary>
    [AttributeLogicalName("owningbusinessunit")]
    [DisplayName("Owning Business Unit")]
    public EntityReference? OwningBusinessUnit
    {
        get => GetAttributeValue<EntityReference?>("owningbusinessunit");
        set => SetAttributeValue("owningbusinessunit", value);
    }

    /// <summary>
    /// <para>Unique identifier of the team who owns the account.</para>
    /// <para>Display Name: Owning Team</para>
    /// </summary>
    [AttributeLogicalName("owningteam")]
    [DisplayName("Owning Team")]
    public EntityReference? OwningTeam
    {
        get => GetAttributeValue<EntityReference?>("owningteam");
        set => SetAttributeValue("owningteam", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who owns the account.</para>
    /// <para>Display Name: Owning User</para>
    /// </summary>
    [AttributeLogicalName("owninguser")]
    [DisplayName("Owning User")]
    public EntityReference? OwningUser
    {
        get => GetAttributeValue<EntityReference?>("owninguser");
        set => SetAttributeValue("owninguser", value);
    }

    /// <summary>
    /// <para>Choose the parent account associated with this account to show parent and child businesses in reporting and analytics.</para>
    /// <para>Display Name: Parent Account</para>
    /// </summary>
    [AttributeLogicalName("parentaccountid")]
    [DisplayName("Parent Account")]
    public EntityReference? ParentAccountId
    {
        get => GetAttributeValue<EntityReference?>("parentaccountid");
        set => SetAttributeValue("parentaccountid", value);
    }

    /// <summary>
    /// <para>For system use only. Legacy Microsoft Dynamics CRM 3.0 workflow data.</para>
    /// <para>Display Name: Participates in Workflow</para>
    /// </summary>
    [AttributeLogicalName("participatesinworkflow")]
    [DisplayName("Participates in Workflow")]
    public bool? ParticipatesInWorkflow
    {
        get => GetAttributeValue<bool?>("participatesinworkflow");
        set => SetAttributeValue("participatesinworkflow", value);
    }

    /// <summary>
    /// <para>Select the payment terms to indicate when the customer needs to pay the total amount.</para>
    /// <para>Display Name: Payment Terms</para>
    /// </summary>
    [AttributeLogicalName("paymenttermscode")]
    [DisplayName("Payment Terms")]
    public account_paymenttermscode? PaymentTermsCode
    {
        get => this.GetOptionSetValue<account_paymenttermscode>("paymenttermscode");
        set => this.SetOptionSetValue("paymenttermscode", value);
    }

    /// <summary>
    /// <para>Select the preferred day of the week for service appointments.</para>
    /// <para>Display Name: Preferred Day</para>
    /// </summary>
    [AttributeLogicalName("preferredappointmentdaycode")]
    [DisplayName("Preferred Day")]
    public account_preferredappointmentdaycode? PreferredAppointmentDayCode
    {
        get => this.GetOptionSetValue<account_preferredappointmentdaycode>("preferredappointmentdaycode");
        set => this.SetOptionSetValue("preferredappointmentdaycode", value);
    }

    /// <summary>
    /// <para>Select the preferred time of day for service appointments.</para>
    /// <para>Display Name: Preferred Time</para>
    /// </summary>
    [AttributeLogicalName("preferredappointmenttimecode")]
    [DisplayName("Preferred Time")]
    public account_preferredappointmenttimecode? PreferredAppointmentTimeCode
    {
        get => this.GetOptionSetValue<account_preferredappointmenttimecode>("preferredappointmenttimecode");
        set => this.SetOptionSetValue("preferredappointmenttimecode", value);
    }

    /// <summary>
    /// <para>Select the preferred method of contact.</para>
    /// <para>Display Name: Preferred Method of Contact</para>
    /// </summary>
    [AttributeLogicalName("preferredcontactmethodcode")]
    [DisplayName("Preferred Method of Contact")]
    public account_preferredcontactmethodcode? PreferredContactMethodCode
    {
        get => this.GetOptionSetValue<account_preferredcontactmethodcode>("preferredcontactmethodcode");
        set => this.SetOptionSetValue("preferredcontactmethodcode", value);
    }

    /// <summary>
    /// <para>Choose the preferred service representative for reference when you schedule service activities for the account.</para>
    /// <para>Display Name: Preferred User</para>
    /// </summary>
    [AttributeLogicalName("preferredsystemuserid")]
    [DisplayName("Preferred User")]
    public EntityReference? PreferredSystemUserId
    {
        get => GetAttributeValue<EntityReference?>("preferredsystemuserid");
        set => SetAttributeValue("preferredsystemuserid", value);
    }

    /// <summary>
    /// <para>Choose the primary contact for the account to provide quick access to contact details.</para>
    /// <para>Display Name: Primary Contact</para>
    /// </summary>
    [AttributeLogicalName("primarycontactid")]
    [DisplayName("Primary Contact")]
    public EntityReference? PrimaryContactId
    {
        get => GetAttributeValue<EntityReference?>("primarycontactid");
        set => SetAttributeValue("primarycontactid", value);
    }

    /// <summary>
    /// <para>Primary Satori ID for Account</para>
    /// <para>Display Name: Primary Satori ID</para>
    /// </summary>
    [AttributeLogicalName("primarysatoriid")]
    [DisplayName("Primary Satori ID")]
    [MaxLength(200)]
    public string? PrimarySatoriId
    {
        get => GetAttributeValue<string?>("primarysatoriid");
        set => SetAttributeValue("primarysatoriid", value);
    }

    /// <summary>
    /// <para>Primary Twitter ID for Account</para>
    /// <para>Display Name: Primary Twitter ID</para>
    /// </summary>
    [AttributeLogicalName("primarytwitterid")]
    [DisplayName("Primary Twitter ID")]
    [MaxLength(128)]
    public string? PrimaryTwitterId
    {
        get => GetAttributeValue<string?>("primarytwitterid");
        set => SetAttributeValue("primarytwitterid", value);
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
    /// <para>Type the annual revenue for the account, used as an indicator in financial performance analysis.</para>
    /// <para>Display Name: Annual Revenue</para>
    /// </summary>
    [AttributeLogicalName("revenue")]
    [DisplayName("Annual Revenue")]
    public decimal? Revenue
    {
        get => this.GetMoneyValue("revenue");
        set => this.SetMoneyValue("revenue", value);
    }

    /// <summary>
    /// <para>Shows the annual revenue converted to the system's default base currency. The calculations use the exchange rate specified in the Currencies area.</para>
    /// <para>Display Name: Annual Revenue (Base)</para>
    /// </summary>
    [AttributeLogicalName("revenue_base")]
    [DisplayName("Annual Revenue (Base)")]
    public decimal? Revenue_Base
    {
        get => this.GetMoneyValue("revenue_base");
        set => this.SetMoneyValue("revenue_base", value);
    }

    /// <summary>
    /// <para>Type the number of shares available to the public for the account. This number is used as an indicator in financial performance analysis.</para>
    /// <para>Display Name: Shares Outstanding</para>
    /// </summary>
    [AttributeLogicalName("sharesoutstanding")]
    [DisplayName("Shares Outstanding")]
    [Range(0, 1000000000)]
    public int? SharesOutstanding
    {
        get => GetAttributeValue<int?>("sharesoutstanding");
        set => SetAttributeValue("sharesoutstanding", value);
    }

    /// <summary>
    /// <para>Select a shipping method for deliveries sent to the account's address to designate the preferred carrier or other delivery option.</para>
    /// <para>Display Name: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("shippingmethodcode")]
    [DisplayName("Shipping Method")]
    public account_shippingmethodcode? ShippingMethodCode
    {
        get => this.GetOptionSetValue<account_shippingmethodcode>("shippingmethodcode");
        set => this.SetOptionSetValue("shippingmethodcode", value);
    }

    /// <summary>
    /// <para>Type the Standard Industrial Classification (SIC) code that indicates the account's primary industry of business, for use in marketing segmentation and demographic analysis.</para>
    /// <para>Display Name: SIC Code</para>
    /// </summary>
    [AttributeLogicalName("sic")]
    [DisplayName("SIC Code")]
    [MaxLength(20)]
    public string? SIC
    {
        get => GetAttributeValue<string?>("sic");
        set => SetAttributeValue("sic", value);
    }

    /// <summary>
    /// <para>Choose the service level agreement (SLA) that you want to apply to the Account record.</para>
    /// <para>Display Name: SLA</para>
    /// </summary>
    [AttributeLogicalName("slaid")]
    [DisplayName("SLA")]
    public EntityReference? SLAId
    {
        get => GetAttributeValue<EntityReference?>("slaid");
        set => SetAttributeValue("slaid", value);
    }

    /// <summary>
    /// <para>Last SLA that was applied to this case. This field is for internal use only.</para>
    /// <para>Display Name: Last SLA applied</para>
    /// </summary>
    [AttributeLogicalName("slainvokedid")]
    [DisplayName("Last SLA applied")]
    public EntityReference? SLAInvokedId
    {
        get => GetAttributeValue<EntityReference?>("slainvokedid");
        set => SetAttributeValue("slainvokedid", value);
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
    /// <para>Shows whether the account is active or inactive. Inactive accounts are read-only and can't be edited unless they are reactivated.</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Status")]
    public account_statecode? StateCode
    {
        get => this.GetOptionSetValue<account_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Select the account's status.</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public account_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<account_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>Type the stock exchange at which the account is listed to track their stock and financial performance of the company.</para>
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
    /// <para>Type the main phone number for this account.</para>
    /// <para>Display Name: Main Phone</para>
    /// </summary>
    [AttributeLogicalName("telephone1")]
    [DisplayName("Main Phone")]
    [MaxLength(50)]
    public string? Telephone1
    {
        get => GetAttributeValue<string?>("telephone1");
        set => SetAttributeValue("telephone1", value);
    }

    /// <summary>
    /// <para>Type a second phone number for this account.</para>
    /// <para>Display Name: Other Phone</para>
    /// </summary>
    [AttributeLogicalName("telephone2")]
    [DisplayName("Other Phone")]
    [MaxLength(50)]
    public string? Telephone2
    {
        get => GetAttributeValue<string?>("telephone2");
        set => SetAttributeValue("telephone2", value);
    }

    /// <summary>
    /// <para>Type a third phone number for this account.</para>
    /// <para>Display Name: Telephone 3</para>
    /// </summary>
    [AttributeLogicalName("telephone3")]
    [DisplayName("Telephone 3")]
    [MaxLength(50)]
    public string? Telephone3
    {
        get => GetAttributeValue<string?>("telephone3");
        set => SetAttributeValue("telephone3", value);
    }

    /// <summary>
    /// <para>Select a region or territory for the account for use in segmentation and analysis.</para>
    /// <para>Display Name: Territory Code</para>
    /// </summary>
    [AttributeLogicalName("territorycode")]
    [DisplayName("Territory Code")]
    public account_territorycode? TerritoryCode
    {
        get => this.GetOptionSetValue<account_territorycode>("territorycode");
        set => this.SetOptionSetValue("territorycode", value);
    }

    /// <summary>
    /// <para>Type the stock exchange symbol for the account to track financial performance of the company. You can click the code entered in this field to access the latest trading information from MSN Money.</para>
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
    /// <para>Total time spent for emails (read and write) and meetings by me in relation to account record.</para>
    /// <para>Display Name: Time Spent by me</para>
    /// </summary>
    [AttributeLogicalName("timespentbymeonemailandmeetings")]
    [DisplayName("Time Spent by me")]
    [MaxLength(1250)]
    public string? TimeSpentByMeOnEmailAndMeetings
    {
        get => GetAttributeValue<string?>("timespentbymeonemailandmeetings");
        set => SetAttributeValue("timespentbymeonemailandmeetings", value);
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
    /// <para>Choose the local currency for the record to make sure budgets are reported in the correct currency.</para>
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
    /// <para>Version number of the account.</para>
    /// <para>Display Name: Version Number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version Number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    /// <summary>
    /// <para>Type the account's website URL to get quick details about the company profile.</para>
    /// <para>Display Name: Website</para>
    /// </summary>
    [AttributeLogicalName("websiteurl")]
    [DisplayName("Website")]
    [MaxLength(200)]
    public string? WebSiteURL
    {
        get => GetAttributeValue<string?>("websiteurl");
        set => SetAttributeValue("websiteurl", value);
    }

    /// <summary>
    /// <para>Type the phonetic spelling of the company name, if specified in Japanese, to make sure the name is pronounced correctly in phone calls and other communications.</para>
    /// <para>Display Name: Yomi Account Name</para>
    /// </summary>
    [AttributeLogicalName("yominame")]
    [DisplayName("Yomi Account Name")]
    [MaxLength(160)]
    public string? YomiName
    {
        get => GetAttributeValue<string?>("yominame");
        set => SetAttributeValue("yominame", value);
    }

    [RelationshipSchemaName("account_activity_parties")]
    [RelationshipMetadata("OneToMany", "accountid", "activityparty", "partyid", "Referenced")]
    public IEnumerable<ActivityParty> account_activity_parties
    {
        get => GetRelatedEntities<ActivityParty>("account_activity_parties", null);
        set => SetRelatedEntities("account_activity_parties", null, value);
    }

    [RelationshipSchemaName("Account_ActivityPointers")]
    [RelationshipMetadata("OneToMany", "accountid", "activitypointer", "regardingobjectid", "Referenced")]
    public IEnumerable<ActivityPointer> Account_ActivityPointers
    {
        get => GetRelatedEntities<ActivityPointer>("Account_ActivityPointers", null);
        set => SetRelatedEntities("Account_ActivityPointers", null, value);
    }

    [RelationshipSchemaName("Account_Email_EmailSender")]
    [RelationshipMetadata("OneToMany", "accountid", "email", "emailsender", "Referenced")]
    public IEnumerable<Email> Account_Email_EmailSender
    {
        get => GetRelatedEntities<Email>("Account_Email_EmailSender", null);
        set => SetRelatedEntities("Account_Email_EmailSender", null, value);
    }

    [RelationshipSchemaName("Account_Email_SendersAccount")]
    [RelationshipMetadata("OneToMany", "accountid", "email", "sendersaccount", "Referenced")]
    public IEnumerable<Email> Account_Email_SendersAccount
    {
        get => GetRelatedEntities<Email>("Account_Email_SendersAccount", null);
        set => SetRelatedEntities("Account_Email_SendersAccount", null, value);
    }

    [RelationshipSchemaName("Account_Emails")]
    [RelationshipMetadata("OneToMany", "accountid", "email", "regardingobjectid", "Referenced")]
    public IEnumerable<Email> Account_Emails
    {
        get => GetRelatedEntities<Email>("Account_Emails", null);
        set => SetRelatedEntities("Account_Emails", null, value);
    }

    [RelationshipSchemaName("Account_Faxes")]
    [RelationshipMetadata("OneToMany", "accountid", "fax", "regardingobjectid", "Referenced")]
    public IEnumerable<Fax> Account_Faxes
    {
        get => GetRelatedEntities<Fax>("Account_Faxes", null);
        set => SetRelatedEntities("Account_Faxes", null, value);
    }

    [AttributeLogicalName("masterid")]
    [RelationshipSchemaName("account_master_account")]
    [RelationshipMetadata("ManyToOne", "masterid", "account", "accountid", "Referencing")]
    public Account account_master_account
    {
        get => GetRelatedEntity<Account>("account_master_account", null);
        set => SetRelatedEntity("account_master_account", null, value);
    }

    [AttributeLogicalName("parentaccountid")]
    [RelationshipSchemaName("account_parent_account")]
    [RelationshipMetadata("ManyToOne", "parentaccountid", "account", "accountid", "Referencing")]
    public Account account_parent_account
    {
        get => GetRelatedEntity<Account>("account_parent_account", null);
        set => SetRelatedEntity("account_parent_account", null, value);
    }

    [AttributeLogicalName("primarycontactid")]
    [RelationshipSchemaName("account_primary_contact")]
    [RelationshipMetadata("ManyToOne", "primarycontactid", "contact", "contactid", "Referencing")]
    public Contact account_primary_contact
    {
        get => GetRelatedEntity<Contact>("account_primary_contact", null);
        set => SetRelatedEntity("account_primary_contact", null, value);
    }

    [RelationshipSchemaName("Account_Tasks")]
    [RelationshipMetadata("OneToMany", "accountid", "task", "regardingobjectid", "Referenced")]
    public IEnumerable<Task> Account_Tasks
    {
        get => GetRelatedEntities<Task>("Account_Tasks", null);
        set => SetRelatedEntities("Account_Tasks", null, value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_accounts")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_accounts
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_accounts", null);
        set => SetRelatedEntity("business_unit_accounts", null, value);
    }

    [RelationshipSchemaName("contact_customer_accounts")]
    [RelationshipMetadata("OneToMany", "accountid", "contact", "parentcustomerid", "Referenced")]
    public IEnumerable<Contact> contact_customer_accounts
    {
        get => GetRelatedEntities<Contact>("contact_customer_accounts", null);
        set => SetRelatedEntities("contact_customer_accounts", null, value);
    }

    [RelationshipSchemaName("ctx_account_contact")]
    [RelationshipMetadata("ManyToMany", "accountid", "contact", "contactid", "Entity1")]
    public IEnumerable<Contact> ctx_account_contact
    {
        get => GetRelatedEntities<Contact>("ctx_account_contact", null);
        set => SetRelatedEntities("ctx_account_contact", null, value);
    }

    [RelationshipSchemaName("ctx_account_ctx_parent")]
    [RelationshipMetadata("OneToMany", "accountid", "ctx_parent", "ctx_accountid", "Referenced")]
    public IEnumerable<ctx_parent> ctx_account_ctx_parent
    {
        get => GetRelatedEntities<ctx_parent>("ctx_account_ctx_parent", null);
        set => SetRelatedEntities("ctx_account_ctx_parent", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_accountbase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_accountbase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_accountbase_createdby", null);
        set => SetRelatedEntity("lk_accountbase_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_accountbase_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_accountbase_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_accountbase_createdonbehalfby", null);
        set => SetRelatedEntity("lk_accountbase_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_accountbase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_accountbase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_accountbase_modifiedby", null);
        set => SetRelatedEntity("lk_accountbase_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_accountbase_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_accountbase_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_accountbase_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_accountbase_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("msa_managingpartnerid")]
    [RelationshipSchemaName("msa_account_managingpartner")]
    [RelationshipMetadata("ManyToOne", "msa_managingpartnerid", "account", "accountid", "Referencing")]
    public Account msa_account_managingpartner
    {
        get => GetRelatedEntity<Account>("msa_account_managingpartner", null);
        set => SetRelatedEntity("msa_account_managingpartner", null, value);
    }

    [RelationshipSchemaName("msa_contact_managingpartner")]
    [RelationshipMetadata("OneToMany", "accountid", "contact", "msa_managingpartnerid", "Referenced")]
    public IEnumerable<Contact> msa_contact_managingpartner
    {
        get => GetRelatedEntities<Contact>("msa_contact_managingpartner", null);
        set => SetRelatedEntities("msa_contact_managingpartner", null, value);
    }

    [AttributeLogicalName("preferredsystemuserid")]
    [RelationshipSchemaName("system_user_accounts")]
    [RelationshipMetadata("ManyToOne", "preferredsystemuserid", "systemuser", "systemuserid", "Referencing")]
    public SystemUser system_user_accounts
    {
        get => GetRelatedEntity<SystemUser>("system_user_accounts", null);
        set => SetRelatedEntity("system_user_accounts", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_accounts")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_accounts
    {
        get => GetRelatedEntity<Team>("team_accounts", null);
        set => SetRelatedEntity("team_accounts", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("transactioncurrency_account")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency transactioncurrency_account
    {
        get => GetRelatedEntity<TransactionCurrency>("transactioncurrency_account", null);
        set => SetRelatedEntity("transactioncurrency_account", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("user_accounts")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_accounts
    {
        get => GetRelatedEntity<SystemUser>("user_accounts", null);
        set => SetRelatedEntity("user_accounts", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Account entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Account, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Account with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Account to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Account</returns>
    public static Account Retrieve(IOrganizationService service, Guid id, params Expression<Func<Account, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
