using System;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestFileOperations : UnitTestBase, IClassFixture<XrmMockupFixture>
    {
        public TestFileOperations(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSingleBlockFileUploadAndDownload()
        {
            // Arrange
            var account = new Account { Name = "Test Account" };
            account.Id = orgAdminService.Create(account);

            var fileName = "test.txt";
            var fileAttributeName = "dg_testfile";
            var fileContent = "Hello, World!";
            var fileData = Encoding.UTF8.GetBytes(fileContent);
            var mimeType = "text/plain";

            // Act - Initialize upload
            var initUploadRequest = new InitializeFileBlocksUploadRequest
            {
                Target = account.ToEntityReference(),
                FileName = fileName,
                FileAttributeName = fileAttributeName
            };
            var initUploadResponse = (InitializeFileBlocksUploadResponse)orgAdminService.Execute(initUploadRequest);

            // Assert - FileContinuationToken is not null
            var uploadToken = initUploadResponse.FileContinuationToken;
            Assert.NotNull(uploadToken);

            // Act - Upload single block
            var blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var uploadBlockRequest = new UploadBlockRequest
            {
                FileContinuationToken = uploadToken,
                BlockId = blockId,
                BlockData = fileData
            };
            orgAdminService.Execute(uploadBlockRequest);

            // Act - Commit upload
            var commitRequest = new CommitFileBlocksUploadRequest
            {
                FileContinuationToken = uploadToken,
                BlockList = new string[] { blockId },
                MimeType = mimeType
            };
            var commitResponse = (CommitFileBlocksUploadResponse)orgAdminService.Execute(commitRequest);

            // Assert - FileId and FileSizeInBytes
            Assert.NotEqual(Guid.Empty, commitResponse.FileId);
            Assert.Equal(fileData.Length, commitResponse.FileSizeInBytes);

            // Act - Initialize download
            var initDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = account.ToEntityReference(),
                FileAttributeName = fileAttributeName
            };
            var initDownloadResponse = (InitializeFileBlocksDownloadResponse)orgAdminService.Execute(initDownloadRequest);

            // Assert - FileName and FileSizeInBytes match
            Assert.Equal(fileName, initDownloadResponse.FileName);
            Assert.Equal(fileData.Length, initDownloadResponse.FileSizeInBytes);

            // Act - Download full file
            var downloadToken = initDownloadResponse.FileContinuationToken;
            var downloadBlockRequest = new DownloadBlockRequest
            {
                FileContinuationToken = downloadToken,
                Offset = 0,
                BlockLength = initDownloadResponse.FileSizeInBytes
            };
            var downloadBlockResponse = (DownloadBlockResponse)orgAdminService.Execute(downloadBlockRequest);

            // Assert - Downloaded data matches original
            Assert.Equal(fileData, downloadBlockResponse.Data);
            var downloadedContent = Encoding.UTF8.GetString(downloadBlockResponse.Data);
            Assert.Equal(fileContent, downloadedContent);
        }

        [Fact]
        public void TestMultiBlockFileUpload()
        {
            // Arrange
            var account = new Account { Name = "Test Account Multi Block" };
            account.Id = orgAdminService.Create(account);

            var fileName = "multiblock.bin";
            var fileAttributeName = "dg_testfile";
            var mimeType = "application/octet-stream";

            // Three blocks of data
            var block1Data = new byte[] { 1, 2, 3 };
            var block2Data = new byte[] { 4, 5, 6 };
            var block3Data = new byte[] { 7, 8, 9 };

            // Act - Initialize upload
            var initUploadRequest = new InitializeFileBlocksUploadRequest
            {
                Target = account.ToEntityReference(),
                FileName = fileName,
                FileAttributeName = fileAttributeName
            };
            var initUploadResponse = (InitializeFileBlocksUploadResponse)orgAdminService.Execute(initUploadRequest);
            var uploadToken = initUploadResponse.FileContinuationToken;

            // Upload three blocks
            var blockId1 = Convert.ToBase64String(BitConverter.GetBytes(1));
            var blockId2 = Convert.ToBase64String(BitConverter.GetBytes(2));
            var blockId3 = Convert.ToBase64String(BitConverter.GetBytes(3));

            orgAdminService.Execute(new UploadBlockRequest
            {
                FileContinuationToken = uploadToken,
                BlockId = blockId1,
                BlockData = block1Data
            });

            orgAdminService.Execute(new UploadBlockRequest
            {
                FileContinuationToken = uploadToken,
                BlockId = blockId2,
                BlockData = block2Data
            });

            orgAdminService.Execute(new UploadBlockRequest
            {
                FileContinuationToken = uploadToken,
                BlockId = blockId3,
                BlockData = block3Data
            });

            // Commit with blocks in correct order
            var commitRequest = new CommitFileBlocksUploadRequest
            {
                FileContinuationToken = uploadToken,
                BlockList = new string[] { blockId1, blockId2, blockId3 },
                MimeType = mimeType
            };
            var commitResponse = (CommitFileBlocksUploadResponse)orgAdminService.Execute(commitRequest);

            // Assert - FileSizeInBytes equals 9
            Assert.Equal(9, commitResponse.FileSizeInBytes);

            // Download and verify assembled data
            var initDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = account.ToEntityReference(),
                FileAttributeName = fileAttributeName
            };
            var initDownloadResponse = (InitializeFileBlocksDownloadResponse)orgAdminService.Execute(initDownloadRequest);

            var downloadBlockRequest = new DownloadBlockRequest
            {
                FileContinuationToken = initDownloadResponse.FileContinuationToken,
                Offset = 0,
                BlockLength = initDownloadResponse.FileSizeInBytes
            };
            var downloadBlockResponse = (DownloadBlockResponse)orgAdminService.Execute(downloadBlockRequest);

            // Assert - Downloaded data is {1,2,3,4,5,6,7,8,9}
            var expectedData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.Equal(expectedData, downloadBlockResponse.Data);
        }

        [Fact]
        public void TestMultiBlockDownloadWithOffset()
        {
            // Arrange - Upload a file with known data
            var account = new Account { Name = "Test Account Offset Download" };
            account.Id = orgAdminService.Create(account);

            var fileName = "offset.bin";
            var fileAttributeName = "dg_testfile";
            var mimeType = "application/octet-stream";
            var fileData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Upload the file
            var initUploadRequest = new InitializeFileBlocksUploadRequest
            {
                Target = account.ToEntityReference(),
                FileName = fileName,
                FileAttributeName = fileAttributeName
            };
            var initUploadResponse = (InitializeFileBlocksUploadResponse)orgAdminService.Execute(initUploadRequest);

            var blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            orgAdminService.Execute(new UploadBlockRequest
            {
                FileContinuationToken = initUploadResponse.FileContinuationToken,
                BlockId = blockId,
                BlockData = fileData
            });

            orgAdminService.Execute(new CommitFileBlocksUploadRequest
            {
                FileContinuationToken = initUploadResponse.FileContinuationToken,
                BlockList = new string[] { blockId },
                MimeType = mimeType
            });

            // Act - Initialize download
            var initDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = account.ToEntityReference(),
                FileAttributeName = fileAttributeName
            };
            var initDownloadResponse = (InitializeFileBlocksDownloadResponse)orgAdminService.Execute(initDownloadRequest);
            var downloadToken = initDownloadResponse.FileContinuationToken;

            var half = (int)(initDownloadResponse.FileSizeInBytes / 2);

            // Download first half
            var downloadFirstHalfRequest = new DownloadBlockRequest
            {
                FileContinuationToken = downloadToken,
                Offset = 0,
                BlockLength = half
            };
            var firstHalfResponse = (DownloadBlockResponse)orgAdminService.Execute(downloadFirstHalfRequest);

            // Download second half
            var downloadSecondHalfRequest = new DownloadBlockRequest
            {
                FileContinuationToken = downloadToken,
                Offset = half,
                BlockLength = half
            };
            var secondHalfResponse = (DownloadBlockResponse)orgAdminService.Execute(downloadSecondHalfRequest);

            // Assert - Both parts combine to original data
            var combinedData = firstHalfResponse.Data.Concat(secondHalfResponse.Data).ToArray();
            Assert.Equal(fileData, combinedData);
        }

        [Fact]
        public void TestUploadBlockExceedingMaxChunkSizeThrows()
        {
            // Arrange
            var account = new Account { Name = "Test Account Chunk Size" };
            account.Id = orgAdminService.Create(account);

            var initUploadRequest = new InitializeFileBlocksUploadRequest
            {
                Target = account.ToEntityReference(),
                FileName = "large.bin",
                FileAttributeName = "dg_testfile"
            };
            var initUploadResponse = (InitializeFileBlocksUploadResponse)orgAdminService.Execute(initUploadRequest);
            var uploadToken = initUploadResponse.FileContinuationToken;

            // Create a block larger than 4 MB (5 MB)
            var oversizedBlock = new byte[5 * 1024 * 1024];

            var uploadBlockRequest = new UploadBlockRequest
            {
                FileContinuationToken = uploadToken,
                BlockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                BlockData = oversizedBlock
            };

            // Act & Assert
            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(uploadBlockRequest));
            Assert.Contains("Maximum chunk size supported: 4 MB", exception.Message);
        }

        [Fact]
        public void TestInvalidUploadTokenThrows()
        {
            // Arrange
            var invalidToken = Guid.NewGuid().ToString();

            // Act & Assert
            var uploadBlockRequest = new UploadBlockRequest
            {
                FileContinuationToken = invalidToken,
                BlockId = "block1",
                BlockData = new byte[] { 1, 2, 3 }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(uploadBlockRequest));
            Assert.Contains("Invalid or expired file continuation token", exception.Message);
        }

        [Fact]
        public void TestInvalidDownloadTokenThrows()
        {
            // Arrange
            var invalidToken = Guid.NewGuid().ToString();

            // Act & Assert
            var downloadBlockRequest = new DownloadBlockRequest
            {
                FileContinuationToken = invalidToken,
                Offset = 0,
                BlockLength = 100
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(downloadBlockRequest));
            Assert.Contains("Invalid or expired file continuation token", exception.Message);
        }

        [Fact]
        public void TestDownloadNonExistentFileThrows()
        {
            // Arrange - Create account but don't upload any file
            var account = new Account { Name = "Test Account No File" };
            account.Id = orgAdminService.Create(account);

            // Act & Assert
            var initDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = account.ToEntityReference(),
                FileAttributeName = "dg_testfile"
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(initDownloadRequest));
            Assert.Contains("No file attachment found", exception.Message);
        }
    }
}
