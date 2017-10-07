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

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// Morph Server api client V1
    /// </summary>
    public class MorphServerApiClient : IMorphServerApiClient
    {
        protected readonly Uri _apiHost;
        protected readonly string UserAgent = "MorphServerApiClient/0.1";
        protected HttpClient _httpClient;
        protected readonly string _api_v1 = "api/v1/";
        protected readonly string _defaultSpaceName = "default";

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
                _httpClient = ConstructHttpClient(_apiHost);
            }
            return _httpClient;
        }


        public event EventHandler<FileEventArgs> FileProgress;


        protected HttpClient ConstructHttpClient(Uri apiHost)
        {
            HttpClientHandler aHandler = new HttpClientHandler();
            aHandler.ClientCertificateOptions = ClientCertificateOption.Automatic;

            var client = new HttpClient(aHandler);
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




        protected async Task<T> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializationHelper.Deserialize<T>(content);                
                return result;
            }
            else
            {
                await HandleErrorResponse(response);
                return default(T);

            }

        }

        protected async Task HandleResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponse(response);
            }

        }

        private static async Task HandleErrorResponse(HttpResponseMessage response)
        {

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                var errorResponse = JsonSerializationHelper.Deserialize<ErrorResponse>(content);
                if (errorResponse.error == null)
                    throw new ParseResponseException("An error occurred while deserializing the response", content);

                switch (errorResponse.error.code)
                {
                    case ReadableErrorTopCode.Conflict: throw new MorphApiConflictException(errorResponse.error.message);
                    case ReadableErrorTopCode.NotFound: throw new MorphApiNotFoundException(errorResponse.error.message);
                    case ReadableErrorTopCode.Forbidden: throw new MorphApiForbiddenException(errorResponse.error.message);
                    case ReadableErrorTopCode.BadArgument: throw new MorphApiBadArgumentException(FieldErrorsMapper.MapFromDto(errorResponse.error), errorResponse.error.message);
                    default: throw new MorphClientGeneralException(errorResponse.error.code, errorResponse.error.message);
                }
            }

            else
            {                
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Conflict: throw new MorphApiConflictException(response.ReasonPhrase ?? "Conflict");
                    case HttpStatusCode.NotFound: throw new MorphApiNotFoundException(response.ReasonPhrase ?? "Not found");
                    case HttpStatusCode.Forbidden: throw new MorphApiForbiddenException(response.ReasonPhrase ?? "Forbidden");                    
                    default: throw new ParseResponseException(response.ReasonPhrase, null);
                }
                
            }
        }

        /// <summary>
        /// Start Task like "fire and forget"
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="taskId">tast guid</param>
        /// <returns></returns>
        public async Task<RunningTaskStatus> StartTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken)
        {
            spaceName = PrepareSpaceName(spaceName);
            var url = "runningtasks/" + taskId.ToString("D");
            using (var response = await GetHttpClient().PostAsync(url, null, cancellationToken))
            {
                var info = await HandleResponse<RunningTaskStatusDto>(response);
                return new RunningTaskStatus
                {
                    Id = Guid.Parse(info.Id),
                    IsRunning = info.IsRunning,
                    ProjectName = info.ProjectName
                };
            }

        }

        /// <summary>
        /// Gets status of the task (Running/Not running) and payload
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="taskId">task guid</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        private async Task<RunningTaskStatus> GetRunningTaskStatusAsync(string spaceName, Guid taskId, CancellationToken cancellationToken)
        {
            spaceName = PrepareSpaceName(spaceName);
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = string.Format("runningtasks/{0}{1}", taskId.ToString("D"), nvc.ToQueryString());

            using (var response = await GetHttpClient().GetAsync(url, cancellationToken))
            {
                var info = await HandleResponse<RunningTaskStatusDto>(response);
                return new RunningTaskStatus
                {
                    Id = Guid.Parse(info.Id),
                    IsRunning = info.IsRunning,
                    ProjectName = info.ProjectName
                };
            }
        }


        /// <summary>
        /// Gets status of the task
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="taskId">task guid</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Returns task status</returns>
        public async Task<Model.TaskStatus> GetTaskStatusAsync(/*string spaceName,*/ Guid taskId, CancellationToken cancellationToken)
        {
            //spaceName = PrepareSpaceName(spaceName);
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = JoinUrl("tasks", taskId.ToString("D")) + nvc.ToQueryString();

            using (var response = await GetHttpClient().GetAsync(url, cancellationToken))
            {
                var dto = await HandleResponse<TaskStatusDto>(response);
                var data = TaskStatusMapper.MapFromDto(dto);
                return data;                
            }
        }

        /// <summary>
        /// Stops the Task
        /// </summary>
        /// <param name="spaceName"></param>
        /// <param name="taskId"></param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public async Task StopTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken)
        {
            spaceName = PrepareSpaceName(spaceName);
            var url = "runningtasks/" + taskId.ToString("D");
            using (var response = await GetHttpClient().DeleteAsync(url, cancellationToken))
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
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());

            var url = "server/status" + nvc.ToQueryString();
            using (var response = await GetHttpClient().GetAsync(url, cancellationToken))
            {
                var dto = await HandleResponse<ServerStatusDto>(response);
                var result = ServerStatusMapper.MapFromDto(dto);
                return result;

            }
        }

        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="remoteFilePath">Path to the remote file. Like /some/folder/file.txt </param>
        /// <param name="streamToWriteTo">stream for writing. You should dispose the stream by yourself</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns file info</returns>
        public async Task<DownloadFileInfo> DownloadFileAsync(string spaceName, string remoteFilePath, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            DownloadFileInfo fileInfo = null;
            await DownloadFileAsync(spaceName, remoteFilePath, (fi) => { fileInfo = fi; return true; }, streamToWriteTo, cancellationToken);
            return fileInfo;
        }
        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="remoteFilePath"> Path to the remote file. Like /some/folder/file.txt </param>
        /// <param name="handleFile">delegate to check file info before accessing to the file stream</param>
        /// <param name="streamToWriteTo">stream for writing. Writing will be executed only if handleFile delegate returns true</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DownloadFileAsync(string spaceName, string remoteFilePath, Func<DownloadFileInfo, bool> handleFile, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            spaceName = PrepareSpaceName(spaceName);
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());
            var url = JoinUrl("space", spaceName, "files", remoteFilePath) + nvc.ToQueryString();
            // it's necessary to add HttpCompletionOption.ResponseHeadersRead to disable caching
            using (HttpResponseMessage response = await GetHttpClient().GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
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
                                //need to fix double quotes, that may come from server response
                                FileName = contentDisposition.FileName.TrimStart('\"').TrimEnd('\"') 
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
        /// <param name="spaceName">space name</param>
        /// <param name="localFilePath">path to the local file</param>
        /// <param name="destFolderPath">detination folder like /path/to/folder </param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="overwriteFileifExists">overwrite file</param>
        /// <returns></returns>
        public async Task UploadFileAsync(string spaceName, string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException(string.Format("File '{0}' not found", localFilePath));
            var fileSize = new System.IO.FileInfo(localFilePath).Length;
            var fileName = Path.GetFileName(localFilePath);
            using (var fsSource = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                await UploadFileAsync(spaceName, fsSource, fileName, fileSize, destFolderPath, cancellationToken, overwriteFileifExists);
                return;
            }

        }
        /// <summary>
        /// Upload file stream to the server
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="inputStream">stream for read from</param>
        /// <param name="fileName">file name</param>
        /// <param name="fileSize">file size in bytes</param>
        /// <param name="destFolderPath">destination folder like /path/to/folder </param>
        /// <param name="cancellationToken">cancellation tokern</param>
        /// <param name="overwriteFileifExists"></param>
        /// <returns></returns>
        public async Task UploadFileAsync(string spaceName, Stream inputStream, string fileName, long fileSize, string destFolderPath, CancellationToken cancellationToken, bool overwriteFileifExists = false)
        {
            try
            {
                spaceName = PrepareSpaceName(spaceName);
                string boundary = "EasyMorphCommandClient--------" + Guid.NewGuid().ToString("N");
                string url = JoinUrl("space", spaceName, "files", destFolderPath);

                using (var content = new MultipartFormDataContent(boundary))
                {
                    var downloadProgress = new FileProgress(fileName, fileSize);
                    downloadProgress.StateChanged += DownloadProgress_StateChanged;
                    using (var streamContent = new ProgressStreamContent(inputStream, downloadProgress))
                    {
                        content.Add(streamContent, "files", Path.GetFileName(fileName));
                        var requestMessage = new HttpRequestMessage()
                        {
                            Content = content,
                            Method = overwriteFileifExists ? HttpMethod.Put : HttpMethod.Post,
                            RequestUri = new Uri(url, UriKind.Relative)
                        };
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

        /// <summary>
        /// Prerforms browsing the Space
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="folderPath">folder path like /path/to/folder</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SpaceBrowsingInfo> BrowseSpaceAsync(string spaceName, string folderPath, CancellationToken cancellationToken)
        {
            spaceName = PrepareSpaceName(spaceName);
            var nvc = new NameValueCollection();
            nvc.Add("_", DateTime.Now.Ticks.ToString());

            var url = JoinUrl("space", spaceName, "browse", folderPath) + nvc.ToQueryString();
            using (var response = await GetHttpClient().GetAsync(url, cancellationToken))
            {
                var dto = await HandleResponse<SpaceBrowsingResponseDto>(response);
                return SpaceBrowsingMapper.MapFromDto(dto);

            }
        }

        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="serverFolder">server folder like /path/to/folder</param>
        /// <param name="fileName">file name </param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns true if file exists.</returns>
        public async Task<bool> FileExistsAsync(string spaceName, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(nameof(fileName));
            var browseResult = await this.BrowseSpaceAsync(spaceName, serverFolder, cancellationToken);

            return browseResult.FileExists(fileName);
        }



        private string PrepareSpaceName(string spaceName)
        {
            return string.IsNullOrWhiteSpace(spaceName) ? _defaultSpaceName : spaceName.ToLower();
        }

        private string JoinUrl(params string[] urls)
        {
            var result = string.Empty;
            for (var i = 0; i < urls.Length; i++)
            {
                var p = urls[i];
                if (p == null)
                    continue;

                p = p.Replace('\\', '/');
                p = p.Trim(new[] { '/' });
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                if (result != string.Empty)
                    result += "/";
                result += p;

            }
            return result;
        }

        /// <summary>
        /// Performs file deletion
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="serverFolder">Path to server folder like /path/to/folder</param>
        /// <param name="fileName">file name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteFileAsync(string spaceName, string serverFolder, string fileName, CancellationToken cancellationToken)
        {

            spaceName = PrepareSpaceName(spaceName);
            var url = JoinUrl("space", spaceName, "files", serverFolder, fileName);

            using (HttpResponseMessage response = await GetHttpClient().DeleteAsync(url, cancellationToken))
            {
                await HandleResponse(response);
            }

        }


        /// <summary>
        /// Validate tasks. Checks that there are no missing parameters in the tasks. 
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="projectPath">project path like /path/to/project.morph </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ValidateTasksResult> ValidateTasksAsync(string spaceName, string projectPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                throw new ArgumentException(nameof(projectPath));
            spaceName = PrepareSpaceName(spaceName);
            var url = "commands/validatetasks";
            var request = new ValidateTasksRequestDto
            {
                SpaceName = spaceName,
                ProjectPath = projectPath
            };
            using (var response = await GetHttpClient().PostAsync(url, new StringContent(JsonSerializationHelper.Serialize(request), Encoding.UTF8, "application/json"), cancellationToken))
            {

                var dto = await HandleResponse<ValidateTasksResponseDto>(response);
                var entity = ValidateTasksResponseMapper.MapFromDto(dto);
                return entity;

            }
        }
    }


}
