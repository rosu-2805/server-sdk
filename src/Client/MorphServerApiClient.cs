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



    public class MorphServerAuthenticator
    {
        private readonly IApiClient apiClient;
        private readonly IMorphServerApiClient morphServerApiClient;

        public MorphServerAuthenticator(IApiClient apiClient, IMorphServerApiClient morphServerApiClient)
        {
            this.apiClient = apiClient;
            this.morphServerApiClient = morphServerApiClient;
        }


        public async Task<ApiSession> OpenSessionAsync(SpaceEnumerationItem desiredSpace, OpenSessionRequest openSessionRequest, CancellationToken cancellationToken)
        {
            // space access restriction is supported since server 3.9.2
            // for previous versions api will return SpaceAccessRestriction.NotSupported 
            // a special fall-back mechanize need to be used to open session in such case
            switch (desiredSpace.SpaceAccessRestriction)
            {
                // anon space
                case SpaceAccessRestriction.None:
                    return ApiSession.Anonymous(openSessionRequest.SpaceName);

                // password protected space                
                case SpaceAccessRestriction.BasicPassword:
                    return await OpenSessionViaSpacePasswordAsync(openSessionRequest.SpaceName, openSessionRequest.Password, cancellationToken);

                // windows authentication
                case SpaceAccessRestriction.WindowsAuthentication:
                    return await OpenSessionViaWindowsAuthenticationAsync(openSessionRequest.SpaceName, cancellationToken);

                // fallback
                case SpaceAccessRestriction.NotSupported:

                    //  if space is public or password is not set - open anon session
                    if (desiredSpace.IsPublic || string.IsNullOrWhiteSpace(openSessionRequest.Password))
                    {
                        return ApiSession.Anonymous(openSessionRequest.SpaceName);
                    }
                    // otherwise open session via space password
                    else
                    {
                        return await OpenSessionViaSpacePasswordAsync(openSessionRequest.SpaceName, openSessionRequest.Password, cancellationToken);
                    }

                default:
                    throw new Exception("Space access restriction method is not supported by this client.");
            }
        }


        protected async Task<ApiSession> OpenSessionViaWindowsAuthenticationAsync(string spaceName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(spaceName))
            {
                throw new ArgumentException("Space name is not set", nameof(spaceName));
            }
            // handler will be disposed automatically
            HttpClientHandler aHandler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                UseDefaultCredentials = true
            };

            using (var httpClient = ConstructHttpClient(_apiHost, aHandler))
            {

                var serverNonce = await internalGetAuthNonceAsync(httpClient, cancellationToken);
                var token = await internalAuthExternalWindowAsync(httpClient, spaceName, serverNonce, cancellationToken);

                return new ApiSession(morphServerApiClient)
                {
                    AuthToken = token,
                    IsAnonymous = false,
                    IsClosed = false,
                    SpaceName = spaceName
                };
            }
        }
        protected static async Task<string> internalGetAuthNonceAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            var url = "auth/nonce";
            using (var response = await httpClient.PostAsync(url, JsonSerializationHelper.SerializeAsStringContent(new GenerateNonceRequestDto()), cancellationToken))
            {
                var dto = await HandleResponse<GenerateNonceResponseDto>(response);
                return dto.Nonce;

            }
        }

        protected static async Task<string> internalAuthExternalWindowAsync(HttpClient httpClient, string spaceName, string serverNonce, CancellationToken cancellationToken)
        {
            var url = "auth/external/windows";
            var requestDto = new WindowsExternalLoginRequestDto
            {
                RequestToken = serverNonce,
                SpaceName = spaceName
            };

            using (var response = await httpClient.PostAsync(url, JsonSerializationHelper.SerializeAsStringContent(requestDto), cancellationToken))
            {
                var responseDto = await HandleResponse<LoginResponseDto>(response);
                return responseDto.Token;
            }
        }

        protected async Task<string> internalAuthLoginAsync(string clientNonce, string serverNonce, string spaceName, string passwordHash, CancellationToken cancellationToken)
        {

            var requestDto = new LoginRequestDto
            {
                ClientSeed = clientNonce,
                Password = passwordHash,
                Provider = "Space",
                UserName = spaceName,
                RequestToken = serverNonce
            };
            var apiResult = await lowLevelApiClient.AuthLoginPasswordAsync(requestDto, cancellationToken);
            FailIfError(apiResult);
            return apiResult.Data.Token;

        }

        /// <summary>
        /// Open a new authenticated session via password
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="password">space password</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ApiSession> OpenSessionViaSpacePasswordAsync(string spaceName, string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(spaceName))
            {
                throw new ArgumentException("Space name is not set.", nameof(spaceName));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var passwordHash = CryptographyHelper.CalculateSha256HEX(password);
            var serverNonce = await internalGetAuthNonceAsync(GetHttpClient(), cancellationToken);
            var clientNonce = ConvertHelper.ByteArrayToHexString(CryptographyHelper.GenerateRandomSequence(16));
            var all = passwordHash + serverNonce + clientNonce;
            var allHash = CryptographyHelper.CalculateSha256HEX(all);


            var token = await internalAuthLoginAsync(clientNonce, serverNonce, spaceName, allHash, cancellationToken);

            return new ApiSession(this)
            {
                AuthToken = token,
                IsAnonymous = false,
                IsClosed = false,
                SpaceName = spaceName
            };
        }



    }




    /// <summary>
    /// Morph Server api client V1
    /// </summary>
    public class MorphServerApiClient : IMorphServerApiClient, IDisposable
    {
        protected readonly Uri _apiHost;
        protected readonly string UserAgent = "MorphServerApiClient/1.3.5";
        protected HttpClient _httpClient;
        protected readonly string _api_v1 = "api/v1/";


        //private IApiClient apiClient;
        private ILowLevelApiClient lowLevelApiClient;

        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan FileTransferTimeout { get; set; } = TimeSpan.FromHours(3);

        /// <summary>
        /// Construct Api client
        /// </summary>
        /// <param name="apiHost">Server url</param>
        public MorphServerApiClient(string apiHost)
        {
            if (!apiHost.EndsWith("/"))
                apiHost += "/";
            _apiHost = new Uri(apiHost);

        }

        protected HttpClient GetHttpClient()
        {
            if (_httpClient == null)
            {
#if NETSTANDARD2_0
                // handler will be disposed automatically
                HttpClientHandler aHandler = new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Automatic,
                    ServerCertificateCustomValidationCallback = new Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>(
                        (request, certificate, chain, sslPolicyErrors) => true)

                };
#elif NET45
                 // handler will be disposed automatically
                HttpClientHandler aHandler = new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Automatic
                    
                };
#endif

                _httpClient = ConstructHttpClient(_apiHost, aHandler);
            }
            return _httpClient;

            
        }


        public event EventHandler<FileEventArgs> FileProgress;


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



            client.Timeout = TimeSpan.FromMinutes(15);

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

        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="remoteFilePath">Path to the remote file. Like /some/folder/file.txt </param>
        /// <param name="streamToWriteTo">stream for writing. You should dispose the stream by yourself</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns file info</returns>
        public async Task<DownloadFileInfo> DownloadFileAsync(ApiSession apiSession, string remoteFilePath, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            DownloadFileInfo fileInfo = null;
            await DownloadFileAsync(apiSession, remoteFilePath, (fi) => { fileInfo = fi; return true; }, streamToWriteTo, cancellationToken);
            return fileInfo;
        }
        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="remoteFilePath"> Path to the remote file. Like /some/folder/file.txt </param>
        /// <param name="handleFile">delegate to check file info before accessing to the file stream</param>
        /// <param name="streamToWriteTo">stream for writing. Writing will be executed only if handleFile delegate returns true</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DownloadFileAsync(ApiSession apiSession, string remoteFilePath, Func<DownloadFileInfo, bool> handleFile, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = UrlHelper.JoinUrl("space", spaceName, "files", remoteFilePath) + nvc.ToQueryString();
            // it's necessary to add HttpCompletionOption.ResponseHeadersRead to disable caching
            using (HttpResponseMessage response = await GetHttpClient()
                .SendAsync(BuildHttpRequestMessage(HttpMethod.Get, url, null, apiSession), HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                if (response.IsSuccessStatusCode)
                {
                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        var contentDisposition = response.Content.Headers.ContentDisposition;
                        DownloadFileInfo dfi = null;
                        if (contentDisposition != null)
                        {
                            dfi = new DownloadFileInfo
                            {
                                // need to fix double quotes, that may come from server response
                                // FileNameStar contains file name encoded in UTF8
                                FileName = (contentDisposition.FileNameStar ?? contentDisposition.FileName).TrimStart('\"').TrimEnd('\"')
                            };
                        }
                        var contentLength = response.Content.Headers.ContentLength;
                        var fileProgress = new FileProgress(dfi.FileName, contentLength.Value);
                        fileProgress.StateChanged += DownloadProgress_StateChanged;

                        var bufferSize = 4096;
                        if (handleFile(dfi))
                        {

                            var buffer = new byte[bufferSize];
                            var size = contentLength.Value;
                            var processed = 0;
                            var lastUpdate = DateTime.MinValue;

                            fileProgress.ChangeState(FileProgressState.Starting);

                            while (true)
                            {
                                // cancel download if cancellation token triggered
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    fileProgress.ChangeState(FileProgressState.Cancelled);
                                    throw new OperationCanceledException();
                                }

                                var length = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length);
                                if (length <= 0) break;
                                await streamToWriteTo.WriteAsync(buffer, 0, length);
                                processed += length;
                                if (DateTime.Now - lastUpdate > TimeSpan.FromMilliseconds(250))
                                {
                                    fileProgress.SetProcessedBytes(processed);
                                    fileProgress.ChangeState(FileProgressState.Processing);
                                    lastUpdate = DateTime.Now;
                                }

                            }

                            fileProgress.ChangeState(FileProgressState.Finishing);

                        }

                    }
                }
                else
                {
                    // TODO: check
                    await HandleErrorResponse(response);

                }


        }

        /// <summary>
        /// Uploads file to the server 
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="localFilePath">path to the local file</param>
        /// <param name="destFolderPath">detination folder like /path/to/folder </param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="overwriteFileifExists">overwrite file</param>
        /// <returns></returns>
        public async Task UploadFileAsync(ApiSession apiSession, string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (!File.Exists(localFilePath))
                throw new FileNotFoundException(string.Format("File '{0}' not found", localFilePath));
            var fileSize = new System.IO.FileInfo(localFilePath).Length;
            var fileName = Path.GetFileName(localFilePath);
            using (var fsSource = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                await UploadFileAsync(apiSession, fsSource, fileName, fileSize, destFolderPath, cancellationToken, overwriteFileifExists);
                return;
            }

        }


        /// <summary>
        /// Uploads local file to the server folder. 
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="localFilePath">path to the local file</param>
        /// <param name="destFolderPath">destination folder like /path/to/folder </param>
        /// <param name="destFileName">destination filename. If it's empty then original file name will be used</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="overwriteFileifExists">overwrite file</param>
        /// <returns></returns>
        public async Task UploadFileAsync(ApiSession apiSession, string localFilePath, string destFolderPath, string destFileName, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException(string.Format("File '{0}' not found", localFilePath));
            }
            var fileName = String.IsNullOrWhiteSpace(destFileName) ? Path.GetFileName(localFilePath) : destFileName;
            var fileSize = new FileInfo(localFilePath).Length;
            using (var fsSource = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                await UploadFileAsync(apiSession, fsSource, fileName, fileSize, destFolderPath, cancellationToken, overwriteFileifExists);
                return;
            }

        }




        /// <summary>
        /// Upload file stream to the server
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="inputStream">stream for read from</param>
        /// <param name="fileName">file name</param>
        /// <param name="fileSize">file size in bytes</param>
        /// <param name="destFolderPath">destination folder like /path/to/folder </param>
        /// <param name="cancellationToken">cancellation tokern</param>
        /// <param name="overwriteFileifExists"></param>
        /// <returns></returns>
        public async Task UploadFileAsync(ApiSession apiSession, Stream inputStream, string fileName, long fileSize, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            try
            {
                var spaceName = apiSession.SpaceName;
                string boundary = "EasyMorphCommandClient--------" + Guid.NewGuid().ToString("N");
                string url = UrlHelper.JoinUrl("space", spaceName, "files", destFolderPath);

                using (var content = new MultipartFormDataContent(boundary))
                {
                    var downloadProgress = new FileProgress(fileName, fileSize);
                    downloadProgress.StateChanged += DownloadProgress_StateChanged;
                    using (cancellationToken.Register(() => downloadProgress.ChangeState(FileProgressState.Cancelled)))
                    {
                        using (var streamContent = new ProgressStreamContent(inputStream, downloadProgress))
                        {
                            content.Add(streamContent, "files", Path.GetFileName(fileName));

                            var requestMessage = BuildHttpRequestMessage(overwriteFileifExists ? HttpMethod.Put : HttpMethod.Post, url, content, apiSession);
                            using (requestMessage)
                            {
                                using (var response = await GetHttpClient().SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                                {
                                    await HandleResponse(response);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            when (ex.InnerException != null && ex.InnerException is WebException)
            {
                var einner = ex.InnerException as WebException;
                if (einner.Status == WebExceptionStatus.ConnectionClosed)
                    throw new MorphApiNotFoundException("Specified folder not found");

            }
        }

        private void DownloadProgress_StateChanged(object sender, FileEventArgs e)
        {
            if (FileProgress != null)
            {
                FileProgress(this, e);
            }

        }


        //protected async Task<T> GetDataWithCancelAfter<T>(Func<CancellationToken, Task<T>> action, TimeSpan timeout, CancellationToken cancellationToken)
        //{
        //    using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        //    {
        //        linkedTokenSource.CancelAfter(timeout);
        //        try
        //        {
        //            return await action(linkedTokenSource.Token);
        //        }

        //        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && linkedTokenSource.IsCancellationRequested)
        //        {
        //            throw new Exception($"Can't connect to host {_apiHost}.  Operation timeout ({timeout})");
        //        }
        //    }
        //}




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
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
        }


    }
}


