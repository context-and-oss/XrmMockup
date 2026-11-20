using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Database
{
    internal sealed class FileUploadSession
    {
        public Guid FileAttachmentId { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public EntityReference Target { get; set; }
        public string FileAttributeName { get; set; }
        public List<FileBlock> Blocks { get; set; } = new List<FileBlock>();
        public DateTime CreatedOn { get; set; }
    }

    internal sealed class FileBlock
    {
        public string BlockId { get; set; }
        public byte[] Data { get; set; }
    }

    internal sealed class CommittedFile
    {
        public Guid FileAttachmentId { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public byte[] Data { get; set; }
        public EntityReference Target { get; set; }
        public string FileAttributeName { get; set; }
    }

    internal sealed class FileBlockStore
    {
        private readonly ConcurrentDictionary<string, FileUploadSession> pendingUploads = new ConcurrentDictionary<string, FileUploadSession>();
        private readonly ConcurrentDictionary<Guid, CommittedFile> committedFiles = new ConcurrentDictionary<Guid, CommittedFile>();
        private readonly ConcurrentDictionary<string, CommittedFile> downloadSessions = new ConcurrentDictionary<string, CommittedFile>();

        public void StartUpload(string token, FileUploadSession session)
        {
            pendingUploads[token] = session;
        }

        public FileUploadSession GetUploadSession(string token)
        {
            pendingUploads.TryGetValue(token, out var session);
            return session;
        }

        public void CommitUpload(string token, CommittedFile committedFile)
        {
            committedFiles[committedFile.FileAttachmentId] = committedFile;
            pendingUploads.TryRemove(token, out _);
        }

        public CommittedFile GetCommittedFile(Guid fileAttachmentId)
        {
            committedFiles.TryGetValue(fileAttachmentId, out var file);
            return file;
        }

        public void StartDownload(string token, CommittedFile committedFile)
        {
            downloadSessions[token] = committedFile;
        }

        public CommittedFile GetDownloadSession(string token)
        {
            downloadSessions.TryGetValue(token, out var file);
            return file;
        }

        public CommittedFile FindCommittedFile(EntityReference target, string fileAttributeName)
        {
            foreach (var file in committedFiles.Values)
            {
                if (file.Target?.Id == target.Id &&
                    file.Target?.LogicalName == target.LogicalName &&
                    file.FileAttributeName == fileAttributeName)
                {
                    return file;
                }
            }
            return null;
        }

        public void Clear()
        {
            pendingUploads.Clear();
            committedFiles.Clear();
            downloadSessions.Clear();
        }
    }
}
