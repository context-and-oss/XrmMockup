using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal sealed class DownloadBlockRequestHandler : RequestHandler
    {
        internal DownloadBlockRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "DownloadBlock") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<DownloadBlockRequest>(orgRequest);

            var committedFile = core.FileBlockStore.GetDownloadSession(request.FileContinuationToken);
            if (committedFile is null)
                throw new FaultException("Invalid or expired file continuation token.");

            var offset = (int)request.Offset;
            var blockLength = (int)request.BlockLength;

            if (offset < 0 || offset >= committedFile.Data.Length)
                throw new FaultException($"Invalid offset: {offset}. File size is {committedFile.Data.Length} bytes.");

            var availableBytes = committedFile.Data.Length - offset;
            var actualLength = Math.Min(blockLength, availableBytes);

            var data = new byte[actualLength];
            Buffer.BlockCopy(committedFile.Data, offset, data, 0, actualLength);

            var resp = new DownloadBlockResponse();
            resp.Results["Data"] = data;
            return resp;
        }
    }
}
