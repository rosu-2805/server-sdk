using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Morph.Server.Sdk.Client
{
    public interface IMorphServerApiClient
    {
        event EventHandler<FileEventArgs> FileProgress;

        Task<SpaceBrowsingInfo> BrowseSpaceAsync(string spaceName, string folderPath, CancellationToken cancellationToken);
        Task DeleteFileAsync(string spaceName, string serverFolder, string fileName, CancellationToken cancellationToken);
        Task DownloadFileAsync(string spaceName, string remoteFilePath, Func<DownloadFileInfo, bool> handleFile, Stream streamToWriteTo, CancellationToken cancellationToken);
        Task<DownloadFileInfo> DownloadFileAsync(string spaceName, string remoteFilePath, Stream streamToWriteTo, CancellationToken cancellationToken);
        Task<bool> FileExistsAsync(string spaceName, string serverFolder, string fileName, CancellationToken cancellationToken);
        Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken);
        Task<Model.TaskStatus> GetTaskStatusAsync(string spaceName, Guid taskId, CancellationToken cancellationToken);
        Task<RunningTaskStatus> StartTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskBaseParameter> taskParameters = null);
        Task StopTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken);
        Task UploadFileAsync(string spaceName, Stream inputStream, string fileName, long fileSize, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false);
        Task UploadFileAsync(string spaceName, string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false);
        Task<ValidateTasksResult> ValidateTasksAsync(string spaceName, string projectPath, CancellationToken cancellationToken);
    }
}