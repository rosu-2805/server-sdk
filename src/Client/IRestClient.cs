using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Client
{
    public interface IRestClient : IDisposable
    {
        HttpClient HttpClient { get; set; }
        Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> HeadAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> PostAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> PutAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new();
        Task<ApiResult<TResult>> DeleteAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> PutFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new();
        Task<ApiResult<TResult>> PostFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new();

        Task<ApiResult<FetchFileStreamData>> RetrieveFileGetAsync(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken);


        Task<ApiResult<ServerPushStreaming>> PushContiniousStreamingDataAsync<TResult>(
            HttpMethod httpMethod, string path, ContiniousStreamingRequest startContiniousStreamingRequest, NameValueCollection urlParameters, HeadersCollection headersCollection,
            CancellationToken cancellationToken)
            where TResult : new();
            

    }



}