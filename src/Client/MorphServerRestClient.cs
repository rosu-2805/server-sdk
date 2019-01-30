using Morph.Server.Sdk.Exceptions;
using Morph.Server.Sdk.Helper;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Specialized;
using Morph.Server.Sdk.Mappers;
using Morph.Server.Sdk.Dto.Errors;
using System.IO;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Events;

namespace Morph.Server.Sdk.Client
{


    internal class MorphServerRestClient : IRestClient
    {
        private HttpClient httpClient;
        public HttpClient HttpClient { get => httpClient; set => httpClient = value; }

        public MorphServerRestClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        public Task<ApiResult<TResult>> DeleteAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendAsyncApiResult<TResult, NoContentRequest>(HttpMethod.Delete, url, null, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            if (urlParameters == null)
            {
                urlParameters = new NameValueCollection();
            }
            urlParameters.Add("_", DateTime.Now.Ticks.ToString());
            return SendAsyncApiResult<TResult, NoContentRequest>(HttpMethod.Get, url, null, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> PostAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendAsyncApiResult<TResult, TModel>(HttpMethod.Post, url, model, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> PutAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendAsyncApiResult<TResult, TModel>(HttpMethod.Put, url, model, urlParameters, headersCollection, cancellationToken);
        }

        protected virtual async Task<ApiResult<TResult>> SendAsyncApiResult<TResult, TModel>(HttpMethod httpMethod, string path, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            StringContent stringContent = null;
            if (model != null)
            {
                var serialized = JsonSerializationHelper.Serialize<TModel>(model);
                stringContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            }

            var url = path + (urlParameters != null ? urlParameters.ToQueryString() : string.Empty);
            var httpRequestMessage = BuildHttpRequestMessage(httpMethod, url, stringContent, headersCollection);

            using (var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken))
            {
                return await HandleResponse<TResult>(response);
            }
        }

        private static async Task<ApiResult<TResult>> HandleResponse<TResult>(HttpResponseMessage response)
            where TResult : new()
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

        protected HttpRequestMessage BuildHttpRequestMessage(HttpMethod httpMethod, string url, HttpContent content, HeadersCollection headersCollection)
        {
            var requestMessage = new HttpRequestMessage()
            {
                Content = content,
                Method = httpMethod,
                RequestUri = new Uri(url, UriKind.Relative)
            };
            if (headersCollection != null)
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

        public void Dispose()
        {
            if (HttpClient != null)
            {
                HttpClient.Dispose();
                HttpClient = null;
            }
        }

        public async Task<ApiResult<TResult>> SendFileStreamAsync<TResult>(
            HttpMethod httpMethod, string path, SendFileStreamData sendFileStreamData,
            NameValueCollection urlParameters, HeadersCollection headersCollection,
            Action<FileTransferProgressEventArgs> onSendProgress,
            CancellationToken cancellationToken)
        where TResult : new()
        {
            try
            {
                string boundary = "MorphRestClient--------" + Guid.NewGuid().ToString("N");

                using (var content = new MultipartFormDataContent(boundary))
                {
                    var uploadProgress = new FileProgress(sendFileStreamData.FileName, sendFileStreamData.FileSize, onSendProgress);
                    
                    using (cancellationToken.Register(() => uploadProgress.ChangeState(FileProgressState.Cancelled)))
                    {
                        using (var streamContent = new ProgressStreamContent(sendFileStreamData.Stream, uploadProgress))
                        {
                            content.Add(streamContent, "files", Path.GetFileName(sendFileStreamData.FileName));
                            var url = path + urlParameters.ToQueryString();
                            var requestMessage = BuildHttpRequestMessage(httpMethod, url, content, headersCollection);
                            using (requestMessage)
                            {
                                using (var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                                {
                                    return await HandleResponse<TResult>(response);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException != null &&
                    ex.InnerException is WebException web &&
                    web.Status == WebExceptionStatus.ConnectionClosed)
            {
                return ApiResult<TResult>.Fail(new MorphApiNotFoundException("Specified folder not found"));
            }
            catch (Exception e)
            {
                return ApiResult<TResult>.Fail(e);
            }
        }

        protected async Task<ApiResult<FetchFileStreamData>> RetrieveFileStreamAsync(HttpMethod httpMethod, string path, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken)
        {
            var url = path + urlParameters.ToQueryString();
            HttpResponseMessage response = await httpClient.SendAsync(
                   BuildHttpRequestMessage(httpMethod, url, null, headersCollection), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            {
                if (response.IsSuccessStatusCode)
                {
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    // need to fix double quotes, that may come from server response
                    // FileNameStar contains file name encoded in UTF8
                    var realFileName = (contentDisposition.FileNameStar ?? contentDisposition.FileName).TrimStart('\"').TrimEnd('\"');
                    var contentLength = response.Content.Headers.ContentLength;

                    FileProgress downloadProgress = null;

                    if (contentLength.HasValue)
                    {
                        downloadProgress = new FileProgress(realFileName, contentLength.Value, onReceiveProgress);
                    }
                    downloadProgress?.ChangeState(FileProgressState.Starting);
                    var totalProcessedBytes = 0;
                  
                    {
                        // stream must be disposed by a caller
                        Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
                        DateTime _lastUpdate = DateTime.MinValue;
                        
                        var streamWithProgress = new StreamWithProgress(streamToReadFrom,
                            e =>
                            {                                
                                if (downloadProgress != null)
                                {
                                    totalProcessedBytes += e.BytesProcessed;
                                    if ((DateTime.Now - _lastUpdate > TimeSpan.FromMilliseconds(500)) || e.BytesProcessed == 0)
                                    {
                                        downloadProgress.SetProcessedBytes(totalProcessedBytes);
                                        _lastUpdate = DateTime.Now;
                                    }
                                    
                                }
                            },
                        () =>
                        {

                            if (downloadProgress != null  && downloadProgress.ProcessedBytes!= totalProcessedBytes)
                            {
                                downloadProgress.ChangeState(FileProgressState.Cancelled);
                            }
                            response.Dispose();
                        });
                        return ApiResult<FetchFileStreamData>.Ok(new FetchFileStreamData(streamWithProgress, realFileName, contentLength));
                    }
                }
                else
                {
                    var error = await BuildExceptionFromResponse(response);
                    return ApiResult<FetchFileStreamData>.Fail(error);
                }
            }
        }


        public Task<ApiResult<TResult>> PutFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress,  CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendFileStreamAsync<TResult>(HttpMethod.Put, url, sendFileStreamData, urlParameters, headersCollection, onSendProgress, cancellationToken);
        }

        public Task<ApiResult<TResult>> PostFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendFileStreamAsync<TResult>(HttpMethod.Post, url, sendFileStreamData, urlParameters, headersCollection, onSendProgress, cancellationToken);
        }


        public Task<ApiResult<FetchFileStreamData>> RetrieveFileGetAsync(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken)
        {
            if (urlParameters == null)
            {
                urlParameters = new NameValueCollection();
            }
            urlParameters.Add("_", DateTime.Now.Ticks.ToString());
            return RetrieveFileStreamAsync(HttpMethod.Get, url, urlParameters, headersCollection, onReceiveProgress, cancellationToken);
        }



       

        public Task<ApiResult<TResult>> HeadAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            if (urlParameters == null)
            {
                urlParameters = new NameValueCollection();
            }
            urlParameters.Add("_", DateTime.Now.Ticks.ToString());
            return SendAsyncApiResult<TResult, NoContentRequest>(HttpMethod.Head, url, null, urlParameters, headersCollection, cancellationToken);
        }
    }



}