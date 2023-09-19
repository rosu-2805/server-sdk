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
using System.Diagnostics.CodeAnalysis;
using Morph.Server.Sdk.Mappers;
using Morph.Server.Sdk.Dto.Errors;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Dto.Upload;
using Morph.Server.Sdk.Events;
using static Morph.Server.Sdk.Helper.StreamWithProgress;

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// Morph rest client
    /// </summary>
    public partial class MorphServerRestClient : IRestClient
    {
        protected readonly IJsonSerializer jsonSerializer;

        private IApiSessionRefresher SessionRefresher { get; }

        private static string HttpsSchemeConstant = "https";
        private static string HttpStateDetectionEndpoint = "server/status";


        public Uri BaseAddress { get; protected set; }

        private HttpClient httpClient;

        public HttpClient HttpClient { get => httpClient; set => httpClient = value; }

        public HttpSecurityState HttpSecurityState { get; protected set; } = HttpSecurityState.NotEvaluated;

        public MorphServerRestClient(HttpClient httpClient, Uri baseAddress, IJsonSerializer jsonSerializer,
            IApiSessionRefresher sessionRefresher,
            HttpSecurityState httpSecurityState = HttpSecurityState.NotEvaluated)
        {
            this.jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            SessionRefresher = sessionRefresher;
            this.BaseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
            HttpSecurityState = httpSecurityState;
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // force strict https 
            if (IsHttps(baseAddress) || httpSecurityState == HttpSecurityState.ForcedHttps)
            {
                UpgradeToForcedHttps();
            }
        }

        public MorphServerRestClient(HttpClient httpClient, Uri baseAddress, IApiSessionRefresher sessionRefresher,
            HttpSecurityState httpSecurityState = HttpSecurityState.NotEvaluated) :
            this(httpClient, baseAddress, new MorphDataContractJsonSerializer(),sessionRefresher,  httpSecurityState)
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
            Task<HttpContent> CreateStringContent(CancellationToken ct)
            {
                if (model == null)
                    return Task.FromResult<HttpContent>(null);

                var serialized = jsonSerializer.Serialize(model);

                return Task.FromResult<HttpContent>(new StringContent(serialized, Encoding.UTF8, "application/json"));
            }

            // for model binding request read and buffer full server response
            // but for HttpHead content reading is not necessary and might raise error.
            //var httpCompletionOption = httpMethod != HttpMethod.Head ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
            var httpCompletionOption = HttpCompletionOption.ResponseHeadersRead;
            using (var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                       httpMethod,
                       path,
                       CreateStringContent,
                       urlParameters,
                       headersCollection,
                       httpCompletionOption,
                       cancellationToken))
            {
                return await HandleResponse<TResult>(response);
            }
        }





        protected virtual async Task<HttpResponseMessage> SendAsyncWithDiscoveryAndAutoUpgrade(HttpMethod httpMethod,
            string path,
            Func<CancellationToken, Task<HttpContent>> httpContentFactory,
            NameValueCollection urlParameters,
            HeadersCollection headersCollection,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken)
        {

            var originalHttpContent = await httpContentFactory(cancellationToken);

            // detect current http/https and upgrade if necessary

            if (originalHttpContent != null && HttpSecurityState == HttpSecurityState.NotEvaluated)
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
                    originalHttpContent,
                    urlParameters,
                    headersCollection,
                    httpCompletionOption,
                    cancellationToken);
            }

            var response = await _SendAsyncAsIs(httpMethod,
                path,
                originalHttpContent,
                urlParameters,
                headersCollection,
                httpCompletionOption,
                cancellationToken);

            try
            {
                if (!await SessionRefresher.IsSessionLostResponse(headersCollection, path, originalHttpContent, response))
                    return response;

                if (!await SessionRefresher.RefreshSessionAsync(headersCollection, cancellationToken))
                    return response;
            }
            catch (Exception)
            {
                //Don't lose the original response if somebody throws an exception in the session refresher
                response?.Dispose();
                throw;
            }

            HttpContent recreatedHttpContent = null;

            if (originalHttpContent != null)
            {
                originalHttpContent.Dispose();

                //We started with non-null http content, but likely it was disposed after the failed attempt.
                //Try to re-create httpContent after session refresh to send request again.

                recreatedHttpContent = await httpContentFactory(cancellationToken);

                if (null == recreatedHttpContent)
                {
                    // We can't re-create httpContent, so we should fail with original response
                    return response;
                }
            }

            //At this point original response will not be seen by method invoker, so we should dispose it
            response?.Dispose();

            return await _SendAsyncAsIs(httpMethod,
                path,
                recreatedHttpContent,
                urlParameters,
                headersCollection,
                httpCompletionOption,
                cancellationToken);
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
        

        private async Task EnsureSessionValid(HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            //This is anonymous request
            if (!headersCollection.Contains(ApiSession.AuthHeaderName))
                return;

            await SessionRefresher.EnsureSessionValid(this, headersCollection, cancellationToken);
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
                await EnsureSessionValid(headersCollection, cancellationToken);

                string boundary = "MorphRestClient--------" + Guid.NewGuid().ToString("N");

                using (var content = new MultipartFormDataContent(boundary))
                {
                    var uploadProgress = new FileProgress(sendFileStreamData.FileName, sendFileStreamData.FileSize, onSendProgress);

                    using (cancellationToken.Register(() => uploadProgress.ChangeState(FileProgressState.Cancelled)))
                    {
                        using (var streamContent = new ProgressStreamContent(sendFileStreamData.Stream, uploadProgress))
                        {
                            // Note: file-metadata should come before file section to be picked up by server first.
                            MaybeAddIfMatchSection(content, "files-metadata", sendFileStreamData.IfMatch);
                            content.Add(streamContent, "files", Path.GetFileName(sendFileStreamData.FileName));

                            using (var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                                       httpMethod,
                                       path,
                                       OnceFactory<HttpContent>.Create(content),
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
                httpContentFactory: _ => Task.FromResult<HttpContent>(null),
                urlParameters: urlParameters,
                headersCollection: headersCollection,
                httpCompletionOption: HttpCompletionOption.ResponseHeadersRead,
                cancellationToken: cancellationToken);

            {
                if (response.IsSuccessStatusCode)
                {
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    // need to fix double quotes, that may come from server response
                    // FileNameStar contains file name encoded in UTF8
                    var realFileName = (contentDisposition.FileNameStar ?? contentDisposition.FileName).TrimStart('\"').TrimEnd('\"');
                    var contentLength = response.Content.Headers.ContentLength;

                    FileProgress downloadProgress = new FileProgress(realFileName, contentLength, onReceiveProgress);
                    
                    downloadProgress.ChangeState(FileProgressState.Starting);
                    
                    long totalProcessedBytes = 0;

                    // stream must be disposed by a caller
                    Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
                    
                    var streamWithProgress = new StreamWithProgress(streamToReadFrom, contentLength, cancellationToken,
                        onReadProgress: e =>
                        {
                            // on read progress handler
                            totalProcessedBytes = e.TotalBytesRead;
                            downloadProgress.SetProcessedBytes(totalProcessedBytes);
                        },
                        onDisposed: () =>
                        {
                            // on disposed handler
                            if (downloadProgress.ProcessedBytes != totalProcessedBytes)
                            {
                                downloadProgress.ChangeState(FileProgressState.Cancelled);
                            }

                            response.Dispose();
                        },
                        onTokenCancelled: (tokenCancellationReason, token) =>
                        {
                            // on tokenCancelled
                            if (tokenCancellationReason == TokenCancellationReason.HttpTimeoutToken)
                                throw new Exception("Timeout");

                            if (tokenCancellationReason == TokenCancellationReason.OperationCancellationToken)
                                throw new OperationCanceledException(token);
                        });
                    
                    return ApiResult<FetchFileStreamData>.Ok(
                        new FetchFileStreamData(streamWithProgress, realFileName, contentLength),
                        response.Content.Headers);
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
        
        public async Task<ApiResult<TResult>> PushStreamAsync<TResult>(HttpMethod httpMethod, string path,
            PushFileStreamData pushFileStreamData, NameValueCollection urlParameters,
            HeadersCollection headersCollection, CancellationToken cancellationToken) where TResult : new()
        {
            HttpContentHeaders httpResponseHeaders = null;

            await EnsureSessionValid(headersCollection, cancellationToken);

            string boundary = $"MorphRestClient-Streaming--------{Guid.NewGuid():N}";

            using (var content = new MultipartFormDataContent(boundary))
            using (var streamContent = new ContiniousSteamingHttpContent(cancellationToken))
            {
                // Note: file-metadata should come before file section to be picked up by server first.
                MaybeAddIfMatchSection(content, "files-metadata", pushFileStreamData.IfMatch);
                content.Add(streamContent, "files", Path.GetFileName(pushFileStreamData.FileName));
                
                // Begin concurrently pushing stream. 
                // Note that this pushStreamTask must be awaited before streamContent is disposed.

                var pushCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var pushStreamTask = Task.Run(async () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    // Reason: pushStreamTask is awaited before streamContent is disposed.
                    using (var connection = new PushStreamingConnection(streamContent))
                    {
                        try
                        {
                            await pushFileStreamData.PushCallback(connection, pushCancellation.Token);

                            // Just return 'Ok', meaning that push sequence didnt result in error and actual response
                            // should be obtained from server in the logic below.
                            return ApiResult<TResult>.Ok(default, httpResponseHeaders);
                        }
                        catch (Exception e)
                        {
                            // If anything bad happens on the push side, capture the exception and return it.
                            return ApiResult<TResult>.Fail(e, httpResponseHeaders);
                        }
                    }
                }, pushCancellation.Token);

                // Actual interaction with server happens here

                try
                {
                    using (var response = await SendAsyncWithDiscoveryAndAutoUpgrade(
                               httpMethod,
                               path,
                               OnceFactory<HttpContent>.Create(content),
                               urlParameters,
                               headersCollection,
                               HttpCompletionOption.ResponseHeadersRead,
                               cancellationToken))
                    {
                        var result = await HandleResponse<TResult>(response);
                        httpResponseHeaders = response.Content.Headers;

                        // no need to push stream further in case we already have an error from server
                        if (!result.IsSucceed)
                            pushCancellation.Cancel();

                        var pushResult = await pushStreamTask; // obtain result from push task (can be cancellation)

                        // If we have an error from server, return it since it's likely more
                        // informative than possible error from push.
                        if (!result.IsSucceed)
                            return result;

                        // If we have an error from push, return it.
                        if (!pushResult.IsSucceed && !(pushResult.Error is OperationCanceledException))
                            return pushResult;

                        // Happy path, just return server response.
                        return result;
                    }
                }
                catch (Exception ex) when (ex.InnerException is WebException web &&
                                           web.Status == WebExceptionStatus.ConnectionClosed)
                {
                    return ApiResult<TResult>.Fail(new MorphApiNotFoundException("Specified folder not found"),
                        httpResponseHeaders);
                }
                catch (Exception e)
                {
                    return ApiResult<TResult>.Fail(e, httpResponseHeaders);
                }
            }
        }
        
        
        private void MaybeAddIfMatchSection(MultipartFormDataContent content, string sectionName, ETag etag)
        {
            if (null == etag)
                return;

            var metadata = new UploadedFileMetadataDto
            {
                IfMatch = new UploadedFileETagDto
                {
                    Size = etag.Size,
                    LastUpdatedUnixTime = etag.LastUpdatedUnixTime
                }
            };

            content.Add(new StringContent(jsonSerializer.Serialize(metadata)), sectionName);
        }

    }



}