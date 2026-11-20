using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System;
using System.ServiceModel;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    // Late-bound tests for the CloseIncident request handler; the Incident/IncidentResolution
    // metadata is supplied by RemovedEntitiesMetadata.xml (the entities are not in the
    // environment, so early-bound types are unavailable).
    public class TestIncident : UnitTestBase
    {
        public TestIncident(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCloseIncidentRequestSuccess()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);

            orgAdminUIService.Update(incident);
            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            var response = orgAdminUIService.Execute(request) as CloseIncidentResponse;
            Assert.NotNull(response);

            var retrievedIncident = orgAdminUIService.Retrieve("incident", incident.Id, new ColumnSet(true));
            Assert.Equal(1, retrievedIncident.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(5, retrievedIncident.GetAttributeValue<OptionSetValue>("statuscode").Value);

            var resolutionQuery = new QueryExpression("incidentresolution")
            {
                ColumnSet = new ColumnSet(true),
                Criteria =
                {
                    Conditions = { new ConditionExpression("incidentid", ConditionOperator.Equal, incident.Id) }
                }
            };
            var retrievedIncidentResolution = orgAdminUIService.RetrieveMultiple(resolutionQuery).Entities.FirstOrDefault();
            Assert.NotNull(retrievedIncidentResolution);
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenPreviouslyResolved()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);
            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            var response = orgAdminUIService.Execute(request) as CloseIncidentResponse;
            Assert.NotNull(response);

            var retrievedIncident = orgAdminUIService.Retrieve("incident", incident.Id, new ColumnSet(true));
            Assert.Equal(1, retrievedIncident.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(5, retrievedIncident.GetAttributeValue<OptionSetValue>("statuscode").Value);

            var resolutionQuery = new QueryExpression("incidentresolution")
            {
                ColumnSet = new ColumnSet(true),
                Criteria =
                {
                    Conditions = { new ConditionExpression("incidentid", ConditionOperator.Equal, incident.Id) }
                }
            };
            var retrievedIncidentResolution = orgAdminUIService.RetrieveMultiple(resolutionQuery).Entities.FirstOrDefault();
            Assert.NotNull(retrievedIncidentResolution);

            var incidentResolution2 = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request2 = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution2,
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request2);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenIncidentResolutionMissing()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var request = new CloseIncidentRequest()
            {
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenLogicalNameMissing()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity();
            incidentResolution.Attributes["incidentid"] = incident.ToEntityReference();

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenLogicalNameDoesNotExist()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity
            {
                LogicalName = "invalidlogicalname",
                Id = Guid.NewGuid()
            };

            incidentResolution.Attributes["incidentid"] = incident.ToEntityReference();

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenLogicalNameWrong()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Account
            {
                Id = Guid.NewGuid()
            };
            incidentResolution.Attributes["incidentid"] = incident.ToEntityReference();

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenStatusMissing()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenStatusDoesNotExist()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(12345678)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenStateOfStatusWrong()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(1)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenIncidentidMissing()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity("incidentresolution")
            {
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenIncidentDoesNotExist()
        {
            var incident = new Entity("incident");

            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(1000)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenIncidentResolutionAlreadyExists()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(0);
            incident["statuscode"] = new OptionSetValue(1);
            orgAdminUIService.Update(incident);

            var incidentResolution = new Entity("incidentresolution")
            {
                ["incidentid"] = incident.ToEntityReference(),
                ["subject"] = "Resolved Incident"
            };

            incidentResolution.Id = orgAdminUIService.Create(incidentResolution);

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };

            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestUpdateResolvedIncidentFailsWhenFieldModificationIsNotAllowed()
        {
            var incident = new Entity("incident")
            {
                ["title"] = "Old Title"
            };
            incident.Id = orgAdminService.Create(incident);

            var resolution = new Entity("incidentresolution")
            {
                ["subject"] = "Case closed",
                ["incidentid"] = incident.ToEntityReference()
            };

            var closeRequest = new CloseIncidentRequest()
            {
                IncidentResolution = resolution,
                Status = new OptionSetValue(5)
            };
            orgAdminService.Execute(closeRequest);

            var incidentUpdate = new Entity("incident", incident.Id)
            {
                ["title"] = "New Title"
            };

            try
            {
                orgAdminService.Update(incidentUpdate);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }

            var retrievedIncident = orgAdminService.Retrieve("incident", incident.Id, new ColumnSet(true));
            Assert.Equal(incident["title"], retrievedIncident.GetAttributeValue<string>("title"));
        }

        [Fact]
        public void TestUpdateResolvedIncidentSucceedsWhenFieldModificationIsAllowed()
        {
            var incident = new Entity("incident")
            {
                ["title"] = "Old Title",
            };
            incident.Id = orgAdminService.Create(incident);

            var resolution = new Entity("incidentresolution")
            {
                ["subject"] = "Case closed",
                ["incidentid"] = incident.ToEntityReference()
            };

            var closeRequest = new CloseIncidentRequest()
            {
                IncidentResolution = resolution,
                Status = new OptionSetValue(5)
            };
            orgAdminService.Execute(closeRequest);

            var incidentUpdate = new Entity("incident", incident.Id)
            {
                ["statecode"] = new OptionSetValue(0)
            };

            orgAdminService.Update(incidentUpdate);

            var retrievedIncident = orgAdminService.Retrieve("incident", incident.Id, new ColumnSet(true));
            Assert.Equal(0, retrievedIncident.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void TestRemovalOfResolutionsAfterClose()
        {
            var incident = new Entity("incident")
            {
                ["title"] = "TestRemovalOfResolutionsAfterClose"
            };
            incident.Id = orgAdminUIService.Create(incident);

            var incidentResolution = new Entity("incidentresolution")
            {
                ["subject"] = "Resolved Sample Incident",
                ["incidentid"] = incident.ToEntityReference()
            };
            var closeIncidentRequest = new CloseIncidentRequest
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(5)
            };
            orgAdminUIService.Execute(closeIncidentRequest);

            var resolutionQuery = new QueryExpression("incidentresolution")
            {
                ColumnSet = new ColumnSet(true),
                Criteria =
                {
                    Conditions = { new ConditionExpression("incidentid", ConditionOperator.Equal, incident.Id) }
                }
            };
            var retrievedIncidentResolutions = orgAdminUIService.RetrieveMultiple(resolutionQuery).Entities;
            Assert.Empty(retrievedIncidentResolutions);
        }

        [Fact]
        public void TestUpdateIncidentAsResolvedFails()
        {
            var incident = new Entity("incident");
            incident.Id = orgAdminUIService.Create(incident);

            incident["statecode"] = new OptionSetValue(1);
            incident["statuscode"] = new OptionSetValue(5);

            Assert.Throws<FaultException>(() => orgAdminService.Update(incident));
        }

        [Fact]
        public void TestCanUpdateOpenIncident()
        {
            var incident = new Entity("incident")
            {
                ["description"] = nameof(TestCanUpdateOpenIncident)
            };

            incident.Id = orgAdminUIService.Create(incident);

            incident["description"] = "Updated description";
            orgAdminService.Update(incident);

            var retrievedIncident = orgAdminService.Retrieve("incident", incident.Id, new ColumnSet(true));
            Assert.Equal("Updated description", retrievedIncident.GetAttributeValue<string>("description"));
        }
    }
}
