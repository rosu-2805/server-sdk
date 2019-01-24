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

namespace Morph.Server.Sdk.Client
{

    public interface IApiClient:IDisposable
    {
        HttpClient HttpClient { get; set; }
        Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken);
        Task<ApiResult<TResult>> PostAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken);
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
        private HttpClient httpClient;
        public HttpClient HttpClient { get => httpClient; set => httpClient = value; }

        public MorphServerRestClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        public Task<ApiResult<TResult>> DeleteAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            return SendAsyncApiResult<TResult, NoContentRequest>(HttpMethod.Delete, url, null, urlParameters, headersCollection, cancellationToken);
        }

        public Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
        {
            if (urlParameters == null)
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
            if(HttpClient!= null)
            {
                HttpClient.Dispose();
                HttpClient = null;
            }
        }
    }


}



