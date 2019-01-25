using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Exceptions;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Morph.Server.Sdk.Events;
using System.Collections.Specialized;
using Morph.Server.Sdk.Dto.Commands;
using Morph.Server.Sdk.Model.Errors;
using Morph.Server.Sdk.Mappers;
using Morph.Server.Sdk.Model.Commands;
using System.Linq;
using Morph.Server.Sdk.Dto.Errors;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Morph.Server.Sdk.Client
{


    public static class MorphServerApiClientConfig
    {
#if NETSTANDARD2_0
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
#endif

        public static string ClientId { get; set; } = string.Empty;

    }



    /// <summary>
    /// Morph Server api client V1
    /// </summary>
    public class MorphServerApiClient : IMorphServerApiClient, IDisposable
    {
        protected readonly Uri _apiHost;
        protected readonly string UserAgent = "MorphServerApiClient/1.3.5";
        
        protected readonly string _api_v1 = "api/v1/";


        //private IApiClient apiClient;
        private ILowLevelApiClient lowLevelApiClient;

        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan FileTransferTimeout { get; set; } = TimeSpan.FromHours(3);
#if NETSTANDARD2_0
        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
#endif

        /// <summary>
        /// Construct Api client
        /// </summary>
        /// <param name="apiHost">Server url</param>
        public MorphServerApiClient(string apiHost)
        {
            if (!apiHost.EndsWith("/"))
                apiHost += "/";
            _apiHost = new Uri(apiHost);
            
            
#if NETSTANDARD2_0
                // handler will be disposed automatically
                HttpClientHandler aHandler = new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Automatic,
                    ServerCertificateCustomValidationCallback = this.ServerCertificateCustomValidationCallback

                };
#elif NET45
                 // handler will be disposed automatically
                HttpClientHandler aHandler = new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Automatic
                    
                };
#endif

            var httpClient = ConstructHttpClient(_apiHost, aHandler);
            var restClient = ConstructRestApiClient(httpClient);
            this.lowLevelApiClient = new LowLevelApiClient(restClient);

        }

        public event EventHandler<FileEventArgs> FileProgress;


        private IApiClient ConstructRestApiClient(HttpClient httpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return new MorphServerRestClient(httpClient);
        }


        protected HttpClient ConstructHttpClient(Uri apiHost, HttpClientHandler httpClientHandler)
        {
            
            if (httpClientHandler == null)
            {
                throw new ArgumentNullException(nameof(httpClientHandler));
            }

            var client = new HttpClient(httpClientHandler, true);
            client.BaseAddress = new Uri(apiHost, new Uri(_api_v1, UriKind.Relative));

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
                {
                    CharSet = "utf-8"
                });
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("X-Client-Type", "EMS-CMD");

            client.MaxResponseContentBufferSize = 100 * 1024;
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };



            client.Timeout = TimeSpan.FromHours(24);

            return client;
        }





        /// <summary>
        /// Start Task like "fire and forget"
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId">tast guid</param>
        /// <param name="cancellationToken"></param>
        /// <param name="taskParameters"></param>
        /// <returns></returns>
        public Task<RunningTaskStatus> StartTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskParameterBase> taskParameters = null)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.StartTaskAsync(apiSession, taskId, token);
                return MapOrFail(apiResult, (dto) => RunningTaskStatusMapper.RunningTaskStatusFromDto(dto));

            }, cancellationToken, OperationTimeout);
        }

        protected Task<TResult> Wrapped<TResult>(Func<CancellationToken, Task<TResult>> fun, CancellationToken orginalCancellationToken, TimeSpan maxExecutionTime)
        {
            using (var derTokenSource = CancellationTokenSource.CreateLinkedTokenSource(orginalCancellationToken))
            {
                derTokenSource.CancelAfter(maxExecutionTime);
                try
                {
                    return fun(derTokenSource.Token);
                }

                catch (OperationCanceledException) when (!orginalCancellationToken.IsCancellationRequested && derTokenSource.IsCancellationRequested)
                {
                    throw new Exception($"Can't connect to host {_apiHost}.  Operation timeout ({maxExecutionTime})");
                }

            }
        }


        protected void FailIfError<TDto>(ApiResult<TDto> apiResult)
        {
            if (!apiResult.IsSucceed)
            {
                throw apiResult.Error;
            }
        }



        protected TDataModel MapOrFail<TDto, TDataModel>(ApiResult<TDto> apiResult, Func<TDto, TDataModel> maper)
        {
            if (apiResult.IsSucceed)
            {
                return maper(apiResult.Data);
            }
            else
            {
                throw apiResult.Error;
            }
        }


        /// <summary>
        /// Close opened session
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task CloseSessionAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            if (apiSession.IsClosed)
                return Task.FromResult(0);
            if (apiSession.IsAnonymous)
                return Task.FromResult(0);

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.AuthLogoutAsync(apiSession, token);
                // if task fail - do nothing. server will close this session after inactivity period
                return Task.FromResult(0);

            }, cancellationToken, OperationTimeout);

        }


        /// <summary>
        /// Gets status of the task (Running/Not running) and payload
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId">task guid</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        private Task<RunningTaskStatus> GetRunningTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.GetRunningTaskStatusAsync(apiSession, taskId, token);
                return MapOrFail(apiResult, (dto) => RunningTaskStatusMapper.RunningTaskStatusFromDto(dto));

            }, cancellationToken, OperationTimeout);

        }


        /// <summary>
        /// Gets status of the task
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId">task guid</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        public Task<Model.TaskStatus> GetTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.GetTaskStatusAsync(apiSession, taskId, token);
                return MapOrFail(apiResult, (dto) => TaskStatusMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);

        }


        /// <summary>
        /// Retrieves space status
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<SpaceStatus> GetSpaceStatusAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.SpacesGetSpaceStatusAsync(apiSession, apiSession.SpaceName, token);
                return MapOrFail(apiResult, (dto) => SpaceStatusMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);

        }



        /// <summary>
        /// Stops the Task
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId"></param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public async Task StopTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            await Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.StopTaskAsync(apiSession, taskId, token);
                FailIfError(apiResult);
                return Task.FromResult(0);

            }, cancellationToken, OperationTimeout);

        }

        /// <summary>
        /// Returns server status. May raise exception if server is unreachable
        /// </summary>
        /// <returns></returns>
        public Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.ServerGetStatusAsync(token);
                return MapOrFail(apiResult, (dto) => ServerStatusMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);
        }

        public Task<SpacesEnumerationList> GetSpacesListAsync(CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.SpacesGetListAsync(token);
                return MapOrFail(apiResult, (dto) => SpacesEnumerationMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);
        }

        private void DownloadProgress_StateChanged(object sender, FileEventArgs e)
        {
            if (FileProgress != null)
            {
                FileProgress(this, e);
            }

        }





        /// <summary>
        /// Prerforms browsing the Space
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="folderPath">folder path like /path/to/folder</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<SpaceBrowsingInfo> BrowseSpaceAsync(ApiSession apiSession, string folderPath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.WebFilesBrowseSpaceAsync(apiSession, folderPath, token);
                return MapOrFail(apiResult, (dto) => SpaceBrowsingMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);
        }


        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolder">server folder like /path/to/folder</param>
        /// <param name="fileName">file name </param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns true if file exists.</returns>
        public Task<bool> FileExistsAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(nameof(fileName));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.WebFilesBrowseSpaceAsync(apiSession, serverFolder, token);
                var browseResult = MapOrFail(apiResult, (dto) => SpaceBrowsingMapper.MapFromDto(dto));
                return browseResult.FileExists(fileName);

            }, cancellationToken, OperationTimeout);
        }





        /// <summary>
        /// Performs file deletion
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolder">Path to server folder like /path/to/folder</param>
        /// <param name="fileName">file name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task DeleteFileAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.WebFilesDeleteFileAsync(apiSession, serverFolder, fileName, token);
                FailIfError(apiResult);
                return Task.FromResult(0);

            }, cancellationToken, OperationTimeout);

        }






        /// <summary>
        /// Validate tasks. Checks that there are no missing parameters in the tasks. 
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="projectPath">project path like /path/to/project.morph </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ValidateTasksResult> ValidateTasksAsync(ApiSession apiSession, string projectPath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentException("projectPath is empty", nameof(projectPath));
            }

            return Wrapped(async (token) =>
            {
                var request = new ValidateTasksRequestDto
                {
                    SpaceName = apiSession.SpaceName,
                    ProjectPath = projectPath
                };
                var apiResult = await lowLevelApiClient.ValidateTasksAsync(apiSession, request, token);
                return MapOrFail(apiResult, (dto) => ValidateTasksResponseMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);

        }
        


        /// <summary>
        /// Opens session based on required authentication mechanism
        /// </summary>
        /// <param name="openSessionRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ApiSession> OpenSessionAsync(OpenSessionRequest openSessionRequest, CancellationToken ct)
        {
            if (openSessionRequest == null)
            {
                throw new ArgumentNullException(nameof(openSessionRequest));
            }
            if (string.IsNullOrWhiteSpace(openSessionRequest.SpaceName))
            {
                throw new ArgumentException("Space name is not set.", nameof(openSessionRequest.SpaceName));
            }

            using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                // no more than 20 sec for session opening
                var timeout = TimeSpan.FromSeconds(20);
                linkedTokenSource.CancelAfter(timeout);
                var cancellationToken = linkedTokenSource.Token;
                try
                {
                    var spacesListApiResult = await lowLevelApiClient.SpacesGetListAsync(cancellationToken);
                    var spacesListResult = MapOrFail(spacesListApiResult, (dto) => SpacesEnumerationMapper.MapFromDto(dto));
                                        
                    var desiredSpace = spacesListResult.Items.FirstOrDefault(x => x.SpaceName.Equals(openSessionRequest.SpaceName, StringComparison.OrdinalIgnoreCase));
                    if (desiredSpace == null)
                    {
                        throw new Exception($"Server has no space '{openSessionRequest.SpaceName}'");
                    }
                    var session = await MorphServerAuthenticator.OpenSessionMultiplexedAsync(desiredSpace,
                        new OpenSessionAuthenticatorContext(
                            lowLevelApiClient, 
                            this, 
                            (handler) => ConstructRestApiClient(ConstructHttpClient(_apiHost, handler))),
                        openSessionRequest, cancellationToken);

                    return session;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && linkedTokenSource.IsCancellationRequested)
                {
                    throw new Exception($"Can't connect to host {_apiHost}.  Operation timeout ({timeout})");
                }
            }

        }

      


        public Task<SpaceTasksList> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.GetTasksListAsync(apiSession, token);
                return MapOrFail(apiResult, (dto) => SpaceTasksListsMapper.MapFromDto(dto));

            }, cancellationToken, OperationTimeout);

        }

        public Task<SpaceTask> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.GetTaskAsync(apiSession, taskId, token);
                return MapOrFail(apiResult, (dto) => SpaceTaskMapper.MapFull(dto));

            }, cancellationToken, OperationTimeout);
        }

        public void Dispose()
        {
            if (lowLevelApiClient != null)
            {
                lowLevelApiClient.Dispose();
                lowLevelApiClient = null;
            }
        }


        public Task<FetchFileStreamData> DownloadFileAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await lowLevelApiClient.WebFilesDownloadFileAsync(apiSession, remoteFilePath, cancellationToken);
                return MapOrFail(apiResult, (data) => data);

            }, cancellationToken, FileTransferTimeout);
        }


        public Task DownloadFileAsync(ApiSession apiSession, string remoteFilePath, Func<DownloadFileInfo, bool> handleFile, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DownloadFileInfo> DownloadFileAsync(ApiSession apiSession, string remoteFilePath, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadFileAsync(ApiSession apiSession, Stream inputStream, string fileName, long fileSize, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            throw new NotImplementedException();
        }

        public Task UploadFileAsync(ApiSession apiSession, string localFilePath, string destFolderPath, string destFileName, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            throw new NotImplementedException();
        }

        public Task UploadFileAsync(ApiSession apiSession, string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            throw new NotImplementedException();
        }
    }
}


