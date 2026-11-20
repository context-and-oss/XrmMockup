using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

[assembly: ProxyTypesAssembly]

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
public class ExtendedEntity : Entity
{
    public ExtendedEntity(string logicalName)
        : base(logicalName)
    {
    }

    public ExtendedEntity(string logicalName, Guid id)
        : base(logicalName, id)
    {
    }

    public new T? GetAttributeValue<T>(string attributeLogicalName)
    {
        return base.GetAttributeValue<T>(attributeLogicalName);
    }

    protected string GetDebuggerDisplay(string primaryNameAttribute)
    {
        string display = GetType().Name;

        var name = this.GetAttributeValue<string>(primaryNameAttribute);
        if (!string.IsNullOrEmpty(name)) display += $" ({name})";
        if (this.Id != Guid.Empty) display += $" [{this.Id}]";

        return display;
    }

    protected void SetId(string primaryIdAttribute, Guid? id)
    {
        base.Id = id.GetValueOrDefault();
        SetAttributeValue(primaryIdAttribute, id);
    }

    protected IEnumerable<T> GetEntityCollection<T>(string attributeName)
        where T : Entity
    {
        var collection = this.GetAttributeValue<EntityCollection>(attributeName);
        if (collection != null && collection.Entities != null)
        {
            return collection.Entities.Select(x => x.ToEntity<T>());
        }
        else
        {
            return Enumerable.Empty<T>();
        }
    }

    protected void SetEntityCollection<T>(string attributeName, IEnumerable<T> entities)
        where T : Entity
    {
        var list = entities?.Cast<Entity>().ToList();
        if (list == null || !list.Any())
        {
            this.SetAttributeValue(attributeName, null);
            return;
        }

        this.SetAttributeValue(attributeName, new EntityCollection(list));
    }

    protected decimal? GetMoneyValue(string attributeName)
    {
        var money = this.GetAttributeValue<Money?>(attributeName);
        if (money != null)
        {
            return money.Value;
        }
        else
        {
            return null;
        }
    }

    protected void SetMoneyValue(string attributeName, decimal? value)
    {
        if (value.HasValue)
        {
            this.SetAttributeValue(attributeName, new Money(value.Value));
        }
        else
        {
            this.SetAttributeValue(attributeName, null);
        }
    }

    protected IEnumerable<T> GetOptionSetCollectionValue<T>(string attributeName)
        where T : struct, IComparable, IConvertible, IFormattable
    {
        var optionSetCollection = this.GetAttributeValue<OptionSetValueCollection>(attributeName);
        if (optionSetCollection != null && optionSetCollection.Count != 0)
        {
            return optionSetCollection
                .Select(osv => (T)Enum.ToObject(typeof(T), osv.Value))
                .ToArray();
        }

        return Enumerable.Empty<T>();
    }

    protected void SetOptionSetCollectionValue<T>(string attributeName, IEnumerable<T> values)
    {
        var list = values?.Cast<object>().Cast<int>().ToList();
        if (list == null || !list.Any())
        {
            this.SetAttributeValue(attributeName, null);
            return;
        }

        var arr = list
            .Select(v => new OptionSetValue(v))
            .ToArray();
        this.SetAttributeValue(attributeName, new OptionSetValueCollection(arr));
    }

    protected T? GetOptionSetValue<T>(string attributeName)
        where T : struct, IComparable, IConvertible, IFormattable
    {
        var optionSet = this.GetAttributeValue<OptionSetValue?>(attributeName);
        if (optionSet != null)
        {
            return (T)Enum.ToObject(typeof(T), optionSet.Value);
        }

        return null;
    }

    protected void SetOptionSetValue<T>(string attributeName, T? value)
    {
        if (value != null)
        {
            this.SetAttributeValue(attributeName, new OptionSetValue((int)(object)value));
        }
        else
        {
            this.SetAttributeValue(attributeName, null);
        }
    }
}
