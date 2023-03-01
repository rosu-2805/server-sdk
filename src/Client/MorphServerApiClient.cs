using Morph.Server.Sdk.Model;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Dto.Commands;
using Morph.Server.Sdk.Mappers;
using Morph.Server.Sdk.Model.Commands;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Dto;
using System.Collections.Concurrent;
using Morph.Server.Sdk.Exceptions;

namespace Morph.Server.Sdk.Client
{


    /// <summary>
    /// Morph Server api client V1
    /// </summary>
    public class MorphServerApiClient : IMorphServerApiClient, IDisposable, ICanCloseSession
    {

        public event EventHandler<FileTransferProgressEventArgs> OnDataDownloadProgress;
        public event EventHandler<FileTransferProgressEventArgs> OnDataUploadProgress;

        protected readonly string _userAgent = "MorphServerApiClient/next";
        protected readonly string _api_v1 = "api/v1/";

        private readonly ILowLevelApiClient _lowLevelApiClient;
        protected readonly IRestClient RestClient;
        private ClientConfiguration clientConfiguration = new ClientConfiguration();

        private bool _disposed = false;
        private object _lock = new object();


        public IClientConfiguration Config => clientConfiguration;

        internal ILowLevelApiClient BuildApiClient(ClientConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

#if NETSTANDARD2_0
            // handler will be disposed automatically
            HttpClientHandler aHandler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                ServerCertificateCustomValidationCallback = configuration.ServerCertificateCustomValidationCallback
            };
#elif NETFRAMEWORK
            // handler will be disposed automatically
            HttpClientHandler aHandler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic
            };
#else
    Not implemented                        
#endif
            var httpClient = BuildHttpClient(configuration, aHandler);
            var restClient = ConstructRestApiClient(httpClient, BuildBaseAddress(configuration), clientConfiguration);
            return new LowLevelApiClient(restClient);
        }



        /// <summary>
        /// Construct Api client
        /// </summary>
        /// <param name="apiHost">Server url</param>
        public MorphServerApiClient(Uri apiHost)
        {
            if (apiHost == null)
            {
                throw new ArgumentNullException(nameof(apiHost));
            }

            var defaultConfig = new ClientConfiguration
            {
                ApiUri = apiHost
            };
            clientConfiguration = defaultConfig;
            _lowLevelApiClient = BuildApiClient(clientConfiguration);
            RestClient = _lowLevelApiClient.RestClient;
        }

        public MorphServerApiClient(ClientConfiguration clientConfiguration)
        {
            this.clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            _lowLevelApiClient = BuildApiClient(clientConfiguration);
            RestClient = _lowLevelApiClient.RestClient;
        }


        protected virtual IRestClient ConstructRestApiClient(HttpClient httpClient, Uri baseAddress, ClientConfiguration clientConfiguration)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return new MorphServerRestClient(httpClient, baseAddress,
                clientConfiguration.SessionRefresher,
                clientConfiguration.HttpSecurityState);
        }


        protected virtual Uri BuildBaseAddress(ClientConfiguration config)
        {
           var baseAddress = new Uri(config.ApiUri, new Uri(_api_v1, UriKind.Relative));
           return baseAddress;

        }


        protected virtual HttpClient BuildHttpClient(ClientConfiguration config, HttpClientHandler httpClientHandler)
        {
            if (httpClientHandler == null)
            {
                throw new ArgumentNullException(nameof(httpClientHandler));
            }

            var client = new HttpClient(httpClientHandler, true);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
                {
                    CharSet = "utf-8"
                });
            client.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            client.DefaultRequestHeaders.Add("X-Client-Type", config.ClientType);
            client.DefaultRequestHeaders.Add("X-Client-Id", config.ClientId);
            client.DefaultRequestHeaders.Add("X-Client-Sdk", config.SDKVersionString);

            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=120");

            
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };



            client.Timeout = config.HttpClientTimeout;

            return client;
        }


        /// <summary>
        /// Start Task like "fire and forget"
        /// </summary>
        public Task<ComputationDetailedItem> StartTaskAsync(ApiSession apiSession, StartTaskRequest startTaskRequest, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (startTaskRequest == null)
            {
                throw new ArgumentNullException(nameof(startTaskRequest));
            }

            return Wrapped(async (token) =>
            {
                var requestDto = new TaskStartRequestDto()
                {
                    TaskId = startTaskRequest.TaskId,
                    TaskParameters = startTaskRequest.TaskParameters?.Select(TaskParameterMapper.ToDto)?.ToList()
                };

                var apiResult = await _lowLevelApiClient.StartTaskAsync(apiSession, requestDto, token);
                return MapOrFail(apiResult, ComputationDetailedItemMapper.FromDto);

            }, cancellationToken, OperationType.ShortOperation);
        }

        public Task<ComputationDetailedItem> GetComputationDetailsAsync(ApiSession apiSession, string computationId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }


            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.GetComputationDetailsAsync(apiSession, computationId, token);
                return MapOrFail(apiResult, ComputationDetailedItemMapper.FromDto);

            }, cancellationToken, OperationType.ShortOperation);
        }

        public Task CancelComputationAsync(ApiSession apiSession, string computationId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }


            return Wrapped(async (token) =>
            {
                await _lowLevelApiClient.CancelComputationAsync(apiSession, computationId, token);
                return Task.FromResult(0);

            }, cancellationToken, OperationType.ShortOperation);
        }

        public Task<WorkflowResultDetails> GetWorkflowResultDetailsAsync(ApiSession apiSession, string resultToken, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }


            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.GetWorkflowResultDetailsAsync(apiSession, resultToken, token);
                return MapOrFail(apiResult, WorkflowResultDetailsMapper.FromDto);

            }, cancellationToken, OperationType.ShortOperation);
        }

        public Task AcknowledgeWorkflowResultAsync(ApiSession apiSession, string resultToken, CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                await _lowLevelApiClient.AcknowledgeWorkflowResultAsync(apiSession, resultToken, token);
                return Task.FromResult(0);

            }, cancellationToken, OperationType.ShortOperation);
        }

        protected virtual async Task<TResult> Wrapped<TResult>(Func<CancellationToken, Task<TResult>> fun, CancellationToken orginalCancellationToken, OperationType operationType)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MorphServerApiClient));
            }


            TimeSpan maxExecutionTime;
            switch (operationType)
            {
                case OperationType.FileTransfer:
                    maxExecutionTime = clientConfiguration.FileTransferTimeout; break;
                case OperationType.ShortOperation:
                    maxExecutionTime = clientConfiguration.OperationTimeout; break;
                case OperationType.SessionOpenAndRelated:
                    maxExecutionTime = clientConfiguration.SessionOpenTimeout; break;
                default: throw new NotImplementedException();
            }


            CancellationTokenSource derTokenSource = null;
            try
            {
                derTokenSource = CancellationTokenSource.CreateLinkedTokenSource(orginalCancellationToken);
                {
                    derTokenSource.CancelAfter(maxExecutionTime);
                    try
                    {
                        return await fun(derTokenSource.Token);
                    }

                    catch (OperationCanceledException) when (!orginalCancellationToken.IsCancellationRequested && derTokenSource.IsCancellationRequested)
                    {
                        if (operationType == OperationType.SessionOpenAndRelated)
                        {
                            throw new Exception($"Can't connect to host {clientConfiguration.ApiUri}.  Operation timeout ({maxExecutionTime})");
                        }
                        else
                        {
                            throw new Exception($"Operation timeout ({maxExecutionTime}) when processing command to host {clientConfiguration.ApiUri}");
                        }
                    }
                }
            }
            finally
            {
                if (derTokenSource != null)
                {
                    if (operationType == OperationType.FileTransfer)
                    {
                        RegisterForDisposing(derTokenSource);
                    }
                    else
                    {
                        derTokenSource.Dispose();
                    }
                }
            }

        }

        private ConcurrentBag<CancellationTokenSource> _ctsForDisposing = new ConcurrentBag<CancellationTokenSource>();

        private void RegisterForDisposing(CancellationTokenSource derTokenSource)
        {
            if (derTokenSource == null)
            {
                throw new ArgumentNullException(nameof(derTokenSource));
            }

            _ctsForDisposing.Add(derTokenSource);
        }

        protected virtual void FailIfError<TDto>(ApiResult<TDto> apiResult)
        {
            if (!apiResult.IsSucceed)
            {
                throw apiResult.Error;
            }
        }



        protected virtual TDataModel MapOrFail<TDto, TDataModel>(ApiResult<TDto> apiResult, Func<TDto, TDataModel> maper)
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
        Task ICanCloseSession.CloseSessionAsync(ApiSession apiSession, CancellationToken cancellationToken)
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
                var apiResult = await _lowLevelApiClient.AuthLogoutAsync(apiSession, token);
                // if task fail - do nothing. server will close this session after inactivity period
                return Task.FromResult(0);

            }, cancellationToken, OperationType.ShortOperation);

        }


      


      
        /// <summary>
        /// Change task mode
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId">task guid</param>
        /// <param name="taskChangeModeRequest"></param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        public Task<SpaceTask> TaskChangeModeAsync(ApiSession apiSession, Guid taskId, TaskChangeModeRequest taskChangeModeRequest, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (taskChangeModeRequest is null)
            {
                throw new ArgumentNullException(nameof(taskChangeModeRequest));
            }

            return Wrapped(async (token) =>
            {
                var request = new SpaceTaskChangeModeRequestDto
                {
                    TaskEnabled = taskChangeModeRequest.TaskEnabled
                };
                var apiResult = await _lowLevelApiClient.TaskChangeModeAsync(apiSession, taskId, request, token);
                return MapOrFail(apiResult, (dto) => SpaceTaskMapper.MapFull(dto));

            }, cancellationToken, OperationType.ShortOperation);

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
                var apiResult = await _lowLevelApiClient.SpacesGetSpaceStatusAsync(apiSession, apiSession.SpaceName, token);
                return MapOrFail(apiResult, (dto) => SpaceStatusMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.ShortOperation);

        }


        public HttpSecurityState HttpSecurityState => RestClient.HttpSecurityState;

        /// <summary>
        /// Returns server status. May raise exception if server is unreachable
        /// </summary>
        /// <returns></returns>
        public Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.ServerGetStatusAsync(token);
                return MapOrFail(apiResult, (dto) => ServerStatusMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.SessionOpenAndRelated);
        }

        public async Task<SpacesEnumerationList> GetSpacesListAsync(CancellationToken cancellationToken)
        {
            return await Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.SpacesGetListAsync(token);
                return MapOrFail(apiResult, (dto) => SpacesEnumerationMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.SessionOpenAndRelated);
        }

        public async Task<SpacesLookupResponse> SpacesLookupAsync(SpacesLookupRequest request, CancellationToken cancellationToken)
        {
            return await Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.SpacesLookupAsync(SpacesLookupMapper.ToDto(request), token);
                return MapOrFail(apiResult, (dto) => SpacesLookupMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.SessionOpenAndRelated);
        }
       

        protected void TriggerOnDataDownloadProgress(FileTransferProgressEventArgs e)
        {
            OnDataDownloadProgress?.Invoke(this, e);
        }

        protected void TriggerOnDataUploadProgress(FileTransferProgressEventArgs e)
        {
            OnDataUploadProgress?.Invoke(this, e);
        }




        /// <summary>
        /// Prerforms browsing the Space
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="folderPath">folder path like /path/to/folder</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<SpaceBrowsingInfo> SpaceBrowseAsync(ApiSession apiSession, string folderPath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.WebFilesBrowseSpaceAsync(apiSession, folderPath, token);
                return MapOrFail(apiResult, (dto) => SpaceBrowsingMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.ShortOperation);
        }


        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolder">server folder like /path/to/folder</param>
        /// <param name="fileName">file name </param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns true if file exists.</returns>
        public Task<bool> SpaceFileExistsAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (string.IsNullOrWhiteSpace(serverFilePath))
            {
                throw new ArgumentException(nameof(serverFilePath));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.WebFileExistsAsync(apiSession, serverFilePath, token);
                return MapOrFail(apiResult, (dto) => dto);
            }, cancellationToken, OperationType.ShortOperation);
        }


        /// <summary>
        /// Performs file deletion
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolder">Path to server folder like /path/to/folder</param>
        /// <param name="fileName">file name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SpaceDeleteFileAsync(ApiSession apiSession, string serverFilePath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.WebFilesDeleteFileAsync(apiSession, serverFilePath, token);
                FailIfError(apiResult);
                return Task.FromResult(0);

            }, cancellationToken, OperationType.ShortOperation);

        }

        /// <summary>
        ///     Deletes folder
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolderPath">Path to server folder like /path/to/folder</param>
        /// <param name="failIfNotExists">Fails with error if folder does not exist</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task SpaceDeleteFolderAsync(ApiSession apiSession, string serverFolderPath, bool failIfNotExists, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.WebFilesDeleteFolderAsync(apiSession, serverFolderPath, failIfNotExists, token);
                FailIfError(apiResult);
                return Task.FromResult(0);
            }, cancellationToken, OperationType.ShortOperation);
        }

        /// <summary>
        ///     Creates a folder
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="parentFolderPath">Path to server folder like /path/to/folder</param>
        /// <param name="folderName"></param>
        /// <param name="failIfExists">Fails with error if target folder exists already</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task SpaceCreateFolderAsync(ApiSession apiSession, string parentFolderPath, string folderName,
            bool failIfExists, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.WebFilesCreateFolderAsync(apiSession, parentFolderPath, folderName, failIfExists, token);
                FailIfError(apiResult);
                return Task.FromResult(0);
            }, cancellationToken, OperationType.ShortOperation);
        }

        /// <summary>
        ///     Renames a folder
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="parentFolderPath">Path to containing server folder like /path/to/folder</param>
        /// <param name="oldFolderName">Old folder name</param>
        /// <param name="newFolderName">New folder name</param>
        /// <param name="failIfExists">Fails with error if target folder exists already</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task SpaceRenameFolderAsync(ApiSession apiSession, string parentFolderPath, string oldFolderName, string newFolderName,
            bool failIfExists, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.WebFilesRenameFolderAsync(apiSession, parentFolderPath, oldFolderName, newFolderName,
                    failIfExists, token);
                FailIfError(apiResult);
                return Task.FromResult(0);
            }, cancellationToken, OperationType.ShortOperation);
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
                var apiResult = await _lowLevelApiClient.ValidateTasksAsync(apiSession, request, token);
                return MapOrFail(apiResult, (dto) => ValidateTasksResponseMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.ShortOperation);

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

                var timeout = clientConfiguration.SessionOpenTimeout;
                linkedTokenSource.CancelAfter(timeout);
                var cancellationToken = linkedTokenSource.Token;
                try
                {
                    // tring to resolve space and space auth method.
                    // Method 1. using get all spaces method
                    var spacesListApiResult = await _lowLevelApiClient.SpacesGetListAsync(cancellationToken);
                    SpaceEnumerationItem desiredSpace = null;
                    if (!spacesListApiResult.IsSucceed && spacesListApiResult.Error is MorphApiForbiddenException)
                    {
                        // space listing disabled has been disabled be server admin. 
                        // Method 2. Using spaces lookup (new endpoint since next version of EM Server 4.3)
                        var lookupApiResult = await _lowLevelApiClient.SpacesLookupAsync(new SpacesLookupRequestDto() { SpaceNames = { openSessionRequest.SpaceName } }, cancellationToken);
                        desiredSpace = MapOrFail(lookupApiResult,
                            (dto) =>
                            {
                                // response have at least 1 element with requested space.
                                var lookup = dto.Values.First();
                                if (lookup.Error != null)
                                {
                                    // seems that space not found.
                                    throw new Exception($"Unable to open session. {lookup.Error.message}");
                                }
                                else
                                {
                                    // otherwise return space
                                    return SpacesEnumerationMapper.MapItemFromDto(lookup.Data);
                                }
                            }
                            );
                    }
                    else
                    {
                        var spacesListResult = MapOrFail(spacesListApiResult, (dto) => SpacesEnumerationMapper.MapFromDto(dto));
                        desiredSpace = spacesListResult.Items.FirstOrDefault(x => x.SpaceName.Equals(openSessionRequest.SpaceName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (desiredSpace == null)
                    {
                        throw new Exception($"Unable to open session. Server has no space '{openSessionRequest.SpaceName}'");
                    }

                    var authenticator = CreateAuthenticator(openSessionRequest, desiredSpace);

                    return await authenticator(cancellationToken);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && linkedTokenSource.IsCancellationRequested)
                {
                    throw new Exception($"Can't connect to host {clientConfiguration.ApiUri}.  Operation timeout ({timeout})");
                }
            }

        }

        private Authenticator CreateAuthenticator(OpenSessionRequest openSessionRequest, SpaceEnumerationItem desiredSpace)
        {
            var requestClone = openSessionRequest.Clone();

            return async ctoken =>
            {
                var response = await MorphServerAuthenticator.OpenSessionMultiplexedAsync(desiredSpace,
                    new OpenSessionAuthenticatorContext(_lowLevelApiClient,
                        this,
                        (handler) =>
                            ConstructRestApiClient(
                                BuildHttpClient(clientConfiguration, handler),
                                BuildBaseAddress(clientConfiguration), clientConfiguration)),
                    requestClone,
                    ctoken);

                if(!string.IsNullOrWhiteSpace(response?.AuthToken))
                    Config.SessionRefresher.AssociateAuthenticator(response, CreateAuthenticator(requestClone, desiredSpace));

                return response;
            };
        }


        public Task<SpaceTasksList> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.GetTasksListAsync(apiSession, token);
                return MapOrFail(apiResult, (dto) => TasksListDtoMapper.MapFromDto(dto));

            }, cancellationToken, OperationType.ShortOperation);

        }

        public Task<SpaceTask> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            return Wrapped(async (token) =>
            {
                var apiResult = await _lowLevelApiClient.GetTaskAsync(apiSession, taskId, token);
                return MapOrFail(apiResult, (dto) => SpaceTaskMapper.MapFull(dto));

            }, cancellationToken, OperationType.ShortOperation);
        }


        public Task<ServerStreamingData> SpaceOpenStreamingDataAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                Action<FileTransferProgressEventArgs> onReceiveProgress = TriggerOnDataDownloadProgress;
                var apiResult = await _lowLevelApiClient.WebFilesDownloadFileAsync(apiSession, remoteFilePath, onReceiveProgress, token);
                return MapOrFail(apiResult, (data) => new ServerStreamingData(data.Stream, data.FileName, data.FileSize)
                );

            }, cancellationToken, OperationType.FileTransfer);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                if (_lowLevelApiClient != null)
                    _lowLevelApiClient.Dispose();

                Array.ForEach(_ctsForDisposing.ToArray(), z => z.Dispose());
                _disposed = true;
            }
        }

        public Task<Stream> SpaceOpenDataStreamAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            return Wrapped(async (token) =>
            {
                Action<FileTransferProgressEventArgs> onReceiveProgress = TriggerOnDataDownloadProgress;
                var apiResult = await _lowLevelApiClient.WebFilesDownloadFileAsync(apiSession, remoteFilePath, onReceiveProgress, token);
                return MapOrFail(apiResult, (data) => data.Stream);

            }, cancellationToken, OperationType.FileTransfer);
        }

        public Task<ContiniousStreamingConnection> SpaceUploadContiniousStreamingAsync(ApiSession apiSession, SpaceUploadContiniousStreamRequest continiousStreamRequest, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (continiousStreamRequest == null)
            {
                throw new ArgumentNullException(nameof(continiousStreamRequest));
            }

            return Wrapped(async (token) =>
            {
                var apiResult =
                  continiousStreamRequest.OverwriteExistingFile ?
                    await _lowLevelApiClient.WebFilesOpenContiniousPutStreamAsync(apiSession, continiousStreamRequest.ServerFolder, continiousStreamRequest.FileName, token) :
                    await _lowLevelApiClient.WebFilesOpenContiniousPostStreamAsync(apiSession, continiousStreamRequest.ServerFolder, continiousStreamRequest.FileName, token);

                var connection = MapOrFail(apiResult, c => c);
                return new ContiniousStreamingConnection(connection);

            }, cancellationToken, OperationType.FileTransfer);

        }

        public Task SpaceUploadDataStreamAsync(ApiSession apiSession, SpaceUploadDataStreamRequest spaceUploadFileRequest, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (spaceUploadFileRequest == null)
            {
                throw new ArgumentNullException(nameof(spaceUploadFileRequest));
            }

            return Wrapped(async (token) =>
            {
                Action<FileTransferProgressEventArgs> onSendProgress = TriggerOnDataUploadProgress;
                var sendStreamData = new SendFileStreamData(
                    spaceUploadFileRequest.DataStream,
                    spaceUploadFileRequest.FileName,
                    spaceUploadFileRequest.FileSize);
                var apiResult =
                    spaceUploadFileRequest.OverwriteExistingFile ?
                    await _lowLevelApiClient.WebFilesPutFileStreamAsync(apiSession, spaceUploadFileRequest.ServerFolder, sendStreamData, onSendProgress, token) :
                    await _lowLevelApiClient.WebFilesPostFileStreamAsync(apiSession, spaceUploadFileRequest.ServerFolder, sendStreamData, onSendProgress, token);
                FailIfError(apiResult);
                return Task.FromResult(0);

            }, cancellationToken, OperationType.FileTransfer);
        }
    }

}