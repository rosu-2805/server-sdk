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
using Morph.Server.Sdk.Exceptions;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Events;
using System.Net.Http;

namespace Morph.Server.Sdk.Client
{

    internal class LowLevelApiClient : ILowLevelApiClient
    {
        private readonly IRestClient apiClient;

        public IRestClient RestClient => apiClient;

        public LowLevelApiClient(IRestClient apiClient)
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

    

        public Task<ApiResult<SpacesEnumerationDto>> SpacesGetListAsync(CancellationToken cancellationToken)
        {
            var url = "spaces/list";
            return apiClient.GetAsync<SpacesEnumerationDto>(url, null, new HeadersCollection(), cancellationToken);

        }

        public Task<ApiResult<SpacesLookupResponseDto>> SpacesLookupAsync(SpacesLookupRequestDto requestDto, CancellationToken cancellationToken)
        {
            var url = "spaces/list/lookup";
            return apiClient.PostAsync<SpacesLookupRequestDto,SpacesLookupResponseDto>(url, requestDto,null, new HeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<TaskFullDto>> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var url = UrlHelper.JoinUrl("space", apiSession.SpaceName, "tasks", taskId.ToString("D"));
            return apiClient.GetAsync<TaskFullDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<TasksListDto>> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var url = UrlHelper.JoinUrl("space", apiSession.SpaceName, "tasks");
            return apiClient.GetAsync<TasksListDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }


     

        public Task<ApiResult<TaskFullDto>> TaskChangeModeAsync(ApiSession apiSession, Guid taskId, SpaceTaskChangeModeRequestDto requestDto, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "tasks", taskId.ToString("D"), "changeMode");

            return apiClient.PostAsync<SpaceTaskChangeModeRequestDto, TaskFullDto>(url, requestDto, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<ServerStatusDto>> ServerGetStatusAsync(CancellationToken cancellationToken)
        {
            var url = "server/status";
            return apiClient.GetAsync<ServerStatusDto>(url, null, new HeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<ComputationDetailedItemDto>> StartTaskAsync(ApiSession apiSession, TaskStartRequestDto taskStartRequestDto, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "computations", "start", "task");
            
            return apiClient.PostAsync<TaskStartRequestDto, ComputationDetailedItemDto>(url, taskStartRequestDto, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<ComputationDetailedItemDto>> GetComputationDetailsAsync(ApiSession apiSession, string computationId, CancellationToken cancellationToken)
        {
            if (apiSession == null) throw new ArgumentNullException(nameof(apiSession));
            if (computationId == null) throw new ArgumentNullException(nameof(computationId));

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "computations", computationId);
            return apiClient.GetAsync<ComputationDetailedItemDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task CancelComputationAsync(ApiSession apiSession, string computationId, CancellationToken cancellationToken)
        {
            if (apiSession == null) throw new ArgumentNullException(nameof(apiSession));
            if (computationId == null) throw new ArgumentNullException(nameof(computationId));

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "computations", computationId);
            return apiClient.DeleteAsync<NoContentRequest>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<WorkflowResultDetailsDto>> GetWorkflowResultDetailsAsync(ApiSession apiSession, string resultToken, CancellationToken cancellationToken)
        {
            if (apiSession == null) throw new ArgumentNullException(nameof(apiSession));
            if (resultToken == null) throw new ArgumentNullException(nameof(resultToken));

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "workflows-result", resultToken, "details");
            return apiClient.GetAsync<WorkflowResultDetailsDto>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task AcknowledgeWorkflowResultAsync(ApiSession apiSession, string resultToken, CancellationToken cancellationToken)
        {
            if (apiSession == null) throw new ArgumentNullException(nameof(apiSession));
            if (resultToken == null) throw new ArgumentNullException(nameof(resultToken));

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "workflows-result", resultToken, "ack");
            return apiClient.PostAsync<NoContentRequest, NoContentResult>(url, null, null, apiSession.ToHeadersCollection(), cancellationToken);
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

        public Task<ApiResult<NoContentResult>> WebFilesDeleteFileAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken)
        {
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFilePath);

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

        public void Dispose()
        {
            apiClient.Dispose();
        }

        public Task<ApiResult<FetchFileStreamData>> WebFilesDownloadFileAsync(ApiSession apiSession, string serverFilePath, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFilePath);
            return apiClient.RetrieveFileGetAsync(url, null, apiSession.ToHeadersCollection(), onReceiveProgress, cancellationToken);
        }

        public async Task<ApiResult<bool>> WebFileExistsAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFilePath);
            var apiResult = await apiClient.HeadAsync<NoContentResult>(url, null, apiSession.ToHeadersCollection(), cancellationToken);
            //  http ok or http no content means that file exists
            if (apiResult.IsSucceed)
            {
                return ApiResult<bool>.Ok(true, apiResult.ResponseHeaders);
            }
            else
            {
                // if not found, return Ok with false result
                if(apiResult.Error is MorphApiNotFoundException)
                {
                    return ApiResult<bool>.Ok(false, apiResult.ResponseHeaders);
                }
                else
                {
                    // some error occured - return internal error from api result
                    return ApiResult<bool>.Fail(apiResult.Error, apiResult.ResponseHeaders);

                }
            }
        }

        public Task<ApiResult<NoContentResult>> WebFilesPutFileStreamAsync(ApiSession apiSession, string serverFolder, SendFileStreamData sendFileStreamData, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (sendFileStreamData == null)
            {
                throw new ArgumentNullException(nameof(sendFileStreamData));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder);
            
            return apiClient.PutFileStreamAsync<NoContentResult>(url,sendFileStreamData,  null, apiSession.ToHeadersCollection(), onSendProgress, cancellationToken);

        }

        public Task<ApiResult<NoContentResult>> WebFilesPostFileStreamAsync(ApiSession apiSession, string serverFolder, SendFileStreamData sendFileStreamData, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (sendFileStreamData == null)
            {
                throw new ArgumentNullException(nameof(sendFileStreamData));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder);

            return apiClient.PostFileStreamAsync<NoContentResult>(url, sendFileStreamData, null, apiSession.ToHeadersCollection(), onSendProgress, cancellationToken);
        }
        public Task<ApiResult<ServerPushStreaming>> WebFilesOpenContiniousPostStreamAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder);

            return apiClient.PushContiniousStreamingDataAsync<NoContentResult>(HttpMethod.Post, url, new ContiniousStreamingRequest(fileName), null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        public Task<ApiResult<ServerPushStreaming>> WebFilesOpenContiniousPutStreamAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder);

            return apiClient.PushContiniousStreamingDataAsync<NoContentResult>(HttpMethod.Put, url, new ContiniousStreamingRequest(fileName), null, apiSession.ToHeadersCollection(), cancellationToken);
        }

       
    }
}



