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
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Reflection;
using System.IO;

namespace Morph.Server.Sdk.Client
{

    public enum OperationType
    {
        ShortOperation = 1,
        FileTransfer = 2
    }


    public static class MorphServerApiClientGlobalConfig
    {
#if NETSTANDARD2_0
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
#endif

        private const string DefaultClientType = "EMS-SDK";

        /// <summary>
        /// Default operation execution timeout
        /// </summary>
        public static TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        /// <summary>
        /// Default File transfer operation timeout
        /// </summary>
        public static TimeSpan FileTransferTimeout { get; set; } = TimeSpan.FromHours(3);

        /// <summary>
        /// HttpClient Timeout
        /// </summary>
        public static TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromHours(24);

        // additional parameter for  client identification
        public static string ClientId { get; set; } = string.Empty;
        public static string ClientType { get; set; } = DefaultClientType;

        // "Morph.Server.Sdk/x.x.x.x"
        internal static string SDKVersionString { get; }


        static MorphServerApiClientGlobalConfig()
        {
            // set sdk version string 
            // "Morph.Server.Sdk/x.x.x.x"
            Assembly thisAssem = typeof(MorphServerApiClientGlobalConfig).Assembly;
            var assemblyVersion = thisAssem.GetName().Version;
            SDKVersionString = "Morph.Server.Sdk/" + assemblyVersion.ToString();

        }



    }


    public interface IMorphApiClientConfiguration
    {
        TimeSpan OperationTimeout { get; }
        TimeSpan FileTransferTimeout { get; }
        TimeSpan HttpClientTimeout { get; }

        Uri ApiUri { get; }
#if NETSTANDARD2_0
        Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; }
#endif
    }

    public class MorphApiClientConfiguration : IMorphApiClientConfiguration
    {

        public TimeSpan OperationTimeout { get; set; } = MorphServerApiClientGlobalConfig.OperationTimeout;
        public TimeSpan FileTransferTimeout { get; set; } = MorphServerApiClientGlobalConfig.FileTransferTimeout;
        public TimeSpan HttpClientTimeout { get; set; } = MorphServerApiClientGlobalConfig.HttpClientTimeout;

        public string ClientId { get; set; } = MorphServerApiClientGlobalConfig.ClientId;
        public string ClientType { get; set; } = MorphServerApiClientGlobalConfig.ClientType;

        public Uri ApiUri { get; set; }
        internal string SDKVersionString { get; set; } = MorphServerApiClientGlobalConfig.SDKVersionString;
#if NETSTANDARD2_0
        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
        = MorphServerApiClientGlobalConfig.ServerCertificateCustomValidationCallback;
#endif



        /// <summary>
        /// Morph Server api client V1
        /// </summary>
        public class MorphServerApiClient : IMorphServerApiClient, IDisposable
        {
            public event EventHandler<FileEventArgs> FileProgress;

            protected readonly string _userAgent = "MorphServerApiClient/next";
            protected readonly string _api_v1 = "api/v1/";

            private readonly ILowLevelApiClient _lowLevelApiClient;
            private MorphApiClientConfiguration clientConfiguration = new MorphApiClientConfiguration();

            public IMorphApiClientConfiguration Config => clientConfiguration;

            internal ILowLevelApiClient BuildApiClient(MorphApiClientConfiguration configuration)
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
#elif NET45
            // handler will be disposed automatically
            HttpClientHandler aHandler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic                    
            };
#else
    Not implemented                        
#endif
                var httpClient = BuildHttpClient(configuration, aHandler);
                var restClient = ConstructRestApiClient(httpClient);
                return new LowLevelApiClient(restClient);
            }



            /// <summary>
            /// Construct Api client
            /// </summary>
            /// <param name="apiHost">Server url</param>
            public MorphServerApiClient(string apiHost)
            {
                if (apiHost == null)
                {
                    throw new ArgumentNullException(nameof(apiHost));
                }

                var defaultConfig = new MorphApiClientConfiguration
                {
                    ApiUri = new Uri(apiHost)
                };
                clientConfiguration = defaultConfig;
                _lowLevelApiClient = BuildApiClient(clientConfiguration);
            }

            public MorphServerApiClient(MorphApiClientConfiguration clientConfiguration)
            {
                this.clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
                _lowLevelApiClient = BuildApiClient(clientConfiguration);
            }


            private IApiClient ConstructRestApiClient(HttpClient httpClient)
            {
                if (httpClient == null)
                {
                    throw new ArgumentNullException(nameof(httpClient));
                }

                return new MorphServerRestClient(httpClient);
            }



            protected HttpClient BuildHttpClient(MorphApiClientConfiguration config, HttpClientHandler httpClientHandler)
            {
                if (httpClientHandler == null)
                {
                    throw new ArgumentNullException(nameof(httpClientHandler));
                }

                var client = new HttpClient(httpClientHandler, true);
                client.BaseAddress = new Uri(config.ApiUri, new Uri(_api_v1, UriKind.Relative));

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

                client.MaxResponseContentBufferSize = 100 * 1024;
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
                    var apiResult = await _lowLevelApiClient.StartTaskAsync(apiSession, taskId, token);
                    return MapOrFail(apiResult, (dto) => RunningTaskStatusMapper.RunningTaskStatusFromDto(dto));

                }, cancellationToken, OperationType.ShortOperation);
            }

            protected Task<TResult> Wrapped<TResult>(Func<CancellationToken, Task<TResult>> fun, CancellationToken orginalCancellationToken, OperationType operationType)
            {
                TimeSpan maxExecutionTime;
                switch (operationType)
                {
                    case OperationType.FileTransfer:
                        maxExecutionTime = clientConfiguration.FileTransferTimeout; break;
                    case OperationType.ShortOperation:
                        maxExecutionTime = clientConfiguration.OperationTimeout; break;
                    default: throw new NotImplementedException();
                }
                using (var derTokenSource = CancellationTokenSource.CreateLinkedTokenSource(orginalCancellationToken))
                {
                    derTokenSource.CancelAfter(maxExecutionTime);
                    try
                    {
                        return fun(derTokenSource.Token);
                    }

                    catch (OperationCanceledException) when (!orginalCancellationToken.IsCancellationRequested && derTokenSource.IsCancellationRequested)
                    {
                        throw new Exception($"Can't connect to host {clientConfiguration.ApiUri}.  Operation timeout ({maxExecutionTime})");
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
                    var apiResult = await _lowLevelApiClient.AuthLogoutAsync(apiSession, token);
                // if task fail - do nothing. server will close this session after inactivity period
                return Task.FromResult(0);

                }, cancellationToken, OperationType.ShortOperation);

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
                    var apiResult = await _lowLevelApiClient.GetRunningTaskStatusAsync(apiSession, taskId, token);
                    return MapOrFail(apiResult, (dto) => RunningTaskStatusMapper.RunningTaskStatusFromDto(dto));

                }, cancellationToken, OperationType.ShortOperation);

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
                    var apiResult = await _lowLevelApiClient.GetTaskStatusAsync(apiSession, taskId, token);
                    return MapOrFail(apiResult, (dto) => TaskStatusMapper.MapFromDto(dto));

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
                    var apiResult = await _lowLevelApiClient.StopTaskAsync(apiSession, taskId, token);
                    FailIfError(apiResult);
                    return Task.FromResult(0);

                }, cancellationToken, OperationType.ShortOperation);

            }

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

                }, cancellationToken, OperationType.ShortOperation);
            }

            public Task<SpacesEnumerationList> GetSpacesListAsync(CancellationToken cancellationToken)
            {
                return Wrapped(async (token) =>
                {
                    var apiResult = await _lowLevelApiClient.SpacesGetListAsync(token);
                    return MapOrFail(apiResult, (dto) => SpacesEnumerationMapper.MapFromDto(dto));

                }, cancellationToken, OperationType.ShortOperation);
            }

            private void DownloadProgress_StateChanged(object sender, FileEventArgs e)
            {
                FileProgress?.Invoke(this, e);
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
                    // no more than 20 sec for session opening
                    var timeout = TimeSpan.FromSeconds(20);
                    linkedTokenSource.CancelAfter(timeout);
                    var cancellationToken = linkedTokenSource.Token;
                    try
                    {
                        var spacesListApiResult = await _lowLevelApiClient.SpacesGetListAsync(cancellationToken);
                        var spacesListResult = MapOrFail(spacesListApiResult, (dto) => SpacesEnumerationMapper.MapFromDto(dto));

                        var desiredSpace = spacesListResult.Items.FirstOrDefault(x => x.SpaceName.Equals(openSessionRequest.SpaceName, StringComparison.OrdinalIgnoreCase));
                        if (desiredSpace == null)
                        {
                            throw new Exception($"Server has no space '{openSessionRequest.SpaceName}'");
                        }
                        var session = await MorphServerAuthenticator.OpenSessionMultiplexedAsync(desiredSpace,
                            new OpenSessionAuthenticatorContext(
                                _lowLevelApiClient,
                                this,
                                (handler) => ConstructRestApiClient(BuildHttpClient(clientConfiguration, handler))),
                            openSessionRequest, cancellationToken);

                        return session;
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested && linkedTokenSource.IsCancellationRequested)
                    {
                        throw new Exception($"Can't connect to host {clientConfiguration.ApiUri}.  Operation timeout ({timeout})");
                    }
                }

            }




            public Task<SpaceTasksList> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
            {
                return Wrapped(async (token) =>
                {
                    var apiResult = await _lowLevelApiClient.GetTasksListAsync(apiSession, token);
                    return MapOrFail(apiResult, (dto) => SpaceTasksListsMapper.MapFromDto(dto));

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


            public Task<FetchFileStreamData> SpaceDownloadFileAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken)
            {
                if (apiSession == null)
                {
                    throw new ArgumentNullException(nameof(apiSession));
                }

                return Wrapped(async (token) =>
                {
                    var apiResult = await _lowLevelApiClient.WebFilesDownloadFileAsync(apiSession, remoteFilePath, token);
                    return MapOrFail(apiResult, (data) => data);

                }, cancellationToken, OperationType.FileTransfer);
            }

            public void Dispose()
            {
                if (_lowLevelApiClient != null)
                {
                    _lowLevelApiClient.Dispose();
                }
            }

            public Task<Stream> SpaceDownloadFileStreamAsync(ApiSession apiSession, string remoteFilePath, CancellationToken cancellationToken)
            {
                if (apiSession == null)
                {
                    throw new ArgumentNullException(nameof(apiSession));
                }

                return Wrapped(async (token) =>
                {
                    var apiResult = await _lowLevelApiClient.WebFilesDownloadFileAsync(apiSession, remoteFilePath, token);
                    return MapOrFail(apiResult, (data) => data.Stream);

                }, cancellationToken, OperationType.FileTransfer);
            }

            public Task SpaceUploadFileAsync(ApiSession apiSession, SpaceUploadFileRequest spaceUploadFileRequest, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}


