using System;
using System.Collections.Generic;
using System.Text;

#if DATAVERSE_SERVICE_CLIENT
using Microsoft.PowerPlatform.Dataverse.Client;
#else
using Microsoft.Xrm.Sdk;
#endif

namespace DG.Tools.XrmMockup
{
    public abstract partial class XrmMockupBase
    {
#if DATAVERSE_SERVICE_CLIENT
        /// <summary>
        /// Create an async organization service for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync2 CreateOrganizationService(Guid userId)
        {
            return ServiceFactory.CreateOrganizationServiceAsync(userId);
        }

        /// <summary>
        /// Create an async organization service, with the given settings, for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync2 CreateOrganizationService(Guid userId, MockupServiceSettings settings)
        {
            return ServiceFactory.CreateOrganizationService(userId, settings);
        }

        /// <summary>
        /// Gets an async system administrator organization service
        /// </summary>
        /// <returns></returns>
        public IOrganizationServiceAsync2 GetAdminService()
        {
            return ServiceFactory.CreateAdminOrganizationService();
        }

        /// <summary>
        /// Gets an async system administrator organization service, with the given settings
        /// </summary>
        /// <param name="Settings"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync2 GetAdminService(MockupServiceSettings Settings)
        {
            return ServiceFactory.CreateAdminOrganizationService(Settings);
        }
#else
        /// <summary>
        /// Gets a system administrator organization service
        /// </summary>
        /// <returns></returns>
        public IOrganizationService GetAdminService()
        {
            return ServiceFactory.CreateAdminOrganizationService();
        }

        /// <summary>
        /// Gets a system administrator organization service, with the given settings
        /// </summary>
        /// <param name="Settings"></param>
        /// <returns></returns>
        public IOrganizationService GetAdminService(MockupServiceSettings Settings)
        {
            return ServiceFactory.CreateAdminOrganizationService(Settings);
        }

        /// <summary>
        /// Create an organization service for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid userId)
        {
            return ServiceFactory.CreateOrganizationService(userId);
        }

        /// <summary>
        /// Create an organization service, with the given settings, for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid userId, MockupServiceSettings settings)
        {
            return ServiceFactory.CreateOrganizationService(userId, settings);
        }
#endif
    }
}
