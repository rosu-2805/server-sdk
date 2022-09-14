using Morph.Server.Sdk.Exceptions;
using Morph.Server.Sdk.Helper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Specialized;
using Morph.Server.Sdk.Mappers;
using Morph.Server.Sdk.Dto.Errors;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Events;
using static Morph.Server.Sdk.Helper.StreamWithProgress;

namespace Morph.Server.Sdk.Client
{
    public class MorphServerRestClient : IRestClient
    {
        protected readonly IJsonSerializer jsonSerializer;
        private static string HttpsSchemeConstant = "https";
        private static string HttpStateDetectionEndpoint = "server/status";


        public Uri BaseAddress { get; protected set; }

        private HttpClient httpClient;
        public HttpClient HttpClient { get => httpClient; set => httpClient = value; }

        public HttpSecurityState HttpSecurityState { get; protected set; } = HttpSecurityState.NotEvaluated;

        public MorphServerRestClient(HttpClient httpClient, Uri baseAddress, IJsonSerializer jsonSerializer, HttpSecurityState httpSecurityState = HttpSecurityState.NotEvaluated)
        {
            this.jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            this.BaseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
            HttpSecurityState = httpSecurityState;
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // force strict https 
            if (IsHttps(baseAddress) || httpSecurityState == HttpSecurityState.ForcedHttps)
            {
                UpgradeToForcedHttps();
            }


        }

        public MorphServerRestClient(HttpClient httpClient, Uri baseAddress, HttpSecurityState httpSecurityState = HttpSecurityState.NotEvaluated) :
            this(httpClient, baseAddress, new MorphDataContractJsonSerializer(), httpSecurityState)
        {

        }


        private bool IsHttps(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            string scheme = uri.Scheme;
            var isHttps = (string.Compare(HttpsSchemeConstant, scheme, StringComparison.OrdinalIgnoreCase) == 0);
            return isHttps;
        }

        protected void UpgradeToForcedHttps()
        {
            UriBuilder builder = new UriBuilder(BaseAddress)
            {
                Scheme = HttpsSchemeConstant
            };
            BaseAddress = builder.Uri;
            HttpSecurityState = HttpSecurityState.ForcedHttps;
        }
        protected virtual void SetToPlainHttp()
        {
            HttpSecurityState = HttpSecurityState.PlainHttp;
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



        protected virtual Uri BuildUri(Uri baseAddress, string path, NameValueCollection urlParameters)
        {
            var requestUri = new Uri(baseAddress, path);
            var uriBuilder = new UriBuilder(requestUri)
            {
                Query = (urlParameters != null ? urlParameters.ToQueryString() : string.Empty)
            };

            var url = uriBuilder.Uri;
            return url;
        }

        protected virtual async Task<ApiResult<TResult>> SendAsyncApiResult<TResult, TModel>(HttpMethod httpMethod, string path, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new()
        {
            StringContent stringContent = null;
            if (model != null)
            {
                var serialized = jsonSerializer.Serialize<TModel>(model);
                stringContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            }

            // for model binding request read and buffer full server response
            // but for HttpHead content reading is not necessary and might raise error.
            //var httpCompletionOption = httpMethod != HttpMethod.Head ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
            var httpCompletionOption = HttpCompletionOption.ResponseHeadersRead;
            using (var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                       httpMethod,
                       path,
                       stringContent,
                       urlParameters,
                       headersCollection,
                       httpCompletionOption,
                       cancellationToken))
            {
                return await HandleResponse<TResult>(response);
            }
        }





        protected virtual async Task<HttpResponseMessage> SendAsyncWithDiscoveryAndAutoUpgrade(
            HttpMethod httpMethod,
            string path,
            HttpContent httpContent,
            NameValueCollection urlParameters,
            HeadersCollection headersCollection,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken
        )
        {

            // detect current http/https and upgrade if necessary 

            if (httpContent != null && HttpSecurityState == HttpSecurityState.NotEvaluated)
            {
                using (await _SendAsyncInDisсoveryMode(
                           HttpMethod.Get,
                           HttpStateDetectionEndpoint,
                           null,
                           null,
                           null,
                           HttpCompletionOption.ResponseHeadersRead,
                           cancellationToken))
                {

                }
            }


            if (HttpSecurityState == HttpSecurityState.NotEvaluated)
            {
                return await _SendAsyncInDisсoveryMode(httpMethod,
                    path,
                    httpContent,
                    urlParameters,
                    headersCollection,
                    httpCompletionOption,
                    cancellationToken);
            }
            else
            {
                return await _SendAsyncAsIs(httpMethod,
                    path,
                    httpContent,
                    urlParameters,
                    headersCollection,
                    httpCompletionOption,
                    cancellationToken);

            }



        }


        protected virtual async Task<HttpResponseMessage> _SendAsyncAsIs
        (
            HttpMethod httpMethod,
            string path,
            HttpContent httpContent,
            NameValueCollection urlParameters,
            HeadersCollection headersCollection,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken
        )
        {
            using (HttpRequestMessage httpRequestMessage = BuildHttpRequestMessage(
                       httpMethod,
                       BuildUri(BaseAddress, path, urlParameters),
                       httpContent,
                       headersCollection))
            {
                HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage,
                    httpCompletionOption,
                    cancellationToken);
                return response;
            }
        }


        protected virtual async Task<HttpResponseMessage> _SendAsyncInDisсoveryMode
        (
            HttpMethod httpMethod,
            string path,
            HttpContent httpContent,
            NameValueCollection urlParameters,
            HeadersCollection headersCollection,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken
        )
        {
            UriBuilder builder = new UriBuilder(BaseAddress)
            {
                Scheme = HttpsSchemeConstant
            };
            var secureBaseUri = builder.Uri;


            HttpRequestMessage httpRequestMessage = BuildHttpRequestMessage(
                httpMethod,
                BuildUri(BaseAddress, path, urlParameters),
                httpContent,
                headersCollection);


            HttpRequestMessage secureRequestMessage = BuildHttpRequestMessage(
                httpMethod,
                BuildUri(secureBaseUri, path, urlParameters),
                httpContent,
                headersCollection);

            {
                var httpRequest =
                    httpClient
                        .SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

                var secureRequest =
                    httpClient
                        .SendAsync(secureRequestMessage, httpCompletionOption, cancellationToken);

                try
                {
                    await Task.WhenAny(secureRequest, httpRequest);


                    if (httpRequest.Status == TaskStatus.RanToCompletion || secureRequest.Status == TaskStatus.Faulted)
                    {
                        var httpResponse = await httpRequest;
                        SetToPlainHttp();
                        return httpResponse;
                    }
                    else if (secureRequest.Status == TaskStatus.RanToCompletion || httpRequest.Status == TaskStatus.Faulted)
                    {
                        var secureResponse = await secureRequest;
                        UpgradeToForcedHttps();
                        return secureResponse;
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        if (secureRequest.Status == TaskStatus.Canceled)
                        {
                            return await secureRequest;
                        }
                        else return await httpRequest;
                    }
                    else
                    {
                        throw new Exception("Unable to detect http/https");
                    }
                }
                finally
                {
                    httpRequestMessage?.Dispose();
                    secureRequestMessage?.Dispose();
                }

            }
        }


        protected virtual async Task<ApiResult<TResult>> HandleResponse<TResult>(HttpResponseMessage response)
            where TResult : new()
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = jsonSerializer.Deserialize<TResult>(content);
                return ApiResult<TResult>.Ok(result, response.Content.Headers);
            }
            else
            {
                var error = await BuildExceptionFromResponse(response);
                return ApiResult<TResult>.Fail(error, response.Content.Headers);
            }
        }

        protected virtual HttpRequestMessage BuildHttpRequestMessage(HttpMethod httpMethod, Uri requestUri, HttpContent content, HeadersCollection headersCollection)
        {
            var requestMessage = new HttpRequestMessage()
            {
                Content = content,
                Method = httpMethod,
                RequestUri = requestUri
            };
            if (headersCollection != null)
            {
                headersCollection.Fill(requestMessage.Headers);
            }
            return requestMessage;
        }



        protected virtual async Task<Exception> BuildExceptionFromResponse(HttpResponseMessage response)
        {

            var rawContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(rawContent))
            {
                ErrorResponse errorResponse = null;
                try
                {
                    errorResponse = DeserializeErrorResponse(rawContent);
                }
                catch (Exception)
                {
                    return new ResponseParseException("An error occurred while deserializing the response", rawContent);
                }
                if (errorResponse.error == null)
                    return new ResponseParseException("An error occurred while deserializing the response", rawContent);

                switch (errorResponse.error.code)
                {
                    case ReadableErrorTopCode.Conflict: return new MorphApiConflictException(errorResponse.error.message);
                    case ReadableErrorTopCode.NotFound: return new MorphApiNotFoundException(errorResponse.error.message);
                    case ReadableErrorTopCode.Forbidden: return new MorphApiForbiddenException(errorResponse.error.message);
                    case ReadableErrorTopCode.Unauthorized: return new MorphApiUnauthorizedException(errorResponse.error.message);
                    case ReadableErrorTopCode.BadArgument: return new MorphApiBadArgumentException(FieldErrorsMapper.MapFromDto(errorResponse.error), errorResponse.error.message);
                    default: return BuildCustomExceptionFromErrorResponse(rawContent, errorResponse);
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

        protected virtual ErrorResponse DeserializeErrorResponse(string rawContent)
        {
            return jsonSerializer.Deserialize<ErrorResponse>(rawContent);
        }

        protected virtual Exception BuildCustomExceptionFromErrorResponse(string rawContent, ErrorResponse errorResponse)
        {
            return new MorphClientGeneralException(errorResponse.error.code, errorResponse.error.message);
        }

        public virtual void Dispose()
        {
            if (HttpClient != null)
            {
                HttpClient.Dispose();
                HttpClient = null;
            }
        }




        public virtual async Task<ApiResult<ServerPushStreaming>> PushContiniousStreamingDataAsync<TResult>(
            HttpMethod httpMethod, string path, ContiniousStreamingRequest startContiniousStreamingRequest, NameValueCollection urlParameters, HeadersCollection headersCollection,
            CancellationToken cancellationToken)
            where TResult : new()
        {
            HttpContentHeaders httpResponseHeaders = null;
            try
            {
                string boundary = "MorphRestClient-Streaming--------" + Guid.NewGuid().ToString("N");

                var content = new MultipartFormDataContent(boundary);


                var streamContent = new ContiniousSteamingHttpContent(cancellationToken);
                var serverPushStreaming = new ServerPushStreaming(streamContent);
                content.Add(streamContent, "files", Path.GetFileName(startContiniousStreamingRequest.FileName));

              

                new Task(async () =>
                  {
                      try
                      {
                          try
                          {
                              using (var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                                         httpMethod,
                                         path,
                                         content,
                                         urlParameters,
                                         headersCollection,
                                         HttpCompletionOption.ResponseHeadersRead,
                                         cancellationToken))
                              {
                                  var result = await HandleResponse<TResult>(response);
                                  httpResponseHeaders = response.Content.Headers;
                                  serverPushStreaming.SetApiResult(result);
                              }

                          }
                          catch (Exception ex) when (ex.InnerException != null &&
                                                     ex.InnerException is WebException web &&
                                                     web.Status == WebExceptionStatus.ConnectionClosed)
                          {
                              serverPushStreaming.SetApiResult(ApiResult<TResult>.Fail(
                                  new MorphApiNotFoundException("Specified folder not found"), httpResponseHeaders));
                          }
                          catch (Exception e)
                          {
                              serverPushStreaming.SetApiResult(ApiResult<TResult>.Fail(e, httpResponseHeaders));
                          }
                          finally
                          {
                              //requestMessage.Dispose();
                              streamContent.Dispose();
                              content.Dispose();
                          }
                      }
                      catch (Exception)
                      {
                          //  nothing
                      }

                  }).Start();
                return ApiResult<ServerPushStreaming>.Ok(serverPushStreaming, httpResponseHeaders);





            }
            catch (Exception ex) when (ex.InnerException != null &&
                    ex.InnerException is WebException web &&
                    web.Status == WebExceptionStatus.ConnectionClosed)
            {
                return ApiResult<ServerPushStreaming>.Fail(new MorphApiNotFoundException("Specified folder not found"), httpResponseHeaders);
            }
            catch (Exception e)
            {
                return ApiResult<ServerPushStreaming>.Fail(e, httpResponseHeaders);
            }
        }


        public virtual async Task<ApiResult<TResult>> SendFileStreamAsync<TResult>(
            HttpMethod httpMethod, string path, SendFileStreamData sendFileStreamData,
            NameValueCollection urlParameters, HeadersCollection headersCollection,
            Action<FileTransferProgressEventArgs> onSendProgress,
            CancellationToken cancellationToken)
        where TResult : new()
        {
            HttpContentHeaders httpResponseHeaders = null;
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


                            using (var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                                       httpMethod,
                                       path,
                                       content,
                                       urlParameters,
                                       headersCollection,
                                       HttpCompletionOption.ResponseHeadersRead,
                                       cancellationToken))
                            {
                                httpResponseHeaders = response.Content.Headers;
                                return await HandleResponse<TResult>(response);
                            }

                        }
                    }
                }
            }
            catch (Exception ex) when (ex.InnerException != null &&
                    ex.InnerException is WebException web &&
                    web.Status == WebExceptionStatus.ConnectionClosed)
            {
                return ApiResult<TResult>.Fail(new MorphApiNotFoundException("Specified folder not found"), httpResponseHeaders);
            }
            catch (Exception e)
            {
                return ApiResult<TResult>.Fail(e, httpResponseHeaders);
            }
        }

        protected virtual async Task<ApiResult<FetchFileStreamData>> RetrieveFileStreamAsync(HttpMethod httpMethod, string path, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken)
        {
            //var url = path + (urlParameters != null ? urlParameters.ToQueryString() : string.Empty);
            //var url = await ComposeRequestUriAsync(path, urlParameters, cancellationToken);
            //HttpResponseMessage response = await httpClient.SendAsync(
            //       BuildHttpRequestMessage(httpMethod, url, null, headersCollection), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                httpMethod,
                path,
                null,
                urlParameters,
                headersCollection,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            {
                if (response.IsSuccessStatusCode)
                {
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    // need to fix double quotes, that may come from server response
                    // FileNameStar contains file name encoded in UTF8
                    var realFileName = (contentDisposition.FileNameStar ?? contentDisposition.FileName).TrimStart('\"').TrimEnd('\"');
                    var contentLength = response.Content.Headers.ContentLength;
                    if (!contentLength.HasValue)
                    {
                        throw new Exception("Response content length header is not set by the server.");
                    }

                    FileProgress downloadProgress = null;

                    if (contentLength.HasValue)
                    {
                        downloadProgress = new FileProgress(realFileName, contentLength.Value, onReceiveProgress);
                    }
                    downloadProgress?.ChangeState(FileProgressState.Starting);
                    long totalProcessedBytes = 0;

                    {
                        // stream must be disposed by a caller
                        Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();


                        var streamWithProgress = new StreamWithProgress(streamToReadFrom, contentLength.Value, cancellationToken,
                            e =>
                            {
                                // on read progress handler
                                if (downloadProgress != null)
                                {
                                    totalProcessedBytes = e.TotalBytesRead;
                                    downloadProgress.SetProcessedBytes(totalProcessedBytes);
                                }
                            },
                        () =>
                        {
                            // on disposed handler
                            if (downloadProgress != null && downloadProgress.ProcessedBytes != totalProcessedBytes)
                            {
                                downloadProgress.ChangeState(FileProgressState.Cancelled);
                            }
                            response.Dispose();
                        },
                        (tokenCancellationReason, token) =>
                        {
                            // on tokenCancelled
                            if (tokenCancellationReason == TokenCancellationReason.HttpTimeoutToken)
                            {
                                throw new Exception("Timeout");
                            }
                            if (tokenCancellationReason == TokenCancellationReason.OperationCancellationToken)
                            {
                                throw new OperationCanceledException(token);
                            }

                        });
                        return ApiResult<FetchFileStreamData>.Ok(new FetchFileStreamData(streamWithProgress, realFileName, contentLength), response.Content.Headers);

                    }
                }
                else
                {
                    try
                    {
                        var error = await BuildExceptionFromResponse(response);
                        return ApiResult<FetchFileStreamData>.Fail(error, response.Content.Headers);
                    }
                    finally
                    {
                        response.Dispose();
                    }
                }
            }
        }


        public virtual Task<ApiResult<TResult>> PutFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendFileStreamAsync<TResult>(HttpMethod.Put, url, sendFileStreamData, urlParameters, headersCollection, onSendProgress, cancellationToken);
        }

        public virtual Task<ApiResult<TResult>> PostFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new()
        {
            return SendFileStreamAsync<TResult>(HttpMethod.Post, url, sendFileStreamData, urlParameters, headersCollection, onSendProgress, cancellationToken);
        }


        public virtual Task<ApiResult<FetchFileStreamData>> RetrieveFileGetAsync(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken)
        {
            if (urlParameters == null)
            {
                urlParameters = new NameValueCollection();
            }
            urlParameters.Add("_", DateTime.Now.Ticks.ToString());
            return RetrieveFileStreamAsync(HttpMethod.Get, url, urlParameters, headersCollection, onReceiveProgress, cancellationToken);
        }





        public virtual Task<ApiResult<TResult>> HeadAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
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