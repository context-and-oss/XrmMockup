using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace XrmMockup.MetadataGenerator.Tool.Context;

/// <summary>
/// <para>Person with whom a business unit has a relationship, such as customer, supplier, and colleague.</para>
/// <para>Display Name: Contact</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("contact")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Contact : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "contact";
    public const int EntityTypeCode = 2;

    public Contact() : base(EntityLogicalName) { }
    public Contact(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("fullname");

    [AttributeLogicalName("contactid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("contactid", value);
        }
    }

    /// <summary>
    /// <para>Unique identifier of the account with which the contact is associated.</para>
    /// <para>Display Name: Account</para>
    /// </summary>
    [AttributeLogicalName("accountid")]
    [DisplayName("Account")]
    public EntityReference? AccountId
    {
        get => GetAttributeValue<EntityReference?>("accountid");
        set => SetAttributeValue("accountid", value);
    }

    /// <summary>
    /// <para>Select the contact's role within the company or sales process, such as decision maker, employee, or influencer.</para>
    /// <para>Display Name: Role</para>
    /// </summary>
    [AttributeLogicalName("accountrolecode")]
    [DisplayName("Role")]
    public contact_accountrolecode? AccountRoleCode
    {
        get => this.GetOptionSetValue<contact_accountrolecode>("accountrolecode");
        set => this.SetOptionSetValue("accountrolecode", value);
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
    public contact_address1_addresstypecode? Address1_AddressTypeCode
    {
        get => this.GetOptionSetValue<contact_address1_addresstypecode>("address1_addresstypecode");
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
    public contact_address1_freighttermscode? Address1_FreightTermsCode
    {
        get => this.GetOptionSetValue<contact_address1_freighttermscode>("address1_freighttermscode");
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
    public contact_address1_shippingmethodcode? Address1_ShippingMethodCode
    {
        get => this.GetOptionSetValue<contact_address1_shippingmethodcode>("address1_shippingmethodcode");
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
    /// <para>Display Name: Address 1: Phone</para>
    /// </summary>
    [AttributeLogicalName("address1_telephone1")]
    [DisplayName("Address 1: Phone")]
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
    public contact_address2_addresstypecode? Address2_AddressTypeCode
    {
        get => this.GetOptionSetValue<contact_address2_addresstypecode>("address2_addresstypecode");
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
    public contact_address2_freighttermscode? Address2_FreightTermsCode
    {
        get => this.GetOptionSetValue<contact_address2_freighttermscode>("address2_freighttermscode");
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
    [MaxLength(100)]
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
    public contact_address2_shippingmethodcode? Address2_ShippingMethodCode
    {
        get => this.GetOptionSetValue<contact_address2_shippingmethodcode>("address2_shippingmethodcode");
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
    /// <para>Unique identifier for address 3.</para>
    /// <para>Display Name: Address 3: ID</para>
    /// </summary>
    [AttributeLogicalName("address3_addressid")]
    [DisplayName("Address 3: ID")]
    public Guid? Address3_AddressId
    {
        get => GetAttributeValue<Guid?>("address3_addressid");
        set => SetAttributeValue("address3_addressid", value);
    }

    /// <summary>
    /// <para>Select the third address type.</para>
    /// <para>Display Name: Address 3: Address Type</para>
    /// </summary>
    [AttributeLogicalName("address3_addresstypecode")]
    [DisplayName("Address 3: Address Type")]
    public contact_address3_addresstypecode? Address3_AddressTypeCode
    {
        get => this.GetOptionSetValue<contact_address3_addresstypecode>("address3_addresstypecode");
        set => this.SetOptionSetValue("address3_addresstypecode", value);
    }

    /// <summary>
    /// <para>Type the city for the 3rd address.</para>
    /// <para>Display Name: Address 3: City</para>
    /// </summary>
    [AttributeLogicalName("address3_city")]
    [DisplayName("Address 3: City")]
    [MaxLength(80)]
    public string? Address3_City
    {
        get => GetAttributeValue<string?>("address3_city");
        set => SetAttributeValue("address3_city", value);
    }

    /// <summary>
    /// <para>Shows the complete third address.</para>
    /// <para>Display Name: Address 3</para>
    /// </summary>
    [AttributeLogicalName("address3_composite")]
    [DisplayName("Address 3")]
    [MaxLength(1000)]
    public string? Address3_Composite
    {
        get => GetAttributeValue<string?>("address3_composite");
        set => SetAttributeValue("address3_composite", value);
    }

    /// <summary>
    /// <para>the country or region for the 3rd address.</para>
    /// <para>Display Name: Address3: Country/Region</para>
    /// </summary>
    [AttributeLogicalName("address3_country")]
    [DisplayName("Address3: Country/Region")]
    [MaxLength(80)]
    public string? Address3_Country
    {
        get => GetAttributeValue<string?>("address3_country");
        set => SetAttributeValue("address3_country", value);
    }

    /// <summary>
    /// <para>Type the county for the third address.</para>
    /// <para>Display Name: Address 3: County</para>
    /// </summary>
    [AttributeLogicalName("address3_county")]
    [DisplayName("Address 3: County")]
    [MaxLength(50)]
    public string? Address3_County
    {
        get => GetAttributeValue<string?>("address3_county");
        set => SetAttributeValue("address3_county", value);
    }

    /// <summary>
    /// <para>Type the fax number associated with the third address.</para>
    /// <para>Display Name: Address 3: Fax</para>
    /// </summary>
    [AttributeLogicalName("address3_fax")]
    [DisplayName("Address 3: Fax")]
    [MaxLength(50)]
    public string? Address3_Fax
    {
        get => GetAttributeValue<string?>("address3_fax");
        set => SetAttributeValue("address3_fax", value);
    }

    /// <summary>
    /// <para>Select the freight terms for the third address to make sure shipping orders are processed correctly.</para>
    /// <para>Display Name: Address 3: Freight Terms</para>
    /// </summary>
    [AttributeLogicalName("address3_freighttermscode")]
    [DisplayName("Address 3: Freight Terms")]
    public contact_address3_freighttermscode? Address3_FreightTermsCode
    {
        get => this.GetOptionSetValue<contact_address3_freighttermscode>("address3_freighttermscode");
        set => this.SetOptionSetValue("address3_freighttermscode", value);
    }

    /// <summary>
    /// <para>Type the latitude value for the third address for use in mapping and other applications.</para>
    /// <para>Display Name: Address 3: Latitude</para>
    /// </summary>
    [AttributeLogicalName("address3_latitude")]
    [DisplayName("Address 3: Latitude")]
    public double? Address3_Latitude
    {
        get => GetAttributeValue<double?>("address3_latitude");
        set => SetAttributeValue("address3_latitude", value);
    }

    /// <summary>
    /// <para>the first line of the 3rd address.</para>
    /// <para>Display Name: Address3: Street 1</para>
    /// </summary>
    [AttributeLogicalName("address3_line1")]
    [DisplayName("Address3: Street 1")]
    [MaxLength(250)]
    public string? Address3_Line1
    {
        get => GetAttributeValue<string?>("address3_line1");
        set => SetAttributeValue("address3_line1", value);
    }

    /// <summary>
    /// <para>the second line of the 3rd address.</para>
    /// <para>Display Name: Address3: Street 2</para>
    /// </summary>
    [AttributeLogicalName("address3_line2")]
    [DisplayName("Address3: Street 2")]
    [MaxLength(250)]
    public string? Address3_Line2
    {
        get => GetAttributeValue<string?>("address3_line2");
        set => SetAttributeValue("address3_line2", value);
    }

    /// <summary>
    /// <para>the third line of the 3rd address.</para>
    /// <para>Display Name: Address3: Street 3</para>
    /// </summary>
    [AttributeLogicalName("address3_line3")]
    [DisplayName("Address3: Street 3")]
    [MaxLength(250)]
    public string? Address3_Line3
    {
        get => GetAttributeValue<string?>("address3_line3");
        set => SetAttributeValue("address3_line3", value);
    }

    /// <summary>
    /// <para>Type the longitude value for the third address for use in mapping and other applications.</para>
    /// <para>Display Name: Address 3: Longitude</para>
    /// </summary>
    [AttributeLogicalName("address3_longitude")]
    [DisplayName("Address 3: Longitude")]
    public double? Address3_Longitude
    {
        get => GetAttributeValue<double?>("address3_longitude");
        set => SetAttributeValue("address3_longitude", value);
    }

    /// <summary>
    /// <para>Type a descriptive name for the third address, such as Corporate Headquarters.</para>
    /// <para>Display Name: Address 3: Name</para>
    /// </summary>
    [AttributeLogicalName("address3_name")]
    [DisplayName("Address 3: Name")]
    [MaxLength(200)]
    public string? Address3_Name
    {
        get => GetAttributeValue<string?>("address3_name");
        set => SetAttributeValue("address3_name", value);
    }

    /// <summary>
    /// <para>the ZIP Code or postal code for the 3rd address.</para>
    /// <para>Display Name: Address3: ZIP/Postal Code</para>
    /// </summary>
    [AttributeLogicalName("address3_postalcode")]
    [DisplayName("Address3: ZIP/Postal Code")]
    [MaxLength(20)]
    public string? Address3_PostalCode
    {
        get => GetAttributeValue<string?>("address3_postalcode");
        set => SetAttributeValue("address3_postalcode", value);
    }

    /// <summary>
    /// <para>the post office box number of the 3rd address.</para>
    /// <para>Display Name: Address 3: Post Office Box</para>
    /// </summary>
    [AttributeLogicalName("address3_postofficebox")]
    [DisplayName("Address 3: Post Office Box")]
    [MaxLength(20)]
    public string? Address3_PostOfficeBox
    {
        get => GetAttributeValue<string?>("address3_postofficebox");
        set => SetAttributeValue("address3_postofficebox", value);
    }

    /// <summary>
    /// <para>Type the name of the main contact at the account's third address.</para>
    /// <para>Display Name: Address 3: Primary Contact Name</para>
    /// </summary>
    [AttributeLogicalName("address3_primarycontactname")]
    [DisplayName("Address 3: Primary Contact Name")]
    [MaxLength(100)]
    public string? Address3_PrimaryContactName
    {
        get => GetAttributeValue<string?>("address3_primarycontactname");
        set => SetAttributeValue("address3_primarycontactname", value);
    }

    /// <summary>
    /// <para>Select a shipping method for deliveries sent to this address.</para>
    /// <para>Display Name: Address 3: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("address3_shippingmethodcode")]
    [DisplayName("Address 3: Shipping Method")]
    public contact_address3_shippingmethodcode? Address3_ShippingMethodCode
    {
        get => this.GetOptionSetValue<contact_address3_shippingmethodcode>("address3_shippingmethodcode");
        set => this.SetOptionSetValue("address3_shippingmethodcode", value);
    }

    /// <summary>
    /// <para>the state or province of the third address.</para>
    /// <para>Display Name: Address3: State/Province</para>
    /// </summary>
    [AttributeLogicalName("address3_stateorprovince")]
    [DisplayName("Address3: State/Province")]
    [MaxLength(50)]
    public string? Address3_StateOrProvince
    {
        get => GetAttributeValue<string?>("address3_stateorprovince");
        set => SetAttributeValue("address3_stateorprovince", value);
    }

    /// <summary>
    /// <para>Type the main phone number associated with the third address.</para>
    /// <para>Display Name: Address 3: Telephone1</para>
    /// </summary>
    [AttributeLogicalName("address3_telephone1")]
    [DisplayName("Address 3: Telephone1")]
    [MaxLength(50)]
    public string? Address3_Telephone1
    {
        get => GetAttributeValue<string?>("address3_telephone1");
        set => SetAttributeValue("address3_telephone1", value);
    }

    /// <summary>
    /// <para>Type a second phone number associated with the third address.</para>
    /// <para>Display Name: Address 3: Telephone2</para>
    /// </summary>
    [AttributeLogicalName("address3_telephone2")]
    [DisplayName("Address 3: Telephone2")]
    [MaxLength(50)]
    public string? Address3_Telephone2
    {
        get => GetAttributeValue<string?>("address3_telephone2");
        set => SetAttributeValue("address3_telephone2", value);
    }

    /// <summary>
    /// <para>Type a third phone number associated with the primary address.</para>
    /// <para>Display Name: Address 3: Telephone3</para>
    /// </summary>
    [AttributeLogicalName("address3_telephone3")]
    [DisplayName("Address 3: Telephone3")]
    [MaxLength(50)]
    public string? Address3_Telephone3
    {
        get => GetAttributeValue<string?>("address3_telephone3");
        set => SetAttributeValue("address3_telephone3", value);
    }

    /// <summary>
    /// <para>Type the UPS zone of the third address to make sure shipping charges are calculated correctly and deliveries are made promptly, if shipped by UPS.</para>
    /// <para>Display Name: Address 3: UPS Zone</para>
    /// </summary>
    [AttributeLogicalName("address3_upszone")]
    [DisplayName("Address 3: UPS Zone")]
    [MaxLength(4)]
    public string? Address3_UPSZone
    {
        get => GetAttributeValue<string?>("address3_upszone");
        set => SetAttributeValue("address3_upszone", value);
    }

    /// <summary>
    /// <para>Select the time zone, or UTC offset, for this address so that other people can reference it when they contact someone at this address.</para>
    /// <para>Display Name: Address 3: UTC Offset</para>
    /// </summary>
    [AttributeLogicalName("address3_utcoffset")]
    [DisplayName("Address 3: UTC Offset")]
    [Range(-1500, 1500)]
    public int? Address3_UTCOffset
    {
        get => GetAttributeValue<int?>("address3_utcoffset");
        set => SetAttributeValue("address3_utcoffset", value);
    }

    /// <summary>
    /// <para>Display Name: Confirm Remove Password</para>
    /// </summary>
    [AttributeLogicalName("adx_confirmremovepassword")]
    [DisplayName("Confirm Remove Password")]
    public bool? adx_ConfirmRemovePassword
    {
        get => GetAttributeValue<bool?>("adx_confirmremovepassword");
        set => SetAttributeValue("adx_confirmremovepassword", value);
    }

    /// <summary>
    /// <para>Display Name: Created By IP Address</para>
    /// </summary>
    [AttributeLogicalName("adx_createdbyipaddress")]
    [DisplayName("Created By IP Address")]
    [MaxLength(100)]
    public string? Adx_CreatedByIPAddress
    {
        get => GetAttributeValue<string?>("adx_createdbyipaddress");
        set => SetAttributeValue("adx_createdbyipaddress", value);
    }

    /// <summary>
    /// <para>Display Name: Created By Username</para>
    /// </summary>
    [AttributeLogicalName("adx_createdbyusername")]
    [DisplayName("Created By Username")]
    [MaxLength(100)]
    public string? Adx_CreatedByUsername
    {
        get => GetAttributeValue<string?>("adx_createdbyusername");
        set => SetAttributeValue("adx_createdbyusername", value);
    }

    /// <summary>
    /// <para>Shows the current count of failed password attempts for the contact.</para>
    /// <para>Display Name: Access Failed Count</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_accessfailedcount")]
    [DisplayName("Access Failed Count")]
    [Range(-2147483648, 2147483647)]
    public int? adx_identity_accessfailedcount
    {
        get => GetAttributeValue<int?>("adx_identity_accessfailedcount");
        set => SetAttributeValue("adx_identity_accessfailedcount", value);
    }

    /// <summary>
    /// <para>Determines if the email is confirmed by the contact.</para>
    /// <para>Display Name: Email Confirmed</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_emailaddress1confirmed")]
    [DisplayName("Email Confirmed")]
    public bool? adx_identity_emailaddress1confirmed
    {
        get => GetAttributeValue<bool?>("adx_identity_emailaddress1confirmed");
        set => SetAttributeValue("adx_identity_emailaddress1confirmed", value);
    }

    /// <summary>
    /// <para>Indicates the last date and time the user successfully signed in to a portal.</para>
    /// <para>Display Name: Last Successful Login</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_lastsuccessfullogin")]
    [DisplayName("Last Successful Login")]
    public DateTime? adx_identity_lastsuccessfullogin
    {
        get => GetAttributeValue<DateTime?>("adx_identity_lastsuccessfullogin");
        set => SetAttributeValue("adx_identity_lastsuccessfullogin", value);
    }

    /// <summary>
    /// <para>Indicates that the contact can no longer sign in to the portal using the local account.</para>
    /// <para>Display Name: Local Login Disabled</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_locallogindisabled")]
    [DisplayName("Local Login Disabled")]
    public bool? adx_identity_locallogindisabled
    {
        get => GetAttributeValue<bool?>("adx_identity_locallogindisabled");
        set => SetAttributeValue("adx_identity_locallogindisabled", value);
    }

    /// <summary>
    /// <para>Determines if this contact will track failed access attempts and become locked after too many failed attempts. To prevent the contact from becoming locked, you can disable this setting.</para>
    /// <para>Display Name: Lockout Enabled</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_lockoutenabled")]
    [DisplayName("Lockout Enabled")]
    public bool? adx_identity_lockoutenabled
    {
        get => GetAttributeValue<bool?>("adx_identity_lockoutenabled");
        set => SetAttributeValue("adx_identity_lockoutenabled", value);
    }

    /// <summary>
    /// <para>Shows the moment in time when the locked contact becomes unlocked again.</para>
    /// <para>Display Name: Lockout End Date</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_lockoutenddate")]
    [DisplayName("Lockout End Date")]
    public DateTime? adx_identity_lockoutenddate
    {
        get => GetAttributeValue<DateTime?>("adx_identity_lockoutenddate");
        set => SetAttributeValue("adx_identity_lockoutenddate", value);
    }

    /// <summary>
    /// <para>Determines if web authentication is enabled for the contact.</para>
    /// <para>Display Name: Login Enabled</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_logonenabled")]
    [DisplayName("Login Enabled")]
    public bool? adx_identity_logonenabled
    {
        get => GetAttributeValue<bool?>("adx_identity_logonenabled");
        set => SetAttributeValue("adx_identity_logonenabled", value);
    }

    /// <summary>
    /// <para>Determines if the phone number is confirmed by the contact.</para>
    /// <para>Display Name: Mobile Phone Confirmed</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_mobilephoneconfirmed")]
    [DisplayName("Mobile Phone Confirmed")]
    public bool? adx_identity_mobilephoneconfirmed
    {
        get => GetAttributeValue<bool?>("adx_identity_mobilephoneconfirmed");
        set => SetAttributeValue("adx_identity_mobilephoneconfirmed", value);
    }

    /// <summary>
    /// <para>Display Name: New Password Input</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_newpassword")]
    [DisplayName("New Password Input")]
    [MaxLength(100)]
    public string? adx_identity_newpassword
    {
        get => GetAttributeValue<string?>("adx_identity_newpassword");
        set => SetAttributeValue("adx_identity_newpassword", value);
    }

    /// <summary>
    /// <para>Display Name: Password Hash</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_passwordhash")]
    [DisplayName("Password Hash")]
    [MaxLength(128)]
    public string? adx_identity_passwordhash
    {
        get => GetAttributeValue<string?>("adx_identity_passwordhash");
        set => SetAttributeValue("adx_identity_passwordhash", value);
    }

    /// <summary>
    /// <para>A token used to manage the web authentication session.</para>
    /// <para>Display Name: Security Stamp</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_securitystamp")]
    [DisplayName("Security Stamp")]
    [MaxLength(100)]
    public string? adx_identity_securitystamp
    {
        get => GetAttributeValue<string?>("adx_identity_securitystamp");
        set => SetAttributeValue("adx_identity_securitystamp", value);
    }

    /// <summary>
    /// <para>Determines if two-factor authentication is enabled for the contact.</para>
    /// <para>Display Name: Two Factor Enabled</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_twofactorenabled")]
    [DisplayName("Two Factor Enabled")]
    public bool? adx_identity_twofactorenabled
    {
        get => GetAttributeValue<bool?>("adx_identity_twofactorenabled");
        set => SetAttributeValue("adx_identity_twofactorenabled", value);
    }

    /// <summary>
    /// <para>Shows the user identity for local web authentication.</para>
    /// <para>Display Name: User Name</para>
    /// </summary>
    [AttributeLogicalName("adx_identity_username")]
    [DisplayName("User Name")]
    [MaxLength(100)]
    public string? adx_identity_username
    {
        get => GetAttributeValue<string?>("adx_identity_username");
        set => SetAttributeValue("adx_identity_username", value);
    }

    /// <summary>
    /// <para>Display Name: Modified By IP Address</para>
    /// </summary>
    [AttributeLogicalName("adx_modifiedbyipaddress")]
    [DisplayName("Modified By IP Address")]
    [MaxLength(100)]
    public string? Adx_ModifiedByIPAddress
    {
        get => GetAttributeValue<string?>("adx_modifiedbyipaddress");
        set => SetAttributeValue("adx_modifiedbyipaddress", value);
    }

    /// <summary>
    /// <para>Display Name: Modified By Username</para>
    /// </summary>
    [AttributeLogicalName("adx_modifiedbyusername")]
    [DisplayName("Modified By Username")]
    [MaxLength(100)]
    public string? Adx_ModifiedByUsername
    {
        get => GetAttributeValue<string?>("adx_modifiedbyusername");
        set => SetAttributeValue("adx_modifiedbyusername", value);
    }

    /// <summary>
    /// <para>Display Name: Organization Name</para>
    /// </summary>
    [AttributeLogicalName("adx_organizationname")]
    [DisplayName("Organization Name")]
    [MaxLength(250)]
    public string? Adx_OrganizationName
    {
        get => GetAttributeValue<string?>("adx_organizationname");
        set => SetAttributeValue("adx_organizationname", value);
    }

    /// <summary>
    /// <para>User’s preferred portal LCID</para>
    /// <para>Display Name: Preferred LCID (Deprecated)</para>
    /// </summary>
    [AttributeLogicalName("adx_preferredlcid")]
    [DisplayName("Preferred LCID (Deprecated)")]
    [Range(-2147483648, 2147483647)]
    public int? adx_preferredlcid
    {
        get => GetAttributeValue<int?>("adx_preferredlcid");
        set => SetAttributeValue("adx_preferredlcid", value);
    }

    /// <summary>
    /// <para>Display Name: Profile Alert</para>
    /// </summary>
    [AttributeLogicalName("adx_profilealert")]
    [DisplayName("Profile Alert")]
    public bool? adx_profilealert
    {
        get => GetAttributeValue<bool?>("adx_profilealert");
        set => SetAttributeValue("adx_profilealert", value);
    }

    /// <summary>
    /// <para>Display Name: Profile Alert Date</para>
    /// </summary>
    [AttributeLogicalName("adx_profilealertdate")]
    [DisplayName("Profile Alert Date")]
    public DateTime? adx_profilealertdate
    {
        get => GetAttributeValue<DateTime?>("adx_profilealertdate");
        set => SetAttributeValue("adx_profilealertdate", value);
    }

    /// <summary>
    /// <para>Display Name: Profile Alert Instructions</para>
    /// </summary>
    [AttributeLogicalName("adx_profilealertinstructions")]
    [DisplayName("Profile Alert Instructions")]
    [MaxLength(4096)]
    public string? adx_profilealertinstructions
    {
        get => GetAttributeValue<string?>("adx_profilealertinstructions");
        set => SetAttributeValue("adx_profilealertinstructions", value);
    }

    /// <summary>
    /// <para>Display Name: Profile Is Anonymous</para>
    /// </summary>
    [AttributeLogicalName("adx_profileisanonymous")]
    [DisplayName("Profile Is Anonymous")]
    public bool? Adx_ProfileIsAnonymous
    {
        get => GetAttributeValue<bool?>("adx_profileisanonymous");
        set => SetAttributeValue("adx_profileisanonymous", value);
    }

    /// <summary>
    /// <para>Display Name: Profile Last Activity</para>
    /// </summary>
    [AttributeLogicalName("adx_profilelastactivity")]
    [DisplayName("Profile Last Activity")]
    public DateTime? Adx_ProfileLastActivity
    {
        get => GetAttributeValue<DateTime?>("adx_profilelastactivity");
        set => SetAttributeValue("adx_profilelastactivity", value);
    }

    /// <summary>
    /// <para>Display Name: Profile Modified On</para>
    /// </summary>
    [AttributeLogicalName("adx_profilemodifiedon")]
    [DisplayName("Profile Modified On")]
    public DateTime? adx_profilemodifiedon
    {
        get => GetAttributeValue<DateTime?>("adx_profilemodifiedon");
        set => SetAttributeValue("adx_profilemodifiedon", value);
    }

    /// <summary>
    /// <para>Display Name: Public Profile Copy</para>
    /// </summary>
    [AttributeLogicalName("adx_publicprofilecopy")]
    [DisplayName("Public Profile Copy")]
    [MaxLength(64000)]
    public string? adx_PublicProfileCopy
    {
        get => GetAttributeValue<string?>("adx_publicprofilecopy");
        set => SetAttributeValue("adx_publicprofilecopy", value);
    }

    /// <summary>
    /// <para>Display Name: Time Zone</para>
    /// </summary>
    [AttributeLogicalName("adx_timezone")]
    [DisplayName("Time Zone")]
    [Range(-1500, 1500)]
    public int? Adx_TimeZone
    {
        get => GetAttributeValue<int?>("adx_timezone");
        set => SetAttributeValue("adx_timezone", value);
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
    /// <para>Shows the Aging 30 field converted to the system's default base currency. The calculations use the exchange rate specified in the Currencies area.</para>
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
    /// <para>Shows the Aging 60 field converted to the system's default base currency. The calculations use the exchange rate specified in the Currencies area.</para>
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
    /// <para>Shows the Aging 90 field converted to the system's default base currency. The calculations use the exchange rate specified in the Currencies area.</para>
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
    /// <para>Enter the date of the contact's wedding or service anniversary for use in customer gift programs or other communications.</para>
    /// <para>Display Name: Anniversary</para>
    /// </summary>
    [AttributeLogicalName("anniversary")]
    [DisplayName("Anniversary")]
    public DateTime? Anniversary
    {
        get => GetAttributeValue<DateTime?>("anniversary");
        set => SetAttributeValue("anniversary", value);
    }

    /// <summary>
    /// <para>Type the contact's annual income for use in profiling and financial analysis.</para>
    /// <para>Display Name: Annual Income</para>
    /// </summary>
    [AttributeLogicalName("annualincome")]
    [DisplayName("Annual Income")]
    public decimal? AnnualIncome
    {
        get => this.GetMoneyValue("annualincome");
        set => this.SetMoneyValue("annualincome", value);
    }

    /// <summary>
    /// <para>Shows the Annual Income field converted to the system's default base currency. The calculations use the exchange rate specified in the Currencies area.</para>
    /// <para>Display Name: Annual Income (Base)</para>
    /// </summary>
    [AttributeLogicalName("annualincome_base")]
    [DisplayName("Annual Income (Base)")]
    public decimal? AnnualIncome_Base
    {
        get => this.GetMoneyValue("annualincome_base");
        set => this.SetMoneyValue("annualincome_base", value);
    }

    /// <summary>
    /// <para>Type the name of the contact's assistant.</para>
    /// <para>Display Name: Assistant</para>
    /// </summary>
    [AttributeLogicalName("assistantname")]
    [DisplayName("Assistant")]
    [MaxLength(100)]
    public string? AssistantName
    {
        get => GetAttributeValue<string?>("assistantname");
        set => SetAttributeValue("assistantname", value);
    }

    /// <summary>
    /// <para>Type the phone number for the contact's assistant.</para>
    /// <para>Display Name: Assistant Phone</para>
    /// </summary>
    [AttributeLogicalName("assistantphone")]
    [DisplayName("Assistant Phone")]
    [MaxLength(50)]
    public string? AssistantPhone
    {
        get => GetAttributeValue<string?>("assistantphone");
        set => SetAttributeValue("assistantphone", value);
    }

    /// <summary>
    /// <para>Enter the contact's birthday for use in customer gift programs or other communications.</para>
    /// <para>Display Name: Birthday</para>
    /// </summary>
    [AttributeLogicalName("birthdate")]
    [DisplayName("Birthday")]
    public DateTime? BirthDate
    {
        get => GetAttributeValue<DateTime?>("birthdate");
        set => SetAttributeValue("birthdate", value);
    }

    /// <summary>
    /// <para>Type a second business phone number for this contact.</para>
    /// <para>Display Name: Business Phone 2</para>
    /// </summary>
    [AttributeLogicalName("business2")]
    [DisplayName("Business Phone 2")]
    [MaxLength(50)]
    public string? Business2
    {
        get => GetAttributeValue<string?>("business2");
        set => SetAttributeValue("business2", value);
    }

    /// <summary>
    /// <para>Type a callback phone number for this contact.</para>
    /// <para>Display Name: Callback Number</para>
    /// </summary>
    [AttributeLogicalName("callback")]
    [DisplayName("Callback Number")]
    [MaxLength(50)]
    public string? Callback
    {
        get => GetAttributeValue<string?>("callback");
        set => SetAttributeValue("callback", value);
    }

    /// <summary>
    /// <para>Type the names of the contact's children for reference in communications and client programs.</para>
    /// <para>Display Name: Children's Names</para>
    /// </summary>
    [AttributeLogicalName("childrensnames")]
    [DisplayName("Children's Names")]
    [MaxLength(255)]
    public string? ChildrensNames
    {
        get => GetAttributeValue<string?>("childrensnames");
        set => SetAttributeValue("childrensnames", value);
    }

    /// <summary>
    /// <para>Type the company phone of the contact.</para>
    /// <para>Display Name: Company Phone</para>
    /// </summary>
    [AttributeLogicalName("company")]
    [DisplayName("Company Phone")]
    [MaxLength(50)]
    public string? Company
    {
        get => GetAttributeValue<string?>("company");
        set => SetAttributeValue("company", value);
    }

    /// <summary>
    /// <para>Display Name: Contact</para>
    /// </summary>
    [AttributeLogicalName("contactid")]
    [DisplayName("Contact")]
    public Guid? ContactId
    {
        get => GetAttributeValue<Guid?>("contactid");
        set => SetId("contactid", value);
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
    /// <para>Type the credit limit of the contact for reference when you address invoice and accounting issues with the customer.</para>
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
    /// <para>Shows the Credit Limit field converted to the system's default base currency for reporting purposes. The calculations use the exchange rate specified in the Currencies area.</para>
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
    /// <para>Select whether the contact is on a credit hold, for reference when addressing invoice and accounting issues.</para>
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
    /// <para>Select the size of the contact's company for segmentation and reporting purposes.</para>
    /// <para>Display Name: Customer Size</para>
    /// </summary>
    [AttributeLogicalName("customersizecode")]
    [DisplayName("Customer Size")]
    public contact_customersizecode? CustomerSizeCode
    {
        get => this.GetOptionSetValue<contact_customersizecode>("customersizecode");
        set => this.SetOptionSetValue("customersizecode", value);
    }

    /// <summary>
    /// <para>Select the category that best describes the relationship between the contact and your organization.</para>
    /// <para>Display Name: Relationship Type</para>
    /// </summary>
    [AttributeLogicalName("customertypecode")]
    [DisplayName("Relationship Type")]
    public contact_customertypecode? CustomerTypeCode
    {
        get => this.GetOptionSetValue<contact_customertypecode>("customertypecode");
        set => this.SetOptionSetValue("customertypecode", value);
    }

    /// <summary>
    /// <para>Type the department or business unit where the contact works in the parent company or business.</para>
    /// <para>Display Name: Department</para>
    /// </summary>
    [AttributeLogicalName("department")]
    [DisplayName("Department")]
    [MaxLength(100)]
    public string? Department
    {
        get => GetAttributeValue<string?>("department");
        set => SetAttributeValue("department", value);
    }

    /// <summary>
    /// <para>Type additional information to describe the contact, such as an excerpt from the company's website.</para>
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
    /// <para>Select whether the contact accepts bulk email sent through marketing campaigns or quick campaigns. If Do Not Allow is selected, the contact can be added to marketing lists, but will be excluded from the email.</para>
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
    /// <para>Select whether the contact accepts bulk postal mail sent through marketing campaigns or quick campaigns. If Do Not Allow is selected, the contact can be added to marketing lists, but will be excluded from the letters.</para>
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
    /// <para>Select whether the contact allows direct email sent from Microsoft Dynamics 365. If Do Not Allow is selected, Microsoft Dynamics 365 will not send the email.</para>
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
    /// <para>Select whether the contact allows faxes. If Do Not Allow is selected, the contact will be excluded from any fax activities distributed in marketing campaigns.</para>
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
    /// <para>Select whether the contact accepts phone calls. If Do Not Allow is selected, the contact will be excluded from any phone call activities distributed in marketing campaigns.</para>
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
    /// <para>Select whether the contact allows direct mail. If Do Not Allow is selected, the contact will be excluded from letter activities distributed in marketing campaigns.</para>
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
    /// <para>Select whether the contact accepts marketing materials, such as brochures or catalogs. Contacts that opt out can be excluded from marketing initiatives.</para>
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
    /// <para>Select the contact's highest level of education for use in segmentation and analysis.</para>
    /// <para>Display Name: Education</para>
    /// </summary>
    [AttributeLogicalName("educationcode")]
    [DisplayName("Education")]
    public contact_educationcode? EducationCode
    {
        get => this.GetOptionSetValue<contact_educationcode>("educationcode");
        set => this.SetOptionSetValue("educationcode", value);
    }

    /// <summary>
    /// <para>Type the primary email address for the contact.</para>
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
    /// <para>Type the secondary email address for the contact.</para>
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
    /// <para>Type an alternate email address for the contact.</para>
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
    /// <para>Type the employee ID or number for the contact for reference in orders, service cases, or other communications with the contact's organization.</para>
    /// <para>Display Name: Employee</para>
    /// </summary>
    [AttributeLogicalName("employeeid")]
    [DisplayName("Employee")]
    [MaxLength(50)]
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
    /// <para>Identifier for an external user.</para>
    /// <para>Display Name: External User Identifier</para>
    /// </summary>
    [AttributeLogicalName("externaluseridentifier")]
    [DisplayName("External User Identifier")]
    [MaxLength(50)]
    public string? ExternalUserIdentifier
    {
        get => GetAttributeValue<string?>("externaluseridentifier");
        set => SetAttributeValue("externaluseridentifier", value);
    }

    /// <summary>
    /// <para>Select the marital status of the contact for reference in follow-up phone calls and other communications.</para>
    /// <para>Display Name: Marital Status</para>
    /// </summary>
    [AttributeLogicalName("familystatuscode")]
    [DisplayName("Marital Status")]
    public contact_familystatuscode? FamilyStatusCode
    {
        get => this.GetOptionSetValue<contact_familystatuscode>("familystatuscode");
        set => this.SetOptionSetValue("familystatuscode", value);
    }

    /// <summary>
    /// <para>Type the fax number for the contact.</para>
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
    /// <para>Type the contact's first name to make sure the contact is addressed correctly in sales calls, email, and marketing campaigns.</para>
    /// <para>Display Name: First Name</para>
    /// </summary>
    [AttributeLogicalName("firstname")]
    [DisplayName("First Name")]
    [MaxLength(50)]
    public string? FirstName
    {
        get => GetAttributeValue<string?>("firstname");
        set => SetAttributeValue("firstname", value);
    }

    /// <summary>
    /// <para>Information about whether to allow following email activity like opens, attachment views and link clicks for emails sent to the contact.</para>
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
    /// <para>Type the URL for the contact's FTP site to enable users to access data and share documents.</para>
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
    /// <para>Combines and shows the contact's first and last names so that the full name can be displayed in views and reports.</para>
    /// <para>Display Name: Full Name</para>
    /// </summary>
    [AttributeLogicalName("fullname")]
    [DisplayName("Full Name")]
    [MaxLength(160)]
    public string? FullName
    {
        get => GetAttributeValue<string?>("fullname");
        set => SetAttributeValue("fullname", value);
    }

    /// <summary>
    /// <para>Select the contact's gender to make sure the contact is addressed correctly in sales calls, email, and marketing campaigns.</para>
    /// <para>Display Name: Gender</para>
    /// </summary>
    [AttributeLogicalName("gendercode")]
    [DisplayName("Gender")]
    public contact_gendercode? GenderCode
    {
        get => this.GetOptionSetValue<contact_gendercode>("gendercode");
        set => this.SetOptionSetValue("gendercode", value);
    }

    /// <summary>
    /// <para>Type the passport number or other government ID for the contact for use in documents or reports.</para>
    /// <para>Display Name: Government</para>
    /// </summary>
    [AttributeLogicalName("governmentid")]
    [DisplayName("Government")]
    [MaxLength(50)]
    public string? GovernmentId
    {
        get => GetAttributeValue<string?>("governmentid");
        set => SetAttributeValue("governmentid", value);
    }

    /// <summary>
    /// <para>Select whether the contact has any children for reference in follow-up phone calls and other communications.</para>
    /// <para>Display Name: Has Children</para>
    /// </summary>
    [AttributeLogicalName("haschildrencode")]
    [DisplayName("Has Children")]
    public contact_haschildrencode? HasChildrenCode
    {
        get => this.GetOptionSetValue<contact_haschildrencode>("haschildrencode");
        set => this.SetOptionSetValue("haschildrencode", value);
    }

    /// <summary>
    /// <para>Type a second home phone number for this contact.</para>
    /// <para>Display Name: Home Phone 2</para>
    /// </summary>
    [AttributeLogicalName("home2")]
    [DisplayName("Home Phone 2")]
    [MaxLength(50)]
    public string? Home2
    {
        get => GetAttributeValue<string?>("home2");
        set => SetAttributeValue("home2", value);
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
    /// <para>Information about whether the contact was auto-created when promoting an email or an appointment.</para>
    /// <para>Display Name: Auto-created</para>
    /// </summary>
    [AttributeLogicalName("isautocreate")]
    [DisplayName("Auto-created")]
    public bool? IsAutoCreate
    {
        get => GetAttributeValue<bool?>("isautocreate");
        set => SetAttributeValue("isautocreate", value);
    }

    /// <summary>
    /// <para>Select whether the contact exists in a separate accounting or other system, such as Microsoft Dynamics GP or another ERP database, for use in integration processes.</para>
    /// <para>Display Name: Back Office Customer</para>
    /// </summary>
    [AttributeLogicalName("isbackofficecustomer")]
    [DisplayName("Back Office Customer")]
    public bool? IsBackofficeCustomer
    {
        get => GetAttributeValue<bool?>("isbackofficecustomer");
        set => SetAttributeValue("isbackofficecustomer", value);
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
    /// <para>Type the job title of the contact to make sure the contact is addressed correctly in sales calls, email, and marketing campaigns.</para>
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
    /// <para>Type the contact's last name to make sure the contact is addressed correctly in sales calls, email, and marketing campaigns.</para>
    /// <para>Display Name: Last Name</para>
    /// </summary>
    [AttributeLogicalName("lastname")]
    [DisplayName("Last Name")]
    [MaxLength(50)]
    public string? LastName
    {
        get => GetAttributeValue<string?>("lastname");
        set => SetAttributeValue("lastname", value);
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
    /// <para>Shows the date when the contact was last included in a marketing campaign or quick campaign.</para>
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
    /// <para>Select the primary marketing source that directed the contact to your organization.</para>
    /// <para>Display Name: Lead Source</para>
    /// </summary>
    [AttributeLogicalName("leadsourcecode")]
    [DisplayName("Lead Source")]
    public contact_leadsourcecode? LeadSourceCode
    {
        get => this.GetOptionSetValue<contact_leadsourcecode>("leadsourcecode");
        set => this.SetOptionSetValue("leadsourcecode", value);
    }

    /// <summary>
    /// <para>Type the name of the contact's manager for use in escalating issues or other follow-up communications with the contact.</para>
    /// <para>Display Name: Manager</para>
    /// </summary>
    [AttributeLogicalName("managername")]
    [DisplayName("Manager")]
    [MaxLength(100)]
    public string? ManagerName
    {
        get => GetAttributeValue<string?>("managername");
        set => SetAttributeValue("managername", value);
    }

    /// <summary>
    /// <para>Type the phone number for the contact's manager.</para>
    /// <para>Display Name: Manager Phone</para>
    /// </summary>
    [AttributeLogicalName("managerphone")]
    [DisplayName("Manager Phone")]
    [MaxLength(50)]
    public string? ManagerPhone
    {
        get => GetAttributeValue<string?>("managerphone");
        set => SetAttributeValue("managerphone", value);
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
    /// <para>Unique identifier of the master contact for merge.</para>
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
    /// <para>Shows whether the account has been merged with a master contact.</para>
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
    /// <para>Type the contact's middle name or initial to make sure the contact is addressed correctly.</para>
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
    /// <para>Type the mobile phone number for the contact.</para>
    /// <para>Display Name: Mobile Phone</para>
    /// </summary>
    [AttributeLogicalName("mobilephone")]
    [DisplayName("Mobile Phone")]
    [MaxLength(50)]
    public string? MobilePhone
    {
        get => GetAttributeValue<string?>("mobilephone");
        set => SetAttributeValue("mobilephone", value);
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
    /// <para>Shows who last updated the record on behalf of another user.</para>
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
    /// <para>Unique identifier for Account associated with Contact.</para>
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
    /// <para>Indicates that the contact has opted out of web tracking.</para>
    /// <para>Display Name: Disable Web Tracking</para>
    /// </summary>
    [AttributeLogicalName("msdyn_disablewebtracking")]
    [DisplayName("Disable Web Tracking")]
    public bool? msdyn_disablewebtracking
    {
        get => GetAttributeValue<bool?>("msdyn_disablewebtracking");
        set => SetAttributeValue("msdyn_disablewebtracking", value);
    }

    /// <summary>
    /// <para>Indicates that the contact is considered a minor in their jurisdiction.</para>
    /// <para>Display Name: Is Minor</para>
    /// </summary>
    [AttributeLogicalName("msdyn_isminor")]
    [DisplayName("Is Minor")]
    public bool? msdyn_isminor
    {
        get => GetAttributeValue<bool?>("msdyn_isminor");
        set => SetAttributeValue("msdyn_isminor", value);
    }

    /// <summary>
    /// <para>Indicates that the contact is considered a minor in their jurisdiction and has parental consent.</para>
    /// <para>Display Name: Is Minor with Parental Consent</para>
    /// </summary>
    [AttributeLogicalName("msdyn_isminorwithparentalconsent")]
    [DisplayName("Is Minor with Parental Consent")]
    public bool? msdyn_isminorwithparentalconsent
    {
        get => GetAttributeValue<bool?>("msdyn_isminorwithparentalconsent");
        set => SetAttributeValue("msdyn_isminorwithparentalconsent", value);
    }

    /// <summary>
    /// <para>Indicates the date and time that the person agreed to the portal terms and conditions.</para>
    /// <para>Display Name: Portal Terms Agreement Date</para>
    /// </summary>
    [AttributeLogicalName("msdyn_portaltermsagreementdate")]
    [DisplayName("Portal Terms Agreement Date")]
    public DateTime? msdyn_portaltermsagreementdate
    {
        get => GetAttributeValue<DateTime?>("msdyn_portaltermsagreementdate");
        set => SetAttributeValue("msdyn_portaltermsagreementdate", value);
    }

    /// <summary>
    /// <para>User’s preferred portal language</para>
    /// <para>Display Name: Preferred Language</para>
    /// </summary>
    [AttributeLogicalName("mspp_userpreferredlcid")]
    [DisplayName("Preferred Language")]
    public powerpagelanguages? mspp_userpreferredlcid
    {
        get => this.GetOptionSetValue<powerpagelanguages>("mspp_userpreferredlcid");
        set => this.SetOptionSetValue("mspp_userpreferredlcid", value);
    }

    /// <summary>
    /// <para>Type the contact's nickname.</para>
    /// <para>Display Name: Nickname</para>
    /// </summary>
    [AttributeLogicalName("nickname")]
    [DisplayName("Nickname")]
    [MaxLength(100)]
    public string? NickName
    {
        get => GetAttributeValue<string?>("nickname");
        set => SetAttributeValue("nickname", value);
    }

    /// <summary>
    /// <para>Type the number of children the contact has for reference in follow-up phone calls and other communications.</para>
    /// <para>Display Name: No. of Children</para>
    /// </summary>
    [AttributeLogicalName("numberofchildren")]
    [DisplayName("No. of Children")]
    [Range(0, 1000000000)]
    public int? NumberOfChildren
    {
        get => GetAttributeValue<int?>("numberofchildren");
        set => SetAttributeValue("numberofchildren", value);
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
    /// <para>Unique identifier of the business unit that owns the contact.</para>
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
    /// <para>Unique identifier of the team who owns the contact.</para>
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
    /// <para>Unique identifier of the user who owns the contact.</para>
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
    /// <para>Type the pager number for the contact.</para>
    /// <para>Display Name: Pager</para>
    /// </summary>
    [AttributeLogicalName("pager")]
    [DisplayName("Pager")]
    [MaxLength(50)]
    public string? Pager
    {
        get => GetAttributeValue<string?>("pager");
        set => SetAttributeValue("pager", value);
    }

    /// <summary>
    /// <para>Unique identifier of the parent contact.</para>
    /// <para>Display Name: Parent Contact</para>
    /// </summary>
    [AttributeLogicalName("parentcontactid")]
    [DisplayName("Parent Contact")]
    public EntityReference? ParentContactId
    {
        get => GetAttributeValue<EntityReference?>("parentcontactid");
        set => SetAttributeValue("parentcontactid", value);
    }

    /// <summary>
    /// <para>Select the parent account or parent contact for the contact to provide a quick link to additional details, such as financial information, activities, and opportunities.</para>
    /// <para>Display Name: Company Name</para>
    /// </summary>
    [AttributeLogicalName("parentcustomerid")]
    [DisplayName("Company Name")]
    public EntityReference? ParentCustomerId
    {
        get => GetAttributeValue<EntityReference?>("parentcustomerid");
        set => SetAttributeValue("parentcustomerid", value);
    }

    /// <summary>
    /// <para>Shows whether the contact participates in workflow rules.</para>
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
    public contact_paymenttermscode? PaymentTermsCode
    {
        get => this.GetOptionSetValue<contact_paymenttermscode>("paymenttermscode");
        set => this.SetOptionSetValue("paymenttermscode", value);
    }

    /// <summary>
    /// <para>Select the preferred day of the week for service appointments.</para>
    /// <para>Display Name: Preferred Day</para>
    /// </summary>
    [AttributeLogicalName("preferredappointmentdaycode")]
    [DisplayName("Preferred Day")]
    public contact_preferredappointmentdaycode? PreferredAppointmentDayCode
    {
        get => this.GetOptionSetValue<contact_preferredappointmentdaycode>("preferredappointmentdaycode");
        set => this.SetOptionSetValue("preferredappointmentdaycode", value);
    }

    /// <summary>
    /// <para>Select the preferred time of day for service appointments.</para>
    /// <para>Display Name: Preferred Time</para>
    /// </summary>
    [AttributeLogicalName("preferredappointmenttimecode")]
    [DisplayName("Preferred Time")]
    public contact_preferredappointmenttimecode? PreferredAppointmentTimeCode
    {
        get => this.GetOptionSetValue<contact_preferredappointmenttimecode>("preferredappointmenttimecode");
        set => this.SetOptionSetValue("preferredappointmenttimecode", value);
    }

    /// <summary>
    /// <para>Select the preferred method of contact.</para>
    /// <para>Display Name: Preferred Method of Contact</para>
    /// </summary>
    [AttributeLogicalName("preferredcontactmethodcode")]
    [DisplayName("Preferred Method of Contact")]
    public contact_preferredcontactmethodcode? PreferredContactMethodCode
    {
        get => this.GetOptionSetValue<contact_preferredcontactmethodcode>("preferredcontactmethodcode");
        set => this.SetOptionSetValue("preferredcontactmethodcode", value);
    }

    /// <summary>
    /// <para>Choose the regular or preferred customer service representative for reference when scheduling service activities for the contact.</para>
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
    /// <para>Type the salutation of the contact to make sure the contact is addressed correctly in sales calls, email messages, and marketing campaigns.</para>
    /// <para>Display Name: Salutation</para>
    /// </summary>
    [AttributeLogicalName("salutation")]
    [DisplayName("Salutation")]
    [MaxLength(100)]
    public string? Salutation
    {
        get => GetAttributeValue<string?>("salutation");
        set => SetAttributeValue("salutation", value);
    }

    /// <summary>
    /// <para>Select a shipping method for deliveries sent to this address.</para>
    /// <para>Display Name: Shipping Method</para>
    /// </summary>
    [AttributeLogicalName("shippingmethodcode")]
    [DisplayName("Shipping Method")]
    public contact_shippingmethodcode? ShippingMethodCode
    {
        get => this.GetOptionSetValue<contact_shippingmethodcode>("shippingmethodcode");
        set => this.SetOptionSetValue("shippingmethodcode", value);
    }

    /// <summary>
    /// <para>Choose the service level agreement (SLA) that you want to apply to the Contact record.</para>
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
    /// <para>Type the name of the contact's spouse or partner for reference during calls, events, or other communications with the contact.</para>
    /// <para>Display Name: Spouse/Partner Name</para>
    /// </summary>
    [AttributeLogicalName("spousesname")]
    [DisplayName("Spouse/Partner Name")]
    [MaxLength(100)]
    public string? SpousesName
    {
        get => GetAttributeValue<string?>("spousesname");
        set => SetAttributeValue("spousesname", value);
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
    /// <para>Shows whether the contact is active or inactive. Inactive contacts are read-only and can't be edited unless they are reactivated.</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Status")]
    public contact_statecode? StateCode
    {
        get => this.GetOptionSetValue<contact_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Select the contact's status.</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public contact_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<contact_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Subscription</para>
    /// </summary>
    [AttributeLogicalName("subscriptionid")]
    [DisplayName("Subscription")]
    public Guid? SubscriptionId
    {
        get => GetAttributeValue<Guid?>("subscriptionid");
        set => SetAttributeValue("subscriptionid", value);
    }

    /// <summary>
    /// <para>Type the suffix used in the contact's name, such as Jr. or Sr. to make sure the contact is addressed correctly in sales calls, email, and marketing campaigns.</para>
    /// <para>Display Name: Suffix</para>
    /// </summary>
    [AttributeLogicalName("suffix")]
    [DisplayName("Suffix")]
    [MaxLength(10)]
    public string? Suffix
    {
        get => GetAttributeValue<string?>("suffix");
        set => SetAttributeValue("suffix", value);
    }

    /// <summary>
    /// <para>Type the main phone number for this contact.</para>
    /// <para>Display Name: Business Phone</para>
    /// </summary>
    [AttributeLogicalName("telephone1")]
    [DisplayName("Business Phone")]
    [MaxLength(50)]
    public string? Telephone1
    {
        get => GetAttributeValue<string?>("telephone1");
        set => SetAttributeValue("telephone1", value);
    }

    /// <summary>
    /// <para>Type a second phone number for this contact.</para>
    /// <para>Display Name: Home Phone</para>
    /// </summary>
    [AttributeLogicalName("telephone2")]
    [DisplayName("Home Phone")]
    [MaxLength(50)]
    public string? Telephone2
    {
        get => GetAttributeValue<string?>("telephone2");
        set => SetAttributeValue("telephone2", value);
    }

    /// <summary>
    /// <para>Type a third phone number for this contact.</para>
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
    /// <para>Select a region or territory for the contact for use in segmentation and analysis.</para>
    /// <para>Display Name: Territory</para>
    /// </summary>
    [AttributeLogicalName("territorycode")]
    [DisplayName("Territory")]
    public contact_territorycode? TerritoryCode
    {
        get => this.GetOptionSetValue<contact_territorycode>("territorycode");
        set => this.SetOptionSetValue("territorycode", value);
    }

    /// <summary>
    /// <para>Total time spent for emails (read and write) and meetings by me in relation to the contact record.</para>
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
    /// <para>Version number of the contact.</para>
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
    /// <para>Type the contact's professional or personal website or blog URL.</para>
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
    /// <para>Type the phonetic spelling of the contact's first name, if the name is specified in Japanese, to make sure the name is pronounced correctly in phone calls with the contact.</para>
    /// <para>Display Name: Yomi First Name</para>
    /// </summary>
    [AttributeLogicalName("yomifirstname")]
    [DisplayName("Yomi First Name")]
    [MaxLength(150)]
    public string? YomiFirstName
    {
        get => GetAttributeValue<string?>("yomifirstname");
        set => SetAttributeValue("yomifirstname", value);
    }

    /// <summary>
    /// <para>Shows the combined Yomi first and last names of the contact so that the full phonetic name can be displayed in views and reports.</para>
    /// <para>Display Name: Yomi Full Name</para>
    /// </summary>
    [AttributeLogicalName("yomifullname")]
    [DisplayName("Yomi Full Name")]
    [MaxLength(450)]
    public string? YomiFullName
    {
        get => GetAttributeValue<string?>("yomifullname");
        set => SetAttributeValue("yomifullname", value);
    }

    /// <summary>
    /// <para>Type the phonetic spelling of the contact's last name, if the name is specified in Japanese, to make sure the name is pronounced correctly in phone calls with the contact.</para>
    /// <para>Display Name: Yomi Last Name</para>
    /// </summary>
    [AttributeLogicalName("yomilastname")]
    [DisplayName("Yomi Last Name")]
    [MaxLength(150)]
    public string? YomiLastName
    {
        get => GetAttributeValue<string?>("yomilastname");
        set => SetAttributeValue("yomilastname", value);
    }

    /// <summary>
    /// <para>Type the phonetic spelling of the contact's middle name, if the name is specified in Japanese, to make sure the name is pronounced correctly in phone calls with the contact.</para>
    /// <para>Display Name: Yomi Middle Name</para>
    /// </summary>
    [AttributeLogicalName("yomimiddlename")]
    [DisplayName("Yomi Middle Name")]
    [MaxLength(150)]
    public string? YomiMiddleName
    {
        get => GetAttributeValue<string?>("yomimiddlename");
        set => SetAttributeValue("yomimiddlename", value);
    }

    [RelationshipSchemaName("account_primary_contact")]
    [RelationshipMetadata("OneToMany", "contactid", "account", "primarycontactid", "Referenced")]
    public IEnumerable<Account> account_primary_contact
    {
        get => GetRelatedEntities<Account>("account_primary_contact", null);
        set => SetRelatedEntities("account_primary_contact", null, value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_contacts")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_contacts
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_contacts", null);
        set => SetRelatedEntity("business_unit_contacts", null, value);
    }

    [RelationshipSchemaName("contact_activity_parties")]
    [RelationshipMetadata("OneToMany", "contactid", "activityparty", "partyid", "Referenced")]
    public IEnumerable<ActivityParty> contact_activity_parties
    {
        get => GetRelatedEntities<ActivityParty>("contact_activity_parties", null);
        set => SetRelatedEntities("contact_activity_parties", null, value);
    }

    [AttributeLogicalName("parentcustomerid")]
    [RelationshipSchemaName("contact_customer_accounts")]
    [RelationshipMetadata("ManyToOne", "parentcustomerid", "account", "accountid", "Referencing")]
    public Account contact_customer_accounts
    {
        get => GetRelatedEntity<Account>("contact_customer_accounts", null);
        set => SetRelatedEntity("contact_customer_accounts", null, value);
    }

    [AttributeLogicalName("parentcustomerid")]
    [RelationshipSchemaName("contact_customer_contacts")]
    [RelationshipMetadata("ManyToOne", "parentcustomerid", "contact", "contactid", "Referencing")]
    public Contact contact_customer_contacts
    {
        get => GetRelatedEntity<Contact>("contact_customer_contacts", null);
        set => SetRelatedEntity("contact_customer_contacts", null, value);
    }

    [AttributeLogicalName("masterid")]
    [RelationshipSchemaName("contact_master_contact")]
    [RelationshipMetadata("ManyToOne", "masterid", "contact", "contactid", "Referencing")]
    public Contact contact_master_contact
    {
        get => GetRelatedEntity<Contact>("contact_master_contact", null);
        set => SetRelatedEntity("contact_master_contact", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("contact_owning_user")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser contact_owning_user
    {
        get => GetRelatedEntity<SystemUser>("contact_owning_user", null);
        set => SetRelatedEntity("contact_owning_user", null, value);
    }

    [RelationshipSchemaName("ctx_account_contact")]
    [RelationshipMetadata("ManyToMany", "contactid", "account", "accountid", "Entity2")]
    public IEnumerable<Account> ctx_account_contact
    {
        get => GetRelatedEntities<Account>("ctx_account_contact", null);
        set => SetRelatedEntities("ctx_account_contact", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_contact_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_contact_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_contact_createdonbehalfby", null);
        set => SetRelatedEntity("lk_contact_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_contact_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_contact_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_contact_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_contact_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_contactbase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_contactbase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_contactbase_createdby", null);
        set => SetRelatedEntity("lk_contactbase_createdby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_contactbase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_contactbase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_contactbase_modifiedby", null);
        set => SetRelatedEntity("lk_contactbase_modifiedby", null, value);
    }

    [AttributeLogicalName("msa_managingpartnerid")]
    [RelationshipSchemaName("msa_contact_managingpartner")]
    [RelationshipMetadata("ManyToOne", "msa_managingpartnerid", "account", "accountid", "Referencing")]
    public Account msa_contact_managingpartner
    {
        get => GetRelatedEntity<Account>("msa_contact_managingpartner", null);
        set => SetRelatedEntity("msa_contact_managingpartner", null, value);
    }

    [AttributeLogicalName("preferredsystemuserid")]
    [RelationshipSchemaName("system_user_contacts")]
    [RelationshipMetadata("ManyToOne", "preferredsystemuserid", "systemuser", "systemuserid", "Referencing")]
    public SystemUser system_user_contacts
    {
        get => GetRelatedEntity<SystemUser>("system_user_contacts", null);
        set => SetRelatedEntity("system_user_contacts", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_contacts")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_contacts
    {
        get => GetRelatedEntity<Team>("team_contacts", null);
        set => SetRelatedEntity("team_contacts", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("transactioncurrency_contact")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency transactioncurrency_contact
    {
        get => GetRelatedEntity<TransactionCurrency>("transactioncurrency_contact", null);
        set => SetRelatedEntity("transactioncurrency_contact", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Contact entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Contact, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Contact with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Contact to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Contact</returns>
    public static Contact Retrieve(IOrganizationService service, Guid id, params Expression<Func<Contact, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
