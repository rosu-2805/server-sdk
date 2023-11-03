using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.Commands;
using Morph.Server.Sdk.Model.SharedMemory;

namespace Morph.Server.Sdk.Client
{
    public interface IHasConfig
    {
        IClientConfiguration Config { get; }
    }

    public interface ICanCloseSession : IHasConfig, IDisposable
    {
        Task CloseSessionAsync(ApiSession apiSession, CancellationToken cancellationToken);
    }

    public interface IMorphServerApiClient : IHasConfig, IDisposable
    {
        HttpSecurityState HttpSecurityState { get; }
        event EventHandler<FileTransferProgressEventArgs> OnDataDownloadProgress;
        event EventHandler<FileTransferProgressEventArgs> OnDataUploadProgress;


        Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken);

        Task<ApiSession> OpenSessionAsync(OpenSessionRequest openSessionRequest, CancellationToken cancellationToken);

        /*COMPUTATIONS*/
        Task<ComputationDetailedItem> StartTaskAsync(ApiSession apiSession, StartTaskRequest startTaskRequest,
            CancellationToken cancellationToken);

        Task<ComputationDetailedItem> GetComputationDetailsAsync(ApiSession apiSession, string computationId,
            CancellationToken cancellationToken);

        Task CancelComputationAsync(ApiSession apiSession, string computationId, CancellationToken cancellationToken);

        Task<WorkflowResultDetails> GetWorkflowResultDetailsAsync(ApiSession apiSession, string resultToken,
            CancellationToken cancellationToken);

        Task AcknowledgeWorkflowResultAsync(ApiSession apiSession, string resultToken,
            CancellationToken cancellationToken);


        Task<SpaceTask> TaskChangeModeAsync(ApiSession apiSession, Guid taskId,
            TaskChangeModeRequest taskChangeModeRequest, CancellationToken cancellationToken);

        Task<ValidateTasksResult> ValidateTasksAsync(ApiSession apiSession, string projectPath,
            CancellationToken cancellationToken);

        Task<SpacesEnumerationList> GetSpacesListAsync(CancellationToken cancellationToken);
        Task<SpacesLookupResponse> SpacesLookupAsync(SpacesLookupRequest request, CancellationToken cancellationToken);

        Task<SpaceStatus> GetSpaceStatusAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<SpaceTasksList> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<SpaceTask> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);

        Task<SpaceBrowsingInfo> SpaceBrowseAsync(ApiSession apiSession, string folderPath,
            CancellationToken cancellationToken);

        Task<SpaceFilesQuickSearchResponse> SpaceFilesQuickSearchAsync(ApiSession apiSession,
            SpaceFilesQuickSearchRequest request,
            CancellationToken cancellationToken,
            int? offset = null, int? limit = null);

        Task SpaceDeleteFileAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken);

        Task SpaceDeleteFolderAsync(ApiSession apiSession, string serverFolderPath, bool failIfNotExists,
            CancellationToken cancellationToken);

        Task SpaceCreateFolderAsync(ApiSession apiSession, string parentFolderPath,
            string folderName, bool failIfExists, CancellationToken cancellationToken);

        Task SpaceRenameFileAsync(ApiSession apiSession, string parentFolderPath, string oldFileName,
            string newFileName,
            CancellationToken cancellationToken);

        Task SpaceRenameFolderAsync(ApiSession apiSession, string parentFolderPath, string oldFolderName,
            string newFolderName,
            bool failIfExists, CancellationToken cancellationToken);

        Task<bool> SpaceFileExistsAsync(ApiSession apiSession, string remoteFilePath,
            CancellationToken cancellationToken);

        Task<ServerStreamingData> SpaceOpenStreamingDataAsync(ApiSession apiSession, string remoteFilePath,
            CancellationToken cancellationToken);

        Task<Stream> SpaceOpenDataStreamAsync(ApiSession apiSession, string remoteFilePath,
            CancellationToken cancellationToken);

        Task SpaceUploadDataStreamAsync(ApiSession apiSession, SpaceUploadDataStreamRequest spaceUploadFileRequest,
            CancellationToken cancellationToken);

        Task SpaceUploadPushDataStreamAsync(ApiSession apiSession,
            SpaceUploadContiniousStreamRequest continuousStreamRequest, PushStreamCallback pushStreamCallback,
            CancellationToken cancellationToken);


        [Obsolete("Obsolete due to flaw in response checking. Use SpaceUploadPushDataStreamAsync instead.")]
        Task<ContiniousStreamingConnection> SpaceUploadContiniousStreamingAsync(ApiSession apiSession,
            SpaceUploadContiniousStreamRequest continiousStreamRequest, CancellationToken cancellationToken);

        /// <summary>
        ///     Set a value for a shared memory item
        /// </summary>
        /// <param name="apiSession"></param>
        /// <param name="key">Key</param>
        /// <param name="value">Value to set</param>
        /// <param name="overwriteBehavior">What to do if key already exists</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Resulting value</returns>
        Task<SharedMemoryValue> SharedMemoryRemember(ApiSession apiSession, string key, SharedMemoryValue value,
            OverwriteBehavior overwriteBehavior, CancellationToken token);

        /// <summary>
        ///     Get value of a shared memory item
        /// </summary>
        /// <param name="apiSession"></param>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Value</returns>
        Task<SharedMemoryValue> SharedMemoryRecall(ApiSession apiSession, string key, CancellationToken token);

        /// <summary>
        ///     List shared memory values
        /// </summary>
        /// <param name="apiSession"></param>
        /// <param name="startsWith">Key prefix</param>
        /// <param name="offset">Offset</param>
        /// <param name="limit">Limit</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>List of values</returns>
        Task<SharedMemoryListResponse> SharedMemoryList(ApiSession apiSession, string startsWith, int offset,
            int limit,
            CancellationToken token);

        /// <summary>
        ///     Delete a shared memory item
        /// </summary>
        /// <param name="apiSession"></param>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Number of deleted items</returns>
        Task<int> SharedMemoryForget(ApiSession apiSession, string key, CancellationToken token);
    }
}