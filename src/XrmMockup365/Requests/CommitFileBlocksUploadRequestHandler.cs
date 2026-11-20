using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal sealed class CommitFileBlocksUploadRequestHandler : RequestHandler
    {
        internal CommitFileBlocksUploadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "CommitFileBlocksUpload") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CommitFileBlocksUploadRequest>(orgRequest);

            var session = core.FileBlockStore.GetUploadSession(request.FileContinuationToken);
            if (session is null)
                throw new FaultException("Invalid or expired file continuation token.");

            var blockDataList = new List<byte[]>();
            foreach (var blockId in request.BlockList)
            {
                var block = session.Blocks.FirstOrDefault(b => b.BlockId == blockId);
                if (block is null)
                    throw new FaultException($"Block with ID '{blockId}' not found in upload session.");

                blockDataList.Add(block.Data);
            }

            var totalSize = blockDataList.Sum(b => b.Length);
            var fileData = new byte[totalSize];
            var offset = 0;
            foreach (var blockData in blockDataList)
            {
                Buffer.BlockCopy(blockData, 0, fileData, offset, blockData.Length);
                offset += blockData.Length;
            }

            var committedFile = new CommittedFile
            {
                FileAttachmentId = session.FileAttachmentId,
                FileName = session.FileName,
                MimeType = request.MimeType,
                FileSize = fileData.Length,
                Data = fileData,
                Target = session.Target,
                FileAttributeName = session.FileAttributeName
            };

            core.FileBlockStore.CommitUpload(request.FileContinuationToken, committedFile);

            var resp = new CommitFileBlocksUploadResponse();
            resp.Results["FileId"] = session.FileAttachmentId;
            resp.Results["FileSizeInBytes"] = (long)fileData.Length;
            return resp;
        }
    }
}
