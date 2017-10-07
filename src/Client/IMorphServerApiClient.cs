using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.Commands;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Morph.Server.Sdk.Client
{
    public interface IMorphServerApiClient
    {

        
        Task DownloadFileAsync(string spaceName, string remoteFilePath, Func<DownloadFileInfo, bool> handleFile, Stream streamToWriteTo, CancellationToken cancellationToken);

        Task<Model.TaskStatus> GetTaskStatusAsync(Guid taskId, CancellationToken cancellationToken);
        Task<DownloadFileInfo> DownloadFileAsync(string spaceName, string remoteFilePath, Stream streamToWriteTo, CancellationToken cancellationToken);
        
        //Task<RunningTaskStatus> GetRunningTaskStatusAsync(string spaceName, Guid taskId, CancellationToken cancellationToken);
        
        Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken);
        
        Task<RunningTaskStatus> StartTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken);

        Task StopTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken);
        Task DeleteFileAsync(string spaceName, string serverFolder, string fileName, CancellationToken cancellationToken);
        
        Task<bool> FileExistsAsync(string spaceName, string serverFolder, string fileName, CancellationToken cancellationToken);

        
        Task<SpaceBrowsingInfo> BrowseSpaceAsync(string spaceName, string folderPath, CancellationToken cancellationToken);
        
        Task UploadFileAsync(string spaceName, Stream inputStream, string fileName, long fileSize, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false);
        
        Task UploadFileAsync(string spaceName, string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false);


        
        Task<ValidateTasksResult> ValidateTasksAsync(string spaceName, string projectPath, CancellationToken cancellationToken);

        


        event EventHandler<FileEventArgs> FileProgress;
    }
}