using Xunit;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DG.XrmMockupTest
{
    public class TestCustomService : UnitTestBase
    {
        private class MockServiceEndpointNotificationService : IServiceEndpointNotificationService, IDisposable
        {
            public List<IExecutionContext> Contexts { get; } = new List<IExecutionContext>();
            public bool Disposed { get; private set; }

            public string Execute(EntityReference serviceEndpoint, IExecutionContext context)
            {
                Assert.Equal("serviceendpoint", serviceEndpoint.LogicalName);
                Contexts.Add(context);
                return null;
            }

            public void Dispose() => Disposed = true;
        }

        private const string ContactDescription = "Test_IServiceEndpointNotificationService";
        private readonly XrmMockupSettings settings;

        public TestCustomService(XrmMockupFixture fixture) : base(fixture)
        {
            settings = fixture.Settings;
        }

        private static Core GetCore(XrmMockup365 instance) =>
            (Core)typeof(XrmMockupBase).GetField("Core", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

        private MockupServiceProviderAndFactory Provider => GetCore(crm).ServiceFactory;

        private void CreateContact() => orgAdminUIService.Create(new Contact { Description = ContactDescription });

        [Fact]
        public void CustomServiceShouldBeAvailableInPlugin()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);

            CreateContact();
            CreateContact();

            Assert.Same(customService, Provider.GetService(typeof(IServiceEndpointNotificationService)));
            Assert.Equal(2, customService.Contexts.Count);
            Assert.All(customService.Contexts, context => Assert.Equal("Create", context.MessageName));
            Assert.Contains("Custom service plugin depth: 1", crm.TraceLog);
        }

        [Fact]
        public void ShouldThrowExceptionOnDuplicateRegistration()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);

            Assert.Throws<ArgumentException>(() =>
                crm.AddService<IServiceEndpointNotificationService>(new MockServiceEndpointNotificationService()));
            Assert.Same(customService, Provider.GetService(typeof(IServiceEndpointNotificationService)));
        }

        [Fact]
        public void ShouldRejectNullWithoutReservingRegistration()
        {
            Assert.Throws<ArgumentNullException>(() => crm.AddService<IServiceEndpointNotificationService>(null));
            var service = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(service);
            Assert.Same(service, Provider.GetService(typeof(IServiceEndpointNotificationService)));
        }

        [Fact]
        public void UnknownServicesReturnNullAndPluginsCanProbeOptionalServices()
        {
            Assert.Null(Provider.GetService(typeof(IServiceEndpointNotificationService)));
            Assert.Null(Provider.GetService(typeof(IDisposable)));
            Assert.Null(Provider.GetService(null));
            Assert.Null(Provider.GetService<IServiceEndpointNotificationService>());
            CreateContact();
        }

        [Fact]
        public void RemovedServicesReturnNullAndCanBeRegisteredAgain()
        {
            var service = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(service);
            crm.RemoveService<IServiceEndpointNotificationService>();
            crm.RemoveService<IServiceEndpointNotificationService>();

            Assert.Null(Provider.GetService(typeof(IServiceEndpointNotificationService)));
            CreateContact();
            Assert.Empty(service.Contexts);
            Assert.False(service.Disposed);

            crm.AddService<IServiceEndpointNotificationService>(service);
            CreateContact();
            Assert.Single(service.Contexts);
        }

        [Fact]
        public void ResetServicesClearsAllRegistrationsWithoutDisposingThem()
        {
            var service = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(service);
            crm.AddService<IDisposable>(service);
            crm.ResetServices();
            crm.ResetServices();

            Assert.Null(Provider.GetService(typeof(IServiceEndpointNotificationService)));
            Assert.Null(Provider.GetService(typeof(IDisposable)));
            CreateContact();
            Assert.Empty(service.Contexts);
            Assert.False(service.Disposed);
        }

        [Fact]
        public void CustomServiceShouldStayAvailableAfterEnvironmentReset()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);
            CreateContact();
            crm.ResetEnvironment();

            Assert.Same(customService, Provider.GetService(typeof(IServiceEndpointNotificationService)));
            CreateContact();
            Assert.Equal(2, customService.Contexts.Count);
            Assert.False(customService.Disposed);
        }

        [Fact]
        public void RegistrationUsesExactGenericTypeRatherThanImplementedInterfaces()
        {
            var service = new MockServiceEndpointNotificationService();
            crm.AddService(service);
            Assert.Same(service, Provider.GetService(typeof(MockServiceEndpointNotificationService)));
            Assert.Null(Provider.GetService(typeof(IServiceEndpointNotificationService)));
            CreateContact();
            Assert.Empty(service.Contexts);

            crm.AddService<IServiceEndpointNotificationService>(service);
            crm.RemoveService<MockServiceEndpointNotificationService>();
            Assert.Null(Provider.GetService(typeof(MockServiceEndpointNotificationService)));
            CreateContact();
            Assert.Single(service.Contexts);
        }

        [Fact]
        public void RegistrationsAreIsolatedBetweenInstancesWithSharedSettings()
        {
            var other = XrmMockup365.GetInstance(settings);
            var first = new MockServiceEndpointNotificationService();
            var second = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(first);
            Assert.Null(GetCore(other).ServiceFactory.GetService(typeof(IServiceEndpointNotificationService)));
            other.AddService<IServiceEndpointNotificationService>(second);

            CreateContact();
            other.GetAdminService().Create(new Contact { Description = ContactDescription });
            Assert.Single(first.Contexts);
            Assert.Single(second.Contexts);
            crm.ResetServices();
            crm.ResetEnvironment();
            Assert.Same(second, GetCore(other).ServiceFactory.GetService(typeof(IServiceEndpointNotificationService)));
            other.GetAdminService().Create(new Contact { Description = ContactDescription });
            Assert.Equal(2, second.Contexts.Count);
        }

        [Fact]
        public void NestedPluginExecutionsShareRegisteredObjectButHaveTheirOwnContexts()
        {
            var service = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(service);
            orgAdminUIService.Create(new Contact { Description = ContactDescription + "_Nested" });

            Assert.Equal(2, service.Contexts.Count);
            Assert.Equal(1, service.Contexts[0].Depth);
            Assert.Equal(2, service.Contexts[1].Depth);
            Assert.NotSame(service.Contexts[0], service.Contexts[1]);
            Assert.Contains("Custom service plugin depth: 2", crm.TraceLog);
        }

        [Fact]
        public void ExistingExecutionProvidersObserveRegistrationChanges()
        {
            var provider = GetCore(crm).CreateServiceProviderAndFactory(new PluginContext());
            var service = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(service);
            Assert.Same(service, provider.GetService(typeof(IServiceEndpointNotificationService)));
            crm.RemoveService<IServiceEndpointNotificationService>();
            Assert.Null(provider.GetService(typeof(IServiceEndpointNotificationService)));
            crm.AddService<IServiceEndpointNotificationService>(service);
            crm.ResetServices();
            Assert.Null(provider.GetService(typeof(IServiceEndpointNotificationService)));
        }

        private class CustomTracingFactory : ITracingServiceFactory
        {
            public ITracingService Service { get; } = new TracingService();
            public int Calls { get; private set; }
            public ITracingService GetService()
            {
                Calls++;
                return Service;
            }
        }

        [Fact]
        public void ExecutionProviderUsesSuppliedTracingFactoryWithCustomServices()
        {
            var factory = new CustomTracingFactory();
            var provider = new MockupServiceProviderAndFactory(GetCore(crm), new PluginContext(), factory);
            var service = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(service);
            Assert.Equal(1, factory.Calls);
            Assert.Same(factory.Service, provider.GetService(typeof(ITracingService)));
            Assert.Same(service, provider.GetService(typeof(IServiceEndpointNotificationService)));
            crm.ResetServices();
            Assert.Same(factory.Service, provider.GetService(typeof(ITracingService)));
        }

        [Theory]
        [InlineData(typeof(IPluginExecutionContext))]
        [InlineData(typeof(IPluginExecutionContext2))]
        [InlineData(typeof(IPluginExecutionContext3))]
        [InlineData(typeof(IPluginExecutionContext4))]
        [InlineData(typeof(IPluginExecutionContext5))]
        [InlineData(typeof(IPluginExecutionContext6))]
        [InlineData(typeof(IPluginExecutionContext7))]
        [InlineData(typeof(ITracingService))]
        [InlineData(typeof(IOrganizationServiceFactory))]
        [InlineData(typeof(IManagedIdentityService))]
        public void BuiltInServicesCannotBeReplacedAndSurviveRemoveAndClear(Type serviceType)
        {
            var context = new PluginContext();
            var provider = GetCore(crm).CreateServiceProviderAndFactory(context);
            var builtIn = provider.GetService(serviceType);
            Assert.NotNull(builtIn);
            if (typeof(IPluginExecutionContext).IsAssignableFrom(serviceType)) Assert.Same(context, builtIn);

            var add = typeof(XrmMockupBase).GetMethod(nameof(XrmMockupBase.AddService)).MakeGenericMethod(serviceType);
            var exception = Assert.Throws<TargetInvocationException>(() => add.Invoke(crm, new[] { builtIn }));
            Assert.IsType<ArgumentException>(exception.InnerException);
            typeof(XrmMockupBase).GetMethod(nameof(XrmMockupBase.RemoveService)).MakeGenericMethod(serviceType).Invoke(crm, null);
            crm.ResetServices();
            Assert.NotNull(provider.GetService(serviceType));
            if (serviceType != typeof(IManagedIdentityService)) Assert.Same(builtIn, provider.GetService(serviceType));
        }
    }
}
