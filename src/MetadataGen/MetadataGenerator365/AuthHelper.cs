using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Configuration;
using System.Net;
using System.IO;
using Microsoft.Xrm.Tooling.Connector;

namespace DG.Tools.XrmMockup.Metadata
{

    internal enum ConnectionType
    {
        Proxy,
        OAuth,
        ClientSecret,
        ConnectionString
    }

    internal class AuthHelper
    {
        private string username = ConfigurationManager.AppSettings["username"];
        private string password = ConfigurationManager.AppSettings["password"];
        private string domain = ConfigurationManager.AppSettings["domain"];
        private AuthenticationProviderType provider = ToProviderType(ConfigurationManager.AppSettings["ap"]);
        private string url = ConfigurationManager.AppSettings["url"];
        private ConnectionType method;
        private string clientId;
        private string returnUrl;
        private string clientSecret;
        private string connectionString;

        public AuthHelper(
            string url,
            string username,
            string password,
            AuthenticationProviderType provider = AuthenticationProviderType.OnlineFederation,
            string domain = null,
            ConnectionType method = ConnectionType.Proxy,
            string clientId = null,
            string returnUrl = null,
            string clientSecret = null,
            string connectionString = null)
        {
            this.url = url;
            this.username = username;
            this.password = password;
            this.provider = provider;
            this.domain = domain;
            this.method = method;
            this.clientId = clientId;
            this.returnUrl = returnUrl;
            this.clientSecret = clientSecret;
            this.connectionString = connectionString;
        }

        public AuthHelper(
            string url,
            string username,
            string password,
            string provider = "OnlineFederation",
            string domain = null,
            string method = "Proxy",
            string clientId = null,
            string returnUrl = null,
            string clientSecret = null,
            string connectionString = null)
            : this(url, username, password, ToProviderType(provider), domain, ToConnectionType(method), clientId, returnUrl, clientSecret, connectionString)
        {
        }

        private static AuthenticationProviderType ToProviderType(string ap)
        {
            if (String.IsNullOrWhiteSpace(ap)) return AuthenticationProviderType.OnlineFederation;
            if (Enum.TryParse(ap, out AuthenticationProviderType parsedAp)) return parsedAp;
            throw new Exception($"Could not interpret AuthenticationProviderType argument given: '{ap}'");
        }

        private static ConnectionType ToConnectionType(string method)
        {
            if (String.IsNullOrWhiteSpace(method)) return ConnectionType.Proxy;
            if (Enum.TryParse(method, out ConnectionType parsedMethod)) return parsedMethod;
            throw new Exception($"Could not interpret ConnectionType argument given: '{method}'");
        }

        internal OrganizationServiceProxy GetOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            AuthenticationCredentials authCredentials)
        {
            var osp = serviceManagement.AuthenticationType == AuthenticationProviderType.ActiveDirectory
                ? new OrganizationServiceProxy(serviceManagement, authCredentials.ClientCredentials)
                : new OrganizationServiceProxy(serviceManagement, authCredentials.SecurityTokenResponse);
            osp.Timeout = new TimeSpan(0, 59, 0);
            return osp;
        }

        internal AuthenticationCredentials GetCredentials()
        {
            var ac = new AuthenticationCredentials();
            switch (provider)
            {
                case AuthenticationProviderType.ActiveDirectory:
                    ac.ClientCredentials.Windows.ClientCredential = new NetworkCredential(username, password, domain);
                    break;
                case AuthenticationProviderType.OnlineFederation:
                case AuthenticationProviderType.Federation:
                    ac.ClientCredentials.UserName.UserName = username;
                    ac.ClientCredentials.UserName.Password = password;
                    break;

                default:
                    throw new NotImplementedException("No valid authentification provider was used.");
            }
            return ac;
        }

        internal IOrganizationService Authenticate()
        {
            switch (this.method)
            {
                case ConnectionType.OAuth:
                    {
                        if (this.username == null || this.password == null || this.clientId == null || this.returnUrl == null)
                        {
                            throw new Exception("Not all required information was entered for connection type OAuth");
                        }

                        Utilities.GetOrgnameAndOnlineRegionFromServiceUri(new Uri(this.url), out string region, out string orgName, out bool isOnPrem);
                        var cacheFileLocation = System.IO.Path.Combine(System.IO.Path.GetTempPath(), orgName, "oauth-cache.txt");
                        var client = new CrmServiceClient(this.username, CrmServiceClient.MakeSecureString(this.password), region, orgName, false, null, null,
                            this.clientId, new Uri(this.returnUrl), cacheFileLocation, null);

                        if (!client.IsReady)
                        {
                            throw new Exception($"Client could not authenticate. If the application user was just created, it might take a while before it is available.\n{client.LastCrmError}");
                        }
                        return client;
                    }

                case ConnectionType.ClientSecret:
                    {
                        if (this.clientId == null || this.clientSecret == null)
                        {
                            throw new Exception("Not all required information was entered for connection type ClientSecret");
                        }

                        var client = new CrmServiceClient(new Uri(this.url), this.clientId, CrmServiceClient.MakeSecureString(this.clientSecret), true,
                            Path.Combine(Path.GetTempPath(), this.clientId, "oauth-cache.txt"));

                        if (!client.IsReady)
                        {
                            throw new Exception($"Client could not authenticate. If the application user was just created, it might take a while before it is available.\n{client.LastCrmError}");
                        }
                        return client;
                    }

                case ConnectionType.ConnectionString:
                    {
                        if (this.connectionString == null)
                        {
                            throw new Exception("Ensure connection string is specified when using connection method ConnectionString");
                        }

                        var client = new CrmServiceClient(this.connectionString);

                        if (!client.IsReady)
                        {
                            throw new Exception($"Client could not authenticate. If the application user was just created, it might take a while before it is available.\n{client.LastCrmError}");
                        }

                        return client;
                    }

                case ConnectionType.Proxy:
                default:
                    var m = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(new Uri(url));
                    var ac = m.Authenticate(GetCredentials());
                    var proxy = GetOrganizationServiceProxy(m, ac);
                    proxy.ServiceConfiguration.CurrentServiceEndpoint.EndpointBehaviors.Add(new ProxyTypesBehavior());
                    return proxy;
            }
        }

    }
}
