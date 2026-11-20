using DG.Tools.XrmMockup.Internal;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {
    internal class PluginContext : IPluginExecutionContext7 {

        private ParameterCollection _propertyBag = new ParameterCollection();

        private ParameterCollection _inputParameters = new ParameterCollection();
        private ParameterCollection _outputParameters = new ParameterCollection();
        private ParameterCollection _sharedVariables = new ParameterCollection();

        private EntityImageCollection _preEntityImages = new EntityImageCollection();
        private EntityImageCollection _postEntityImages = new EntityImageCollection();

        private EntityImageCollection[] _preEntityImagesCollection = Array.Empty<EntityImageCollection>();
        private EntityImageCollection[] _postEntityImagesCollection = Array.Empty<EntityImageCollection>();

        public int? ExtensionDepth;

        public PluginContext() {
            this.CorrelationId = Guid.NewGuid();
        }

        public Guid BusinessUnitId {
            get {
                _propertyBag.TryGetValue(nameof(BusinessUnitId), out object val);
                return (Guid)val;
            }
            set {
                _propertyBag[nameof(BusinessUnitId)] = value;
            }
        }

        public Guid CorrelationId {
            get {
                _propertyBag.TryGetValue(nameof(CorrelationId), out object val);
                return (Guid)val;
            }
            set {
                _propertyBag[nameof(CorrelationId)] = value;
            }
        }

        public int Depth {
            get {
                _propertyBag.TryGetValue(nameof(Depth), out object val);
                return (int)val;
            }
            set {
                _propertyBag[nameof(Depth)] = value;
            }
        }

        public Guid InitiatingUserId {
            get {
                _propertyBag.TryGetValue(nameof(InitiatingUserId), out object val);
                return (Guid)val;
            }
            set {
                _propertyBag[nameof(InitiatingUserId)] = value;
            }
        }

        public ParameterCollection InputParameters {
            get {
                return _inputParameters;
            }
        }

        public bool IsExecutingOffline {
            get {
                _propertyBag.TryGetValue(nameof(IsExecutingOffline), out object val);
                return (bool)val;
            }
            set {
                _propertyBag[nameof(IsExecutingOffline)] = value;
            }
        }

        public bool IsInTransaction {
            get {
                _propertyBag.TryGetValue(nameof(IsInTransaction), out object val);
                return (bool)val;
            }
            set {
                _propertyBag[nameof(IsInTransaction)] = value;
            }
        }

        public bool IsOfflinePlayback {
            get {
                _propertyBag.TryGetValue(nameof(IsOfflinePlayback), out object val);
                return (bool)val;
            }
            set {
                _propertyBag[nameof(IsOfflinePlayback)] = value;
            }
        }

        public int IsolationMode {
            get {
                _propertyBag.TryGetValue(nameof(IsolationMode), out object val);
                return (int)val;
            }
            set {
                _propertyBag[nameof(IsolationMode)] = value;
            }
        }

        public string MessageName {
            get {
                _propertyBag.TryGetValue(nameof(MessageName), out object val);
                return (string)val;
            }
            set {
                _propertyBag[nameof(MessageName)] = value;
            }
        }

        public int Mode {
            get {
                _propertyBag.TryGetValue(nameof(Mode), out object val);
                return (int)val;
            }
            set {
                _propertyBag[nameof(Mode)] = value;
            }
        }

        public DateTime OperationCreatedOn {
            get {
                _propertyBag.TryGetValue(nameof(OperationCreatedOn), out object val);
                return (DateTime)val;
            }
            set {
                _propertyBag[nameof(OperationCreatedOn)] = value;
            }
        }

        public Guid OperationId {
            get {
                _propertyBag.TryGetValue(nameof(OperationId), out object val);
                return (Guid)val;
            }
            set {
                _propertyBag[nameof(OperationId)] = value;
            }
        }

        public Guid OrganizationId {
            get {
                _propertyBag.TryGetValue(nameof(OrganizationId), out object val);
                return (Guid)val;
            }
            set {
                _propertyBag[nameof(OrganizationId)] = value;
            }
        }

        public string OrganizationName {
            get {
                _propertyBag.TryGetValue(nameof(OrganizationName), out object val);
                return (string)val;
            }
            set {
                _propertyBag[nameof(OrganizationName)] = value;
            }
        }

        public ParameterCollection OutputParameters {
            get {
                return _outputParameters;
            }
        }

        public EntityReference OwningExtension {
            get {
                _propertyBag.TryGetValue(nameof(OwningExtension), out object val);
                return (EntityReference)val;
            }
            set {
                _propertyBag[nameof(OwningExtension)] = value;
            }
        }

        public IPluginExecutionContext ParentContext {
            get {
                _propertyBag.TryGetValue(nameof(ParentContext), out object val);
                return (IPluginExecutionContext)val;
            }
            set {
                _propertyBag[nameof(ParentContext)] = value;
            }
        }

        public EntityImageCollection PostEntityImages {
            get {
                return _postEntityImages;
            }
        }

        public EntityImageCollection PreEntityImages {
            get {
                return _preEntityImages;
            }
        }

        public Guid PrimaryEntityId {
            get {
                _propertyBag.TryGetValue(nameof(PrimaryEntityId), out object val);

                return (val is Guid guid)
                    ? guid
                    : Guid.Empty;
            }
            set {
                _propertyBag[nameof(PrimaryEntityId)] = value;
            }
        }

        public string PrimaryEntityName {
            get {
                _propertyBag.TryGetValue(nameof(PrimaryEntityName), out object val);
                return (string)val;
            }
            set {
                _propertyBag[nameof(PrimaryEntityName)] = value;
            }
        }

        public Guid? RequestId {
            get {
                _propertyBag.TryGetValue(nameof(RequestId), out object val);
                return (Guid?)val;
            }
            set {
                _propertyBag[nameof(RequestId)] = value;
            }
        }

        public string SecondaryEntityName {
            get {
                _propertyBag.TryGetValue(nameof(SecondaryEntityName), out object val);
                return (string)val;
            }
            set {
                _propertyBag[nameof(SecondaryEntityName)] = value;
            }
        }

        public ParameterCollection SharedVariables {
            get {
                return _sharedVariables;
            }
        }

        public int Stage {
            get {
                _propertyBag.TryGetValue(nameof(Stage), out object val);
                return (int)val;
            }
            set {
                _propertyBag[nameof(Stage)] = value;
            }
        }

        public Guid UserId {
            get {
                _propertyBag.TryGetValue(nameof(UserId), out object val);
                return (Guid)val;
            }
            set {
                _propertyBag[nameof(UserId)] = value;
            }
        }

        // IPluginExecutionContext2
        public Guid UserAzureActiveDirectoryObjectId {
            get {
                return _propertyBag.TryGetValue(nameof(UserAzureActiveDirectoryObjectId), out object val) && val is Guid guid
                    ? guid : Guid.Empty;
            }
            set { _propertyBag[nameof(UserAzureActiveDirectoryObjectId)] = value; }
        }

        public Guid InitiatingUserAzureActiveDirectoryObjectId {
            get {
                return _propertyBag.TryGetValue(nameof(InitiatingUserAzureActiveDirectoryObjectId), out object val) && val is Guid guid
                    ? guid : Guid.Empty;
            }
            set { _propertyBag[nameof(InitiatingUserAzureActiveDirectoryObjectId)] = value; }
        }

        public Guid InitiatingUserApplicationId {
            get {
                return _propertyBag.TryGetValue(nameof(InitiatingUserApplicationId), out object val) && val is Guid guid
                    ? guid : Guid.Empty;
            }
            set { _propertyBag[nameof(InitiatingUserApplicationId)] = value; }
        }

        public Guid PortalsContactId {
            get {
                return _propertyBag.TryGetValue(nameof(PortalsContactId), out object val) && val is Guid guid
                    ? guid : Guid.Empty;
            }
            set { _propertyBag[nameof(PortalsContactId)] = value; }
        }

        public bool IsPortalsClientCall {
            get {
                return _propertyBag.TryGetValue(nameof(IsPortalsClientCall), out object val) && val is bool b && b;
            }
            set { _propertyBag[nameof(IsPortalsClientCall)] = value; }
        }

        // IPluginExecutionContext3
        public Guid AuthenticatedUserId {
            get {
                return _propertyBag.TryGetValue(nameof(AuthenticatedUserId), out object val) && val is Guid guid
                    ? guid : Guid.Empty;
            }
            set { _propertyBag[nameof(AuthenticatedUserId)] = value; }
        }

        // IPluginExecutionContext4
        public EntityImageCollection[] PreEntityImagesCollection {
            get { return _preEntityImagesCollection; }
            internal set { _preEntityImagesCollection = value ?? Array.Empty<EntityImageCollection>(); }
        }

        public EntityImageCollection[] PostEntityImagesCollection {
            get { return _postEntityImagesCollection; }
            internal set { _postEntityImagesCollection = value ?? Array.Empty<EntityImageCollection>(); }
        }

        // IPluginExecutionContext5
        public string InitiatingUserAgent {
            get {
                _propertyBag.TryGetValue(nameof(InitiatingUserAgent), out object val);
                return val as string;
            }
            set { _propertyBag[nameof(InitiatingUserAgent)] = value; }
        }

        // IPluginExecutionContext6
        public string EnvironmentId {
            get {
                _propertyBag.TryGetValue(nameof(EnvironmentId), out object val);
                return val as string;
            }
            set { _propertyBag[nameof(EnvironmentId)] = value; }
        }

        public Guid TenantId {
            get {
                return _propertyBag.TryGetValue(nameof(TenantId), out object val) && val is Guid guid
                    ? guid : Guid.Empty;
            }
            set { _propertyBag[nameof(TenantId)] = value; }
        }

        // IPluginExecutionContext7
        public bool IsApplicationUser {
            get {
                return _propertyBag.TryGetValue(nameof(IsApplicationUser), out object val) && val is bool b && b;
            }
            set { _propertyBag[nameof(IsApplicationUser)] = value; }
        }

        public PluginContext Clone() {
            var clone = new PluginContext();
            CloneDataCollection(this._propertyBag, clone._propertyBag, CloneFunc);
            CloneDataCollection(this._inputParameters, clone._inputParameters, CloneFunc);
            CloneDataCollection(this._outputParameters, clone._outputParameters, CloneFunc);
            CloneDataCollection(this._sharedVariables, clone._sharedVariables, CloneFunc);

            CloneDataCollection(this._postEntityImages, clone._postEntityImages, x => x.CloneEntity());
            CloneDataCollection(this._preEntityImages, clone._preEntityImages, x => x.CloneEntity());

            clone._preEntityImagesCollection = this._preEntityImagesCollection
                .Select(c => { var cl = new EntityImageCollection(); CloneDataCollection(c, cl, x => x.CloneEntity()); return cl; })
                .ToArray();
            clone._postEntityImagesCollection = this._postEntityImagesCollection
                .Select(c => { var cl = new EntityImageCollection(); CloneDataCollection(c, cl, x => x.CloneEntity()); return cl; })
                .ToArray();

            return clone;
        }

        private void CloneDataCollection<T>(DataCollection<string,T> from, DataCollection<string, T> to, Func<T,T> cloneHelper) {
            foreach (var keyVal in from) {
                to[keyVal.Key] = cloneHelper(keyVal.Value);
            }
        }

        private object CloneFunc(object obj) {
            var entityRef = obj as EntityReference;
            if (entityRef != null) {
                return new EntityReference(entityRef.LogicalName, entityRef.Id);
            }
            return obj;
        }

    }
}
