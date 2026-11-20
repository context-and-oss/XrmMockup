using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal sealed class InitializeFileBlocksDownloadRequestHandler : RequestHandler
    {
        internal InitializeFileBlocksDownloadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "InitializeFileBlocksDownload") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InitializeFileBlocksDownloadRequest>(orgRequest);

            var committedFile = core.FileBlockStore.FindCommittedFile(request.Target, request.FileAttributeName);
            if (committedFile is null)
                throw new FaultException($"No file attachment found for target entity '{request.Target.LogicalName}' with ID '{request.Target.Id}' and attribute '{request.FileAttributeName}'.");

            var token = Guid.NewGuid().ToString();
            core.FileBlockStore.StartDownload(token, committedFile);

            var resp = new InitializeFileBlocksDownloadResponse();
            resp.Results["FileContinuationToken"] = token;
            resp.Results["FileSizeInBytes"] = committedFile.FileSize;
            resp.Results["FileName"] = committedFile.FileName;
            return resp;
        }
    }
}
