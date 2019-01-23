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

    public class ApiResult<T>
    {
        public T Data { get; set; } = default(T);
        public Exception Error { get; set; } = default(Exception);
        public bool IsSucceed { get { return Error == null; } }
        public static ApiResult<T> Fail(Exception exception)
        {
            return new ApiResult<T>()
            {
                Data = default(T),
                Error = exception
            };
        }

        public static ApiResult<T> Ok(T data)
        {
            return new ApiResult<T>()
            {
                Data = data,
                Error = null
            };
        }
    }

    public static class ApiSessionExtension
    {
        public static HeadersCollection ToHeadersCollection(this ApiSession apiSession)
        {
            var collection = new HeadersCollection();
            if (apiSession != null && !apiSession.IsAnonymous && !apiSession.IsClosed)
            {
                collection.Add(ApiSession.AuthHeaderName, apiSession.AuthToken);
            }
            return collection;
        }
    }



    public class HeadersCollection
    {
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        public HeadersCollection()
        {
            
        }

        

        public void Add(string header, string value)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _headers[header] = value;
        }

        public void Fill(HttpRequestHeaders reqestHeaders)
        {
            if (reqestHeaders == null)
            {
                throw new ArgumentNullException(nameof(reqestHeaders));
            }
            foreach(var item in _headers)
            {
                reqestHeaders.Add(item.Key, item.Value);
            }
        }
    }
    

    public interface IApiClient
    {
        Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken);
        Task<ApiResult<TResult>> PostAsync<TModel, TResult>(string url,TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken);
        Task<ApiResult<TResult>> PutAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken);
        Task<ApiResult<TResult>> DeleteAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken);
    }

    public sealed class NoContentResult
    {

    }

    public sealed class NoContentRequest
    {

    }

    public class MorphServerRestClient : IApiClient
    {
        private readonly HttpClient httpClient;

        public MorphServerRestClient(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
        public Task<ApiResult<TResult>> DeleteAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            return SendAsyncApiResult<TResult, NoContentRequest>(HttpMethod.Delete, url, null, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {            
            if(urlParameters == null)
            {
                urlParameters = new NameValueCollection();
            }
            urlParameters.Add("_", DateTime.Now.Ticks.ToString());
            return SendAsyncApiResult<TResult, NoContentRequest>(HttpMethod.Get, url, null, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> PostAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            return SendAsyncApiResult<TResult, TModel>(HttpMethod.Post, url, model, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> PutAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            return SendAsyncApiResult<TResult, TModel>(HttpMethod.Put, url, model, urlParameters, headersCollection, cancellationToken);
        }

        protected virtual async Task<ApiResult<TResult>> SendAsyncApiResult<TResult, TModel>(HttpMethod httpMethod, string path, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            StringContent stringContent = null;
            if (model != null)
            {
                var serialized = JsonSerializationHelper.Serialize<TModel>(model);
                stringContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            }

            var url = path + urlParameters.ToQueryString();
            var httpRequestMessage = BuildHttpRequestMessage(httpMethod, url, stringContent, headersCollection);

            using (var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializationHelper.Deserialize<TResult>(content);
                    return ApiResult<TResult>.Ok(result);
                }
                else
                {
                    var error = await BuildExceptionFromResponse(response);
                    return ApiResult<TResult>.Fail(error);
                }
            }
        }

        protected HttpRequestMessage BuildHttpRequestMessage(HttpMethod httpMethod, string url, HttpContent content, HeadersCollection headersCollection)
        {
            var requestMessage = new HttpRequestMessage()
            {
                Content = content,
                Method = httpMethod,
                RequestUri = new Uri(url, UriKind.Relative)
            };
            if(headersCollection != null)
            {
                headersCollection.Fill(requestMessage.Headers);
            }            
            return requestMessage;
        }



        private static async Task<Exception> BuildExceptionFromResponse(HttpResponseMessage response)
        {

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                ErrorResponse errorResponse = null;
                try
                {
                    errorResponse = JsonSerializationHelper.Deserialize<ErrorResponse>(content);
                }
                catch (Exception)
                {
                    return new ResponseParseException("An error occurred while deserializing the response", content);
                }
                if (errorResponse.error == null)
                    return new ResponseParseException("An error occurred while deserializing the response", content);

                switch (errorResponse.error.code)
                {
                    case ReadableErrorTopCode.Conflict: return new MorphApiConflictException(errorResponse.error.message);
                    case ReadableErrorTopCode.NotFound: return new MorphApiNotFoundException(errorResponse.error.message);
                    case ReadableErrorTopCode.Forbidden: return new MorphApiForbiddenException(errorResponse.error.message);
                    case ReadableErrorTopCode.Unauthorized: return new MorphApiUnauthorizedException(errorResponse.error.message);
                    case ReadableErrorTopCode.BadArgument: return new MorphApiBadArgumentException(FieldErrorsMapper.MapFromDto(errorResponse.error), errorResponse.error.message);
                    default: return new MorphClientGeneralException(errorResponse.error.code, errorResponse.error.message);
                }
            }

            else
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Conflict: return new MorphApiConflictException(response.ReasonPhrase ?? "Conflict");
                    case HttpStatusCode.NotFound: return new MorphApiNotFoundException(response.ReasonPhrase ?? "Not found");
                    case HttpStatusCode.Forbidden: return new MorphApiForbiddenException(response.ReasonPhrase ?? "Forbidden");
                    case HttpStatusCode.Unauthorized: return new MorphApiUnauthorizedException(response.ReasonPhrase ?? "Unauthorized");
                    case HttpStatusCode.BadRequest: return new MorphClientGeneralException("Unknown", response.ReasonPhrase ?? "Unknown error");
                    default: return new ResponseParseException(response.ReasonPhrase, null);
                }

            }
        }


    }



    internal interface ILowLevelApiClient
    {
        // TASKS
        Task<ApiResult<TaskStatusDto>> GetTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);
        Task<ApiResult<RunningTaskStatus>> StartTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskParameterBase> taskParameters = null);
        Task<ApiResult<NoContentResult>> StopTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);
        Task<ApiResult<SpaceTasksList>> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken);
        Task<ApiResult<SpaceTask>> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken);
    }

    internal class LowLevelApiClient : ILowLevelApiClient
    {
        private readonly IApiClient apiClient;

        public LowLevelApiClient(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }
        public Task<ApiResult<SpaceTask>> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResult<SpaceTasksList>> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        public Task<ApiResult<RunningTaskStatus>> StartTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskParameterBase> taskParameters = null)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResult<NoContentResult>> StopTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
        public async Task<RunningTaskStatus> StartTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken, IEnumerable<TaskParameterBase> taskParameters = null)
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
            var result = await apiClient.PostAsync<TaskStartRequestDto,RunningTaskStatusDto>(url, dto, new NameValueCollection(), apiSession.ToHeadersCollection(), cancellationToken);
            


         
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Post, url, request, apiSession), cancellationToken))
            {
                var info = await HandleResponse<RunningTaskStatusDto>(response);
                return RunningTaskStatusMapper.RunningTaskStatusFromDto(info);
            }

        }

        protected Task<TResult> WrappedShort<TResult>(Func<CancellationToken, Task<TResult>> fun, CancellationToken orginalCancellationToken)
        {
            return fun(orginalCancellationToken);
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
        /// Gets status of the task (Running/Not running) and payload
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId">task guid</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        private async Task<RunningTaskStatus> GetRunningTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = UrlHelper.JoinUrl("space", spaceName, "runningtasks", taskId.ToString("D")) + nvc.ToQueryString();
           
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Get, url, null, apiSession), cancellationToken))
            {
                var info = await HandleResponse<RunningTaskStatusDto>(response);
                return RunningTaskStatusMapper.RunningTaskStatusFromDto(info);
            }
        }


        /// <summary>
        /// Gets status of the task
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="taskId">task guid</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        public  Task<Model.TaskStatus> GetTaskStatusAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            return WrappedShort(async (token) =>
            {
                var apiResult = await lowLevelApiClient.GetTaskStatusAsync(apiSession, taskId, token);
                return MapOrFail(apiResult, (dto) => TaskStatusMapper.MapFromDto(dto));

            },cancellationToken);

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

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "runningtasks", taskId.ToString("D"));
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Delete, url, null, apiSession), cancellationToken))
            {
                await HandleResponse(response);
            }
        }

        /// <summary>
        /// Returns server status. May raise exception if server is unreachable
        /// </summary>
        /// <returns></returns>
        public async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken)
        {
            return await GetDataWithCancelAfter(async (token) =>
            {
                var nvc = new NameValueCollection();
                nvc.Add("_", DateTime.Now.Ticks.ToString());

                var url = "server/status" + nvc.ToQueryString();
                using (var response = await GetHttpClient().GetAsync(url, token))
                {
                    var dto = await HandleResponse<ServerStatusDto>(response);
                    var result = ServerStatusMapper.MapFromDto(dto);
                    return result;

                }
            }, TimeSpan.FromSeconds(20), cancellationToken);
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


        protected async Task<T> GetDataWithCancelAfter<T>(Func<CancellationToken, Task<T>> action, TimeSpan timeout,  CancellationToken cancellationToken)
        {
            using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {                                
                linkedTokenSource.CancelAfter(timeout);
                try
                {
                    return await action(linkedTokenSource.Token);
                }

                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && linkedTokenSource.IsCancellationRequested)
                {
                    throw new Exception($"Can't connect to host {_apiHost}.  Operation timeout ({timeout})");
                }
            }
        }

        public async Task<SpacesEnumerationList> GetSpacesListAsync(CancellationToken cancellationToken)
        {
            return await GetDataWithCancelAfter(async (token) =>
            {
                var nvc = new NameValueCollection();
                nvc.Add("_", DateTime.Now.Ticks.ToString());
                var url = "spaces/list" + nvc.ToQueryString();
                using (var response = await GetHttpClient().GetAsync(url, token))
                {
                    var dto = await HandleResponse<SpacesEnumerationDto>(response);
                    return SpacesEnumerationMapper.MapFromDto(dto);
                }
            }, TimeSpan.FromSeconds(20), cancellationToken);

        }


        /// <summary>
        /// Prerforms browsing the Space
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="folderPath">folder path like /path/to/folder</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SpaceBrowsingInfo> BrowseSpaceAsync(ApiSession apiSession, string folderPath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());

            var url = UrlHelper.JoinUrl("space", spaceName, "browse", folderPath) + nvc.ToQueryString();
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Get, url, null, apiSession), cancellationToken))
            {
                var dto = await HandleResponse<SpaceBrowsingResponseDto>(response);
                return SpaceBrowsingMapper.MapFromDto(dto);

            }
        }

        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolder">server folder like /path/to/folder</param>
        /// <param name="fileName">file name </param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns true if file exists.</returns>
        public async Task<bool> FileExistsAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(nameof(fileName));
            var browseResult = await this.BrowseSpaceAsync(apiSession, serverFolder, cancellationToken);

            return browseResult.FileExists(fileName);
        }





        /// <summary>
        /// Performs file deletion
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="serverFolder">Path to server folder like /path/to/folder</param>
        /// <param name="fileName">file name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteFileAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder, fileName);

            using (HttpResponseMessage response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Delete, url, null, apiSession), cancellationToken))
            {
                await HandleResponse(response);
            }

        }


        /// <summary>
        /// Retrieves space status
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SpaceStatus> GetSpaceStatusAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("spaces", spaceName, "status");

            using (HttpResponseMessage response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Get, url, null, apiSession), cancellationToken))
            {
                var dto = await HandleResponse<SpaceStatusDto>(response);
                var entity = SpaceStatusMapper.MapFromDto(dto);
                return entity;
            }

        }


        /// <summary>
        /// Validate tasks. Checks that there are no missing parameters in the tasks. 
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="projectPath">project path like /path/to/project.morph </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ValidateTasksResult> ValidateTasksAsync(ApiSession apiSession, string projectPath, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }

            if (string.IsNullOrWhiteSpace(projectPath))
                throw new ArgumentException(nameof(projectPath));
            var spaceName = apiSession.SpaceName;
            var url = "commands/validatetasks";
            var request = new ValidateTasksRequestDto
            {
                SpaceName = spaceName,
                ProjectPath = projectPath
            };
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Post, url, JsonSerializationHelper.SerializeAsStringContent(request), apiSession), cancellationToken))
            {

                var dto = await HandleResponse<ValidateTasksResponseDto>(response);
                var entity = ValidateTasksResponseMapper.MapFromDto(dto);
                return entity;

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

        protected async Task<string> internalAuthLoginAsync(string clientNonce, string serverNonce, string spaceName, string passwordHash, CancellationToken cancellationToken)
        {
            var url = "auth/login";
            var requestDto = new LoginRequestDto
            {
                ClientSeed = clientNonce,
                Password = passwordHash,
                Provider = "Space",
                UserName = spaceName,
                RequestToken = serverNonce
            };

            using (var response = await GetHttpClient().PostAsync(url, JsonSerializationHelper.SerializeAsStringContent(requestDto), cancellationToken))
            {
                var responseDto = await HandleResponse<LoginResponseDto>(response);
                return responseDto.Token;
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
                    var spacesListResult = await GetSpacesListAsync(cancellationToken);
                    var desiredSpace = spacesListResult.Items.FirstOrDefault(x => x.SpaceName.Equals(openSessionRequest.SpaceName, StringComparison.OrdinalIgnoreCase));
                    if (desiredSpace == null)
                    {
                        throw new Exception($"Server has no space '{openSessionRequest.SpaceName}'");
                    }
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
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && linkedTokenSource.IsCancellationRequested)
                {
                    throw new Exception($"Can't connect to host {_apiHost}.  Operation timeout ({timeout})");
                }
            }

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

        /// <summary>
        /// Close opened session
        /// </summary>
        /// <param name="apiSession">api session</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CloseSessionAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            if (apiSession.IsClosed)
                return;
            if (apiSession.IsAnonymous)
                return;


            var url = "auth/logout";

            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Post, url, null, apiSession), cancellationToken))
            {

                await HandleResponse(response);

            }


        }

        public async Task<SpaceTasksList> GetTasksListAsync(ApiSession apiSession, CancellationToken cancellationToken)
        {
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = UrlHelper.JoinUrl("space", apiSession.SpaceName, "tasks");
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Get, url, null, apiSession), cancellationToken))
            {
                var dto = await HandleResponse<SpaceTasksListDto>(response);
                return SpaceTasksListsMapper.MapFromDto(dto);
            }
        }

        public async Task<SpaceTask> GetTaskAsync(ApiSession apiSession, Guid taskId, CancellationToken cancellationToken)
        {
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = UrlHelper.JoinUrl("space", apiSession.SpaceName, "tasks", taskId.ToString("D"));
            using (var response = await GetHttpClient().SendAsync(BuildHttpRequestMessage(HttpMethod.Get, url, null, apiSession), cancellationToken))
            {
                var dto = await HandleResponse<SpaceTaskDto>(response);
                return SpaceTaskMapper.MapFull(dto);
            }
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
