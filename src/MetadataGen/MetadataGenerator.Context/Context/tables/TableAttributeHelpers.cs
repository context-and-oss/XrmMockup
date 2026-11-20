using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
public static class TableAttributeHelpers
{
    /// <summary>
    /// Gets the logical column name for a property on the entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <typeparam name="T">Type of Entity</typeparam>
    /// <param name="lambda">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName<T>(Expression<Func<T, object?>> lambda)
        where T : Entity
    {
        if (lambda == null) throw new ArgumentNullException(nameof(lambda));

        MemberExpression? body = lambda.Body as MemberExpression;

        if (body == null)
        {
            UnaryExpression ubody = (UnaryExpression)lambda.Body;
            body = ubody.Operand as MemberExpression;
        }

        var attribute = body.Member.GetCustomAttributes(false)
            .Where(x => x is AttributeLogicalNameAttribute)
            .FirstOrDefault() as AttributeLogicalNameAttribute;

        if (attribute == null)
        {
            return body.Member.Name;
        }
        return attribute.LogicalName;
    }

    /// <summary>
    /// Checks if all specified attributes are present on the entity.
    /// </summary>
    /// <typeparam name="T">Type of Entity</typeparam>
    /// <param name="entity">Entity to check if contains attribute</param>
    /// <param name="attrGetters">Expression that specify attributes to check</param>
    /// <returns>Bool for whether all attributes are on the entity</returns>
    /// <exception cref="ArgumentNullException">If Entity is null</exception>
    public static bool ContainsAttributes<T>(this T entity, params Expression<Func<T, object>>[] attrGetters)
        where T : Microsoft.Xrm.Sdk.Entity
    {
        if (attrGetters == null)
        {
            return true;
        }
        return attrGetters.Select(a => GetColumnName(a).ToLower()).All(a => entity.Contains(a));
    }

    /// <summary>
    /// Removes all specified attributes from the entity. Returns true if any attribute was removed.
    /// </summary>
    /// <typeparam name="T">Type of Entity</typeparam>
    /// <param name="entity">Entity to remove attributes from</param>
    /// <param name="attrGetters">Expression that specify attributes to remove</param>
    /// <returns>Return true if any attributes were removed</returns>
    /// <exception cref="ArgumentNullException">If entity is null</exception>
    public static bool RemoveAttributes<T>(this T entity, params Expression<Func<T, object>>[] attrGetters)
        where T : Microsoft.Xrm.Sdk.Entity
    {
        if (attrGetters == null)
        {
            return false;
        }
        return attrGetters.Select(a => GetColumnName(a).ToLower()).Any(a => entity.Attributes.Remove(a));
    }

    /// <summary>
    /// Retrieves an entity of type T with the specified attributes.
    /// </summary>
    /// <typeparam name="T">Type of Entity to retrieve</typeparam>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of entity to retrieve</param>
    /// <param name="attrs">Expressions that specify attributes to retrieve</param>
    /// <returns>The retrieved entity</returns>
    public static T Retrieve<T>(this IOrganizationService service, Guid id, params Expression<Func<T, object>>[] attrs)
        where T : Entity, new()
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        var entity = new T();
        var entityLogicalName = entity.LogicalName;

        if (attrs == null || attrs.Length == 0)
        {
            return service.Retrieve(entityLogicalName, id, new ColumnSet(true)).ToEntity<T>();
        }

        var columnNames = attrs.Select(attr => GetColumnName(attr)).ToArray();
        var columnSet = new ColumnSet(columnNames);

        return service.Retrieve(entityLogicalName, id, columnSet).ToEntity<T>();
    }

    /// <summary>
    /// Retrieves an entity of type T using an alternate key with the specified attributes.
    /// </summary>
    /// <typeparam name="T">Type of Entity to retrieve</typeparam>
    /// <param name="service">Organization service</param>
    /// <param name="keyedEntityReference">EntityReference with alternate key values</param>
    /// <param name="attrs">Expressions that specify attributes to retrieve</param>
    /// <returns>The retrieved entity</returns>
    public static T Retrieve<T>(this IOrganizationService service, EntityReference keyedEntityReference, params Expression<Func<T, object>>[] attrs)
        where T : Entity, new()
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (keyedEntityReference == null) throw new ArgumentNullException(nameof(keyedEntityReference));

        var req = new Microsoft.Xrm.Sdk.Messages.RetrieveRequest();
        req.Target = keyedEntityReference;

        if (attrs == null || attrs.Length == 0)
        {
            req.ColumnSet = new ColumnSet(true);
        }
        else
        {
            var columnNames = attrs.Select(attr => GetColumnName(attr)).ToArray();
            req.ColumnSet = new ColumnSet(columnNames);
        }

        return (service.Execute(req) as Microsoft.Xrm.Sdk.Messages.RetrieveResponse)?.Entity?.ToEntity<T>();
    }
}
