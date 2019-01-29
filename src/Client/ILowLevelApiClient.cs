using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Dto.Commands;
using System.Collections.Generic;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Client
{
    internal interface ILowLevelApiClient: IDisposable
    {
        IRestClient RestClient { get; }

        // TASKS
        Task<ApiResult<TaskStatusDto>> GetTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);
        Task<ApiResult<SpaceTasksListDto>> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<ApiResult<SpaceTaskDto>> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);

        // RUN-STOP Task
        Task<ApiResult<RunningTaskStatusDto>> GetRunningTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);
        Task<ApiResult<RunningTaskStatusDto>> StartTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskParameterBase> taskParameters = null);
        Task<ApiResult<NoContentResult>> StopTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);

        // Tasks validation
        Task<ApiResult<ValidateTasksResponseDto>> ValidateTasksAsync(ApiSession apiSession, ValidateTasksRequestDto validateTasksRequestDto, CancellationToken cancellationToken);


        // Auth and sessions
        Task<ApiResult<NoContentResult>> AuthLogoutAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<ApiResult<LoginResponseDto>> AuthLoginPasswordAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken);
        Task<ApiResult<GenerateNonceResponseDto>> AuthGenerateNonce(CancellationToken cancellationToken);
        


        // Server interaction
        Task<ApiResult<ServerStatusDto>> ServerGetStatusAsync(CancellationToken cancellationToken);


        // spaces

        Task<ApiResult<SpacesEnumerationDto>> SpacesGetListAsync(CancellationToken cancellationToken);
        Task<ApiResult<SpaceStatusDto>> SpacesGetSpaceStatusAsync(ApiSession apiSession, string spaceName, CancellationToken cancellationToken);

        // WEB FILES
        Task<ApiResult<SpaceBrowsingResponseDto>> WebFilesBrowseSpaceAsync(ApiSession apiSession, string folderPath, CancellationToken cancellationToken);
        Task<ApiResult<bool>> WebFileExistsAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken);
        Task<ApiResult<NoContentResult>> WebFilesDeleteFileAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken);
        Task<ApiResult<FetchFileStreamData>> WebFilesDownloadFileAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken);
        Task<ApiResult<NoContentResult>> WebFilesPutFileAsync(ApiSession apiSession, string serverFolder, SendFileStreamData sendFileStreamData, CancellationToken cancellationToken);
        Task<ApiResult<NoContentResult>> WebFilesPostFileAsync(ApiSession apiSession, string serverFolder, SendFileStreamData sendFileStreamData, CancellationToken cancellationToken);

    }
}



