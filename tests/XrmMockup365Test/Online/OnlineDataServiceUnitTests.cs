#if DATAVERSE_SERVICE_CLIENT
using System;
using System.Linq;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest.Online
{
    /// <summary>
    /// Unit tests verifying XrmMockup's integration with IOnlineDataService.
    /// Uses mock IOnlineDataService to verify correct behavior without real proxy.
    /// Only available on net10.0 (DATAVERSE_SERVICE_CLIENT).
    /// </summary>
    public class OnlineDataServiceUnitTests : IClassFixture<XrmMockupFixture>
    {
        private readonly XrmMockupFixture _fixture;

        public OnlineDataServiceUnitTests(XrmMockupFixture fixture)
        {
            _fixture = fixture;
        }

        private (XrmMockup365 crm, MockOnlineDataService mockService) CreateMockupWithOnlineService()
        {
            var mockService = new MockOnlineDataService();
            var settings = new XrmMockupSettings
            {
                BasePluginTypes = _fixture.Settings.BasePluginTypes,
                BaseCustomApiTypes = _fixture.Settings.BaseCustomApiTypes,
                CodeActivityInstanceTypes = _fixture.Settings.CodeActivityInstanceTypes,
                EnableProxyTypes = _fixture.Settings.EnableProxyTypes,
                IncludeAllWorkflows = _fixture.Settings.IncludeAllWorkflows,
                ExceptionFreeRequests = _fixture.Settings.ExceptionFreeRequests,
                MetadataDirectoryPath = _fixture.Settings.MetadataDirectoryPath,
                IPluginMetadata = _fixture.Settings.IPluginMetadata,
                OnlineDataServiceFactory = () => mockService
            };

            var crm = XrmMockup365.GetInstance(settings);
            return (crm, mockService);
        }

        [Fact]
        public void GetDbRow_EntityNotInDb_CallsOnlineServiceRetrieve()
        {
            // Arrange
            var (crm, mockService) = CreateMockupWithOnlineService();
            var service = crm.GetAdminService();

            var onlineAccountId = Guid.NewGuid();
            var onlineAccount = new Entity(Account.EntityLogicalName, onlineAccountId)
            {
                ["name"] = "Online Account",
                ["accountnumber"] = "ONLINE-001"
            };
            mockService.SetupEntity(onlineAccount);

            // Act - Try to retrieve an account that doesn't exist locally
            // This should trigger a call to the online service
            var retrieved = service.Retrieve(Account.EntityLogicalName, onlineAccountId, new ColumnSet(true));

            // Assert
            Assert.Single(mockService.RetrieveCalls);
            Assert.Equal(Account.EntityLogicalName, mockService.RetrieveCalls[0].EntityName);
            Assert.Equal(onlineAccountId, mockService.RetrieveCalls[0].Id);
            Assert.Equal("Online Account", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void GetDbRow_EntityInDb_DoesNotCallOnlineService()
        {
            // Arrange
            var (crm, mockService) = CreateMockupWithOnlineService();
            var service = crm.GetAdminService();

            // Create account locally
            var localAccount = new Account { Name = "Local Account" };
            localAccount.Id = service.Create(localAccount);

            mockService.ClearCalls(); // Clear any calls from Create

            // Act - Retrieve the locally created account
            var retrieved = service.Retrieve(Account.EntityLogicalName, localAccount.Id, new ColumnSet(true));

            // Assert - No calls to online service since entity exists locally
            Assert.Empty(mockService.RetrieveCalls);
            Assert.Equal("Local Account", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void GetDbRow_RetrievedEntityAddedToLocalDb()
        {
            // Arrange
            var (crm, mockService) = CreateMockupWithOnlineService();
            var service = crm.GetAdminService();

            var onlineAccountId = Guid.NewGuid();
            var onlineAccount = new Entity(Account.EntityLogicalName, onlineAccountId)
            {
                ["name"] = "Online Account",
                ["accountnumber"] = "ONLINE-002"
            };
            mockService.SetupEntity(onlineAccount);

            // Act - First retrieve fetches from online
            service.Retrieve(Account.EntityLogicalName, onlineAccountId, new ColumnSet(true));
            mockService.ClearCalls();

            // Second retrieve should use local cache
            var retrieved = service.Retrieve(Account.EntityLogicalName, onlineAccountId, new ColumnSet(true));

            // Assert - Second retrieve should not call online service
            Assert.Empty(mockService.RetrieveCalls);
            Assert.Equal("Online Account", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void PrefillDBWithOnlineData_CallsRetrieveMultiple()
        {
            // Arrange
            var (crm, mockService) = CreateMockupWithOnlineService();

            var onlineAccount1 = new Entity(Account.EntityLogicalName, Guid.NewGuid())
            {
                ["name"] = "Online Account 1"
            };
            var onlineAccount2 = new Entity(Account.EntityLogicalName, Guid.NewGuid())
            {
                ["name"] = "Online Account 2"
            };
            mockService.SetupEntities(new[] { onlineAccount1, onlineAccount2 });

            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };

            // Act
            crm.PrefillDBWithOnlineData(query);

            // Assert
            Assert.Single(mockService.RetrieveMultipleCalls);
            Assert.Equal(Account.EntityLogicalName, mockService.RetrieveMultipleCalls[0].EntityName);
        }

        [Fact]
        public void PrefillDBWithOnlineData_AddsEntitiesToLocalDb()
        {
            // Arrange
            var (crm, mockService) = CreateMockupWithOnlineService();
            var service = crm.GetAdminService();

            var onlineAccount1 = new Entity(Account.EntityLogicalName, Guid.NewGuid())
            {
                ["name"] = "Online Account 1"
            };
            var onlineAccount2 = new Entity(Account.EntityLogicalName, Guid.NewGuid())
            {
                ["name"] = "Online Account 2"
            };
            mockService.SetupEntities(new[] { onlineAccount1, onlineAccount2 });

            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };

            // Act
            crm.PrefillDBWithOnlineData(query);
            mockService.ClearCalls();

            // Retrieve entities - should come from local DB, not online
            var retrieved1 = service.Retrieve(Account.EntityLogicalName, onlineAccount1.Id, new ColumnSet(true));
            var retrieved2 = service.Retrieve(Account.EntityLogicalName, onlineAccount2.Id, new ColumnSet(true));

            // Assert - No online calls since entities are now local
            Assert.Empty(mockService.RetrieveCalls);
            Assert.Equal("Online Account 1", retrieved1.GetAttributeValue<string>("name"));
            Assert.Equal("Online Account 2", retrieved2.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void PrefillDBWithOnlineData_SkipsExistingEntities()
        {
            // Arrange
            var (crm, mockService) = CreateMockupWithOnlineService();
            var service = crm.GetAdminService();

            // Create a local account first
            var localAccount = new Account { Name = "Local Account" };
            localAccount.Id = service.Create(localAccount);

            // Setup online service to return an account with the same ID but different name
            var onlineAccount = new Entity(Account.EntityLogicalName, localAccount.Id)
            {
                ["name"] = "Online Account (should not overwrite)"
            };
            mockService.SetupEntity(onlineAccount);

            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };

            // Act
            crm.PrefillDBWithOnlineData(query);

            // Retrieve the entity
            var retrieved = service.Retrieve(Account.EntityLogicalName, localAccount.Id, new ColumnSet(true));

            // Assert - Local entity should NOT be overwritten
            Assert.Equal("Local Account", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void NoOnlineService_EntityNotFound_ThrowsException()
        {
            // Arrange - Create mockup without online service
            var crm = XrmMockup365.GetInstance(_fixture.Settings);
            var service = crm.GetAdminService();

            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException>(() =>
                service.Retrieve(Account.EntityLogicalName, nonExistentId, new ColumnSet(true)));
        }
    }
}
#endif
