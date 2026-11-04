using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using System.Runtime.ExceptionServices;
using XrmMockupShared.Plugin;
using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using System.Diagnostics;
using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Extensions;

namespace DG.Tools.XrmMockup
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class PluginTrigger : IComparable<PluginTrigger>
    {
        public Action<MockupServiceProviderAndFactory> PluginExecute { get; }

        internal string EntityName { get; }
        internal string Operation { get; }
        internal ExecutionStage Stage { get; }
        internal ExecutionMode Mode { get; }
        internal int Order { get; }
        internal Guid? ImpersonatingUserId { get; }

        readonly SortedDictionary<string, EntityMetadata> Metadata;
        readonly HashSet<string> Attributes;
        readonly IEnumerable<IImageSpecification> Images;

        private string DebuggerDisplay => $"{Stage} {Operation} {EntityName} [{string.Join(", ", Attributes)}]";

        public  PluginTrigger(string operation, ExecutionStage stage,
                Action<MockupServiceProviderAndFactory> pluginExecute, IPluginStepConfig pluginRegistration, SortedDictionary<string, EntityMetadata> metadata)
        {
            PluginExecute = pluginExecute;
            EntityName = pluginRegistration.EntityLogicalName;
            Operation = operation;
            Stage = stage;
            Mode = pluginRegistration.ExecutionMode;
            Order = pluginRegistration.ExecutionOrder;
            Images = pluginRegistration.ImageSpecifications;
            Metadata = metadata;
            ImpersonatingUserId = pluginRegistration.ImpersonatingUserId;

            var attrs = pluginRegistration.FilteredAttributes ?? "";
            Attributes = string.IsNullOrWhiteSpace(attrs) ? new HashSet<string>() : new HashSet<string>(attrs.Split(','));
        }

        public ExecutionMode GetExecutionMode()
        {
            return Mode;
        }

        public int GetExecutionOrder()
        {
            return Order;
        }

        // Saves "execution" for Async plugins to be executed after sync plugins.
        public PluginExecutionProvider ToPluginExecution(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, ICoreOperations core,
            ISet<string> systemInjectedAttributes = null)
        {
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = (entity != null) ? entity.Id : entityRef.Id;
            var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;

            if (VerifyPluginTrigger(entity, logicalName, guid, preImage, postImage, pluginContext, systemInjectedAttributes))
            {
                // Create the plugin context
                var thisPluginContext = CreatePluginContext(pluginContext, guid, logicalName, preImage, postImage);
                var provider = core.CreateServiceProviderAndFactory(thisPluginContext);
                // Preserve async exception semantics (no TargetInvocationException unwrapping), but
                // still time the execution and capture its trace messages into the grouped trace log.
                return new PluginExecutionProvider(
                    p => ExecuteAndRecord(p, thisPluginContext, core, unwrapTargetInvocation: false, record: true),
                    provider);
            }

            return null;
        }

        public void ExecuteIfMatch(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, ICoreOperations core, bool recordTrace = true,
            ISet<string> systemInjectedAttributes = null)
        {
            // Check if it is supposed to execute. Returns preemptively, if it should not.
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;
            var entityCollection = entityObject as EntityCollection;

            var guid =
                entity != null
                ? entity.Id :
                entityRef != null
                ? entityRef.Id
                : Guid.Empty;
            var logicalName =
                entity != null
                ? entity.LogicalName :
                entityRef != null
                ? entityRef.LogicalName
                : entityCollection.EntityName;

            if (VerifyPluginTrigger(entity, logicalName, guid, preImage, postImage, pluginContext, systemInjectedAttributes))
            {
                var thisPluginContext = CreatePluginContext(pluginContext, guid, logicalName, preImage, postImage);

                //Create Serviceprovider, and execute plugin
                MockupServiceProviderAndFactory provider = core.CreateServiceProviderAndFactory(thisPluginContext);
                ExecuteAndRecord(provider, thisPluginContext, core, unwrapTargetInvocation: true, record: recordTrace);

                foreach (var parameter in thisPluginContext.SharedVariables)
                {
                    pluginContext.SharedVariables[parameter.Key] = parameter.Value;
                }
            }
        }

        /// <summary>
        /// Executes the plugin while timing it and capturing any thrown exception, then (when
        /// <paramref name="record"/> is true) records a grouped <see cref="PluginTraceLog"/> entry
        /// containing this execution's trace messages and metadata.
        /// </summary>
        private void ExecuteAndRecord(MockupServiceProviderAndFactory provider, PluginContext ctx, ICoreOperations core, bool unwrapTargetInvocation, bool record)
        {
            PluginTraceRecorder.Run(
                core,
                provider,
                ctx,
                typeName: PluginExecute.Target?.GetType().FullName,
                messageName: !string.IsNullOrEmpty(ctx.MessageName) ? ctx.MessageName : Operation,
                primaryEntity: ctx.PrimaryEntityName,
                operationType: PluginTraceOperationType.Plugin,
                mode: this.Mode,
                execute: () => PluginExecute(provider),
                unwrapTargetInvocation: unwrapTargetInvocation,
                record: record);
        }

        private void CheckInfiniteLoop(PluginContext pluginContext)
        {
            if (pluginContext.Depth > 8)
            {
                throw new FaultException(
                    "This workflow job was canceled because the workflow that started it included an infinite loop." +
                    " Correct the workflow logic and try again.");
            }
        }

        private void CheckSpecialRequest()
        {
            if (EntityName != "" && (Operation.Matches(EventOperation.Associate) || Operation.Matches(EventOperation.Disassociate)))
            {
                throw new MockupException(
                    $"An {Operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
            }
        }

        private Entity AddPostImageAttributesToEntity(Entity entity, Entity preImage, Entity postImage)
        {
            if (Operation.Matches(EventOperation.Update) && Stage == ExecutionStage.PostOperation
                && preImage != null && postImage != null)
            {
                var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                entity = entity.CloneEntity();
                entity.Attributes.AddRange(shadowAddedAttributes);
            }
            return entity;
        }

        private bool FilteredAttributesMatches(Entity entity, ISet<string> systemInjectedAttributes)
        {
            if (!Operation.Matches(EventOperation.Update) || Attributes.Count == 0)
            {
                return true;
            }

            bool foundAttr = false;
            foreach (var attr in entity.Attributes)
            {
                // System-managed attributes that XrmMockup copied onto the Target for post-operation
                // visibility were not part of the update the caller asked for, so they must not
                // satisfy the filter — otherwise a step filtered on e.g. statecode would fire on
                // every update of the table.
                if (systemInjectedAttributes != null && systemInjectedAttributes.Contains(attr.Key))
                {
                    continue;
                }

                if (Attributes.Contains(attr.Key))
                {
                    foundAttr = true;
                    break;
                }
            }
            return foundAttr;
        }

        private bool VerifyPluginTrigger(Entity entity, string logicalName, Guid guid, Entity preImage, Entity postImage, PluginContext pluginContext,
            ISet<string> systemInjectedAttributes = null)
        {
            if (EntityName != "" && EntityName != logicalName) return false;

            if (entity != null && Metadata.GetMetadata(logicalName)?.PrimaryIdAttribute != null)
            {
                entity[Metadata.GetMetadata(logicalName).PrimaryIdAttribute] = guid;
            }

            CheckInfiniteLoop(pluginContext);
            entity = AddPostImageAttributesToEntity(entity, preImage, postImage);
            CheckSpecialRequest();

            if (FilteredAttributesMatches(entity, systemInjectedAttributes))
            {
                return true;
            }
            return false;
        }

        private PluginContext CreatePluginContext(PluginContext pluginContext, Guid guid, string logicalName, Entity preImage, Entity postImage)
        {
            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Mode = (int)this.Mode;
            thisPluginContext.Stage = (int)this.Stage;
            if (thisPluginContext.PrimaryEntityId == Guid.Empty)
            {
                thisPluginContext.PrimaryEntityId = guid;
            }
            thisPluginContext.PrimaryEntityName = logicalName;
            if (ImpersonatingUserId is Guid impersonatingUserId && impersonatingUserId != Guid.Empty)
            {
                thisPluginContext.UserId = impersonatingUserId;
            }

            foreach (var image in this.Images)
            {
                var imageType = image.ImageType;
                var cols = !string.IsNullOrEmpty(image.Attributes) ? new ColumnSet(image.Attributes.Split(',')) : new ColumnSet(true);
                if (postImage != null && Stage == ExecutionStage.PostOperation && (imageType == ImageType.PostImage || imageType == ImageType.Both))
                {
                    thisPluginContext.PostEntityImages.Add(image.ImageName, postImage.CloneEntity(Metadata.GetMetadata(postImage.LogicalName), cols));
                }
                if (preImage != null && (imageType == ImageType.PreImage || imageType == ImageType.Both))
                {
                    thisPluginContext.PreEntityImages.Add(image.ImageName, preImage.CloneEntity(Metadata.GetMetadata(preImage.LogicalName), cols));
                }
            }
            return thisPluginContext;
        }

        public int CompareTo(PluginTrigger other)
        {
            return Order.CompareTo(other.Order);
        }
    }
}
