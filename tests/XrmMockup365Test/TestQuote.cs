using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Xunit;
using Xunit.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;

namespace DG.XrmMockupTest
{
    // Late-bound tests for the Win/Close/Revise quote request handlers; Quote/QuoteClose/QuoteDetail
    // metadata is supplied by RemovedEntitiesMetadata.xml (entities not in the environment).
    public class TestQuote : UnitTestBase
    {
        public TestQuote(XrmMockupFixture fixture) : base(fixture) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRetrieveCalculatedDecimalMarginFromMoneyFields(bool retrieveMultiple)
        {
            // The supplemental quote metadata defines the classic decimal calculation
            // ((msdyn_totalamount - msdyn_estimatedcost) / msdyn_totalamount) * 100.
            // Seed the Money operands directly to isolate the decimal calculation from
            // the separate calculation of msdyn_totalamount (which is not requested here).
            var quote = new Entity("quote")
            {
                ["msdyn_totalamount"] = new Money(150m),
                ["msdyn_estimatedcost"] = new Money(112.5m),
            };
            crm.PopulateWith(quote);

            Entity retrieved;
            if (retrieveMultiple)
            {
                var query = new QueryExpression("quote")
                {
                    ColumnSet = new ColumnSet("msdyn_estimatedquotemargin"),
                };
                query.Criteria.AddCondition("quoteid", ConditionOperator.Equal, quote.Id);
                retrieved = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);
            }
            else
            {
                retrieved = orgAdminService.Retrieve("quote", quote.Id,
                    new ColumnSet("msdyn_estimatedquotemargin"));
            }

            Assert.Equal(25m, Assert.IsType<decimal>(retrieved["msdyn_estimatedquotemargin"]));
        }

        [Fact]
        public void TestWinQuoteUsingStandardStatus()
        {
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);

            var winReq = new WinQuoteRequest()
            {
                QuoteClose = new Entity("quoteclose")
                {
                    ["quoteid"] = quote.ToEntityReference(),
                    ["subject"] = "Quote won"
                },
                Status = new OptionSetValue(-1) // default to Won
            };

            orgAdminUIService.Execute(winReq);

            var retrieved = orgAdminUIService.Retrieve("quote", quote.Id, new ColumnSet(true));

            Assert.Equal(2, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(4, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void TestWinQuote()
        {
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);

            var winReq = new WinQuoteRequest()
            {
                QuoteClose = new Entity("quoteclose")
                {
                    ["quoteid"] = quote.ToEntityReference(),
                    ["subject"] = "Quote won"
                },
                Status = new OptionSetValue(4)
            };

            orgAdminUIService.Execute(winReq);

            var retrieved = orgAdminUIService.Retrieve("quote", quote.Id, new ColumnSet(true));

            Assert.Equal(2, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(4, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void TestCloseQuoteUsingStandardStatus()
        {
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);

            var winReq = new CloseQuoteRequest()
            {
                QuoteClose = new Entity("quoteclose")
                {
                    ["quoteid"] = quote.ToEntityReference(),
                    ["subject"] = "Quote closed"
                },
                Status = new OptionSetValue(-1) // default to Lost
            };

            orgAdminUIService.Execute(winReq);

            var retrieved = orgAdminUIService.Retrieve("quote", quote.Id, new ColumnSet(true));

            Assert.Equal(3, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(5, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void TestCloseQuote()
        {
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);

            var winReq = new CloseQuoteRequest()
            {
                QuoteClose = new Entity("quoteclose")
                {
                    ["quoteid"] = quote.ToEntityReference(),
                    ["subject"] = "Quote closed"
                },
                Status = new OptionSetValue(5)
            };

            orgAdminUIService.Execute(winReq);

            var retrieved = orgAdminUIService.Retrieve("quote", quote.Id, new ColumnSet(true));

            Assert.Equal(3, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(5, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void ReviseDraftQuote_Fails()
        {
            // Arrange
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);

            var reviseReq = new ReviseQuoteRequest()
            {
                QuoteId = quote.Id,
                ColumnSet = new ColumnSet(true)
            };

            // Act & Assert
            var ex = Assert.Throws<MockupException>(() => orgAdminUIService.Execute(reviseReq));
            Assert.Equal("Only quotes in the closed state can be revised.", ex.Message);
        }

        [Fact]
        public void ReviseActiveQuote_Fails()
        {
            // Arrange
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);
            orgAdminUIService.Execute(new SetStateRequest
            {
                EntityMoniker = quote.ToEntityReference(),
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            });

            var reviseReq = new ReviseQuoteRequest()
            {
                QuoteId = quote.Id,
                ColumnSet = new ColumnSet(true)
            };

            // Act & Assert
            var ex = Assert.Throws<MockupException>(() => orgAdminUIService.Execute(reviseReq));
            Assert.Equal("Only quotes in the closed state can be revised.", ex.Message);
        }

        [Fact]
        public void ReviseWonQuote_Fails()
        {
            // Arrange
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);
            orgAdminUIService.Execute(new SetStateRequest
            {
                EntityMoniker = quote.ToEntityReference(),
                State = new OptionSetValue(2),
                Status = new OptionSetValue(4)
            });

            var reviseReq = new ReviseQuoteRequest()
            {
                QuoteId = quote.Id,
                ColumnSet = new ColumnSet(true)
            };

            // Act & Assert
            var ex = Assert.Throws<MockupException>(() => orgAdminUIService.Execute(reviseReq));
            Assert.Equal("Only quotes in the closed state can be revised.", ex.Message);
        }

        [Fact]
        public void ReviseClosedQuote()
        {
            // Arrange
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);
            orgAdminUIService.Execute(new SetStateRequest
            {
                EntityMoniker = quote.ToEntityReference(),
                State = new OptionSetValue(3),
                Status = new OptionSetValue(7)
            });

            var reviseReq = new ReviseQuoteRequest()
            {
                QuoteId = quote.Id,
                ColumnSet = new ColumnSet(true)
            };

            // Act
            var resp = (ReviseQuoteResponse)orgAdminUIService.Execute(reviseReq);

            // Assert
            Assert.NotNull(resp.Entity);
            var revisedQuote = orgAdminUIService.Retrieve("quote", resp.Entity.Id, new ColumnSet(true));
            Assert.Equal(1, revisedQuote.GetAttributeValue<int>("revisionnumber"));
            Assert.Equal(0, revisedQuote.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(1, revisedQuote.GetAttributeValue<OptionSetValue>("statuscode").Value); // InProgress_2 (draft default)
        }

        [Fact]
        public void ReviseClosedWithQuoteDetailsQuote()
        {
            // Arrange
            var quote = new Entity("quote");
            quote.Id = orgAdminUIService.Create(quote);
            orgAdminUIService.Execute(new SetStateRequest
            {
                EntityMoniker = quote.ToEntityReference(),
                State = new OptionSetValue(3),
                Status = new OptionSetValue(7)
            });
            var quoteLine = new Entity("quotedetail") { ["quoteid"] = quote.ToEntityReference() };
            quoteLine.Id = orgAdminUIService.Create(quoteLine);

            var reviseReq = new ReviseQuoteRequest()
            {
                QuoteId = quote.Id,
                ColumnSet = new ColumnSet(true)
            };

            // Act
            var resp = (ReviseQuoteResponse)orgAdminUIService.Execute(reviseReq);

            // Assert
            Assert.NotNull(resp.Entity);
            var revisedQuote = orgAdminUIService.Retrieve("quote", resp.Entity.Id, new ColumnSet(true));
            Assert.Equal(1, revisedQuote.GetAttributeValue<int>("revisionnumber"));
            Assert.Equal(0, revisedQuote.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(1, revisedQuote.GetAttributeValue<OptionSetValue>("statuscode").Value); // InProgress_2 (draft default)
            var query = new QueryExpression("quotedetail");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition("quoteid", ConditionOperator.Equal, revisedQuote.Id);
            var relatedQuoteLines = orgAdminUIService.RetrieveMultiple(query).Entities;
            Assert.Single(relatedQuoteLines);
        }
    }
}
