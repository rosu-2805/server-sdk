using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Dto.Commands;
using Morph.Server.Sdk.Mappers;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Morph.Server.Sdk.Client
{



    internal interface ILowLevelApiClient
    {
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
        Task<ApiResult<NoContentResult>> WebFilesDeleteFileAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken);

    }


    public interface IWebFilesLowLevelApiClient
    {


        Task DownloadFileAsync(ApiSession apiSession, string remoteFilePath, Func<DownloadFileInfo, bool> handleFile, Stream streamToWriteTo, CancellationToken cancellationToken);
        Task<DownloadFileInfo> DownloadFileAsync(ApiSession apiSession, string remoteFilePath, Stream streamToWriteTo, CancellationToken cancellationToken);



        Task UploadFileAsync(ApiSession apiSession, Stream inputStream, string fileName, long fileSize, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false);
        Task UploadFileAsync(ApiSession apiSession, string localFilePath, string destFolderPath, string destFileName, CancellationToken cancellationToken, bool overwriteFileifExists = false);
        Task UploadFileAsync(ApiSession apiSession, string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false);


    }



    internal class LowLevelApiClient : ILowLevelApiClient
    {
        private readonly IApiClient apiClient;

        public LowLevelApiClient(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public Task<ApiResult<NoContentResult>> AuthLogoutAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var url = "auth/logout";
            return apiClient.PostAsync<NoContentRequest, NoContentResult>(url, null, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<RunningTaskStatusDto>> GetRunningTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "runningtasks", taskId.ToString("D"));
            return apiClient.GetAsync<RunningTaskStatusDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<SpacesEnumerationDto>> SpacesGetListAsync(CancellationToken cancellationToken)
        {
            var url = "spaces/list";
            return apiClient.GetAsync<SpacesEnumerationDto>(url, null, new HeadersCollection(), cancellationToken);

        }

        public Task<ApiResult<SpaceTaskDto>> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var url = UrlHelper.JoinUrl("space", apiSession.SpaceName, "tasks", taskId.ToString("D"));
            return apiClient.GetAsync<SpaceTaskDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<SpaceTasksListDto>> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var url = UrlHelper.JoinUrl("space", apiSession.SpaceName, "tasks");
            return apiClient.GetAsync<SpaceTasksListDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }


        public Task<ApiResult<TaskStatusDto>> GetTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "tasks", taskId.ToString("D"));
            return apiClient.GetAsync<TaskStatusDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);

        }

        public Task<ApiResult<ServerStatusDto>> ServerGetStatusAsync(CancellationToken cancellationToken)
        {
            var url = "server/status";
            return apiClient.GetAsync<ServerStatusDto>(url, null, new HeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<RunningTaskStatusDto>> StartTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskParameterBase> taskParameters = null)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "runningtasks", taskId.ToString("D"), "payload");
            var dto = new TaskStartRequestDto();
            if (taskParameters != null)
            {
                dto.TaskParameters = taskParameters.Select(TaskParameterMapper.ToDto).ToList();
            }

            return apiClient.PostAsync<TaskStartRequestDto, RunningTaskStatusDto>(url, dto, null, apiSession.ToHeadersCollection(), cancellationToken);

        }

        public Task<ApiResult<NoContentResult>> StopTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "runningtasks", taskId.ToString("D"));
            return apiClient.DeleteAsync<NoContentResult>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<ValidateTasksResponseDto>> ValidateTasksAsync(ApiSession apiSession, ValidateTasksRequestDto validateTasksRequestDto, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = "commands/validatetasks";

            return apiClient.PostAsync<ValidateTasksRequestDto, ValidateTasksResponseDto>(url, validateTasksRequestDto, null, apiSession.ToHeadersCollection(), cancellationToken);

        }

        public Task<ApiResult<SpaceStatusDto>> SpacesGetSpaceStatusAsync(ApiSession apiSession, string spaceName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (spaceName == null)
            {
                throw new ArgumentNullException(nameof(spaceName));
            }

            spaceName = spaceName.Trim();
            var url = UrlHelper.JoinUrl("spaces", spaceName, "status");

            return apiClient.GetAsync<SpaceStatusDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);

        }

        public Task<ApiResult<SpaceBrowsingResponseDto>> WebFilesBrowseSpaceAsync(ApiSession apiSession, string folderPath, CancellationToken cancellationToken)
        {
            var spaceName = apiSession.SpaceName;

            var url = UrlHelper.JoinUrl("space", spaceName, "browse", folderPath);
            return apiClient.GetAsync<SpaceBrowsingResponseDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<NoContentResult>> WebFilesDeleteFileAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder, fileName);

            return apiClient.DeleteAsync<NoContentResult>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<LoginResponseDto>> AuthLoginPasswordAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken)
        {
            var url = "auth/login";
            return apiClient.PostAsync<LoginRequestDto, LoginResponseDto>(url, loginRequestDto, null, new HeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<GenerateNonceResponseDto>> AuthGenerateNonce(CancellationToken cancellationToken)
        {
            var url = "auth/nonce";
            return apiClient.PostAsync<GenerateNonceRequestDto, GenerateNonceResponseDto>(url, new GenerateNonceRequestDto(), null, new HeadersCollection(), cancellationToken);
        }

    }
}



