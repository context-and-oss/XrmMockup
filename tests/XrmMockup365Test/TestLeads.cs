using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    // Late-bound tests for the QualifyLead request handler. Lead metadata is supplied by
    // LeadMetadata.xml (the lead entity is not part of the leaned test environment).
    public class TestLeads : UnitTestBase
    {
        // Lead statuscode values (from the generated lead metadata): 3 = Qualified (state 1),
        // 4 = Disqualified/Lost (state 2). 1/2 are Open-state statuses.
        private const int LeadStatusQualified = 3;
        private const int LeadStatusDisqualified = 4;
        private const int StateOpen = 0;
        private const int StateQualified = 1;
        private const int StateDisqualified = 2;

        private readonly XrmMockupFixture _fixture;

        public TestLeads(XrmMockupFixture fixture) : base(fixture) { _fixture = fixture; }

        private Guid CreateLead(Action<Entity> customize = null)
        {
            var lead = new Entity("lead")
            {
                ["subject"] = "Interested in widgets",
                ["companyname"] = "Contoso Ltd",
                ["firstname"] = "Jane",
                ["lastname"] = "Doe",
                ["jobtitle"] = "Buyer",
                ["emailaddress1"] = "jane@contoso.example",
                ["description"] = "A promising lead",
            };
            customize?.Invoke(lead);
            return orgAdminService.Create(lead);
        }

        [Fact]
        public void TestQualifyLeadCreatesAllRelatedRecords()
        {
            var leadId = CreateLead();

            var response = (QualifyLeadResponse)orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateAccount = true,
                CreateContact = true,
                CreateOpportunity = true,
                Status = new OptionSetValue(LeadStatusQualified),
            });

            var created = response.CreatedEntities;
            Assert.Equal(3, created.Count);
            Assert.Contains(created, e => e.LogicalName == "account");
            Assert.Contains(created, e => e.LogicalName == "contact");
            Assert.Contains(created, e => e.LogicalName == "opportunity");

            // Records were actually persisted with attributes mapped from the lead.
            var account = orgAdminService.Retrieve("account", created.First(e => e.LogicalName == "account").Id, new ColumnSet(true));
            Assert.Equal("Contoso Ltd", account.GetAttributeValue<string>("name"));

            var contact = orgAdminService.Retrieve("contact", created.First(e => e.LogicalName == "contact").Id, new ColumnSet(true));
            Assert.Equal("Jane", contact.GetAttributeValue<string>("firstname"));
            Assert.Equal("Doe", contact.GetAttributeValue<string>("lastname"));

            var opportunity = orgAdminService.Retrieve("opportunity", created.First(e => e.LogicalName == "opportunity").Id, new ColumnSet(true));
            Assert.Equal("Interested in widgets", opportunity.GetAttributeValue<string>("name"));
            // opportunity has the originatingleadid attribute, so it links back to the lead.
            Assert.Equal(leadId, opportunity.GetAttributeValue<EntityReference>("originatingleadid")?.Id);
        }

        [Fact]
        public void TestQualifyLeadMarksLeadQualified()
        {
            var leadId = CreateLead();

            orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateAccount = false,
                CreateContact = false,
                CreateOpportunity = false,
                Status = new OptionSetValue(LeadStatusQualified),
            });

            var lead = orgAdminService.Retrieve("lead", leadId, new ColumnSet(true));
            Assert.Equal(StateQualified, lead.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(LeadStatusQualified, lead.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void TestQualifyLeadOnlyCreatesRequestedRecords()
        {
            var leadId = CreateLead();

            var response = (QualifyLeadResponse)orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateAccount = true,
                CreateContact = false,
                CreateOpportunity = false,
                Status = new OptionSetValue(LeadStatusQualified),
            });

            Assert.Single(response.CreatedEntities);
            Assert.Equal("account", response.CreatedEntities.Single().LogicalName);
        }

        [Fact]
        public void TestQualifyLeadDisqualifyDoesNotCreateRecords()
        {
            var leadId = CreateLead();

            var response = (QualifyLeadResponse)orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateAccount = false,
                CreateContact = false,
                CreateOpportunity = false,
                Status = new OptionSetValue(LeadStatusDisqualified),
            });

            Assert.Empty(response.CreatedEntities);
            var lead = orgAdminService.Retrieve("lead", leadId, new ColumnSet("statecode", "statuscode"));
            Assert.Equal(StateDisqualified, lead.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(LeadStatusDisqualified, lead.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void TestQualifyLeadSetsOpportunityCustomerAndCurrency()
        {
            var leadId = CreateLead();
            var accountId = orgAdminService.Create(new Entity("account") { ["name"] = "Existing Customer" });
            var currency = orgAdminService.RetrieveMultiple(new QueryExpression("transactioncurrency") { ColumnSet = new ColumnSet(false), TopCount = 1 })
                .Entities.First().ToEntityReference();

            var response = (QualifyLeadResponse)orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateOpportunity = true,
                OpportunityCustomerId = new EntityReference("account", accountId),
                OpportunityCurrencyId = currency,
                Status = new OptionSetValue(LeadStatusQualified),
            });

            var opportunityRef = response.CreatedEntities.Single(e => e.LogicalName == "opportunity");
            var opportunity = orgAdminService.Retrieve("opportunity", opportunityRef.Id, new ColumnSet(true));
            Assert.Equal(accountId, opportunity.GetAttributeValue<EntityReference>("customerid")?.Id);
            Assert.Equal(currency.Id, opportunity.GetAttributeValue<EntityReference>("transactioncurrencyid")?.Id);
        }

        [Fact]
        public void TestQualifyLeadMissingLeadIdThrows()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                Status = new OptionSetValue(LeadStatusQualified),
            }));
            Assert.Contains("LeadId", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadNonexistentLeadThrows()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", Guid.NewGuid()),
                Status = new OptionSetValue(LeadStatusQualified),
            }));
            Assert.Contains("Does Not Exist", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadMissingStatusThrows()
        {
            var leadId = CreateLead();
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
            }));
            Assert.Contains("Status", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadInvalidStatusThrows()
        {
            var leadId = CreateLead();
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                Status = new OptionSetValue(999999),
            }));
            Assert.Contains("not a valid status code", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadAlreadyClosedThrows()
        {
            var leadId = CreateLead();
            var request = new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                Status = new OptionSetValue(LeadStatusQualified),
            };
            orgAdminService.Execute(request);

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(request));
            Assert.Contains("already closed", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadInvalidCurrencyTypeThrows()
        {
            var leadId = CreateLead();
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateOpportunity = true,
                OpportunityCurrencyId = new EntityReference("account", Guid.NewGuid()),
                Status = new OptionSetValue(LeadStatusQualified),
            }));
            Assert.Contains("transactioncurrency", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadNonexistentCurrencyThrowsFault()
        {
            // A currency that does not exist must surface as a FaultException from the create
            // pipeline, not an unhandled NullReferenceException.
            var leadId = CreateLead();
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateOpportunity = true,
                OpportunityCurrencyId = new EntityReference("transactioncurrency", Guid.NewGuid()),
                Status = new OptionSetValue(LeadStatusQualified),
            }));
            Assert.DoesNotContain("NullReference", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadInvalidCustomerTypeThrows()
        {
            var leadId = CreateLead();
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateOpportunity = true,
                OpportunityCustomerId = new EntityReference("systemuser", Guid.NewGuid()),
                Status = new OptionSetValue(LeadStatusQualified),
            }));
            Assert.Contains("CustomerIdType", ex.Message);
        }

        [Fact]
        public void TestQualifyLeadWarnsWhenTargetLacksAttribute()
        {
            // account/contact do not define originatingleadid in this environment, so the handler
            // must skip it (rather than throw) and emit a warning.
            var capture = new CaptureLoggerFactory();
            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[0],
                MetadataDirectoryPath = _fixture.Settings.MetadataDirectoryPath,
                EnableProxyTypes = false,
                LoggerFactory = capture,
                MinLogLevel = LogLevel.Warning,
            };
            var localCrm = XrmMockup365.GetInstance(settings);
            var service = localCrm.GetAdminService();

            var leadId = service.Create(new Entity("lead") { ["companyname"] = "NoLink Co" });
            var response = (QualifyLeadResponse)service.Execute(new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateAccount = true,
                Status = new OptionSetValue(LeadStatusQualified),
            });

            // The account was still created despite lacking originatingleadid ...
            var accountRef = response.CreatedEntities.Single();
            var account = service.Retrieve("account", accountRef.Id, new ColumnSet(true));
            Assert.False(account.Contains("originatingleadid"));
            // ... and a warning was surfaced.
            Assert.Contains(capture.Warnings, w => w.Contains("originatingleadid") && w.Contains("account"));
        }

        private sealed class CaptureLoggerFactory : ILoggerFactory
        {
            public List<string> Warnings { get; } = new List<string>();
            public void AddProvider(ILoggerProvider provider) { }
            public ILogger CreateLogger(string categoryName) => new CaptureLogger(Warnings);
            public void Dispose() { }

            private sealed class CaptureLogger : ILogger
            {
                private readonly List<string> _warnings;
                public CaptureLogger(List<string> warnings) { _warnings = warnings; }
                public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    if (logLevel == LogLevel.Warning)
                    {
                        lock (_warnings) { _warnings.Add(formatter(state, exception)); }
                    }
                }

                private sealed class NullScope : IDisposable
                {
                    public static NullScope Instance { get; } = new NullScope();
                    public void Dispose() { }
                }
            }
        }
    }
}
