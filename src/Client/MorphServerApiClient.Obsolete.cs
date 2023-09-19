using System;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// Morph Server api client V1. This part of the client is obsolete and will be removed in future versions.
    /// </summary>
    public partial class MorphServerApiClient
    {
        [Obsolete(
            "Obsolete due to flaw in response checking. Use SpaceUploadPushDataStreamAsync instead. Will be removed in next major version.")]
        public Task<ContiniousStreamingConnection> SpaceUploadContiniousStreamingAsync(ApiSession apiSession,
            SpaceUploadContiniousStreamRequest continiousStreamRequest, CancellationToken cancellationToken)
        {
            if (apiSession == null)
                throw new ArgumentNullException(nameof(apiSession));

            if (continiousStreamRequest == null)
                throw new ArgumentNullException(nameof(continiousStreamRequest));

            return Wrapped(async (token) =>
            {
                var apiResult =
                    continiousStreamRequest.OverwriteExistingFile
                        ? await _lowLevelApiClient.WebFilesOpenContiniousPutStreamAsync(apiSession,
                            continiousStreamRequest.ServerFolder, continiousStreamRequest.FileName, token)
                        : await _lowLevelApiClient.WebFilesOpenContiniousPostStreamAsync(apiSession,
                            continiousStreamRequest.ServerFolder, continiousStreamRequest.FileName, token);

                var connection = MapOrFail(apiResult, c => c);
                return new ContiniousStreamingConnection(connection);
            }, cancellationToken, OperationType.FileTransfer);
        }
    }
}