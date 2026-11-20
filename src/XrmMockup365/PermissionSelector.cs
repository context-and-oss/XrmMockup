using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using XrmPluginCore.Enums;
using XrmMockup.PermissionAnalyzer;

namespace DG.Tools.XrmMockup
{
    /// <summary>Typed static permission scopes. Uses SDK logical-name attributes, never CLR member names.</summary>
    public static class PermissionSelector
    {
        public static PermissionScope Plugin<TPlugin>(EventOperation? operation = null, ExecutionStage? stage = null) where TPlugin : IPlugin
        {
            if (operation.HasValue && !Enum.IsDefined(typeof(EventOperation), operation.Value)) throw new ArgumentOutOfRangeException(nameof(operation));
            return Plugin<TPlugin>(operation.HasValue ? operation.Value.ToString() : null, stage);
        }

        /// <summary>Selects steps for a custom SDK message, or all messages when omitted.</summary>
        public static PermissionScope Plugin<TPlugin>(string message, ExecutionStage? stage = null) where TPlugin : IPlugin
        {
            if (stage.HasValue && !Enum.IsDefined(typeof(ExecutionStage), stage.Value)) throw new ArgumentOutOfRangeException(nameof(stage));
            var type = typeof(TPlugin);
            return PermissionScope.ForPlugin(type.FullName, type.Assembly.GetName().Name, message, stage.HasValue ? stage.Value.ToString() : null);
        }

        public static PermissionScope Operation<TEntity>(EventOperation operation, params Expression<Func<TEntity, object>>[] attributes) where TEntity : Entity
        {
            if (!Enum.IsDefined(typeof(EventOperation), operation)) throw new ArgumentOutOfRangeException(nameof(operation));
            return Operation(operation.ToString(), attributes);
        }

        public static PermissionScope Operation<TEntity>(string message, params Expression<Func<TEntity, object>>[] attributes) where TEntity : Entity
        {
            var logicalName = typeof(TEntity).GetCustomAttribute<EntityLogicalNameAttribute>();
            if (logicalName == null) throw new ArgumentException("The entity type must have an SDK EntityLogicalName attribute.", nameof(TEntity));
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            var columns = new string[attributes.Length];
            for (var i = 0; i < attributes.Length; i++)
            {
                var expression = attributes[i];
                if (expression == null) throw new ArgumentException("An attribute expression cannot be null.", nameof(attributes));
                Expression body = expression.Body;
                if (body is UnaryExpression unary && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked)) body = unary.Operand;
                if (!(body is MemberExpression member) || member.Expression != expression.Parameters[0] || !(member.Member is PropertyInfo property))
                    throw new ArgumentException("Select a direct SDK entity property, such as account => account.Name.", nameof(attributes));
                var attribute = property.GetCustomAttribute<AttributeLogicalNameAttribute>();
                if (attribute == null) throw new ArgumentException("The selected property must have an SDK AttributeLogicalName attribute.", nameof(attributes));
                columns[i] = attribute.LogicalName;
            }
            return PermissionScope.ForOperation(message, logicalName.LogicalName, columns.Length == 0 ? null : columns);
        }
    }
}
