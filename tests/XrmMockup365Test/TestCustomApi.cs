using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System.Linq;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestCustomApi : UnitTestBase
    {
        private readonly XrmMockupFixture fixture;

        public TestCustomApi(XrmMockupFixture fixture) : base(fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void TestCreateAccountApi()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_CreateAccount")
            {
                Parameters =
                {
                    { "Name", "TestAccount" },
                }
            });

            using (var context = new Xrm(orgAdminUIService))
            {
                var account = context.AccountSet.FirstOrDefault(x => x.Name == "TestAccount");
                Assert.NotNull(account);
                Assert.Equal(resp.Results["Name"] as string, account.Name);
            }
        }

        [Fact]
        public void TestLegacyCreateAccountApi()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_" + nameof(LegacyCreateAccountApi))
            {
                Parameters =
                {
                    { "Name", "TestAccount" },
                }
            });

            using (var context = new Xrm(orgAdminUIService))
            {
                var account = context.AccountSet.FirstOrDefault(x => x.Name == "TestAccount");
                Assert.NotNull(account);
                Assert.Equal(resp.Results["Name"] as string, account.Name);
            }
        }

        [Fact]
        public void TestUnregisteredOutputParameterIsNotReturned()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "SetUnregisteredOutput", true },
                }
            });

            Assert.True(resp.Results.ContainsKey("InputKeys"));
            Assert.False(resp.Results.ContainsKey("Unregistered"));
        }

        [Fact]
        public void TestUnregisteredRequestParameterIsNotPassedToApi()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "Input", "value" },
                    { "Unregistered", "value" },
                }
            });

            var inputKeys = ((string)resp.Results["InputKeys"]).Split(',');
            Assert.Contains("Input", inputKeys);
            Assert.DoesNotContain("Unregistered", inputKeys);
        }

        [Fact]
        public void TestTagIsPassedAsSharedVariable()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "tag", "my tag" },
                }
            });

            Assert.Equal("my tag", resp.Results["Tag"]);
            Assert.DoesNotContain("tag", ((string)resp.Results["InputKeys"]).Split(','));
        }

        [Fact]
        public void TestStrictOptionalParametersSucceed()
        {
            var service = CreateStrictService();

            var resp = service.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "tag", "my tag" },
                    { "BypassBusinessLogicExecution", "CustomSync" },
                }
            });

            Assert.Equal("my tag", resp.Results["Tag"]);
        }

        [Fact]
        public void TestStrictMessageSpecificOptionalParameterThrows()
        {
            var service = CreateStrictService();

            Assert.Throws<MockupException>(() => service.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "SolutionUniqueName", "MySolution" },
                }
            }));
        }

        [Fact]
        public void TestStrictUnregisteredOutputParameterThrows()
        {
            var service = CreateStrictService();

            var ex = Assert.Throws<MockupException>(() => service.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "SetUnregisteredOutput", true },
                }
            }));
            Assert.Contains("'Unregistered'", ex.Message);
        }

        [Fact]
        public void TestStrictUnregisteredRequestParameterThrows()
        {
            var service = CreateStrictService();

            var ex = Assert.Throws<MockupException>(() => service.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "Input", "value" },
                    { "Unregistered", "value" },
                }
            }));
            Assert.Contains("'Unregistered'", ex.Message);
        }

        [Fact]
        public void TestStrictRegisteredParametersSucceed()
        {
            var service = CreateStrictService();

            var resp = service.Execute(new OrganizationRequest("dg_UnregisteredParametersApi")
            {
                Parameters =
                {
                    { "Input", "value" },
                    { "SetUnregisteredOutput", false },
                }
            });

            var inputKeys = ((string)resp.Results["InputKeys"]).Split(',');
            Assert.Contains("Input", inputKeys);
            Assert.Contains("SetUnregisteredOutput", inputKeys);
        }

        private IOrganizationService CreateStrictService()
        {
            var settings = new XrmMockupSettings
            {
                BasePluginTypes = fixture.Settings.BasePluginTypes,
                BaseCustomApiTypes = fixture.Settings.BaseCustomApiTypes,
                CodeActivityInstanceTypes = fixture.Settings.CodeActivityInstanceTypes,
                EnableProxyTypes = fixture.Settings.EnableProxyTypes,
                IncludeAllWorkflows = fixture.Settings.IncludeAllWorkflows,
                ExceptionFreeRequests = fixture.Settings.ExceptionFreeRequests,
                MetadataDirectoryPath = fixture.Settings.MetadataDirectoryPath,
                IPluginMetadata = fixture.Settings.IPluginMetadata,
                StrictCustomApiParameters = true,
            };
            return XrmMockup365.GetInstance(settings).GetAdminService();
        }
    }
}
