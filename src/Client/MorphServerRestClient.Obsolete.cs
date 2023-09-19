using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Exceptions;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// This part of the class contains obsolete methods that should be removed in 6.0 version
    /// </summary>
    public partial class MorphServerRestClient : IRestClient
    {
        [Obsolete("Obsolete due to flaw in response checking. Will be removed in the next major release.")]
        public virtual async Task<ApiResult<ServerPushStreaming>> PushContiniousStreamingDataAsync<TResult>(
            HttpMethod httpMethod, string path, ContiniousStreamingRequest startContiniousStreamingRequest,
            NameValueCollection urlParameters, HeadersCollection headersCollection,
            CancellationToken cancellationToken)
            where TResult : new()
        {
            HttpContentHeaders httpResponseHeaders = null;

            try
            {
                await EnsureSessionValid(headersCollection, cancellationToken);

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
                                       OnceFactory<HttpContent>.Create(content),
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
                return ApiResult<ServerPushStreaming>.Fail(new MorphApiNotFoundException("Specified folder not found"),
                    httpResponseHeaders);
            }
            catch (Exception e)
            {
                return ApiResult<ServerPushStreaming>.Fail(e, httpResponseHeaders);
            }
        }
    }
}