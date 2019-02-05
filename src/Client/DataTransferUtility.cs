using Morph.Server.Sdk.Model;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// Transfer file from/to server to/from local file
    /// </summary>
    public class DataTransferUtility : IDataTransferUtility
    {
        private int BufferSize { get; set; }  = 81920;
        private readonly IMorphServerApiClient _morphServerApiClient;
        private readonly ApiSession _apiSession;

        public DataTransferUtility(IMorphServerApiClient morphServerApiClient, ApiSession apiSession)
        {
            this._morphServerApiClient = morphServerApiClient ?? throw new ArgumentNullException(nameof(morphServerApiClient));
            this._apiSession = apiSession ?? throw new ArgumentNullException(nameof(apiSession));
        }

        public async Task SpaceUploadFileAsync(string localFilePath, string serverFolder, CancellationToken cancellationToken, bool overwriteExistingFile = false)
        {        
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException(string.Format("File '{0}' not found", localFilePath));
            }
            var fileSize = new FileInfo(localFilePath).Length;
            var fileName = Path.GetFileName(localFilePath);            
            using (var fsSource = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                var request = new SpaceUploadDataStreamRequest
                {
                    DataStream = fsSource,
                    FileName = fileName,
                    FileSize = fileSize,
                    OverwriteExistingFile = overwriteExistingFile,
                    ServerFolder = serverFolder
                };
                await _morphServerApiClient.SpaceUploadDataStreamAsync(_apiSession, request, cancellationToken);
                return;
            }
        }

        


        public async Task SpaceDownloadFileIntoFileAsync(string remoteFilePath, string targetLocalFilePath, CancellationToken cancellationToken, bool overwriteExistingFile = false)
        {
            if (remoteFilePath == null)
            {
                throw new ArgumentNullException(nameof(remoteFilePath));
            }

            if (targetLocalFilePath == null)
            {
                throw new ArgumentNullException(nameof(targetLocalFilePath));
            }

            string destFileName = Path.GetFileName(targetLocalFilePath);
            var localFolder = Path.GetDirectoryName(targetLocalFilePath);
            var tempFile = Path.Combine(localFolder, Guid.NewGuid().ToString("D") + ".emtmp");

            if (!overwriteExistingFile && File.Exists(destFileName))
            {
                throw new Exception($"Destination file '{destFileName}' already exists.");
            }


            try
            {
                using (Stream tempFileStream = File.Open(tempFile, FileMode.Create))
                {
                    using (var serverStreamingData = await _morphServerApiClient.SpaceOpenStreamingDataAsync(_apiSession, remoteFilePath, cancellationToken))
                    {
                        await serverStreamingData.Stream.CopyToAsync(tempFileStream, BufferSize, cancellationToken);
                    }
                }

                if (File.Exists(destFileName))
                {
                    File.Delete(destFileName);
                }
                File.Move(tempFile, destFileName);

            }
            finally
            {
                //drop file
                if (tempFile != null && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

            }
        }

        public async Task SpaceDownloadFileIntoFolderAsync(string remoteFilePath, string targetLocalFolder, CancellationToken cancellationToken, bool overwriteExistingFile = false)
        {
            if (remoteFilePath == null)
            {
                throw new ArgumentNullException(nameof(remoteFilePath));
            }

            if (targetLocalFolder == null)
            {
                throw new ArgumentNullException(nameof(targetLocalFolder));
            }

            string destFileName = null;
            var tempFile = Path.Combine(targetLocalFolder, Guid.NewGuid().ToString("D") + ".emtmp");
            try
            {
                using (Stream tempFileStream = File.Open(tempFile, FileMode.Create))
                {

                    using (var serverStreamingData = await _morphServerApiClient.SpaceOpenStreamingDataAsync(_apiSession, remoteFilePath, cancellationToken))
                    {
                        destFileName = Path.Combine(targetLocalFolder, serverStreamingData.FileName);

                        if (!overwriteExistingFile && File.Exists(destFileName))
                        {
                            throw new Exception($"Destination file '{destFileName}' already exists.");
                        }

                        await serverStreamingData.Stream.CopyToAsync(tempFileStream, BufferSize, cancellationToken);
                    }
                }

                if (File.Exists(destFileName))
                {
                    File.Delete(destFileName);
                }
                File.Move(tempFile, destFileName);

            }
            finally
            {
                //drop file
                if (tempFile != null && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

            }

        }

        public async Task SpaceUploadFileAsync(string localFilePath, string serverFolder, string destFileName, CancellationToken cancellationToken, bool overwriteExistingFile = false)
        {
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException(string.Format("File '{0}' not found", localFilePath));
            }
            var fileSize = new FileInfo(localFilePath).Length;
            
            using (var fsSource = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                var request = new SpaceUploadDataStreamRequest
                {
                    DataStream = fsSource,
                    FileName = destFileName,
                    FileSize = fileSize,
                    OverwriteExistingFile = overwriteExistingFile,
                    ServerFolder = serverFolder
                };
                await _morphServerApiClient.SpaceUploadDataStreamAsync(_apiSession, request, cancellationToken);
                return;
            }
        }
    }

}


