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
        private readonly IMorphServerApiClient _morphServerApiClient;
        private readonly ApiSession _apiSession;

        public DataTransferUtility(IMorphServerApiClient morphServerApiClient, ApiSession apiSession)
        {
            this._morphServerApiClient = morphServerApiClient ?? throw new ArgumentNullException(nameof(morphServerApiClient));
            this._apiSession = apiSession ?? throw new ArgumentNullException(nameof(apiSession));
        }

        public async Task SpaceUploadFileAsync(string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteExistingFile = false)
        {        
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException(string.Format("File '{0}' not found", localFilePath));
            }
            var fileSize = new FileInfo(localFilePath).Length;
            var fileName = Path.GetFileName(localFilePath);            
            using (var fsSource = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                var request = new SpaceUploadFileRequest
                {
                    DataStream = fsSource,
                    FileName = fileName,
                    FileSize = fileSize,
                    OverwriteExistingFile = overwriteExistingFile,
                    ServerFolder = destFolderPath
                };
                await _morphServerApiClient.SpaceUploadFileAsync(_apiSession, request, cancellationToken);
                return;
            }
        }

    }

}


