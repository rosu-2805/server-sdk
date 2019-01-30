using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.Commands;

namespace Morph.Server.Sdk.Client
{
    
    public interface IMorphServerApiClient:IDisposable
    {
        event EventHandler<FileTransferProgressEventArgs> OnFileDownloadProgress;
        event EventHandler<FileTransferProgressEventArgs> OnFileUploadProgress;

        IClientConfiguration Config { get; }

        Task CloseSessionAsync(ApiSession apiSession, CancellationToken cancellationToken);
        
        Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken);
        Task<Model.TaskStatus> GetTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);        
        Task<ApiSession> OpenSessionAsync(OpenSessionRequest openSessionRequest, CancellationToken cancellationToken);
        
        Task<RunningTaskStatus> StartTaskAsync(ApiSession apiSession, StartTaskRequest startTaskRequest, CancellationToken cancellationToken);
        Task StopTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);
        
        Task<ValidateTasksResult> ValidateTasksAsync(ApiSession apiSession, string projectPath, CancellationToken cancellationToken);
        Task<SpacesEnumerationList> GetSpacesListAsync(CancellationToken cancellationToken);
        Task<SpaceStatus> GetSpaceStatusAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<SpaceTasksList> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<SpaceTask> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);

        Task<SpaceBrowsingInfo> SpaceBrowseAsync(ApiSession apiSession, string folderPath, CancellationToken cancellationToken);
        Task SpaceDeleteFileAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken);
        Task<bool> SpaceFileExistsAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken);

        Task<ServerStreamingData> SpaceDownloadFileAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken);
        Task<Stream> SpaceDownloadFileStreamAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken);

        Task SpaceUploadFileAsync(ApiSession apiSession, SpaceUploadFileRequest spaceUploadFileRequest, CancellationToken cancellationToken);


    }
}