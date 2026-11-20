using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal sealed class UploadBlockRequestHandler : RequestHandler
    {
        internal UploadBlockRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "UploadBlock") { }

        private const int MaxChunkSizeBytes = 4 * 1024 * 1024; // 4 MB

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UploadBlockRequest>(orgRequest);

            var session = core.FileBlockStore.GetUploadSession(request.FileContinuationToken);
            if (session is null)
                throw new FaultException("Invalid or expired file continuation token.");

            if (request.BlockData != null && request.BlockData.Length > MaxChunkSizeBytes)
            {
                var actualMB = request.BlockData.Length / (1024.0 * 1024.0);
                throw new FaultException($"Invalid file chunk size: {actualMB:0.##} MB. Maximum chunk size supported: 4 MB.");
            }

            var block = new FileBlock
            {
                BlockId = request.BlockId,
                Data = request.BlockData
            };

            session.Blocks.Add(block);

            var resp = new UploadBlockResponse();
            return resp;
        }
    }
}
